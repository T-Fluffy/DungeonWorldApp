import React from 'react';
import { motion } from 'framer-motion';
import { Shield, Sword, Sparkles, Ghost, Zap } from 'lucide-react';

export const QuickGear = () => {
  const slots = [
    { icon: Sword, label: 'Main' }, { icon: Shield, label: 'Off' },
    { icon: Ghost, label: 'Armor' }, { icon: Sparkles, label: 'Relic' },
    { icon: Zap, label: 'Charm' }, { icon: Shield, label: 'Back' }
  ];

  return (
    <motion.div 
      initial={{ y: 20, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      className="w-full flex flex-col bg-black/40 border border-white/10 rounded-2xl p-5 backdrop-blur-md"
    >
      <div className="flex items-center gap-2 mb-4 border-b border-white/5 pb-3">
        <Shield size={16} className="text-blue-400" />
        <span className="text-[10px] font-mono text-gray-400 uppercase tracking-[0.2em]">Equipment</span>
      </div>

      <div className="grid grid-cols-3 xl:grid-cols-2 gap-3">
        {slots.map((slot, i) => (
          <div key={i} className="aspect-square bg-white/[0.03] border border-white/5 rounded-xl flex flex-col items-center justify-center group hover:bg-white/[0.07] transition-all cursor-pointer">
            <slot.icon size={20} className="text-white/20 group-hover:text-blue-400 transition-colors" />
            <span className="text-[8px] text-gray-600 mt-2 uppercase tracking-tighter">{slot.label}</span>
          </div>
        ))}
      </div>
    </motion.div>
  );
};