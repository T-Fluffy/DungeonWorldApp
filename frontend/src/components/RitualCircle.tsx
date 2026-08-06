import React from 'react';
import { motion } from 'framer-motion';

export function RitualCircle({ isProcessing }: { isProcessing: boolean }) {
  return (
    <div className="relative w-64 h-64 flex items-center justify-center">
      {/* Outer Glow */}
      <div className={`absolute inset-0 rounded-full transition-opacity duration-1000 ${isProcessing ? 'bg-ember/10 blur-3xl opacity-100' : 'opacity-0'}`} />
      
      {/* Outer Ring */}
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 20, repeat: Infinity, ease: "linear" }}
        className={`absolute inset-0 border-2 border-dashed border-ember/20 rounded-full`}
      />

      {/* Main Sigil Ring */}
      <motion.div
        animate={{ 
          rotate: -360,
          scale: isProcessing ? [1, 1.05, 1] : 1,
        }}
        transition={{ 
          rotate: { duration: 30, repeat: Infinity, ease: "linear" },
          scale: { duration: 2, repeat: Infinity }
        }}
        className={`absolute inset-4 border border-ember/40 rounded-full flex items-center justify-center`}
      >
        {/* Decorative Runes (Simulated with dots/lines) */}
        {[...Array(8)].map((_, i) => (
          <div
            key={i}
            className="absolute w-1 h-1 bg-ember/60 rounded-full"
            style={{
              transform: `rotate(${i * 45}deg) translateY(-92px)`
            }}
          />
        ))}
      </motion.div>

      {/* Inner Rotating Hexagram */}
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 10, repeat: Infinity, ease: "linear" }}
        className="relative w-32 h-32 border-2 border-ember/50 transition-colors duration-500"
        style={{ clipPath: 'polygon(50% 0%, 100% 25%, 100% 75%, 50% 100%, 0% 75%, 0% 25%)' }}
      >
        <div className="absolute inset-2 border border-ember/30" style={{ clipPath: 'polygon(50% 0%, 100% 25%, 100% 75%, 50% 100%, 0% 75%, 0% 25%)' }} />
      </motion.div>

      {/* Central Pulse */}
      <motion.div 
        animate={isProcessing ? { scale: [1, 1.5, 1], opacity: [0.3, 0.6, 0.3] } : {}}
        transition={{ duration: 1.5, repeat: Infinity }}
        className="absolute w-12 h-12 bg-ember/20 rounded-full blur-xl"
      />
    </div>
  );
}