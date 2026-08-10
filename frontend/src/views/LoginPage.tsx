import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import { useGame } from '../Context/useGame';
import { apiError } from '../api/client';
import { Skull, Ghost, KeyRound } from 'lucide-react';

export function LoginPage() {
  const [charName, setCharName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const { login } = useGame();
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!charName.trim() || !password) return;

    setSubmitting(true);
    setError(null);
    try {
      await login(charName.trim(), password);
      navigate('/log');
    } catch (err) {
      setError(apiError(err));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-6">
      <motion.div 
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="max-w-md w-full bg-black/40 backdrop-blur-xl border border-white/5 p-8 rounded-2xl shadow-2xl relative"
      >
        <div className="text-center mb-10">
          <Skull className="w-12 h-12 text-ember mx-auto mb-4 text-glow" />
          <h1 className="text-3xl font-gothic text-white mb-2">Identify Yourself</h1>
          <p className="text-gray-500 text-xs uppercase tracking-widest">Speak your name and word of pact</p>
        </div>

        <form onSubmit={handleLogin} className="space-y-6">
          <div className="relative group">
            <input
              type="text"
              required
              value={charName}
              onChange={(e) => setCharName(e.target.value)}
              className="w-full bg-white/5 border border-white/10 px-4 py-4 rounded-lg text-white placeholder:text-gray-600 focus:outline-none focus:border-ember/50 transition-all"
              placeholder="Username or Email..."
            />
            <Ghost className="absolute right-4 top-4 w-5 h-5 text-gray-600" />
          </div>

          <div className="relative group">
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-white/5 border border-white/10 px-4 py-4 rounded-lg text-white placeholder:text-gray-600 focus:outline-none focus:border-ember/50 transition-all"
              placeholder="Word of the Pact..."
            />
            <KeyRound className="absolute right-4 top-4 w-5 h-5 text-gray-600" />
          </div>

          {error && (
            <p className="text-xs text-red-400 border border-red-500/30 bg-red-500/10 rounded-lg px-4 py-3">
              {error}
            </p>
          )}

          <button type="submit" disabled={submitting} className="w-full group py-4 bg-transparent border border-ember/40 hover:border-ember text-ember font-gothic text-xl tracking-widest transition-all disabled:opacity-50">
            {submitting ? 'Summoning...' : 'Proceed to the Void'}
          </button>
        </form>

        <div className="mt-8 text-center border-t border-white/5 pt-6">
          <p className="text-gray-500 text-xs uppercase tracking-widest mb-2">New Wanderer?</p>
          <Link to="/register" className="text-ember hover:text-white transition-colors font-gothic text-lg">
            Create Your Legend
          </Link>
        </div>
      </motion.div>
    </div>
  );
}
