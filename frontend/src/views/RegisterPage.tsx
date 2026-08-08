import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import { useGame } from '../Context/useGame';
import { apiError, uploadAvatar } from '../api/client';
import { UserPlus, Shield, Zap, Target, Sparkles, Heart, Sword, Flame, KeyRound, Mail, Camera, ArrowLeft, LucideIcon } from 'lucide-react';
import { CharacterClass } from '../types/game';

export function RegisterPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [selectedClass, setSelectedClass] = useState<CharacterClass>('Dreadknight');
  const { register, updateStat, setAvatar } = useGame();
  const navigate = useNavigate();

  const classes = [
    { 
      id: 'Dreadknight' as CharacterClass, 
      icon: Shield, 
      desc: 'High Vitality. A relentless wall of shadow steel.',
      color: 'text-blue-400',
      stats: { vit: 100, mgt: 50, ess: 20 }
    },
    { 
      id: 'Abyssal Mage' as CharacterClass, 
      icon: Zap, 
      desc: 'High Essence. Bending the void to their will.',
      color: 'text-purple-400',
      stats: { vit: 50, mgt: 30, ess: 100 }
    },
    { 
      id: 'Shadow Rogue' as CharacterClass, 
      icon: Target, 
      desc: 'High Might. Lethal strikes from the darkness.',
      color: 'text-ember',
      stats: { vit: 60, mgt: 100, ess: 40 }
    }
  ];

  const currentClassStats = classes.find(c => c.id === selectedClass)?.stats;

  const handleAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type)) {
      setError('Avatar must be a JPG, PNG, WEBP or GIF image.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setError('Avatar must be 5MB or smaller.');
      return;
    }
    setError(null);
    setAvatarFile(file);
    setAvatarPreview(URL.createObjectURL(file));
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !email.trim() || !password) return;

    setSubmitting(true);
    setError(null);
    try {
      await register({
        username: name.trim(),
        email: email.trim(),
        password,
        className: selectedClass
      });

      // Upload the chosen avatar once the new account is active.
      if (avatarFile) {
        const { avatarPath } = await uploadAvatar(avatarFile);
        setAvatar(avatarPath);
      }

      // Sync stats to context
      updateStat('vitality', currentClassStats!.vit);
      updateStat('might', currentClassStats!.mgt);
      updateStat('essence', currentClassStats!.ess);

      navigate('/files');
    } catch (err) {
      setError(apiError(err));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-6 relative z-20">
      <motion.div 
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        className="max-w-4xl w-full grid grid-cols-1 md:grid-cols-2 gap-8 bg-black/80 backdrop-blur-2xl border border-ember/20 p-8 rounded-2xl shadow-2xl"
      >
        {/* Left Side: Form */}
        <div className="space-y-8">
          <div className="flex items-center justify-between">
            <Link to="/login" className="flex items-center gap-2 text-gray-400 hover:text-ember transition-colors text-xs uppercase tracking-widest">
              <ArrowLeft className="w-4 h-4" />
              Return
            </Link>
            <span className="text-gray-600 text-[10px] uppercase tracking-widest">Step 1 of 1</span>
          </div>

          <div className="text-left">
            <h1 className="text-4xl font-gothic text-white uppercase tracking-tighter">Forge Your Soul</h1>
            <p className="text-gray-500 text-[10px] uppercase tracking-[0.3em] mt-2">The ink is dry, the pact awaits</p>
          </div>

          <form onSubmit={handleRegister} className="space-y-6">
            <div className="space-y-2">
              <label className="text-[10px] uppercase tracking-widest text-ember/70">Identity</label>
              <div className="relative">
                <input 
                  type="text" 
                  required 
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full bg-white/5 border border-white/10 p-4 rounded-lg text-white focus:border-ember/50 outline-none transition-all"
                  placeholder="Name your legend..." 
                />
                <UserPlus className="absolute right-4 top-4 w-5 h-5 text-gray-600" />
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-[10px] uppercase tracking-widest text-ember/70">Email</label>
              <div className="relative">
                <input 
                  type="email" 
                  required 
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full bg-white/5 border border-white/10 p-4 rounded-lg text-white focus:border-ember/50 outline-none transition-all"
                  placeholder="raven@shadowvale.com" 
                />
                <Mail className="absolute right-4 top-4 w-5 h-5 text-gray-600" />
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-[10px] uppercase tracking-widest text-ember/70">Word of the Pact</label>
              <div className="relative">
                <input 
                  type="password" 
                  required 
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full bg-white/5 border border-white/10 p-4 rounded-lg text-white focus:border-ember/50 outline-none transition-all"
                  placeholder="A secret only the void knows..." 
                />
                <KeyRound className="absolute right-4 top-4 w-5 h-5 text-gray-600" />
              </div>
            </div>

            <div className="space-y-3">
              <label className="text-[10px] uppercase tracking-widest text-ember/70">Countenance</label>
              <div className="flex items-center gap-4">
                <div className="w-16 h-16 rounded-full overflow-hidden border border-ember/30 bg-white/5 flex items-center justify-center shrink-0">
                  {avatarPreview ? (
                    <img src={avatarPreview} alt="Avatar preview" className="w-full h-full object-cover" />
                  ) : (
                    <Camera className="w-6 h-6 text-gray-600" />
                  )}
                </div>
                <label className="flex-1 cursor-pointer">
                  <span className="block w-full text-center bg-white/5 border border-white/10 p-3 rounded-lg text-sm text-gray-400 hover:border-ember/50 hover:text-white transition-all">
                    {avatarFile ? avatarFile.name : 'Choose an image (optional)'}
                  </span>
                  <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" onChange={handleAvatarChange} className="hidden" />
                </label>
              </div>
            </div>

            <div className="space-y-3">
              <label className="text-[10px] uppercase tracking-widest text-ember/70">Select Path</label>
              {classes.map((c) => (
                <button
                  key={c.id}
                  type="button"
                  onClick={() => setSelectedClass(c.id)}
                  className={`w-full flex items-center gap-4 p-4 rounded-lg border transition-all ${
                    selectedClass === c.id ? 'border-ember bg-ember/10' : 'border-white/5 bg-white/5'
                  }`}
                >
                  <c.icon className={`w-5 h-5 ${selectedClass === c.id ? 'text-ember' : 'text-gray-500'}`} />
                  <span className={`text-sm ${selectedClass === c.id ? 'text-white' : 'text-gray-400'}`}>{c.id}</span>
                </button>
              ))}
            </div>

            {error && (
              <p className="text-xs text-red-400 border border-red-500/30 bg-red-500/10 rounded-lg px-4 py-3">
                {error}
              </p>
            )}

            <button type="submit" disabled={submitting} className="w-full py-4 bg-ember text-black font-gothic text-xl hover:brightness-125 transition-all disabled:opacity-50">
              {submitting ? 'Sealing the Pact...' : 'Sign the Pact'}
            </button>
          </form>
        </div>

        {/* Right Side: Stat Preview */}
        <div className="bg-white/5 rounded-xl p-8 border border-white/5 flex flex-col justify-center relative overflow-hidden">
          <div className="absolute top-0 right-0 p-4 opacity-10">
            <Sparkles className="w-24 h-24 text-ember" />
          </div>

          <h3 className="text-ember font-gothic text-2xl mb-6 text-center tracking-widest uppercase">Initial Potential</h3>
          
          <div className="space-y-6">
            <StatRow icon={Heart} label="Vitality" value={currentClassStats?.vit} color="text-red-400" />
            <StatRow icon={Sword} label="Might" value={currentClassStats?.mgt} color="text-orange-400" />
            <StatRow icon={Flame} label="Essence" value={currentClassStats?.ess} color="text-purple-400" />
          </div>

          <div className="mt-8 pt-8 border-t border-white/5">
            <p className="text-gray-500 text-[10px] leading-relaxed italic text-center uppercase tracking-tighter">
              {classes.find(c => c.id === selectedClass)?.desc}
            </p>
          </div>

          <div className="mt-6 text-center">
            <Link to="/login" className="text-gray-500 hover:text-white transition-colors text-xs uppercase tracking-widest">
              Already sworn? Return
            </Link>
          </div>
        </div>
      </motion.div>
    </div>
  );
}

// Helper component for the Stat Rows
function StatRow({ icon: Icon, label, value, color }: { icon: LucideIcon; label: string; value?: number; color: string }) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-3">
        <Icon className={`w-4 h-4 ${color}`} />
        <span className="text-[10px] uppercase tracking-widest text-gray-400">{label}</span>
      </div>
      <AnimatePresence mode="wait">
        <motion.span
          key={value}
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: 10 }}
          className="text-xl font-mono text-white"
        >
          {value}
        </motion.span>
      </AnimatePresence>
    </div>
  );
}
