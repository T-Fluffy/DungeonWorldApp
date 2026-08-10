import { useCallback, useRef, useState } from 'react';
import type { SectionDto } from '../api/client';
import { parseEncounters, resolveRound, type Enemy } from '../utils/combat';

export interface CombatStats {
  skill: number;
  stamina: number;
  luck: number;
}

interface UseCombatOptions {
  section: SectionDto | null;
  stats: CombatStats;
  onPlayerStatsChange?: (patch: Partial<CombatStats>) => void;
  onLog: (content: string, type: 'narrator' | 'system') => void;
  onDeath: () => void;
}

const DEATH_DELAY_MS = 2500;

export function useCombat({ section, stats, onPlayerStatsChange, onLog, onDeath }: UseCombatOptions) {
  const [enemies, setEnemies] = useState<Enemy[]>([]);
  const [enemyIndex, setEnemyIndex] = useState(0);
  const [inCombat, setInCombat] = useState(false);
  const [playerSkill, setPlayerSkill] = useState(stats.skill);
  const [playerStamina, setPlayerStamina] = useState(stats.stamina);
  const [playerLuck, setPlayerLuck] = useState(stats.luck);
  const startingStats = useRef<CombatStats>(stats);

  // Reset combat state whenever a new section is visited (render-time adjustment).
  const [activeSection, setActiveSection] = useState<SectionDto | null>(section);
  if (activeSection !== section) {
    setActiveSection(section);
    const found = section ? parseEncounters(section.content) : [];
    setEnemies(found);
    setEnemyIndex(0);
    setInCombat(section ? section.hasCombat && found.length > 0 : false);
  }

  // Mirror authoritative (context) stats so combat works even when unsigned-in.
  const [prevStats, setPrevStats] = useState(stats);
  if (stats.skill !== prevStats.skill || stats.stamina !== prevStats.stamina || stats.luck !== prevStats.luck) {
    setPrevStats(stats);
    setPlayerSkill(stats.skill);
    setPlayerStamina(stats.stamina);
    setPlayerLuck(stats.luck);
  }

  const currentEnemy = enemies[enemyIndex] ?? null;

  const attack = useCallback(() => {
    if (!inCombat) {
      onLog('There is no foe before you. Steel stays sheathed.', 'system');
      return;
    }
    const enemy = currentEnemy;
    if (!enemy) {
      onLog('The foe before you has already been defeated.', 'system');
      setInCombat(false);
      return;
    }

    const round = resolveRound(playerSkill, enemy);
    const lines: string[] = [
      `You roll: ${round.playerRolls.join(' + ')} = ${round.playerAttack} Attack Strength.`,
      `${enemy.name} rolls: ${round.enemyRolls.join(' + ')} = ${round.enemyAttack} Attack Strength.`,
    ];

    let nextSkill = playerSkill;
    let nextStamina = playerStamina;
    let nextLuck = playerLuck;
    let nextEnemyStamina = enemy.stamina;

    if (round.winner === 'draw') {
      lines.push('The blows are matched — both miss!');
    } else if (round.winner === 'player') {
      nextEnemyStamina = enemy.stamina - round.enemyDamage;
      lines.push(
        `Your blade finds its mark! ${enemy.name} takes ${round.enemyDamage} STAMINA damage (${nextEnemyStamina} left).`
      );
    } else {
      if (round.specialEffect === 'skill') {
        nextSkill = Math.max(0, playerSkill - 1);
        lines.push(`${enemy.name} strikes home (special hit, die ${round.specialDie}): you lose 1 SKILL (${nextSkill}).`);
      } else if (round.specialEffect === 'luck') {
        nextLuck = Math.max(0, playerLuck - 1);
        lines.push(`${enemy.name} strikes home (special hit, die ${round.specialDie}): you lose 1 LUCK (${nextLuck}).`);
      } else {
        nextStamina = Math.max(0, playerStamina - round.playerDamage);
        lines.push(`${enemy.name} strikes home! You lose ${round.playerDamage} STAMINA (${nextStamina} left).`);
      }
    }

    onLog(lines.join('\n'), 'system');

    setPlayerSkill(nextSkill);
    setPlayerStamina(nextStamina);
    setPlayerLuck(nextLuck);
    onPlayerStatsChange?.({ skill: nextSkill, stamina: nextStamina, luck: nextLuck });

    // Death: narrate, then restore starting stats and tear the chronicle asunder.
    if (nextStamina <= 0) {
      setInCombat(false);
      onLog('Your strength fails... the world darkens. Your story ends here.', 'narrator');
      window.setTimeout(() => {
        onPlayerStatsChange?.(startingStats.current);
        onDeath();
      }, DEATH_DELAY_MS);
      return;
    }

    // Enemy slain.
    if (nextEnemyStamina <= 0) {
      onLog(`${enemy.name} is defeated!`, 'system');
      if (enemyIndex + 1 < enemies.length) {
        const next = enemies[enemyIndex + 1];
        setEnemyIndex(enemyIndex + 1);
        onLog(`Another foe steps forward: ${next.name}.`, 'system');
      } else {
        setInCombat(false);
        onLog('The last foe falls. Read the section for your reward, then follow "turn to N".', 'system');
      }
      return;
    }

    // Persist the enemy's wounds so the next BATTLE continues the same fight.
    setEnemies((prev) => prev.map((e, i) => (i === enemyIndex ? { ...e, stamina: nextEnemyStamina } : e)));
  }, [
    inCombat,
    currentEnemy,
    playerSkill,
    playerStamina,
    playerLuck,
    enemyIndex,
    enemies,
    onLog,
    onPlayerStatsChange,
    onDeath,
  ]);

  const flee = useCallback(() => {
    if (!inCombat) {
      onLog('There is no foe before you.', 'system');
      return;
    }
    const text = (section?.content ?? '').toLowerCase();
    if (/(can try to escape|attempt to escape|may escape|escape if|try to flee|try to escape)/.test(text)) {
      onLog('You break off the fight and flee! The foe loses interest.', 'system');
      setInCombat(false);
    } else {
      onLog('There is no escape from this foe. Face it in battle or fall.', 'system');
    }
  }, [inCombat, section, onLog]);

  return {
    inCombat,
    enemies,
    enemyIndex,
    currentEnemy,
    playerSkill,
    playerStamina,
    playerLuck,
    attack,
    flee,
  };
}
