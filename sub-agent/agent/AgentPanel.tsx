'use client'

// ============================================================
// ui/AgentPanel.tsx — Complete AI Agent UI with SSE Streaming
// ============================================================

import { useState, useRef, useEffect, useCallback } from 'react'
import type { AgentId, StreamEvent } from '../types/agents'
import { ALL_AGENTS } from '../agents/definitions'

// ─────────────────────────────────────────────────────────────
// TYPES
// ─────────────────────────────────────────────────────────────

interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: Date
  agentEvents?: AgentEventDisplay[]
  totalTokens?: number
  durationMs?: number
}

interface AgentEventDisplay {
  type: string
  agentId?: AgentId
  text?: string
  toolName?: string
  input?: Record<string, unknown>
  result?: string
  success?: boolean
  timestamp: Date
}

interface AgentCardProps {
  agentId: AgentId
  name: string
  icon: string
  color: string
  status: 'idle' | 'active' | 'done' | 'error'
  task?: string
  tokens?: number
}

// ─────────────────────────────────────────────────────────────
// SUB-COMPONENTS
// ─────────────────────────────────────────────────────────────

function AgentCard({ agentId, name, icon, color, status, task, tokens }: AgentCardProps) {
  const statusConfig = {
    idle: { dot: 'bg-gray-300 dark:bg-gray-700', ring: '', text: 'text-gray-400', label: 'En attente' },
    active: { dot: 'bg-amber-400 animate-pulse', ring: 'ring-2 ring-amber-400/30', text: 'text-amber-600 dark:text-amber-400', label: 'En cours…' },
    done: { dot: 'bg-green-500', ring: 'ring-2 ring-green-400/20', text: 'text-green-600 dark:text-green-400', label: 'Terminé' },
    error: { dot: 'bg-red-500', ring: 'ring-2 ring-red-400/20', text: 'text-red-600 dark:text-red-400', label: 'Erreur' },
  }
  const cfg = statusConfig[status]

  return (
    <div className={`relative flex flex-col gap-1.5 p-3 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl transition-all ${cfg.ring}`}>
      <div className="flex items-center gap-2">
        <div className="w-8 h-8 rounded-lg flex items-center justify-center text-lg shrink-0"
          style={{ background: `${color}20` }}>
          {icon}
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-xs font-semibold text-gray-800 dark:text-gray-200 truncate">{name}</div>
          {tokens && <div className="text-xs text-gray-400">{tokens.toLocaleString()} tokens</div>}
        </div>
        <div className="flex items-center gap-1.5">
          <div className={`w-2 h-2 rounded-full ${cfg.dot}`} />
        </div>
      </div>
      {task && status === 'active' && (
        <div className="text-xs text-gray-500 dark:text-gray-400 leading-tight truncate bg-gray-50 dark:bg-gray-800 rounded-lg px-2 py-1">
          {task}
        </div>
      )}
      {status !== 'idle' && (
        <div className={`text-xs font-medium ${cfg.text}`}>{cfg.label}</div>
      )}
    </div>
  )
}

function ThinkingBlock({ agentName, icon, color, text }: { agentName: string; icon: string; color: string; text: string }) {
  return (
    <div className="flex gap-2 items-start">
      <div className="w-6 h-6 rounded-lg flex items-center justify-center text-sm shrink-0 mt-0.5"
        style={{ background: `${color}20` }}>
        {icon}
      </div>
      <div className="flex-1 bg-gray-50 dark:bg-gray-800/60 rounded-xl px-3 py-2 text-xs text-gray-600 dark:text-gray-400 italic leading-relaxed border border-gray-100 dark:border-gray-800">
        <span className="font-medium not-italic" style={{ color }}>{agentName} </span>
        {text}
      </div>
    </div>
  )
}

