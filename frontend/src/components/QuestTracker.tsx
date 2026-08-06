import React from 'react';
import { motion } from 'framer-motion';
import { BookOpen, Circle } from 'lucide-react';

export const QuestTracker = () => {
  return (
    <motion.div 
      initial={{ y: 20, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      className="w-full flex flex-col bg-black/40 border border-white/10 rounded-2xl p-5 backdrop-blur-md"
    >
      <div className="flex items-center gap-2 mb-4 border-b border-white/5 pb-3">
        <BookOpen size={16} className="text-ember" />
        <span className="text-[10px] font-mono text-gray-400 uppercase tracking-[0.2em]">Objectives</span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-1 gap-6">
        <div>
          <p className="text-[11px] text-ember mb-3 uppercase tracking-widest font-bold">The Iron Threshold</p>
          <ul className="space-y-3 ml-2 border-l border-white/10 pl-4">
            <li className="text-[10px] text-gray-300 flex items-center gap-3">
              <Circle size={8} className="text-ember animate-pulse fill-ember/20" /> Find the Key
            </li>
            <li className="text-[10px] text-gray-500 flex items-center gap-3 font-serif italic">
              <Circle size={8} /> Survive the Whispers
            </li>
          </ul>
        </div>
        
        <div className="xl:pt-4 xl:border-t xl:border-white/5">
          <p className="text-[10px] text-gray-600 mb-2 uppercase tracking-widest">Active Rumors</p>
          <p className="text-[9px] text-gray-700 italic leading-relaxed">
            The villagers speak of a hooded figure near the well...
          </p>
        </div>
      </div>
    </motion.div>
  );
};