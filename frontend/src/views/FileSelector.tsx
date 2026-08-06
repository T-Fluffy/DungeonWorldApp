import React, { useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Upload, CheckCircle, AlertCircle, RefreshCw, ArrowRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useGame } from '../Context/GameContext';
import { RitualCircle } from '../components/RitualCircle';
import { uploadPdf, ingestBook } from '../api/client';

type UploadState = 'idle' | 'processing' | 'success' | 'error';

export function FileSelector() {
  const { addItem, setCurrentBook } = useGame();
  const [uploadState, setUploadState] = useState<UploadState>('idle');
  const [progress, setProgress] = useState(0);
  const [statusMessage, setStatusMessage] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();

  const showError = (msg: string) => {
    setUploadState('error');
    setStatusMessage(msg);
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      if (selectedFile.type === 'application/pdf') {
        startRitual(selectedFile);
      } else {
        showError('Only ancient grimoires (.pdf) are accepted.');
      }
    }
  };

  const startRitual = async (selectedFile: File) => {
    setUploadState('processing');
    setProgress(0);

    const steps = [
      { p: 25, m: 'Breaking the seal...' },
      { p: 45, m: 'Channelling runes...' },
      { p: 65, m: 'Consulting the parser...' },
      { p: 85, m: 'Binding shadows...' },
    ];

    let currentStep = 0;
    const interval = setInterval(() => {
      if (currentStep < steps.length) {
        setProgress(steps[currentStep].p);
        setStatusMessage(steps[currentStep].m);
        currentStep++;
      }
    }, 900);

    try {
      // 1. Deliver the grimoire to the engine
      const { fileName } = await uploadPdf(selectedFile);

      // 2. Let the engine parse it into game data
      setStatusMessage('Ingesting the grimoire...');
      const result = await ingestBook(fileName);

      clearInterval(interval);
      setProgress(100);
      setStatusMessage('Ritual Complete.');
      setUploadState('success');

      // Remember which book was bound so the Chronicle can load it
      setCurrentBook(result.bookTitle);

      // Add to Game Inventory
      addItem({
        id: `grimoire-${Date.now()}`,
        name: result.bookTitle || selectedFile.name,
        description: `A deciphered text from the void. ${result.sections} sections revealed.`,
        type: 'artifact',
        rarity: 'rare'
      });
    } catch (err: unknown) {
      clearInterval(interval);
      const error = err as { response?: { data?: { error?: string; Error?: string } } };
      showError(error?.response?.data?.error || error?.response?.data?.Error || 'The ritual failed. The shadows rejected the grimoire.');
    }
  };

  const resetRitual = () => {
    setUploadState('idle');
    if (fileInputRef.current) fileInputRef.current.value = '';
    setProgress(0);
    setStatusMessage('');
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-center relative p-6">
      {/* Header Info */}
      <motion.div 
        initial={{ opacity: 0, y: -20 }} 
        animate={{ opacity: 1, y: 0 }} 
        className="text-center mb-12 z-20"
      >
        <h2 className="text-4xl font-gothic text-white text-glow mb-2">
          Grimoire Summoning
        </h2>
        <p className="text-gray-500 font-sans text-sm tracking-widest uppercase">
          Offer a PDF to the shadows
        </p>
      </motion.div>

      {/* Main Interaction Area */}
      <div className="relative group flex items-center justify-center">
        <input 
          type="file" 
          ref={fileInputRef} 
          onChange={handleFileSelect} 
          accept=".pdf" 
          className="hidden" 
        />

        {/* The Animated Ritual Circle */}
        <div 
          className="relative cursor-pointer transition-transform duration-500 hover:scale-105"
          onClick={() => uploadState === 'idle' && fileInputRef.current?.click()}
        >
          <RitualCircle isProcessing={uploadState === 'processing'} />

          {/* Center Overlay Content */}
          <div className="absolute inset-0 flex flex-col items-center justify-center z-30 pointer-events-none">
            <AnimatePresence mode="wait">
              {uploadState === 'idle' && (
                <motion.div 
                  key="idle"
                  initial={{ opacity: 0 }} 
                  animate={{ opacity: 1 }} 
                  exit={{ opacity: 0 }}
                  className="flex flex-col items-center gap-2"
                >
                  <Upload className="w-8 h-8 text-ember/60 group-hover:text-ember transition-colors" />
                  <span className="text-[10px] text-ember/40 uppercase tracking-[0.3em] font-gothic">Begin</span>
                </motion.div>
              )}

              {uploadState === 'processing' && (
                <motion.div 
                  key="proc"
                  initial={{ opacity: 0 }} 
                  animate={{ opacity: 1 }}
                  className="text-center px-4"
                >
                  <p className="text-ember font-gothic text-sm animate-pulse mb-3">
                    {statusMessage}
                  </p>
                  <div className="w-32 h-0.5 bg-white/5 rounded-full overflow-hidden mx-auto">
                    <motion.div 
                      className="h-full bg-ember shadow-[0_0_10px_#ff6b35]" 
                      initial={{ width: 0 }}
                      animate={{ width: `${progress}%` }}
                    />
                  </div>
                </motion.div>
              )}

              {uploadState === 'success' && (
                <motion.div 
                  key="success"
                  initial={{ opacity: 0, scale: 0.5 }} 
                  animate={{ opacity: 1, scale: 1 }}
                  className="flex flex-col items-center gap-2"
                >
                  <CheckCircle className="w-10 h-10 text-ember" />
                  <span className="text-xs text-white font-gothic tracking-widest uppercase">Deciphered</span>
                </motion.div>
              )}

              {uploadState === 'error' && (
                <motion.div 
                  key="error"
                  initial={{ opacity: 0 }} 
                  animate={{ opacity: 1 }}
                  className="flex flex-col items-center gap-2 text-center"
                >
                  <AlertCircle className="w-10 h-10 text-crimson" />
                  <p className="text-[10px] text-crimson max-w-[120px] uppercase font-sans">{statusMessage}</p>
                  <button 
                    onClick={(e) => { e.stopPropagation(); resetRitual(); }}
                    className="pointer-events-auto mt-2 text-white/40 hover:text-white transition-colors"
                  >
                    <RefreshCw className="w-4 h-4" />
                  </button>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>

      {/* Action Buttons for Success State */}
      <AnimatePresence>
        {uploadState === 'success' && (
          <motion.div 
            initial={{ opacity: 0, y: 20 }} 
            animate={{ opacity: 1, y: 0 }} 
            className="mt-12 flex flex-col items-center gap-4 z-20"
          >
            <button 
              onClick={() => navigate('/log')} 
              className="group relative px-10 py-4 bg-transparent border border-ember/30 hover:border-ember text-ember font-gothic text-xl tracking-widest transition-all duration-300"
            >
              <span className="flex items-center gap-3">
                Read the Chronicles <ArrowRight className="w-5 h-5 group-hover:translate-x-2 transition-transform" />
              </span>
              <div className="absolute top-0 left-0 w-2 h-2 border-t border-l border-ember opacity-50" />
              <div className="absolute bottom-0 right-0 w-2 h-2 border-b border-r border-ember opacity-50" />
            </button>
            
            <button 
              onClick={resetRitual}
              className="text-gray-500 hover:text-gray-300 text-[10px] uppercase tracking-[0.2em] font-sans transition-colors"
            >
              Summon Another
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}