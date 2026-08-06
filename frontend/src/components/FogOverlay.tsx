export function FogOverlay() {
  return (
    <div className="fixed inset-0 pointer-events-none z-10 overflow-hidden opacity-20">
      {/* Change opacity-30 to opacity-20 or lower to let the Torchlight shine through */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/asfalt-dark.png')] animate-fog-drift scale-150 mix-blend-overlay" />
    </div>
  );
}