import React from 'react';
import { motion } from 'framer-motion';

export function RitualLoading() {
  return (
    <div className="fixed inset-0 z-[100] flex flex-col items-center justify-center bg-black">
      {/* The Spinning Ritual Circle */}
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 10, repeat: Infinity, ease: "linear" }}
        className="relative w-48 h-48 border-2 border-ember/20 rounded-full flex items-center justify-center"
      >
        <div className="absolute inset-2 border border-ember/40 rounded-full border-dashed" />
        <div className="absolute inset-8 border-2 border-ember/60 rounded-full" />
        
        {/* Glow dots */}
        <div className="absolute top-0 w-2 h-2 bg-ember rounded-full shadow-[0_0_10px_#ff6b35]" />
        <div className="absolute bottom-0 w-2 h-2 bg-ember rounded-full shadow-[0_0_10px_#ff6b35]" />
      </motion.div>
      
      <motion.p 
        initial={{ opacity: 0 }}
        animate={{ opacity: [0, 1, 0] }}
        transition={{ duration: 2, repeat: Infinity }}
        className="mt-8 font-gothic text-ember tracking-[0.5em] text-xl"
      >
        Consulting the Void...
      </motion.p>
    </div>
  );
}