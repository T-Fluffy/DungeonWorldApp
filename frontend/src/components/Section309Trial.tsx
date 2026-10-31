import { useState } from 'react';

const BOARD: (number | null)[][] = [
  [1, 35, 231, 2, 115, 63, 3],
  [null, 136, 99, null, 9, 130, null],
  [226, 22, 11, 200, 15, 60, 222],
  [5, null, 86, 0, 19, null, 6],
  [55, 93, 45, 47, 21, 92, 51],
  [null, 123, 102, null, 183, 80, null],
  [null, null, 23, 4, 7, null, null],
];

const DIE_STARTS: Record<number, [number, number]> = {
  1: [0, 0],
  2: [0, 3],
  3: [0, 6],
  4: [6, 3],
  5: [3, 0],
  6: [3, 6],
};

const JUDGES = [3, 3] as const;

function isAdjacent([row, col]: readonly number[], [otherRow, otherCol]: readonly number[]) {
  return Math.max(Math.abs(row - otherRow), Math.abs(col - otherCol)) === 1;
}

function isReachable(row: number, col: number, route: [number, number][]) {
  if (!route.length) return false;
  const [lastRow, lastCol] = route[route.length - 1];
  return Math.max(Math.abs(row - lastRow), Math.abs(col - lastCol)) === 1;
}

interface Section309TrialProps {
  onNavigate: (sectionNumber: number) => Promise<boolean>;
  onFailure: (message: string) => Promise<void>;
  isProcessing: boolean;
}

export function Section309Trial({ onNavigate, onFailure, isProcessing }: Section309TrialProps) {
  const [dieRoll, setDieRoll] = useState<number | null>(null);
  const [route, setRoute] = useState<[number, number][]>([]);
  const [resolving, setResolving] = useState(false);
  const [failed, setFailed] = useState(false);

  const rollStartingSquare = () => {
    const roll = Math.floor(Math.random() * 6) + 1;
    setDieRoll(roll);
    setRoute([DIE_STARTS[roll]]);
    setFailed(false);
  };

  const chooseSquare = (row: number, col: number) => {
    const existingIndex = route.findIndex(([r, c]) => r === row && c === col);
    if (existingIndex >= 0) {
      setRoute(route.slice(0, existingIndex + 1));
      return;
    }

    if (route.length >= 5 || !isReachable(row, col, route)) return;
    if (route.some(([r, c]) => r === row && c === col)) return;
    if (route.length === 4 && !isAdjacent([row, col], JUDGES)) return;
    if (BOARD[row][col] === null) return;

    setRoute([...route, [row, col]]);
  };

  const resolveRoute = async () => {
    if (route.length !== 5 || resolving) return;
    const total = route.reduce((sum, [row, col]) => sum + (BOARD[row][col] ?? 0), 0);

    if (total < 1 || total > 400) {
      setFailed(true);
      await onFailure(`Your route totals ${total}, but there is no section ${total}. You have failed the Trial of Ghosts.`);
      return;
    }

    setResolving(true);
    const navigated = await onNavigate(total);
    setResolving(false);
    if (!navigated) {
      setFailed(true);
      await onFailure(`Section ${total} is unavailable. You have failed the Trial of Ghosts.`);
    }
  };

  const total = route.length === 5
    ? route.reduce((sum, [row, col]) => sum + (BOARD[row][col] ?? 0), 0)
    : null;

  return (
    <section className="border-t border-white/10 bg-white/[0.02] px-4 py-4" aria-label="Trial of Ghosts">
      <div className="mx-auto max-w-md space-y-3">
        <div>
          <h2 className="font-mono text-xs uppercase tracking-widest text-ember">Trial of Ghosts</h2>
          <p className="mt-1 text-xs text-gray-400">
            Roll to choose your starting square, then trace exactly five adjacent squares ending beside the judges.
            Diagonal moves are allowed. Your route cannot revisit a square.
          </p>
        </div>

        {dieRoll === null ? (
          <button
            type="button"
            onClick={rollStartingSquare}
            disabled={isProcessing}
            className="rounded border border-ember/40 px-3 py-2 text-xs font-mono text-ember hover:bg-ember/10"
          >
            Roll the starting square
          </button>
        ) : (
          <>
            <p className="text-xs font-mono text-gray-300">
              Starting roll: <span className="text-ember">{dieRoll}</span>
            </p>

            <div className="grid grid-cols-7 gap-1" role="group" aria-label="Trial route squares">
              {BOARD.map((row, rowIndex) => row.map((value, colIndex) => {
                const isJudges = rowIndex === JUDGES[0] && colIndex === JUDGES[1];
                if (value === null) {
                  return <div key={`${rowIndex}-${colIndex}`} className="aspect-square rounded-sm bg-black/70" aria-hidden="true" />;
                }

                const routeIndex = route.findIndex(([r, c]) => r === rowIndex && c === colIndex);
                const canChoose = route.length > 0 &&
                  route.length < 5 &&
                  isReachable(rowIndex, colIndex, route) &&
                  routeIndex < 0 &&
                  !isJudges &&
                  (route.length !== 4 || isAdjacent([rowIndex, colIndex], JUDGES));
                const isStart = DIE_STARTS[dieRoll]?.[0] === rowIndex && DIE_STARTS[dieRoll]?.[1] === colIndex;

                return (
                  <button
                    key={`${rowIndex}-${colIndex}`}
                    type="button"
                    onClick={() => chooseSquare(rowIndex, colIndex)}
                    disabled={(!canChoose && routeIndex < 0) || resolving || failed || isProcessing}
                    aria-label={`${isJudges ? 'Judges' : `Square ${value}`}${routeIndex >= 0 ? `, route step ${routeIndex + 1}` : ''}${isStart ? ', starting square' : ''}`}
                    className={`relative aspect-square rounded-sm border text-[10px] font-mono transition-colors disabled:cursor-default ${
                      isJudges
                        ? 'border-ember/70 bg-ember/20 text-ember'
                        : routeIndex >= 0
                          ? 'border-ember bg-ember/20 text-ember'
                          : canChoose
                            ? 'border-white/30 bg-white/10 text-white hover:border-ember/70 hover:bg-ember/10'
                            : 'border-white/10 bg-black/30 text-gray-500 disabled:opacity-70'
                    }`}
                  >
                    {isJudges ? '?' : value}
                    {routeIndex >= 0 && (
                      <span className="absolute right-0.5 top-0 text-[8px] text-white/70">{routeIndex + 1}</span>
                    )}
                    {isStart && <span className="absolute bottom-0 left-0.5 text-[7px] text-ember">START</span>}
                  </button>
                );
              }))}
            </div>

            <p className="text-xs font-mono text-gray-400">
              Route: {route.map(([row, col]) => BOARD[row][col]).join(' → ') || 'choose squares'}
              {total !== null && <span className="ml-2 text-ember">Total: {total}</span>}
            </p>

            {route.length > 0 && !failed && (
              <button
                type="button"
                onClick={resolveRoute}
                disabled={route.length !== 5 || resolving || isProcessing}
                className="rounded border border-ember/40 px-3 py-2 text-xs font-mono text-ember hover:bg-ember/10 disabled:cursor-not-allowed disabled:opacity-40"
              >
                {resolving ? 'Resolving route…' : total !== null && total > 400 ? 'Resolve failed trial' : total !== null ? `Turn to section ${total}` : 'Choose five squares'}
              </button>
            )}

          </>
        )}
      </div>
    </section>
  );
}
