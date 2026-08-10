// Pure combat/dice helpers for the Fighting Fantasy engine (Seas of Blood rules).

export interface Enemy {
  name: string;
  skill: number;
  stamina: number;
  staminaMax: number;
  /** Optional special-hit table on this enemy (e.g. Awkmute: 1-2 normal, 3-4 -1 SKILL, 5-6 -1 LUCK). */
  specialHit?: 'skill-luck';
}

export type RoundWinner = 'player' | 'enemy' | 'draw';

export interface RoundResult {
  playerRolls: number[];
  enemyRolls: number[];
  playerAttack: number;
  enemyAttack: number;
  winner: RoundWinner;
  playerDamage: number;
  enemyDamage: number;
  specialDie: number | null;
  specialEffect: 'normal' | 'skill' | 'luck' | null;
}

export function rollDie(sides = 6): number {
  return Math.floor(Math.random() * sides) + 1;
}

export interface DiceResult {
  total: number;
  rolls: number[];
}

export function rollDice(count: number, sides: number): DiceResult {
  const rolls = Array.from({ length: count }, () => rollDie(sides));
  return { total: rolls.reduce((a, b) => a + b, 0), rolls };
}

/** Parse a dice expression like "3d6", "1d10", "D6", "6" (a single die) or "2d6" by default. */
export function parseDiceExpr(expr?: string | null): { count: number; sides: number } {
  if (!expr) return { count: 2, sides: 6 };
  const m = expr.toLowerCase().match(/(\d+)?\s*d\s*(\d+)/);
  if (m) {
    const count = m[1] ? Math.min(20, Math.max(1, parseInt(m[1], 10))) : 2;
    const sides = Math.min(100, Math.max(1, parseInt(m[2], 10)));
    return { count, sides };
  }
  const bare = expr.match(/\d+/);
  if (bare) {
    const sides = Math.min(100, Math.max(1, parseInt(bare[0], 10)));
    return { count: 1, sides };
  }
  return { count: 2, sides: 6 };
}

export function formatDiceResult(expr: string, res: DiceResult): string {
  return `You roll ${expr}: ${res.rolls.join(' + ')} = ${res.total}`;
}

function detectSpecialHit(content: string): Enemy['specialHit'] {
  const low = content.toLowerCase();
  const hasRoll = /roll[^.!?]{0,80}(die|dice)/.test(low);
  const hasSkill = low.includes('skill');
  const hasLuck = low.includes('luck');
  return hasRoll && hasSkill && hasLuck ? 'skill-luck' : undefined;
}

function makeEnemy(content: string, name: string, skill: number, stamina: number): Enemy {
  return {
    name: name.trim(),
    skill,
    stamina,
    staminaMax: stamina,
    specialHit: detectSpecialHit(content),
  };
}

/**
 * Heuristically extract encounter-box enemies from a section's prose.
 * Handles:
 *   - inline single enemy: "SITH ORB SKILL 10 STAMINA 10" (may trail into prose)
 *   - multi-row boxes:  header "SKILL STAMINA" then rows "FIRST PIRATE 8 4"
 *                       or rows "FIRST PIRATE SKILL 8 STAMINA 4"
 *   - single-enemy boxes: "NAME" on its own line, then "SKILL 8 STAMINA 10"
 *   - common OCR artifacts: SKILLS -> SKILL, STAMINAS -> STAMINA
 * Unparseable sections return [] (caller degrades to manual ROLL DICE).
 */
