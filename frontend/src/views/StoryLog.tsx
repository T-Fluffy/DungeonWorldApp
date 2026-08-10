import React, { useEffect, useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Terminal, ChevronRight, ChevronLeft, ChevronDown, RefreshCw, Package, Zap, CheckCircle2, BookOpen, Image as ImageIcon, Map as MapIcon, ScrollText, Sparkles } from 'lucide-react';
import { useGame } from '../Context/useGame';
import { StatusHUD } from '../components/StatusHUD';
import { Navigation } from '../components/Navigation';
import { useGameSession } from '../hooks/useGameSession';
import { listBooks, getAdventures, getBookMeta } from '../api/client';
import type { AdventureResponse, BookMetaDto } from '../api/client';
import type { LogEntry } from '../types/game';

type FeedbackType = 'item' | 'level' | 'success';
interface FeedbackNotification {
  id: number;
  text: string;
  type: FeedbackType;
}

type LeftTab = 'art' | 'map' | 'sheet';

export function StoryLog() {
  const { user, addItem, currentBook, setCurrentBook } = useGame();
  const [input, setInput] = useState('');
  const [books, setBooks] = useState<string[]>([]);
  const [adventures, setAdventures] = useState<AdventureResponse[]>([]);
  const [bookMetas, setBookMetas] = useState<Record<string, BookMetaDto>>({});
  const [booksError, setBooksError] = useState<string | null>(null);
  const [switcherOpen, setSwitcherOpen] = useState(false);
  const [artFailed, setArtFailed] = useState(false);
  const [failedCovers, setFailedCovers] = useState<Set<string>>(new Set());
  const [leftTab, setLeftTab] = useState<LeftTab>('art');
  const [choicesOpen, setChoicesOpen] = useState(true);
  const scrollRef = useRef<HTMLDivElement>(null);
  const sliderRef = useRef<HTMLDivElement>(null);
  const [notifications, setNotifications] = useState<FeedbackNotification[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);

  const {
    logs: sessionLogs,
    meta,
    section,
    isLoading,
    isProcessing,
    combat,
    goTo,
    processCommand,
    save,
  } = useGameSession(currentBook);

  useEffect(() => {
    setLogs(sessionLogs);
  }, [sessionLogs]);

  useEffect(() => {
    setArtFailed(false);
  }, [section?.sectionNumber]);

  useEffect(() => {
    if (scrollRef.current) scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
  }, [logs, isProcessing]);

  // List all known grimoires + the player's saved runs so the switcher can show progress
  useEffect(() => {
    listBooks()
      .then((titles) => {
        setBooks(titles);
        if (titles.length === 0) {
          setBooksError('No grimoires have been ingested. Visit the Summoning circle to bind one.');
        }
      })
      .catch(() => setBooksError('The engine could not be reached. Is the backend running?'));

    if (user?.isLoggedIn) {
      getAdventures()
        .then(setAdventures)
        .catch(() => setAdventures([]));
    }
  }, [user?.isLoggedIn]);

  // Fetch each book's meta so the left showcase can render cover art
  useEffect(() => {
    if (books.length === 0) return;
    let cancelled = false;
    (async () => {
      const entries = await Promise.all(
        books.map(async (title) => [title, await getBookMeta(title)] as const)
      );
      if (!cancelled) {
        setBookMetas(Object.fromEntries(entries));
      }
    })().catch(() => {});
    return () => { cancelled = true; };
  }, [books]);

  const coverArtOf = (title: string): string | null =>
    bookMetas[title]?.mapPath ?? bookMetas[title]?.adventureSheetPath ?? null;

  const progressOf = (title: string): AdventureResponse | undefined =>
    adventures.find(a => a.bookTitle === title);

  const switchBook = async (title: string) => {
    setSwitcherOpen(false);
    if (title === currentBook) return;
    // Sealing the current chronicle before switching grimoires
    await save();
    setCurrentBook(title);
  };

  const nudgeSlider = (dir: 1 | -1) => {
    sliderRef.current?.scrollBy({ left: dir * 120, behavior: 'smooth' });
  };

  const triggerFeedback = (text: string, type: FeedbackType = 'success') => {
    const id = Date.now();
    setNotifications(prev => [...prev, { id, text, type }]);
    setTimeout(() => setNotifications(prev => prev.filter(n => n.id !== id)), 3000);
  };

  const handleCommand = (e?: React.FormEvent, manualCmd?: string) => {
    if (e) e.preventDefault();
    const finalCmd = manualCmd || input;
    if (!finalCmd.trim() || isProcessing) return;

    setInput('');
    if (finalCmd.toLowerCase().includes('take')) {
      triggerFeedback("Item Discovered: Iron Key", 'item');
      addItem({
        id: `iron-key-${Date.now()}`,
        name: 'Iron Key',
        description: 'A heavy key that hums with old magic.',
        type: 'quest',
        rarity: 'rare',
      });
    }
    processCommand(finalCmd);
  };

  // If no book is selected: show a picker before the terminal
  if (!currentBook) {
    return (
      <div className="min-h-screen bg-black overflow-x-hidden">
        <div className="relative z-20 w-full max-w-3xl mx-auto p-8 mt-10">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="bg-black/40 backdrop-blur-xl border border-white/10 rounded-2xl shadow-2xl p-8"
          >
            <div className="text-center mb-8">
              <BookOpen className="w-12 h-12 text-ember mx-auto mb-4" />
              <h2 className="text-3xl font-gothic text-white tracking-widest uppercase">The Chronicle Awaits</h2>
              <p className="text-gray-500 text-xs uppercase tracking-widest mt-2">
                Choose a bound grimoire to begin your journey
              </p>
            </div>

            {isLoading && (
              <div className="text-center py-10">
                <RefreshCw className="w-8 h-8 text-ember animate-spin mx-auto" />
              </div>
            )}

            {booksError && (
              <div className="text-center py-10">
                <p className="text-crimson font-gothic mb-6">{booksError}</p>
              </div>
            )}

            <div className="space-y-3">
              {books.map((title) => {
                const run = progressOf(title);
                return (
                  <button
                    key={title}
                    onClick={() => setCurrentBook(title)}
                    className="w-full flex items-center justify-between p-4 border border-white/10 bg-white/5 hover:border-ember/40 hover:bg-ember/5 transition-all rounded-lg group"
                  >
                    <span className="text-sm text-gray-300 group-hover:text-white font-serif italic">{title}</span>
                    <span className="flex items-center gap-3">
                      {run && (
                        <span className={`text-[11px] font-mono uppercase tracking-widest px-2 py-0.5 rounded border ${
                          run.isComplete
                            ? 'text-emerald-400 border-emerald-500/30'
                            : 'text-ember/70 border-ember/20'
                        }`}>
                          {run.isComplete ? 'Completed' : `S${run.currentSection}`}
                        </span>
                      )}
                      <ChevronRight size={16} className="text-ember" />
                    </span>
                  </button>
                );
              })}
            </div>
          </motion.div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-black overflow-x-hidden">
      
      {/* FEEDBACK OVERLAY */}
      <div className="fixed top-24 left-1/2 -translate-x-1/2 z-[100] flex flex-col items-center pointer-events-none">
        <AnimatePresence>
          {notifications.map((n) => (
            <motion.div key={n.id} initial={{ y: -20, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: -40, opacity: 0 }}
              className={`mb-3 px-6 py-2 rounded-full border shadow-xl flex items-center gap-3 font-gothic tracking-widest text-xs
                ${n.type === 'item' ? 'bg-blue-900/40 border-blue-400 text-blue-200 backdrop-blur-md' : ''}
                ${n.type === 'level' ? 'bg-ember border-white text-black font-bold' : ''}
                ${n.type === 'success' ? 'bg-green-900/40 border-green-400 text-green-200 backdrop-blur-md' : ''}
              `}>
              {n.type === 'item' && <Package size={14} />}
              {n.type === 'level' && <Zap size={14} />}
              {n.type === 'success' && <CheckCircle2 size={14} />}
              {n.text}
            </motion.div>
          ))}
        </AnimatePresence>
      </div>

      {/* PORTRAIT WARNING */}
      <div className="portrait-warning bg-black flex md:hidden fixed inset-0 z-[110] flex-col items-center justify-center text-center p-6">
        <RefreshCw size={48} className="text-ember animate-spin mb-6" />
        <h2 className="text-2xl font-gothic text-white uppercase">Landscape Required</h2>
      </div>

      {/* MAIN CONTENT AREA */}
      <div className="relative z-20 w-full max-w-[1500px] mx-auto p-4 md:p-8 pb-32">
        <StatusHUD />

        {/* Book header — title plate + grimoire slider */}
        <div className="w-full h-20 md:h-24 mb-6 rounded-2xl overflow-hidden border border-white/10 bg-[#050505] relative">
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,_rgba(251,191,36,0.05)_0%,_transparent_70%)]" />
          <div className="absolute inset-0 flex items-center justify-between px-6 md:px-10">
            <div className="flex flex-col">
              <p className="text-gray-500 text-[11px] uppercase tracking-[0.3em] mb-1">Bound Grimoire</p>
              <h2 className="text-xl md:text-3xl font-gothic text-white tracking-[0.2em] uppercase">
                {meta?.title || 'The Whispering Catacombs'}
              </h2>
              {meta && (
                <p className="text-gray-500 text-[12px] uppercase tracking-[0.3em] mt-1">
                  {meta.sectionCount} sections bound
                </p>
              )}
            </div>
            {/* Book slider */}
            <div className="hidden md:flex items-center gap-2">
              <button
                onClick={() => nudgeSlider(-1)}
                className="w-7 h-7 flex items-center justify-center rounded border border-white/10 hover:border-ember/40 text-gray-400 hover:text-ember transition-colors"
                aria-label="Previous grimoire"
              >
                <ChevronLeft size={14} />
              </button>
              <div ref={sliderRef} className="flex gap-2 overflow-x-auto max-w-[340px] py-1 custom-scrollbar no-scrollbar">
                {books.map((title) => {
                  const cover = coverArtOf(title);
                  const coverFailed = failedCovers.has(title);
                  const isActive = title === currentBook;
                  return (
                    <button
                      key={title}
                      onClick={() => switchBook(title)}
                      title={title}
                      className={`relative w-11 h-14 shrink-0 rounded-md overflow-hidden border transition-all ${
                        isActive
                          ? 'border-ember/70 ring-1 ring-ember/50'
                          : 'border-white/10 opacity-60 hover:opacity-100 hover:border-ember/40'
                      }`}
                    >
                      {cover && !coverFailed ? (
                        <img
                          src={cover}
                          alt={title}
                          onError={() => setFailedCovers(prev => new Set(prev).add(title))}
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        <div className="w-full h-full bg-gradient-to-br from-ember/25 via-black to-black flex items-center justify-center">
                          <span className="font-gothic text-[8px] text-center leading-tight px-0.5 text-ember/80 line-clamp-3">{title}</span>
                        </div>
                      )}
                      {isActive && <span className="absolute bottom-0.5 right-0.5 w-1.5 h-1.5 rounded-full bg-ember animate-pulse" />}
                    </button>
                  );
                })}
              </div>
              <button
                onClick={() => nudgeSlider(1)}
                className="w-7 h-7 flex items-center justify-center rounded border border-white/10 hover:border-ember/40 text-gray-400 hover:text-ember transition-colors"
                aria-label="Next grimoire"
              >
                <ChevronRight size={14} />
              </button>
            </div>
          </div>
        </div>

        {/* OPEN BOOK — two facing pages */}
        <div className="relative flex flex-col lg:flex-row items-stretch gap-3 lg:gap-0">

          {/* LEFT PAGE — imagery, maps, animations */}
          <div className="w-full lg:w-1/2 relative flex flex-col bg-gradient-to-br from-[#15100b] via-[#0e0a06] to-[#0a0705] border border-white/10 rounded-2xl lg:rounded-r-none lg:rounded-l-2xl p-5 lg:h-[calc(100vh-300px)] min-h-[380px] overflow-hidden shadow-2xl">
            {/* page fold highlight */}
            <div className="pointer-events-none absolute inset-y-0 right-0 w-24 bg-gradient-to-l from-black/50 to-transparent lg:hidden" />
            <div className="pointer-events-none absolute inset-y-0 right-0 w-10 bg-gradient-to-l from-black/40 to-transparent hidden lg:block" />

            <div className="flex items-center justify-between mb-4 border-b border-white/5 pb-3">
              <div className="flex items-center gap-2">
                <ImageIcon size={16} className="text-ember" />
                <span className="text-[12px] font-mono text-gray-400 uppercase tracking-[0.2em]">
                  The Left Page{section ? ` · ${section.sectionNumber}` : ''}
                </span>
              </div>
              <div className="flex items-center gap-1">
                {([
                  ['art', ImageIcon],
                  ['map', MapIcon],
                  ['sheet', ScrollText],
                ] as [LeftTab, typeof ImageIcon][]).map(([tab, Icon]) => (
                  <button
                    key={tab}
                    onClick={() => setLeftTab(tab)}
                    className={`w-7 h-7 flex items-center justify-center rounded border transition-colors ${
                      leftTab === tab
                        ? 'border-ember/50 text-ember bg-ember/10'
                        : 'border-white/10 text-gray-500 hover:text-ember/70 hover:border-ember/30'
                    }`}
                    title={tab === 'art' ? 'Section art' : tab === 'map' ? 'Grimoire map' : 'Adventure sheet'}
                  >
                    <Icon size={14} />
                  </button>
                ))}
              </div>
            </div>

            <div className="flex-1 min-h-0 rounded-lg overflow-hidden border border-white/10 bg-black/40">
              {leftTab === 'art' && (
                section?.imagePath && !artFailed ? (
                  <img
                    src={section.imagePath}
                    alt={`Section ${section.sectionNumber}`}
                    onError={() => setArtFailed(true)}
                    className="w-full h-full object-contain"
                  />
                ) : (
                  <div className="w-full h-full flex flex-col items-center justify-center text-center p-6">
                    <ImageIcon size={28} className="text-ember/40 mb-3" />
                    <p className="text-[12px] text-gray-600 italic">
                      {section?.imagePath && artFailed
                        ? 'Illustration unavailable.'
                        : 'No illustration for this section.'}
                    </p>
                  </div>
                )
              )}

              {leftTab === 'map' && (
                meta?.mapPath ? (
                  <img src={meta.mapPath} alt="Grimoire map" className="w-full h-full object-contain" />
                ) : (
                  <div className="w-full h-full flex flex-col items-center justify-center text-center p-6">
                    <MapIcon size={28} className="text-ember/40 mb-3" />
                    <p className="text-[12px] text-gray-600 italic">No map is bound to this grimoire.</p>
                  </div>
                )
              )}

              {leftTab === 'sheet' && (
                meta?.adventureSheetPath ? (
                  <img src={meta.adventureSheetPath} alt="Adventure sheet" className="w-full h-full object-contain" />
                ) : (
                  <div className="w-full h-full flex flex-col items-center justify-center text-center p-6">
                    <ScrollText size={28} className="text-ember/40 mb-3" />
                    <p className="text-[12px] text-gray-600 italic">No adventure sheet is bound to this grimoire.</p>
                  </div>
                )
              )}
            </div>

            {/* future animations / flavour placeholder */}
            <div className="mt-4 flex items-center gap-2 text-[11px] font-mono text-gray-600 uppercase tracking-widest">
              <Sparkles size={12} className="text-ember/40" />
              <span>Animated scenes will unfold upon this page.</span>
            </div>
          </div>

          {/* BOOK SPINE / GUTTER */}
          <div className="shrink-0 relative hidden lg:flex lg:w-12 items-stretch">
            <div className="w-3 mx-auto my-2 bg-gradient-to-b from-transparent via-[#2a1f14] to-transparent rounded-full shadow-[0_0_12px_rgba(0,0,0,0.9)]" />
            <div className="absolute inset-0 flex items-center justify-center">
              <div className="w-px h-full bg-white/5" />
            </div>
          </div>

          {/* RIGHT PAGE — the chronicle terminal */}
          <div className="w-full lg:w-1/2 relative flex flex-col bg-gradient-to-br from-[#15100b] via-[#0e0a06] to-[#0a0705] border border-white/10 rounded-2xl lg:rounded-l-none lg:rounded-r-2xl overflow-hidden shadow-2xl lg:h-[calc(100vh-300px)] min-h-[560px]">
            {/* page fold highlight */}
            <div className="pointer-events-none absolute inset-y-0 left-0 w-10 bg-gradient-to-r from-black/40 to-transparent hidden lg:block" />

            <div className="h-10 bg-white/5 border-b border-white/5 flex items-center px-4 shrink-0 justify-between">
              <div className="flex items-center gap-2">
                <Terminal size={14} className="text-ember" />
                <span className="text-[12px] font-mono text-gray-500 uppercase tracking-widest">Chronicle_Feed</span>
              </div>

              {/* Grimoire switcher */}
              <div className="relative">
                <button
                  onClick={() => setSwitcherOpen(o => !o)}
                  className="flex items-center gap-2 text-[12px] font-mono text-ember/70 uppercase tracking-widest border border-white/10 hover:border-ember/40 rounded px-2 py-1 transition-colors"
                >
                  <BookOpen size={12} />
                  <span className="truncate max-w-[160px]">{currentBook}</span>
                  <ChevronDown size={12} className={`transition-transform ${switcherOpen ? 'rotate-180' : ''}`} />
                </button>

                {switcherOpen && (
                  <>
                    <div className="fixed inset-0 z-40" onClick={() => setSwitcherOpen(false)} />
                    <div className="absolute right-0 top-full mt-2 w-72 bg-black/95 border border-white/10 rounded-xl shadow-2xl z-50 p-2 space-y-1 max-h-72 overflow-y-auto custom-scrollbar">
                      <p className="text-[11px] font-mono text-gray-500 uppercase tracking-widest px-2 pt-1 pb-2">
                        Switch Grimoire
                      </p>
                      {books.length === 0 && booksError && (
                        <p className="text-gray-500 text-[12px] px-2 py-2 italic">{booksError}</p>
                      )}
                      {books.map((title) => {
                        const run = progressOf(title);
                        const isActive = title === currentBook;
                        return (
                          <button
                            key={title}
                            onClick={() => switchBook(title)}
                            disabled={isActive}
                            className={`w-full flex items-center justify-between gap-3 px-3 py-2 rounded-lg text-xs border transition-all ${
                              isActive
                                ? 'border-ember/40 bg-ember/10 text-ember'
                                : 'border-white/5 bg-white/[0.02] text-gray-300 hover:bg-white/5 hover:border-ember/30'
                            } disabled:opacity-60`}
                          >
                            <span className="font-serif italic truncate">{title}</span>
                            <span className="flex items-center gap-2 shrink-0">
                              {isActive && <span className="text-[11px] font-mono uppercase tracking-widest text-ember">Active</span>}
                              {!isActive && run && (
                                <span className={`text-[11px] font-mono uppercase tracking-widest px-1.5 py-0.5 rounded border ${
                                  run.isComplete ? 'text-emerald-400 border-emerald-500/30' : 'text-ember/70 border-ember/20'
                                }`}>
                                  {run.isComplete ? 'Complete' : `S${run.currentSection}`}
                                </span>
                              )}
                              {!isActive && !run && <span className="text-[11px] font-mono uppercase tracking-widest text-gray-600">New</span>}
                            </span>
                          </button>
                        );
                      })}
                    </div>
                  </>
                )}
              </div>
            </div>

            {/* Combat HUD: shown while a fight is active */}
            {combat.inCombat && combat.currentEnemy && (
              <div className="shrink-0 border-b border-ember/20 bg-ember/[0.03] px-4 py-2 flex items-center justify-between gap-3">
                <div className="flex items-center gap-2 min-w-0">
                  <Zap size={13} className="text-ember shrink-0" />
                  <span className="text-[11px] font-mono text-ember uppercase tracking-widest truncate">
                    {combat.currentEnemy.name}
                  </span>
                  <span className="text-[11px] font-mono text-gray-400 shrink-0">
                    STAMINA {Math.max(0, combat.currentEnemy.stamina)}/{combat.currentEnemy.staminaMax}
                  </span>
                </div>
                <div className="flex items-center gap-3 text-[11px] font-mono text-gray-400 shrink-0">
                  <span>SKILL {combat.playerSkill}</span>
                  <span className={combat.playerStamina <= 0 ? 'text-crimson' : 'text-red-400'}>
                    STAMINA {Math.max(0, combat.playerStamina)}
                  </span>
                  <span>LUCK {combat.playerLuck}</span>
                </div>
              </div>
            )}

            <div ref={scrollRef} className="flex-1 min-h-0 overflow-y-auto p-4 space-y-4 custom-scrollbar">
              {logs.map(log => (
                <div key={log.id} className={`flex flex-col ${log.type === 'player' ? 'items-end' : 'items-start'}`}>
                  <div className={`p-3 rounded-xl text-xs md:text-sm max-w-[85%] whitespace-pre-line ${
                    log.type === 'player' 
                      ? 'bg-ember/10 border border-ember/20 text-ember/80 font-mono' 
                      : 'bg-white/[0.03] border border-white/5 text-gray-300 font-serif italic'
                  }`}>
                    {log.content}
                  </div>
                </div>
              ))}
              {isProcessing && <div className="text-ember/30 animate-pulse text-[11px] font-mono ml-2">INTERPRETING...</div>}
            </div>

            {/* Choices for the current section */}
            {section && section.choices.length > 0 && (
              <div className="shrink-0 border-t border-white/10 bg-white/[0.02]">
                <button
                  onClick={() => setChoicesOpen(o => !o)}
                  className="w-full flex items-center justify-between px-4 py-2 text-[12px] font-mono text-gray-500 uppercase tracking-widest hover:text-ember/70 transition-colors"
                >
                  <span className="flex items-center gap-2">
                    Your Path
                    <span className="px-1.5 py-0.5 rounded border border-ember/20 text-ember/70 text-[10px] leading-none">
                      {section.choices.length}
                    </span>
                  </span>
                  <ChevronDown size={14} className={`transition-transform ${choicesOpen ? 'rotate-180' : ''}`} />
                </button>
                <AnimatePresence initial={false}>
                  {choicesOpen && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      transition={{ duration: 0.2 }}
                      className="overflow-hidden"
                    >
                      <div className="px-4 pb-3 space-y-1.5">
                        {section.choices.map((choice, i) => (
                          <button
                            key={i}
                            onClick={() => goTo(choice.targetSectionNumber)}
                            disabled={isProcessing}
                            className="w-full text-left px-3 py-1.5 text-xs border border-white/10 bg-black/30 hover:border-ember/40 hover:bg-ember/5 rounded-md text-gray-300 hover:text-white transition-all disabled:opacity-50"
                          >
                            <span className="flex items-center justify-between gap-3">
                              <span className="font-serif italic">{choice.description}</span>
                              <span className="text-ember font-mono shrink-0">{choice.targetSectionNumber}</span>
                            </span>
                          </button>
                        ))}
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
            )}

            <div className="shrink-0 bg-black/60 border-t border-white/10 p-4">
              <form onSubmit={handleCommand} className="flex items-center gap-3 px-1 mb-3">
                <ChevronRight size={18} className="text-ember" />
                <input 
                  value={input} 
                  onChange={e => setInput(e.target.value)} 
                  placeholder={isProcessing ? 'Interpreting...' : 'Type your intent, or GO [section number]...'} 
                  disabled={isProcessing}
                  className="bg-transparent border-none outline-none text-white font-mono text-sm w-full"
                />
              </form>
              <div className="flex gap-2 overflow-x-auto no-scrollbar pb-1">
                {['Look', 'Inventory', 'Help', ...(combat.inCombat ? ['Battle', 'Flee'] : []), 'Roll'].map(cmd => (
                  <button key={cmd} onClick={() => handleCommand(undefined, cmd)} disabled={isProcessing} className={`px-4 py-1.5 text-[11px] font-mono border rounded transition-colors whitespace-nowrap disabled:opacity-50 ${
                    cmd === 'Battle' || cmd === 'Flee'
                      ? 'border-ember/40 hover:bg-ember/10 text-ember'
                      : 'border-white/10 hover:border-ember/40 text-gray-500 uppercase'
                  }`}>
                    {cmd}
                  </button>
                ))}
                <button onClick={() => handleCommand(undefined, 'Reset')} disabled={isProcessing} className="px-4 py-1.5 text-[11px] font-mono border border-white/10 rounded hover:border-ember/40 text-gray-500 uppercase transition-colors whitespace-nowrap disabled:opacity-50">
                  Reset
                </button>
                {section && (
                  <button onClick={() => goTo(section.sectionNumber)} disabled={isProcessing} className="px-4 py-1.5 text-[11px] font-mono border border-white/10 rounded hover:border-ember/40 text-gray-500 uppercase transition-colors whitespace-nowrap disabled:opacity-50">
                    Reread
                  </button>
                )}
              </div>
            </div>

            <div className="shrink-0 pt-2 pb-1 bg-black/40">
              <Navigation docked />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
