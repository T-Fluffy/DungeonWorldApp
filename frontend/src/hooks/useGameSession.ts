import { useState, useCallback, useEffect } from 'react';
import { getSection, getBookMeta, upsertAdventure, getAdventure } from '../api/client';
import { useGame } from '../Context/GameContext';
import type { SectionDto, BookMetaDto } from '../api/client';

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
  const { user, stats } = useGame();
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [meta, setMeta] = useState<BookMetaDto | null>(null);
  const [section, setSection] = useState<SectionDto | null>(null);
  const [history, setHistory] = useState<number[]>([]);
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
  const save = useCallback(async (complete: boolean = false) => {
    if (!bookTitle || !user?.isLoggedIn || !section) return;

    try {
      await upsertAdventure({
        bookTitle,
        currentSection: section.sectionNumber,
        skill: stats.might,
        stamina: stats.vitality,
        luck: stats.essence,
        isComplete: complete,
      });
    } catch {
      // Saving is best-effort; navigation should never block on it.
    }
  }, [bookTitle, user?.isLoggedIn, section, stats]);

  const goTo = useCallback(async (sectionNumber: number) => {
    if (!bookTitle || isProcessing) return;

    setIsProcessing(true);
    addLog(`> ${sectionNumber}`, 'player');

    try {
      const data = await getSection(bookTitle, sectionNumber);
      setSection(data);
      setHistory((prev) => [...prev, sectionNumber]);
      addLog(`- Section ${data.sectionNumber} -`, 'system');
      addLog(data.content, 'narrator');
      if (data.hasCombat) {
        addLog('A fight erupts! Steel meets shadow.', 'system');
      }
      if (data.choices.length === 0) {
        addLog('The path ends here. (Victory, or a dead end?)', 'system');
      }

      // Auto-save after every navigation (best-effort)
      const completed = sectionNumber >= 400 || data.choices.length === 0;
      await save(completed);
    } catch (err) {
      addLog(extractError(err), 'system');
    } finally {
      setIsProcessing(false);
    }
  }, [bookTitle, isProcessing, addLog, save]);

  const reset = useCallback(() => {
    setLogs([]);
    setMeta(null);
    setSection(null);
    setHistory([]);
    setError(null);
  }, []);

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
        addLog(`- Section ${first.sectionNumber} -`, 'system');
        addLog(first.content, 'narrator');
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
      await goTo(parseInt(goMatch[1], 10));
      return;
    }

    setIsProcessing(true);
    await new Promise((resolve) => setTimeout(resolve, 600));

    let responseContent = '';
    let responseType: LogType = 'narrator';
    if (lower.includes('look')) {
      responseContent = section?.content.slice(0, 120) || 'Shadows veil your sight.';
    } else if (lower.includes('help')) {
      responseContent = 'Commands: LOOK, GO [section number], INVENTORY, SAVE';
      responseType = 'system';
    } else if (lower.includes('save')) {
      await save();
      responseContent = user?.isLoggedIn
        ? 'Progress sealed into your Grimoire.'
        : 'You must be signed in to seal your progress.';
      responseType = 'system';
    } else if (lower.includes('inventory')) {
      responseContent = 'Your pack feels light. Perhaps fate will provide.';
      responseType = 'system';
    } else {
      responseContent = 'The shadows shift, but nothing happens. Try LOOK, GO [number], or INVENTORY.';
    }

    addLog(responseContent, responseType);
    setIsProcessing(false);
  }, [bookTitle, section, addLog, goTo, save, user?.isLoggedIn]);

  return {
    logs,
    meta,
    section,
    history,
    isLoading,
    isProcessing,
    error,
    goTo,
    processCommand,
    reset,
  };
};
