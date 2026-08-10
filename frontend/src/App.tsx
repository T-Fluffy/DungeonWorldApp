import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, useLocation, Navigate } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';

// Context & Components
import { GameProvider } from './Context/GameContext'; 
import { useGame } from './Context/useGame';
import { RitualLoading } from './components/RitualLoading';
import { Navigation } from './components/Navigation';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Vignette } from './components/Vignette';
import { TorchlightEffect } from './components/TorchlightEffect';
import { FogOverlay } from './components/FogOverlay';

// Views
import { HomePage } from './views/HomePage';
import { LoginPage } from './views/LoginPage';
import { RegisterPage } from './views/RegisterPage';
import { StoryLog } from './views/StoryLog';
import { ProfilePage } from './views/ProfilePage';

function AnimatedRoutes() {
  const location = useLocation();
  const { user } = useGame();
  const [globalLoading, setGlobalLoading] = useState(false);

  // We listen for changes in the login state to trigger the ritual
  useEffect(() => {
    if (!user?.isLoggedIn) return;
    const showTimer = setTimeout(() => setGlobalLoading(true), 0);
    const hideTimer = setTimeout(() => setGlobalLoading(false), 2000);
    return () => {
      clearTimeout(showTimer);
      clearTimeout(hideTimer);
    };
  }, [user?.isLoggedIn]);

  if (globalLoading) return <RitualLoading />;

  return (
    <div className="relative min-h-screen w-full bg-[#1A1A1D] text-white overflow-hidden">
      <Vignette />
      <TorchlightEffect />
      <FogOverlay />
      
      {user?.isLoggedIn && location.pathname !== '/log' && <Navigation />}

      <AnimatePresence mode="wait">
        <motion.div
          key={location.pathname}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.5 }}
          className="w-full relative z-10"
        >
          <Routes location={location}>
            <Route path="/" element={<HomePage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            <Route path="/log" element={<ProtectedRoute><StoryLog /></ProtectedRoute>} />
            <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
            
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </motion.div>
      </AnimatePresence>
    </div>
  );
}

export function App() {
  return (
    <Router>
      <GameProvider> 
        <AnimatedRoutes />
      </GameProvider>
    </Router>
  );
}