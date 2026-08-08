import React, { useEffect, useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Terminal, ChevronRight, RefreshCw, Package, Zap, CheckCircle2, BookOpen } from 'lucide-react';
import { useGame } from '../Context/useGame';
import { StatusHUD } from '../components/StatusHUD';
import { QuestTracker } from '../components/QuestTracker';
import { QuickGear } from '../components/QuickGear';
import { useGameSession } from '../hooks/useGameSession';
import { listBooks } from '../api/client';
import type { LogEntry } from '../types/game';

type FeedbackType = 'item' | 'level' | 'success';
interface FeedbackNotification {
  id: number;
  text: string;
  type: FeedbackType;
}

export function StoryLog() {
  const { addItem, currentBook, setCurrentBook } = useGame();
  const [input, setInput] = useState('');
  const [books, setBooks] = useState<string[]>([]);
  const [booksError, setBooksError] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const [notifications, setNotifications] = useState<FeedbackNotification[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);

  const {
    logs: sessionLogs,
    meta,
    section,
    isLoading,
    isProcessing,
    goTo,
    processCommand,
  } = useGameSession(currentBook);

  useEffect(() => {
    setLogs(sessionLogs);
  }, [sessionLogs]);

  useEffect(() => {
    if (scrollRef.current) scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
  }, [logs, isProcessing]);

  // If no grimoire is bound yet, list the ones the engine already knows
  useEffect(() => {
    if (currentBook) return;
    listBooks()
      .then((titles) => {
        setBooks(titles);
        if (titles.length === 0) {
          setBooksError('No grimoires have been ingested. Visit the Summoning circle to bind one.');
        }
      })
      .catch(() => setBooksError('The engine could not be reached. Is the backend running?'));
  }, [currentBook]);

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
              {books.map((title) => (
                <button
                  key={title}
                  onClick={() => setCurrentBook(title)}
                  className="w-full flex items-center justify-between p-4 border border-white/10 bg-white/5 hover:border-ember/40 hover:bg-ember/5 transition-all rounded-lg group"
                >
                  <span className="text-sm text-gray-300 group-hover:text-white font-serif italic">{title}</span>
                  <ChevronRight size={16} className="text-ember" />
                </button>
              ))}
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
      <div className="relative z-20 w-full max-w-[1400px] mx-auto p-4 md:p-8 pb-32">
        <StatusHUD />

        {/* Cinematic Area */}
        <div className="w-full h-[40vh] min-h-[280px] mb-6 rounded-2xl overflow-hidden border border-white/10 bg-[#050505] relative">
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,_rgba(251,191,36,0.05)_0%,_transparent_70%)]" />
          <div className="absolute inset-0 flex flex-col items-center justify-center p-6 text-center">
            <h2 className="text-3xl md:text-5xl font-gothic text-white tracking-[0.2em] uppercase">
              {meta?.title || 'The Whispering Catacombs'}
            </h2>
            <div className="h-px w-32 bg-ember/30 mt-4" />
            {meta && (
              <p className="text-gray-500 text-[10px] uppercase tracking-[0.3em] mt-3">
                {meta.sectionCount} sections bound
              </p>
            )}
          </div>
        </div>

        {/* Responsive Grid System */}
        <div className="flex flex-col xl:flex-row gap-6 items-start">
          
          <div className="w-full xl:w-72 order-2 xl:order-1">
            <QuestTracker />
          </div>

          <div className="w-full flex-1 order-1 xl:order-2 flex flex-col max-h-[600px] bg-black/40 border border-white/10 rounded-2xl overflow-hidden shadow-2xl">
            <div className="h-10 bg-white/5 border-b border-white/5 flex items-center px-4 shrink-0 justify-between">
              <div className="flex items-center gap-2">
                <Terminal size={14} className="text-ember" />
                <span className="text-[10px] font-mono text-gray-500 uppercase tracking-widest">Chronicle_Feed</span>
              </div>
              {currentBook && (
                <span className="text-[10px] font-mono text-ember/70 uppercase tracking-widest truncate max-w-[50%]">
                  {currentBook}
                </span>
              )}
            </div>

            {/* Section image, when one was extracted */}
            {section?.imagePath && (
              <div className="shrink-0 border-b border-white/5 overflow-hidden max-h-64">
                <img
                  src={section.imagePath}
                  alt={`Section ${section.sectionNumber}`}
                  className="w-full h-48 object-cover opacity-80"
                />
              </div>
            )}

            <div ref={scrollRef} className="h-[150px] md:h-auto md:flex-1 overflow-y-auto p-4 space-y-4 custom-scrollbar">
              {logs.map(log => (
                <div key={log.id} className={`flex flex-col ${log.type === 'player' ? 'items-end' : 'items-start'}`}>
                  <div className={`p-3 rounded-xl text-xs md:text-sm max-w-[85%] ${
                    log.type === 'player' 
                      ? 'bg-ember/10 border border-ember/20 text-ember/80 font-mono' 
                      : 'bg-white/[0.03] border border-white/5 text-gray-300 font-serif italic'
                  }`}>
                    {log.content}
                  </div>
                </div>
              ))}
              {isProcessing && <div className="text-ember/30 animate-pulse text-[9px] font-mono ml-2">INTERPRETING...</div>}
            </div>

            {/* Choices for the current section */}
            {section && section.choices.length > 0 && (
              <div className="shrink-0 border-t border-white/10 bg-white/[0.02] px-4 py-3 space-y-2">
                <p className="text-[10px] font-mono text-gray-500 uppercase tracking-widest">Your Path</p>
                {section.choices.map((choice, i) => (
                  <button
                    key={i}
                    onClick={() => goTo(choice.targetSectionNumber)}
                    disabled={isProcessing}
                    className="w-full text-left px-4 py-2.5 text-xs border border-white/10 bg-black/30 hover:border-ember/40 hover:bg-ember/5 rounded-lg text-gray-300 hover:text-white transition-all disabled:opacity-50"
                  >
                    <span className="flex items-center justify-between gap-3">
                      <span className="font-serif italic">{choice.description}</span>
                      <span className="text-ember font-mono shrink-0">{choice.targetSectionNumber}</span>
                    </span>
                  </button>
                ))}
              </div>
            )}

            <div className="bg-black/80 border-t border-white/10 p-4">
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
                {['Look', 'Inventory', 'Help'].map(cmd => (
                  <button key={cmd} onClick={() => handleCommand(undefined, cmd)} disabled={isProcessing} className="px-4 py-1.5 text-[9px] font-mono border border-white/10 rounded hover:border-ember/40 text-gray-500 uppercase transition-colors whitespace-nowrap disabled:opacity-50">
                    {cmd}
                  </button>
                ))}
                {section && (
                  <button onClick={() => goTo(section.sectionNumber)} disabled={isProcessing} className="px-4 py-1.5 text-[9px] font-mono border border-white/10 rounded hover:border-ember/40 text-gray-500 uppercase transition-colors whitespace-nowrap disabled:opacity-50">
                    Reread
                  </button>
                )}
              </div>
            </div>
          </div>

          <div className="w-full xl:w-72 order-3">
            <QuickGear />
          </div>
        </div>
      </div>
    </div>
  );
}
