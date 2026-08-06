import React from 'react';
import { m } from 'framer-motion';
import { Heart, Zap, Shield } from 'lucide-react';
import { useGame } from '../Context/GameContext';

export const StatusHUD = () => {
  const { stats, user } = useGame();
  
  return (
    <div className="w-full mb-4 px-2">
      <div className="flex flex-row items-center justify-between bg-black/60 border border-white/10 rounded-xl px-6 py-2 backdrop-blur-md shadow-2xl">
        
        {/* Compact Stats Section */}
        <div className="flex items-center gap-8">
          {/* Vitality */}
          <div className="flex items-center gap-3">
            <Heart size={16} className="text-red-500 fill-red-500/20" />
            <div className="w-32 h-1.5 bg-gray-900 rounded-full overflow-hidden border border-white/5">
              <m.div animate={{ width: `${stats.vitality}%` }} className="h-full bg-red-600 shadow-[0_0_8px_rgba(220,38,38,0.5)]" />
            </div>
            <span className="text-[10px] font-mono text-white/80">{stats.vitality}</span>
          </div>

          {/* Essence */}
          <div className="flex items-center gap-3">
            <Zap size={16} className="text-blue-400 fill-blue-400/20" />
            <div className="w-32 h-1.5 bg-gray-900 rounded-full overflow-hidden border border-white/5">
              <m.div animate={{ width: `${stats.essence}%` }} className="h-full bg-blue-500 shadow-[0_0_8px_rgba(59,130,246,0.5)]" />
            </div>
            <span className="text-[10px] font-mono text-white/80">{stats.essence}</span>
          </div>
        </div>

        {/* Level & Class Info */}
        <div className="flex items-center gap-4 border-l border-white/10 pl-6">
          <div className="text-right">
            <div className="text-[10px] font-gothic text-ember tracking-widest uppercase leading-none">
              LVL {user?.level || 1}
            </div>
            <div className="text-[8px] text-gray-500 font-mono uppercase mt-1">
              {user?.class || 'Initiate'}
            </div>
          </div>
          <div className="w-8 h-8 rounded border border-white/10 bg-white/5 flex items-center justify-center">
             <Shield size={14} className="text-gray-400" />
          </div>
        </div>

      </div>
    </div>
  );
};