import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';

const PARTING_WORDS = [
  'The flame gutters, but the ember remembers your name...',
  'Close the book, yet the ink does not forget.',
  'The shadows part to let you pass... but they will whisper of you.',
  'Your legend sleeps here. The threshold is patient.',
  'Slumber, wayfarer. The dungeon waits to welcome you home.',
  'The pact is paused, never broken. We shall speak again.',
];

export function LogoutLoading() {
  const phrase = PARTING_WORDS[Math.floor(Math.random() * PARTING_WORDS.length)];

  return (
    <div className="fixed inset-0 z-[100] flex flex-col items-center justify-center bg-black">
      {/* Fading gate / rising gate of ember */}
      <motion.div
        initial={{ scale: 1, opacity: 1 }}
        animate={{ scale: 0.4, opacity: 0 }}
        transition={{ duration: 2.4, ease: 'easeInOut' }}
        className="relative w-48 h-48"
      >
        <div className="absolute inset-0 border-2 border-ember/30 rounded-full" />
        <div className="absolute inset-6 border border-ember/50 rounded-full border-dashed" />
        <div className="absolute inset-12 border-2 border-ember/70 rounded-full" />
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-2 h-2 bg-ember rounded-full shadow-[0_0_10px_#ff6b35]" />
        <div className="absolute bottom-0 left-1/2 -translate-x-1/2 w-2 h-2 bg-ember rounded-full shadow-[0_0_10px_#ff6b35]" />
        <div className="absolute inset-0 flex items-center justify-center">
          <span className="font-gothic text-3xl text-ember/80">✦</span>
        </div>
      </motion.div>

      {/* Fading ember motes */}
      {[0, 1, 2, 3, 4].map((i) => (
        <motion.div
          key={i}
          initial={{ y: 0, opacity: 0.8 }}
          animate={{ y: -120 - i * 24, opacity: 0 }}
          transition={{ duration: 4, delay: i * 0.35, repeat: Infinity, ease: 'easeOut' }}
          className="absolute w-1.5 h-1.5 rounded-full bg-ember/60 shadow-[0_0_8px_#ff6b35]"
          style={{ left: `${32 + i * 12}%`, bottom: '18%' }}
        />
      ))}

      <AnimatePresence>
        <motion.p
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 1, delay: 0.3 }}
          exit={{ opacity: 0 }}
          className="mt-8 font-gothic text-ember tracking-[0.4em] text-lg md:text-xl uppercase text-center px-6 max-w-2xl"
        >
          Until the Shadows Call Again
        </motion.p>

        <motion.p
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 1.2, delay: 1 }}
          exit={{ opacity: 0 }}
          className="mt-4 font-serif italic text-gray-400 text-sm md:text-base text-center px-8 max-w-xl"
        >
          {phrase}
        </motion.p>
      </AnimatePresence>
    </div>
  );
}
