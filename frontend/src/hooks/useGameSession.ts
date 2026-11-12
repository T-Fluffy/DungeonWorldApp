import { useState, useCallback, useEffect } from 'react';
import { getSection, getBookMeta, upsertAdventure, getAdventure, getAdventureCommands } from '../api/client';
import { useGame } from '../Context/useGame';
import { useCombat } from './useCombat';
import { parseDiceExpr, rollDice, formatDiceResult } from '../utils/combat';
import type { SectionDto, BookMetaDto, GameCommandResponse } from '../api/client';

export type LogType = 'narrator' | 'player' | 'system';

export interface LogEntry {
  id: string;
  type: LogType;
  content: string;
  timestamp: string;
}

const extractError = (err: unknown): string => {
  const e = err as { response?: { data?: { error?: string; Error?: string } } };
  return e?.response?.data?.error || e?.response?.data?.Error || 'The shadows do not answer. Check that the engine is awake.';
};

export const useGameSession = (bookTitle: string | null) => {
  const { user, stats, logout, updatePlayerStats } = useGame();
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [meta, setMeta] = useState<BookMetaDto | null>(null);
  const [section, setSection] = useState<SectionDto | null>(null);
  const [history, setHistory] = useState<number[]>([]);
  // Commands this particular adventure supports (served per book by the API).
  const [commands, setCommands] = useState<GameCommandResponse[]>([]);
  // Traveller's pack: what the reader has picked up this run (TAKE/DROP).
  const [pack, setPack] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createLog = useCallback((content: string, type: LogType): LogEntry => ({
    id: Date.now().toString() + Math.random().toString().slice(2, 5),
    type,
    content,
    timestamp: new Date().toLocaleTimeString(),
  }), []);

  const addLog = useCallback((content: string, type: LogType) => {
    setLogs((prev) => [...prev, createLog(content, type)]);
  }, [createLog]);

  // Persist the current section + stats to the backend
  const save = useCallback(async (complete: boolean = false, sectionNumberOverride?: number) => {
    if (!bookTitle || !user?.isLoggedIn || !section) return;

    try {
      await upsertAdventure({
        bookTitle,
        currentSection: sectionNumberOverride ?? section.number,
        skill: user.skill ?? stats.might,
        stamina: user.stamina ?? stats.vitality,
        luck: user.luck ?? stats.essence,
        isComplete: complete,
      });
    } catch {
      // Saving is best-effort; navigation should never block on it.
    }
  }, [bookTitle, user?.isLoggedIn, user?.skill, user?.stamina, user?.luck, section, stats]);

  const goTo = useCallback(async (sectionNumber: number): Promise<boolean> => {
    if (!bookTitle || isProcessing) return false;

    setIsProcessing(true);
    addLog(`> ${sectionNumber}`, 'player');

    try {
      const data = await getSection(bookTitle, sectionNumber);
      setSection(data);
      setHistory((prev) => [...prev, sectionNumber]);
      addLog(`- Section ${data.number} -`, 'system');

      // A few sections have no recoverable body (FF26 §§14-17: their printed spread is
      // missing from the scan). Say so plainly instead of printing the parser's
      // "[Text missing or unreadable in PDF]" placeholder, and never treat it as an
      // ending - a missing body has no choices, which the rule below would otherwise
      // read as a victory and reward a medallion.
      const bodyMissing = data.features.missingText;
      const deadEnd = data.choices.length === 0;
      if (bodyMissing) {
        addLog(
          deadEnd
            ? 'This was an ending, but its text is missing from the scanned book. Type RESET to begin again.'
            : 'This section’s text is missing from the scanned book, but both paths below still work.',
          'system',
        );
      } else {
        addLog(data.clean || data.raw, 'narrator');
      }

      if (data.features.hasCombat) {
        addLog('A fight erupts! Steel meets shadow.', 'system');
      }
      if (deadEnd && !bodyMissing) {
        addLog('The path ends here. (Victory, or a dead end?)', 'system');
      }

      // Auto-save after every navigation (best-effort).
      // Pass the fetched section number explicitly — the `section` state in the
      // closure is still the *previous* section until the next render.
      const completed = !bodyMissing && (sectionNumber >= 400 || data.choices.length === 0);
      await save(completed, data.number);
      return true;
    } catch (err) {
      addLog(extractError(err), 'system');
      return false;
    } finally {
      setIsProcessing(false);
    }
  }, [bookTitle, isProcessing, addLog, save]);

  const failTrial = useCallback(async (message: string) => {
    addLog(message, 'system');
    await save(true);
  }, [addLog, save]);

  const reset = useCallback(() => {
    setLogs([]);
    setMeta(null);
    setSection(null);
    setHistory([]);
    setError(null);
  }, []);

  // RESET: tear the chronicle back to section 1, clear the feed + history, and seal section 1.
  const resetRun = useCallback(async () => {
    if (!bookTitle || isProcessing) return;

    setIsProcessing(true);
    setLogs([]);
    setHistory([]);
    setError(null);
    try {
      const first = await getSection(bookTitle, 1);
      setSection(first);
      setHistory([1]);
      addLog('The chronicle is torn asunder and written anew...', 'system');
      // Seal the fresh start into the Grimoire (sectionNumberOverride forces section 1).
      await save(false, 1);
      addLog(`- Section ${first.number} -`, 'system');
      addLog(first.clean || first.raw, 'narrator');
    } catch (err) {
      addLog(extractError(err), 'system');
    } finally {
      setIsProcessing(false);
    }
  }, [bookTitle, isProcessing, addLog, save]);

  // Combat: mirror the authoritative stats (fall back to the decorative HUD bars)
  const combatStats = {
    skill: user?.skill ?? stats.might,
    stamina: user?.stamina ?? stats.vitality,
    luck: user?.luck ?? stats.essence,
  };

  const combat = useCombat({
    section,
    stats: combatStats,
    onPlayerStatsChange: updatePlayerStats,
    onLog: addLog,
    onDeath: () => {
      void resetRun();
    },
  });
  const combatAttack = combat.attack;
  const combatFlee = combat.flee;
  const isCombatActive = combat.inCombat;

  // Load the book on mount (or when the title changes), resuming a saved run
  useEffect(() => {
    if (!bookTitle) return;

    let cancelled = false;
    const boot = async () => {
      reset();
      setIsLoading(true);
      try {
        const bookMeta = await getBookMeta(bookTitle);
        if (cancelled) return;
        setMeta(bookMeta);
        addLog(`--- ${bookMeta.title} ---`, 'system');
        if (bookMeta.introduction) {
          addLog(bookMeta.introduction, 'narrator');
        }

        // Which verbs this book actually supports; best-effort so a catalog hiccup never blocks play.
        try {
          const available = await getAdventureCommands(bookTitle);
          if (!cancelled) setCommands(available);
        } catch {
          if (!cancelled) setCommands([]);
        }

        // Resume from a saved adventure if one exists
        const saved = user?.isLoggedIn ? await getAdventure(bookTitle) : null;
        if (cancelled) return;
        const startSection = saved && !saved.isComplete && saved.currentSection > 1
          ? saved.currentSection
          : 1;
        if (saved && saved.currentSection > 1 && !saved.isComplete) {
          addLog('Resuming from your saved chronicle...', 'system');
        }

        const first = await getSection(bookTitle, startSection);
        if (cancelled) return;
        setSection(first);
        setHistory([startSection]);
        addLog(`- Section ${first.number} -`, 'system');
        addLog(first.clean || first.raw, 'narrator');
      } catch (err) {
        if (!cancelled) setError(extractError(err));
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    boot();
    return () => {
      cancelled = true;
    };
  }, [bookTitle, addLog, reset, user?.isLoggedIn]);

  const processCommand = useCallback(async (command: string) => {
    if (!command.trim() || !bookTitle) return;

    addLog(command, 'player');

    const lower = command.toLowerCase();

    // Allow jumping straight to a section number, e.g. "go 12" or "12"
    const goMatch = lower.match(/\bgo\s+(\d+)\b/) || lower.match(/^\s*(\d+)\s*$/);
    if (goMatch) {
      if (section?.number === 309 && /phantoms? of fear/i.test(bookTitle)) {
        addLog('Choose a five-square route to reach the judges before turning to a section.', 'system');
        return;
      }
      await goTo(parseInt(goMatch[1], 10));
      return;
    }

    // RESET / RESTART: rewind the chronicle to section 1.
    if (lower.includes('reset') || lower.includes('restart') || lower.includes('newgame')) {
      await resetRun();
      return;
    }

    // LOGOUT / SEVER: sever the pact immediately.
    if (lower.includes('logout') || lower.includes('log out') || lower.includes('sever')) {
      addLog('The pact is severed. Until the shadows call again...', 'system');
      logout();
      return;
    }

    // BACK: step to the previous section of this run.
    if (/^(back|prev|previous)(\s|$)/.test(lower)) {
      if (history.length < 2) {
        addLog('You stand at the beginning of this chronicle.', 'system');
        return;
      }
      await goTo(history[history.length - 2]);
      return;
    }

    // REREAD: print the current section again without moving.
    if (/^(reread|repeat|again)(\s|$)/.test(lower)) {
      if (section) addLog(section.clean || section.raw, 'narrator');
      return;
    }

    // TAKE: put something the section mentions into the pack.
    const takeMatch = lower.match(/^(?:take|get|pickup|grab)\s+(.+)$/);
    if (takeMatch) {
      const item = takeMatch[1].trim();
      const here = `${section?.clean ?? ''} ${section?.raw ?? ''}`.toLowerCase();
      if (!here.includes(item.split(' ')[0])) {
        addLog(`There is no ${item} here to take.`, 'system');
      } else if (pack.some((p) => p.toLowerCase() === item)) {
        addLog(`You already carry the ${item}.`, 'system');
      } else {
        setPack([...pack, item]);
        addLog(`Taken: ${item}. Into your pack it goes.`, 'system');
      }
      return;
    }

    // DROP: remove something from the pack.
    const dropMatch = lower.match(/^(?:drop|discard|remove)\s+(.+)$/);
    if (dropMatch) {
      const item = dropMatch[1].trim();
      const next = pack.filter((p) => p.toLowerCase() !== item);
      if (next.length === pack.length) {
        addLog(`You are not carrying a ${item}.`, 'system');
      } else {
        setPack(next);
        addLog(`Dropped: ${item}.`, 'system');
      }
      return;
    }

    // USE: acknowledge equipment use; the pack is the source of truth.
    const useMatch = lower.match(/^(?:use|equip|wield)\s+(.+)$/);
    if (useMatch) {
      const item = useMatch[1].trim();
      const held = pack.some((p) => p.toLowerCase() === item);
      addLog(
        held
          ? `You ready the ${item}. The chronicle records the act.`
          : `You do not carry a ${item}. Try TAKE <item> first, or INVENTORY.`,
        'system',
      );
      return;
    }

    // CAST: only meaningful where the book has spells; the API advertises it per adventure.
    const castMatch = lower.match(/^(?:cast|channel|spell)\s+(.+)$/);
    if (castMatch) {
      addLog(
        `You invoke ${castMatch[1].trim()}. The air shivers, but this chronicle records no such working yet.`,
        'system',
      );
      return;
    }

    setIsProcessing(true);
    await new Promise((resolve) => setTimeout(resolve, 600));

    let responseContent: string | null = '';
    let responseType: LogType = 'narrator';
    if (lower.includes('look')) {
      responseContent = section?.clean.slice(0, 120) || 'Shadows veil your sight.';
    } else if (lower.includes('help')) {
      responseContent = commands.length > 0
        ? [
            'COMMANDS FOR THIS ADVENTURE:',
            ...commands.map((c) => `  ${c.name.padEnd(10)} ${c.description}`),
          ].join('\n')
        : [
            'AVAILABLE COMMANDS:',
            '  LOOK       Re-read the current section.',
            '  GO <n>     Jump to section <n> (or type the number directly).',
            '  BACK       Step back to the previous section.',
            '  BATTLE     Strike one combat round against the current foe.',
            '  ROLL DICE  Roll 2d6 (or ROLL <n>d<m>) for the book\'s dice checks.',
            '  FLEE       Try to escape from combat.',
            '  INVENTORY  Inspect the contents of your pack.',
            '  SAVE       Seal your progress into the Grimoire.',
            '  RESET      Tear the chronicle asunder and begin again at section 1.',
            '  HELP       Display this guide.',
          ].join('\n');
      responseType = 'system';
    } else if (/^(battle|fight|attack|strike)\b/i.test(lower)) {
      if (!section?.features.hasCombat) {
        responseContent = 'There is no foe to fight. Steel stays sheathed.';
      } else if (isCombatActive) {
        combatAttack();
        responseContent = null;
      } else {
        responseContent = 'A foe stands before you, but the encounter box is unreadable. Use ROLL DICE and resolve the fight by hand.';
      }
      responseType = 'system';
    } else if (/^(flee|escape|run)\b/i.test(lower)) {
      combatFlee();
      responseContent = null;
      responseType = 'system';
    } else if (/^(roll|dice)\b/i.test(lower)) {
      const diceMatch = command.toLowerCase().match(/(\d+\s*d\s*\d+|\d+)/);
      const expr = diceMatch ? diceMatch[1].replace(/\s+/g, '') : '2d6';
      const parsed = parseDiceExpr(expr);
      const result = rollDice(parsed.count, parsed.sides);
      const luck = user?.luck ?? stats.essence;
      responseContent = formatDiceResult(expr, result);
      if (parsed.count === 2 && parsed.sides === 6) {
        responseContent += `\n(Test your Luck? You are Lucky on ${luck} or less — testing costs 1 LUCK.)`;
      }
      responseType = 'system';
    } else if (lower.includes('save')) {
      await save();
      responseContent = user?.isLoggedIn
        ? 'Progress sealed into your Grimoire.'
        : 'You must be signed in to seal your progress.';
      responseType = 'system';
    } else if (lower.includes('inventory')) {
      responseContent = pack.length > 0
        ? `YOUR PACK:\n${pack.map((p) => `  ${p}`).join('\n')}`
        : 'Your pack is empty. TAKE anything the section offers, or type LOOK.';
      responseType = 'system';
    } else {
      responseContent = 'The shadows shift, but nothing happens. Try HELP to see this adventure\'s commands.';
    }

    if (responseContent) {
      addLog(responseContent, responseType);
    }
    setIsProcessing(false);
  }, [bookTitle, section, addLog, goTo, save, resetRun, logout, user?.isLoggedIn, user?.luck, stats, combatAttack, combatFlee, isCombatActive, history, commands, pack]);

  return {
    logs,
    meta,
    section,
    history,
    commands,
    pack,
    isLoading,
    isProcessing,
    error,
    combat,
    goTo,
    failTrial,
    processCommand,
    reset,
    save,
  };
};
