import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { 
  Shield, 
  Sword, 
  Zap, 
  BookOpen, 
  Award, 
  Play, 
  Clock, 
  Save, 
  Skull, 
  Scroll, 
  Crown,
  Ghost,
  Loader
} from 'lucide-react';
import { useGame } from '../Context/GameContext';
import { Item, ItemType } from '../types/game';
import { getUser, UserResponse, AssetResponse, apiError } from '../api/client';

function toItem(asset: AssetResponse): Item {
  const validTypes: ItemType[] = ['weapon', 'consumable', 'quest', 'artifact'];
  const type = validTypes.includes(asset.type as ItemType) ? (asset.type as ItemType) : 'quest';
  const lower = asset.type.toLowerCase();
  return {
    id: asset.id,
    name: asset.name,
    description: asset.description ?? '',
    type,
    rarity: lower.includes('artifact') || lower.includes('rare') || lower.includes('legendary') ? 'rare' : 'common'
  };
}

export function ProfilePage() {
  const { user, items, stats, setCurrentBook } = useGame();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<UserResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    if (!user?.isLoggedIn) return;

    getUser()
      .then((data) => { if (!cancelled) setProfile(data); })
      .catch((err) => { if (!cancelled) setError(apiError(err)); });

    return () => { cancelled = true; };
  }, [user?.isLoggedIn]);

  const statDisplay = [
    { label: 'Vitality', value: stats.vitality, color: 'bg-crimson', icon: Shield },
    { label: 'Might', value: stats.might, color: 'bg-orange-600', icon: Sword },
    { label: 'Essence', value: stats.essence, color: 'bg-purple-600', icon: Zap },
    { label: 'Corruption', value: stats.corruption, color: 'bg-ember', icon: Skull }
  ];

  const displayedItems: Item[] =
    profile && profile.assets.length > 0 ? profile.assets.map(toItem) : items;

  const achievements = profile?.achievements ?? [];
  const adventures = profile?.adventures ?? [];
  const subscription = profile?.subscription ?? null;

  const resumeAdventure = (bookTitle: string) => {
    setCurrentBook(bookTitle);
    navigate('/log');
  };

  return (
    <div className="min-h-screen pt-24 pb-32 px-4 flex flex-col items-center relative z-20">
      
      {/* 1. Character Silhouette Header */}
      <motion.div 
        initial={{ opacity: 0, scale: 0.9 }} 
        animate={{ opacity: 1, scale: 1 }} 
        transition={{ duration: 1 }} 
        className="relative w-48 h-48 md:w-64 md:h-64 mb-12"
      >
        <div className="absolute inset-0 rounded-full border-2 border-ember/30 animate-pulse" />
        <div className="absolute inset-4 rounded-full border border-white/10" />
        <div className="absolute inset-0 flex items-center justify-center bg-black/50 rounded-full overflow-hidden backdrop-blur-sm">
          <div className="w-32 h-32 bg-gray-900 rounded-full relative overflow-hidden">
            <div className="absolute bottom-0 left-1/2 -translate-x-1/2 w-24 h-24 bg-gray-800 rounded-t-full" />
            <div className="absolute bottom-6 left-1/2 -translate-x-1/2 w-16 h-16 bg-gray-700 rounded-full" />
          </div>
        </div>
        <div className="absolute -bottom-4 -right-4 w-16 h-16 bg-charcoal border-2 border-ember rounded-full flex flex-col items-center justify-center shadow-[0_0_20px_rgba(255,107,53,0.4)]">
          <span className="text-[10px] text-gray-400 uppercase leading-none">Level</span>
          <span className="text-2xl font-gothic text-white">{user?.level ?? 1}</span>
        </div>
      </motion.div>

      <h1 className="text-4xl font-gothic text-white mb-2 text-glow">{user?.name ?? 'Shadow Walker'}</h1>
      <p className="text-ember/80 font-medium tracking-widest uppercase text-xs mb-2">{user?.title ?? 'Cursed Bloodline'}</p>
      <div className="flex items-center gap-2 mb-12">
        <Ghost size={12} className="text-gray-600" />
        <span className="text-gray-600 text-xs font-mono">{user?.username}</span>
        {subscription && (
          <span className="flex items-center gap-1 text-[10px] uppercase tracking-widest text-amber-500 border border-amber-700/40 rounded-full px-3 py-1">
            <Crown size={10} /> {subscription.plan}
          </span>
        )}
      </div>

      {error && (
        <p className="text-xs text-red-400 border border-red-500/30 bg-red-500/10 rounded-lg px-4 py-3 mb-12 max-w-2xl w-full">
          {error}
        </p>
      )}

      {/* 2. Stats Grid */}
      <div className="w-full max-w-2xl grid grid-cols-1 md:grid-cols-2 gap-8 mb-12">
        {statDisplay.map((stat, index) => (
          <motion.div 
            key={stat.label} 
            initial={{ opacity: 0, x: -20 }} 
            animate={{ opacity: 1, x: 0 }} 
            transition={{ delay: index * 0.1 + 0.5 }} 
            className="bg-white/5 p-4 rounded-lg border border-white/5 hover:border-ember/30 transition-colors"
          >
            <div className="flex justify-between items-center mb-2">
              <div className="flex items-center gap-2 text-gray-300">
                <stat.icon size={18} className={stat.label === 'Corruption' ? 'text-ember' : ''} />
                <span className="font-gothic tracking-wide text-lg">{stat.label}</span>
              </div>
              <span className="text-white font-bold">{stat.value}%</span>
            </div>
            <div className="h-2 w-full bg-black/50 rounded-full overflow-hidden">
              <motion.div 
                className={`h-full ${stat.color} shadow-[0_0_10px_currentColor]`} 
                initial={{ width: 0 }} 
                animate={{ width: `${stat.value}%` }} 
                transition={{ duration: 1.5, delay: index * 0.1 + 0.8 }} 
              />
            </div>
          </motion.div>
        ))}
      </div>

      {/* 3. Traveler's Pack */}
      <div className="w-full max-w-2xl mb-12">
        <h3 className="text-xl font-gothic text-white mb-6 border-b border-white/10 pb-2 flex items-center gap-2">
          <Scroll className="text-ember" size={20} /> Traveler's Pack
        </h3>
        <div className="grid grid-cols-4 sm:grid-cols-8 gap-2">
          {displayedItems.map((item: Item) => (
            <div 
              key={item.id} 
              className={`aspect-square rounded border border-white/10 bg-white/5 flex flex-col items-center justify-center p-1 group relative transition-all hover:border-ember/50 cursor-help ${
                item.rarity === 'rare' ? 'border-ember/40 shadow-[0_0_10px_rgba(255,107,53,0.2)]' : ''
              }`}
            >
              {item.type === 'artifact' || item.type === 'quest' ? <Scroll size={16} className="text-ember" /> : <Sword size={16} className="text-gray-400" />}
              <div className="absolute bottom-full mb-2 left-1/2 -translate-x-1/2 w-32 p-2 bg-black border border-white/20 rounded opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity z-50">
                <p className="text-[10px] text-white font-bold">{item.name}</p>
                <p className="text-[8px] text-gray-500 italic leading-tight">{item.description}</p>
              </div>
            </div>
          ))}
          {/* Filler Slots to make exactly 16 */}
          {Array.from({ length: Math.max(0, 16 - displayedItems.length) }).map((_, i) => (
            <div key={`empty-${i}`} className="aspect-square rounded border border-white/5 bg-black/20 opacity-40 flex items-center justify-center">
               <div className="w-0.5 h-0.5 bg-white/10 rounded-full" />
            </div>
          ))}
        </div>
      </div>

      {/* 4. Saved Adventures */}
      <div className="w-full max-w-2xl mb-12">
        <h3 className="text-xl font-gothic text-white mb-6 border-b border-white/10 pb-2 flex items-center gap-2">
          <BookOpen className="text-ember" size={20} /> Grimoire of Past Lives
        </h3>
        {adventures.length === 0 && !profile && (
          <div className="flex items-center justify-center gap-3 py-10 text-gray-500">
            <Loader className="w-4 h-4 animate-spin" /> Summoning the grimoire...
          </div>
        )}
        {adventures.length === 0 && profile && (
          <p className="text-gray-500 text-sm text-center py-8 italic">
            No chronicles yet. Venture forth and begin your legend.
          </p>
        )}
        <div className="space-y-4">
          {adventures.map((adventure, index) => {
            const progress = adventure.isComplete
              ? 100
              : Math.min(100, Math.round((adventure.currentSection / 400) * 100));
            return (
              <motion.div 
                key={adventure.id} 
                initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} 
                transition={{ delay: index * 0.1 }} 
                className="group relative bg-white/5 border border-white/10 rounded-lg overflow-hidden hover:border-ember/50 transition-all duration-300"
              >
                <div className="flex flex-col sm:flex-row">
                  <div className="h-32 sm:h-auto sm:w-32 bg-gradient-to-br from-indigo-950 to-black relative flex items-center justify-center">
                    <Save className="text-white/20 w-8 h-8 group-hover:text-ember/50 transition-colors" />
                  </div>
                  <div className="p-4 flex-1 flex flex-col justify-between">
                    <div className="flex justify-between items-start mb-1">
                      <h4 className="font-gothic text-lg text-white group-hover:text-ember transition-colors">{adventure.bookTitle}</h4>
                      <span className="text-xs text-gray-500 font-mono border border-white/10 px-2 py-0.5 rounded flex items-center gap-1">
                        <Clock size={10} /> {new Date(adventure.updatedAt).toLocaleDateString()}
                      </span>
                    </div>
                    <p className="text-sm text-gray-400 mb-3 flex items-center gap-1">
                      <span className="text-ember/70">📍</span> Section {adventure.currentSection}
                      {adventure.isComplete && <span className="text-emerald-400 text-xs border border-emerald-500/30 rounded-full px-2 py-0.5 ml-2">Completed</span>}
                    </p>
                    <div className="flex items-center justify-between gap-4">
                      <div className="flex-1">
                        <div className="flex justify-between text-[10px] uppercase tracking-wider text-gray-500 mb-1">
                          <span>Progress</span><span>{progress}%</span>
                        </div>
                        <div className="h-1.5 bg-black/50 rounded-full overflow-hidden">
                          <div className="h-full bg-ember/70" style={{ width: `${progress}%` }} />
                        </div>
                      </div>
                      <button
                        onClick={() => resumeAdventure(adventure.bookTitle)}
                        className="flex items-center gap-2 px-3 py-1.5 bg-ember/10 hover:bg-ember/20 text-ember text-xs font-bold uppercase tracking-wider rounded border border-ember/20 hover:border-ember transition-all"
                      >
                        <Play size={12} fill="currentColor" /> Resume
                      </button>
                    </div>
                  </div>
                </div>
              </motion.div>
            );
          })}
        </div>
      </div>

      {/* 5. Medallions */}
      <div className="w-full max-w-2xl">
        <h3 className="text-xl font-gothic text-white mb-6 border-b border-white/10 pb-2 flex items-center gap-2">
          <Award className="text-amber-500" size={20} /> Medallions
        </h3>
        {achievements.length === 0 && (
          <p className="text-gray-500 text-sm text-center py-4 italic">
            No medallions earned yet. Great deeds await.
          </p>
        )}
        <div className="flex gap-4 overflow-x-auto pb-4">
          {achievements.map(achievement => (
            <motion.div 
              key={achievement.id} whileHover={{ scale: 1.1, rotate: 5 }} 
              className="flex-shrink-0 w-16 h-16 rounded-full bg-gradient-to-br from-gray-700 to-black border border-amber-700/50 flex items-center justify-center shadow-lg cursor-pointer group relative"
              title={`${achievement.title}${achievement.description ? ` - ${achievement.description}` : ''}`}
            >
              <Award className="text-amber-600/70 group-hover:text-amber-500 transition-colors" />
              <div className="absolute inset-0 rounded-full border border-white/10 opacity-50" />
              <div className="absolute bottom-full mb-2 left-1/2 -translate-x-1/2 w-40 p-2 bg-black border border-white/20 rounded opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity z-50">
                <p className="text-[10px] text-white font-bold">{achievement.title}</p>
                <p className="text-[8px] text-gray-500 italic leading-tight">{achievement.code}</p>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  );
}
