import React from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import { Sword } from 'lucide-react';

export function HomePage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center p-6">
      <motion.div 
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="text-center space-y-8"
      >
        <div className="relative">
          <Sword className="w-16 h-16 text-ember mx-auto mb-4 animate-bounce" />
          <h1 className="text-6xl md:text-8xl font-gothic text-white tracking-tighter text-glow-strong">
            Dungeon World
          </h1>
        </div>

        <p className="max-w-xl mx-auto text-gray-400 text-lg font-light leading-relaxed tracking-wide italic">
          "Where steel meets shadow, and legends are forged in the ink of the abyss."
        </p>

        <Link to="/login" className="inline-block group">
          <div className="px-12 py-5 bg-transparent border-2 border-ember text-ember font-gothic text-3xl hover:bg-ember hover:text-black transition-all duration-500 transform group-hover:scale-105 shadow-[0_0_15px_rgba(255,107,53,0.3)]">
            Begin the Ritual
          </div>
        </Link>
      </motion.div>
    </div>
  );
}