import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { FileText, Book, User, LogOut } from 'lucide-react';
import { useGame } from '../Context/useGame';

export function Navigation() {
  const location = useLocation();
  const { user, logout } = useGame();

  // Hide nav if no user is signed in
  if (!user?.isLoggedIn) return null;
  
  const navItems = [
    { path: '/profile', icon: User, label: 'Soul' },
    { path: '/files', icon: FileText, label: 'Summon' },
    { path: '/log', icon: Book, label: 'Chronicle' },
  ];

  return (
    <nav className="fixed bottom-8 left-1/2 -translate-x-1/2 z-50 px-6 py-3 bg-black/60 backdrop-blur-md border border-white/10 rounded-full shadow-[0_0_20px_rgba(0,0,0,0.5)]">
      <ul className="flex items-center gap-8">
        {navItems.map((item) => {
          const isActive = location.pathname === item.path;
          return (
            <li key={item.path}>
              <Link
                to={item.path}
                className={`flex flex-col items-center gap-1 transition-colors duration-300 ${
                  isActive ? 'text-ember' : 'text-gray-500 hover:text-white'
                }`}
              >
                <item.icon size={20} className={isActive ? 'animate-pulse' : ''} />
                <span className="text-[10px] uppercase tracking-widest font-mono">
                  {item.label}
                </span>
              </Link>
            </li>
          );
        })}
        <li>
          <button
            onClick={logout}
            className="flex flex-col items-center gap-1 text-gray-500 hover:text-red-400 transition-colors duration-300"
            title="Sever the pact"
          >
            <LogOut size={20} />
            <span className="text-[10px] uppercase tracking-widest font-mono">Sever</span>
          </button>
        </li>
      </ul>
    </nav>
  );
}
