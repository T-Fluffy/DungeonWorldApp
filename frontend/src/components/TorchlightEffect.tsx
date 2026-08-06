import React, { useEffect, useState } from 'react';

export function TorchlightEffect() {
  const [position, setPosition] = useState({ x: 0, y: 0 });

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      setPosition({ x: e.clientX, y: e.clientY });
    };
    window.addEventListener('mousemove', handleMouseMove);
    return () => window.removeEventListener('mousemove', handleMouseMove);
  }, []);

  return (
    <div 
    className="fixed inset-0 pointer-events-none z-0 transition-opacity duration-1000"
    style={{
      background: `radial-gradient(circle 600px at ${position.x}px ${position.y}px, rgba(255,107,53,0.15), transparent 80%)`
    }}
  />
  );
}