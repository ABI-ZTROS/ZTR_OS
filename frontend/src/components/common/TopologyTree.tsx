import type { CpuTopologyNode } from '@/types'
import './TopologyTree.css'

interface TopologyTreeProps {
  nodes: CpuTopologyNode[]
  selectedId?: number
  onSelect?: (node: CpuTopologyNode) => void
  title?: string
}

export function TopologyTree({ nodes, selectedId, onSelect, title }: TopologyTreeProps) {
  const renderNode = (node: CpuTopologyNode, depth: number) => {
    const hasChildren = node.children && node.children.length > 0
    const isSelected = node.id === selectedId
    const isPackage = node.type === 'package'
    const isCore = node.type === 'core'

    return (
      <g
        key={node.id}
        className={`topology-node topology-node--${node.type} ${isSelected ? 'topology-node--selected' : ''}`}
        onClick={() => onSelect?.(node)}
        style={{ cursor: onSelect ? 'pointer' : 'default' }}
      >
        <foreignObject
          x={depth * 140}
          y={node.id * 48}
          width={130}
          height={44}
        >
          <div
            className={`topology-card topology-card--${node.type} ${isSelected ? 'topology-card--selected' : ''}`}
          >
            <span className="topology-name">{node.name}</span>
            {node.usage !== undefined && (
              <div className="topology-bar">
                <div
                  className="topology-bar-fill"
                  style={{
                    width: `${node.usage}%`,
                    background:
                      node.usage > 80
                        ? 'var(--danger)'
                        : node.usage > 50
                          ? 'var(--warning)'
                          : 'var(--primary)',
                  }}
                />
              </div>
            )}
            {node.temperature !== undefined && (
              <span className="topology-temp">{node.temperature.toFixed(0)}°C</span>
            )}
          </div>
        </foreignObject>
        {hasChildren &&
          node.children!.map((child) => (
            <g key={`${node.id}-${child.id}`}>
              <line
                x1={depth * 140 + 130}
                y1={node.id * 48 + 22}
                x2={(depth + 1) * 140}
                y2={child.id * 48 + 22}
                className="topology-edge"
              />
              {renderNode(child, depth + 1)}
            </g>
          ))}
        {isPackage && !hasChildren && (
          <circle
            cx={depth * 140 + 130}
            cy={node.id * 48 + 22}
            r={3}
            className="topology-connector"
          />
        )}
        {isCore && hasChildren && (
          <circle
            cx={depth * 140 + 130}
            cy={node.id * 48 + 22}
            r={3}
            className="topology-connector"
          />
        )}
      </g>
    )
  }

  const totalHeight = Math.max(...nodes.flatMap((n) => {
    const count = 1 + (n.children?.length ?? 0)
    return (n.id + count) * 48 + 20
  }), 200)

  const totalWidth = (Math.max(
    ...nodes.flatMap((n) => {
      if (!n.children || n.children.length === 0) return 1
      return 2 + Math.max(...n.children.map(() => 0))
    })
  ) + 1) * 140

  return (
    <div className="topology-tree-container">
      {title && <div className="topology-tree-title">{title}</div>}
      <div className="topology-tree-scroll">
        <svg
          viewBox={`0 0 ${totalWidth} ${totalHeight}`}
          className="topology-svg"
          preserveAspectRatio="xMinYMin meet"
        >
          {nodes.map((node) => renderNode(node, 0))}
        </svg>
      </div>
    </div>
  )
}
