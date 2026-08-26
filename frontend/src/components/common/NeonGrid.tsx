import './NeonGrid.css'

export function NeonGrid() {
  return (
    <div className="neon-grid">
      <div className="neon-grid__lines" />
      <div className="neon-grid__scan" />
      <div className="neon-grid__pulse" />
      <div className="neon-grid__glow neon-grid__glow--tl" />
      <div className="neon-grid__glow neon-grid__glow--br" />
    </div>
  )
}
