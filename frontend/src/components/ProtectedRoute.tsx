import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useGame } from '../Context/useGame';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { user } = useGame();
  const location = useLocation();

  if (!user || !user.isLoggedIn) {
    // Save the location they were trying to access
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}