function ToolCallBlock({
  agentName, icon, color, toolName, input, result, success,
}: {
  agentName: string; icon: string; color: string
  toolName: string; input?: Record<string, unknown>
  result?: string; success?: boolean
}) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div className="flex gap-2 items-start">
      <div className="w-6 h-6 rounded-lg flex items-center justify-center text-sm shrink-0 mt-0.5"
        style={{ background: `${color}20` }}>
        {icon}
      </div>
      <div className="flex-1">
        <button
          onClick={() => setExpanded(!expanded)}
          className="w-full text-left bg-gray-50 dark:bg-gray-800/60 rounded-xl px-3 py-2 border border-gray-100 dark:border-gray-800 hover:border-gray-200 dark:hover:border-gray-700 transition"
        >
          <div className="flex items-center gap-2">
            <span className="text-xs font-medium text-gray-500 dark:text-gray-400">
              🔧 <code className="bg-gray-100 dark:bg-gray-700 px-1 py-0.5 rounded text-xs font-mono">{toolName}</code>
            </span>
            {result && (
              <span className={`text-xs font-medium ml-auto ${success ? 'text-green-600' : 'text-red-500'}`}>
                {success ? '✓' : '✗'}
              </span>
            )}
            <span className="text-xs text-gray-400 ml-auto">{expanded ? '▲' : '▼'}</span>
          </div>
        </button>
        {expanded && (
          <div className="mt-1.5 space-y-1.5">
            {input && (
              <div className="bg-blue-50 dark:bg-blue-950/30 border border-blue-100 dark:border-blue-900/50 rounded-xl px-3 py-2">
                <div className="text-xs font-medium text-blue-600 dark:text-blue-400 mb-1">Input</div>
                <pre className="text-xs text-gray-700 dark:text-gray-300 overflow-x-auto whitespace-pre-wrap">
                  {JSON.stringify(input, null, 2)}
                </pre>
              </div>
            )}
            {result && (
              <div className={`rounded-xl px-3 py-2 border ${success ? 'bg-green-50 dark:bg-green-950/30 border-green-100 dark:border-green-900/50' : 'bg-red-50 dark:bg-red-950/30 border-red-100 dark:border-red-900/50'}`}>
                <div className={`text-xs font-medium mb-1 ${success ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                  {success ? 'Résultat' : 'Erreur'}
                </div>
                <pre className="text-xs text-gray-700 dark:text-gray-300 overflow-x-auto whitespace-pre-wrap max-h-40">
                  {result.length > 500 ? result.slice(0, 500) + '…' : result}
                </pre>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

function EventsTimeline({ events }: { events: AgentEventDisplay[] }) {
  if (!events?.length) return null
  return (
    <div className="mt-3 space-y-2 pl-2 border-l-2 border-gray-100 dark:border-gray-800">
      {events.map((evt, i) => {
        const agentDef = ALL_AGENTS.find(a => a.id === evt.agentId)
        const name = agentDef?.name ?? (evt.agentId ?? 'Agent')
        const icon = agentDef?.icon ?? '🤖'
        const color = agentDef?.color ?? '#6b7280'

        if (evt.type === 'thinking' && evt.text) {
          return <ThinkingBlock key={i} agentName={name} icon={icon} color={color} text={evt.text} />
        }
        if ((evt.type === 'tool_call' || evt.type === 'tool_result') && evt.toolName) {
          return (
            <ToolCallBlock key={i} agentName={name} icon={icon} color={color}
              toolName={evt.toolName} input={evt.input} result={evt.result} success={evt.success} />
          )
        }
        return null
      })}
    </div>
  )
}

// ─────────────────────────────────────────────────────────────
// QUICK PROMPTS
// ─────────────────────────────────────────────────────────────

const QUICK_PROMPTS = [
  { label: '🔍 Analyser panne P0301', prompt: "Analyse la panne P0301 sur le véhicule AB-123-CD (Peugeot 308, 2021). Quelles sont les causes et actions à effectuer ?" },
  { label: '📅 Révisions en retard', prompt: "Liste tous les véhicules avec des révisions en retard ou à venir dans les 7 prochains jours et envoie des rappels aux clients." },
  { label: '📦 Alertes stock', prompt: "Vérifie les niveaux de stock et identifie les pièces en rupture critique. Crée des bons de commande si nécessaire." },
  { label: '📊 Rapport journalier', prompt: "Génère le rapport de performance du jour : CA, révisions effectuées, nouveaux diagnostics, et recommandations." },
  { label: '👥 Clients à risque', prompt: "Identifie les clients qui n'ont pas visité le garage depuis plus de 6 mois et propose une campagne de réactivation." },
  { label: '🚨 Urgence sécurité', prompt: "Le véhicule MN-012-OP a un code C0034 (circuit frein). Analyse l'urgence et prends les actions nécessaires." },
]

// ─────────────────────────────────────────────────────────────
// MAIN AGENT PANEL
// ─────────────────────────────────────────────────────────────

export default function AgentPanel() {
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      content: "Bonjour ! Je suis l'Orchestrateur MecaPro. Je coordonne 9 agents spécialisés pour vous aider à gérer votre garage intelligemment. Que puis-je faire pour vous ?",
      timestamp: new Date(),
    },
  ])
  const [input, setInput] = useState('')
  const [isStreaming, setIsStreaming] = useState(false)
  const [agentStates, setAgentStates] = useState<Record<string, AgentCardProps>>({})
  const [showAgents, setShowAgents] = useState(true)
  const [totalTokensUsed, setTotalTokensUsed] = useState(0)

  const messagesEndRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const sessionId = useRef(`session_${Date.now()}`)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const getAgentInfo = (agentId: AgentId) => ALL_AGENTS.find(a => a.id === agentId)

  const sendMessage = useCallback(async (userMessage: string) => {
    if (!userMessage.trim() || isStreaming) return

    const msgId = `msg_${Date.now()}`

    setMessages(prev => [
      ...prev,
      {
        id: `user_${Date.now()}`,
        role: 'user',
        content: userMessage,
        timestamp: new Date(),
      },
      {
        id: msgId,
        role: 'assistant',
        content: '',
        timestamp: new Date(),
        agentEvents: [],
      },
    ])

    setInput('')
    setIsStreaming(true)
    setAgentStates({})

    try {
      const response = await fetch('/api/ai-agent', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: userMessage,
          sessionId: sessionId.current,
        }),
      })

      if (!response.body) throw new Error('No response body')

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      let finalAnswerText = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() ?? ''

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue
          const rawData = line.slice(6).trim()
          if (!rawData) continue

          try {
            const streamEvent = JSON.parse(rawData) as StreamEvent

            switch (streamEvent.event) {
              case 'agent_start': {
                const { agentId, task } = streamEvent.data as { agentId: AgentId; task: string }
                const def = getAgentInfo(agentId)
                if (def) {
                  setAgentStates(prev => ({
                    ...prev,
                    [agentId]: { agentId, name: def.name, icon: def.icon, color: def.color, status: 'active', task },
                  }))
                }
                break
              }

              case 'thinking': {
                const { agentId, text } = streamEvent.data as { agentId: AgentId; text: string }
                setMessages(prev => prev.map(m => m.id === msgId ? {
                  ...m,
                  agentEvents: [...(m.agentEvents ?? []), {
                    type: 'thinking', agentId, text, timestamp: new Date(),
                  }],
                } : m))
                break
              }

              case 'tool_call': {
                const { agentId, toolName, input: toolInput } = streamEvent.data as {
                  agentId: AgentId; toolName: string; input: Record<string, unknown>
                }
                setMessages(prev => prev.map(m => m.id === msgId ? {
                  ...m,
                  agentEvents: [...(m.agentEvents ?? []), {
                    type: 'tool_call', agentId, toolName, input: toolInput, timestamp: new Date(),
                  }],
                } : m))
                break
              }

              case 'tool_result': {
                const { agentId, toolName, result, success } = streamEvent.data as {
                  agentId: AgentId; toolName: string; result: string; success: boolean
                }
                setMessages(prev => prev.map(m => m.id === msgId ? {
                  ...m,
                  agentEvents: (m.agentEvents ?? []).map((e, idx) => {
                    if (idx === (m.agentEvents?.length ?? 0) - 1 && e.type === 'tool_call' && e.toolName === toolName) {
                      return { ...e, type: 'tool_result', result, success }
                    }
                    return e
                  }),
                } : m))
                break
              }

              case 'agent_done': {
                const { agentId, tokens } = streamEvent.data as { agentId: AgentId; output: string; tokens: number }
                const def = getAgentInfo(agentId)
                if (def) {
                  setAgentStates(prev => ({
                    ...prev,
                    [agentId]: { ...prev[agentId], status: 'done', tokens },
                  }))
                }
                break
              }

              case 'final_answer': {
                const { text } = streamEvent.data as { text: string }
                finalAnswerText = text
                setMessages(prev => prev.map(m => m.id === msgId ? { ...m, content: text } : m))
                break
              }

              case 'done': {
                const { totalTokens, durationMs } = streamEvent.data as { totalTokens: number; durationMs: number }
                setTotalTokensUsed(prev => prev + totalTokens)
                setMessages(prev => prev.map(m => m.id === msgId ? {
                  ...m,
                  content: finalAnswerText || m.content,
                  totalTokens,
                  durationMs,
                } : m))
                break
              }

              case 'error': {
                const { message } = streamEvent.data as { message: string }
                setMessages(prev => prev.map(m => m.id === msgId ? {
                  ...m,
                  content: `❌ Erreur: ${message}`,
                } : m))
                break
              }
            }
          } catch {
            // Skip malformed JSON lines
          }
        }
      }
    } catch (err) {
      setMessages(prev => prev.map(m => m.id === msgId ? {
        ...m,
        content: `❌ Erreur de connexion: ${err instanceof Error ? err.message : 'Erreur inconnue'}`,
      } : m))
    } finally {
      setIsStreaming(false)
    }
  }, [isStreaming])

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      sendMessage(input)
    }
  }

  const activeAgents = Object.values(agentStates).filter(a => a.status !== 'idle')
  const orchestratorDef = ALL_AGENTS.find(a => a.id === 'orchestrator')!

  return (
    <div className="flex h-screen bg-gray-50 dark:bg-gray-950 font-sans">

      {/* ── SIDEBAR — Agents ─────────────────────────────── */}
      {showAgents && (
        <div className="w-64 shrink-0 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 flex flex-col overflow-hidden">
          <div className="p-4 border-b border-gray-200 dark:border-gray-800">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-lg bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center text-lg">🎯</div>
              <div>
                <div className="text-sm font-bold text-gray-900 dark:text-white">MecaPro AI</div>
                <div className="text-xs text-gray-400">9 agents spécialisés</div>
              </div>
            </div>
            {totalTokensUsed > 0 && (
              <div className="mt-2 text-xs text-gray-400">
                Total tokens: <span className="font-mono text-amber-600">{totalTokensUsed.toLocaleString()}</span>
              </div>
            )}
          </div>

          <div className="flex-1 overflow-y-auto p-3 space-y-2">
            {/* Orchestrator */}
            <div className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-2">Orchestrateur</div>
            <AgentCard
              agentId="orchestrator"
              name={orchestratorDef.name}
              icon={orchestratorDef.icon}
              color={orchestratorDef.color}
              status={isStreaming ? 'active' : 'idle'}
              task={isStreaming ? 'Coordination des agents…' : undefined}
            />

            <div className="text-xs font-medium text-gray-400 uppercase tracking-wider mt-4 mb-2">Agents spécialisés</div>
            {ALL_AGENTS.filter(a => a.id !== 'orchestrator').map(agent => {
              const state = agentStates[agent.id]
              return (
                <AgentCard
                  key={agent.id}
                  agentId={agent.id}
                  name={agent.name}
                  icon={agent.icon}
                  color={agent.color}
                  status={state?.status ?? 'idle'}
                  task={state?.task}
                  tokens={state?.tokens}
                />
              )
            })}
          </div>
        </div>
      )}

      {/* ── MAIN CHAT ───────────────────────────────────── */}
      <div className="flex-1 flex flex-col min-w-0">

        {/* Topbar */}
        <div className="h-14 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 flex items-center px-4 gap-3 shrink-0">
          <button
            onClick={() => setShowAgents(!showAgents)}
            className="w-8 h-8 flex items-center justify-center rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 transition text-sm"
          >
            ☰
          </button>
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
            <span className="text-sm font-semibold text-gray-900 dark:text-white">Orchestrateur MecaPro</span>
          </div>
          {activeAgents.length > 0 && (
            <div className="flex items-center gap-1 ml-2">
              {activeAgents.map(a => (
                <div key={a.agentId} className="w-6 h-6 rounded-lg flex items-center justify-center text-sm"
                  style={{ background: `${a.color}20` }} title={a.name}>
                  {a.icon}
                </div>
              ))}
              <span className="text-xs text-amber-600 font-medium ml-1">{activeAgents.length} actif(s)</span>
            </div>
          )}
          <div className="ml-auto flex gap-2">
            <button
              onClick={() => { setMessages(messages.slice(0, 1)); setAgentStates({}); setTotalTokensUsed(0) }}
              className="text-xs px-3 py-1.5 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition text-gray-600 dark:text-gray-400"
            >
              Nouvelle conversation
            </button>
          </div>
        </div>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto px-4 py-6 space-y-6">

          {/* Quick prompts */}
          {messages.length === 1 && (
            <div className="max-w-2xl mx-auto">
              <div className="text-center mb-6">
                <div className="text-4xl mb-3">🎯</div>
                <h2 className="text-lg font-bold text-gray-900 dark:text-white">Que puis-je faire pour vous ?</h2>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Posez une question ou choisissez un exemple</p>
              </div>
              <div className="grid grid-cols-2 gap-3">
                {QUICK_PROMPTS.map((p) => (
                  <button key={p.label} onClick={() => sendMessage(p.prompt)}
                    className="text-left p-3 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl hover:border-amber-400 hover:shadow-sm transition text-sm text-gray-700 dark:text-gray-300 font-medium">
                    {p.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Chat messages */}
          <div className="max-w-3xl mx-auto space-y-6">
            {messages.map((msg) => (
              <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                {msg.role === 'user' ? (
                  <div className="max-w-lg bg-amber-500 text-white rounded-2xl rounded-br-sm px-4 py-3 text-sm leading-relaxed">
                    {msg.content}
                  </div>
                ) : (
                  <div className="flex-1 max-w-full">
                    <div className="flex items-start gap-3">
                      <div className="w-8 h-8 rounded-xl bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center text-lg shrink-0 mt-0.5">🎯</div>
                      <div className="flex-1 min-w-0">

                        {/* Agent events */}
                        {msg.agentEvents && msg.agentEvents.length > 0 && (
                          <details className="mb-3" open={isStreaming && msg.id === messages[messages.length - 1]?.id}>
                            <summary className="text-xs text-gray-400 cursor-pointer hover:text-gray-600 dark:hover:text-gray-300 select-none mb-2 list-none flex items-center gap-1">
                              <span className="border border-gray-200 dark:border-gray-700 rounded-lg px-2 py-0.5 hover:bg-gray-50 dark:hover:bg-gray-800 transition">
                                {isStreaming ? '⚡ ' : ''}
                                {msg.agentEvents.length} action(s) d'agent {isStreaming ? '(en cours…)' : '▼'}
                              </span>
                            </summary>
                            <EventsTimeline events={msg.agentEvents} />
                          </details>
                        )}

                        {/* Final answer */}
                        {msg.content ? (
                          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl rounded-tl-sm px-4 py-3 text-sm text-gray-800 dark:text-gray-200 leading-relaxed whitespace-pre-wrap">
                            {msg.content}
                          </div>
                        ) : (
                          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl rounded-tl-sm px-4 py-3">
                            <div className="flex gap-1.5 items-center">
                              <div className="w-1.5 h-1.5 bg-amber-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                              <div className="w-1.5 h-1.5 bg-amber-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                              <div className="w-1.5 h-1.5 bg-amber-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                            </div>
                          </div>
                        )}

                        {/* Metadata */}
                        {msg.totalTokens && (
                          <div className="flex gap-3 mt-1.5 text-xs text-gray-400">
                            <span>🪙 {msg.totalTokens.toLocaleString()} tokens</span>
                            {msg.durationMs && <span>⏱ {(msg.durationMs / 1000).toFixed(1)}s</span>}
                            <span className="text-gray-300 dark:text-gray-700">
                              {msg.timestamp.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>

          <div ref={messagesEndRef} />
        </div>

        {/* Input */}
        <div className="p-4 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800">
          <div className="max-w-3xl mx-auto">
            <div className="flex gap-3 items-end">
              <div className="flex-1 relative">
                <textarea
                  ref={inputRef}
                  value={input}
                  onChange={e => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Posez une question à l'orchestrateur… (Entrée pour envoyer)"
                  disabled={isStreaming}
                  rows={1}
                  style={{ resize: 'none', minHeight: 42 }}
                  className="w-full text-sm border border-gray-300 dark:border-gray-700 rounded-xl px-4 py-2.5 bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:border-amber-400 disabled:opacity-60 transition"
                />
              </div>
              <button
                onClick={() => sendMessage(input)}
                disabled={!input.trim() || isStreaming}
                className="w-10 h-10 flex items-center justify-center bg-amber-500 hover:bg-amber-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl font-medium transition shrink-0"
              >
                {isStreaming ? (
                  <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                ) : '➤'}
              </button>
            </div>
            <div className="flex items-center justify-between mt-2">
              <p className="text-xs text-gray-400">
                Shift+Entrée pour nouvelle ligne · {totalTokensUsed.toLocaleString()} tokens utilisés
              </p>
              <p className="text-xs text-gray-400">Propulsé par Claude Sonnet 4</p>
            </div>
          </div>
        </div>

      </div>
    </div>
  )
}