export function parseEncounters(content: string): Enemy[] {
  const lines = content
    .replace(/\r/g, '')
    .replace(/SKILLS/g, 'SKILL')
    .replace(/STAMINAS/g, 'STAMINA')
    .replace(/STRIKES/g, 'STRIKE')
    .replace(/STRENGTHS/g, 'STRENGTH')
    .split('\n')
    .map((l) => l.replace(/\s+/g, ' ').trim())
    .filter(Boolean);

  const nameSrc = '[A-Za-z0-9][A-Za-z0-9\' .\\-]{1,50}';
  const statPair = 'SKILL\\s*(\\d{1,2})\\s*STAMINA\\s*(\\d{1,3})';
  const enemies: Enemy[] = [];
  const add = (name: string, skill: number, stamina: number) => {
    enemies.push(makeEnemy(content, name, skill, stamina));
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Inline single enemy: NAME SKILL <n> STAMINA <n> (trailing prose allowed)
    const inline = line.match(new RegExp(`^(${nameSrc}?)\\s+${statPair}(?=[.,;!?]|\\s|$)`, 'i'));
    if (inline && inline[1]) {
      add(inline[1], parseInt(inline[2], 10), parseInt(inline[3], 10));
      continue;
    }

    // Encounter box header: (NAME) SKILL STAMINA — name+space is one optional unit
    const header = line.match(new RegExp(`^(?:(${nameSrc})\\s+)?SKILL\\s+STAMINA$`, 'i'));
    if (header) {
      const boxName = header[1];
      let parsedRows = false;
      let j = i + 1;
      while (j < lines.length) {
        const row = lines[j];
        const rowInline = row.match(new RegExp(`^(${nameSrc})\\s+${statPair}(?=[.,;!?]|\\s|$)`, 'i'));
        if (rowInline) {
          add(rowInline[1], parseInt(rowInline[2], 10), parseInt(rowInline[3], 10));
          parsedRows = true;
          j++;
          continue;
        }
        const rowPair = row.match(new RegExp(`^(${nameSrc})\\s+(\\d{1,2})\\s+(\\d{1,3})$`));
        if (rowPair) {
          add(rowPair[1], parseInt(rowPair[2], 10), parseInt(rowPair[3], 10));
          parsedRows = true;
          j++;
          continue;
        }
        break;
      }
      // Header with a name but the numbers sit on a nearby line: "SKILL 8 STAMINA 10"
      if (!parsedRows) {
        const near = lines.slice(i, i + 4).join(' ');
        const nums = near.match(new RegExp(statPair, 'i'));
        if (nums) {
          add(boxName || 'The Foe', parseInt(nums[1], 10), parseInt(nums[2], 10));
        }
      }
      continue;
    }

    // Bare stats on their own line: "SKILL 8 STAMINA 10" — name is the previous line
    const bare = line.match(new RegExp(`^${statPair}$`, 'i'));
    if (bare && i > 0) {
      const prev = lines[i - 1].replace(/^[-•*•]\s*/, '').trim();
      const isName = !/SKILL|STAMINA|STRIKE|STRENGTH/i.test(prev) && prev.length >= 2;
      add(isName ? prev : 'The Foe', parseInt(bare[1], 10), parseInt(bare[2], 10));
      continue;
    }
  }

  return enemies;
}

/** Resolve one combat round (2 dice + SKILL vs 2 dice + SKILL; loser -2 STAMINA; ties miss). */
export function resolveRound(playerSkill: number, enemy: Enemy): RoundResult {
  const playerRolls = rollDice(2, 6).rolls;
  const enemyRolls = rollDice(2, 6).rolls;
  const playerAttack = playerSkill + playerRolls.reduce((a, b) => a + b, 0);
  const enemyAttack = enemy.skill + enemyRolls.reduce((a, b) => a + b, 0);

  let winner: RoundWinner;
  if (playerAttack > enemyAttack) winner = 'player';
  else if (enemyAttack > playerAttack) winner = 'enemy';
  else winner = 'draw';

  let specialDie: number | null = null;
  let specialEffect: RoundResult['specialEffect'] = null;
  if (winner === 'enemy' && enemy.specialHit === 'skill-luck') {
    specialDie = rollDie(6);
    specialEffect = specialDie <= 2 ? 'normal' : specialDie <= 4 ? 'skill' : 'luck';
  }

  const enemyHurt = winner === 'player';
  const playerHurt = winner === 'enemy';
  const playerDamage = playerHurt && (specialEffect === null || specialEffect === 'normal') ? 2 : 0;
  const enemyDamage = enemyHurt ? 2 : 0;

  return {
    playerRolls,
    enemyRolls,
    playerAttack,
    enemyAttack,
    winner,
    playerDamage,
    enemyDamage,
    specialDie,
    specialEffect,
  };
}
