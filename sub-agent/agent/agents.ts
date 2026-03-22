// ============================================================
// types/agents.ts — Complete Type System for AI Sub-Agents
// ============================================================

// ─────────────────────────────────────────────────────────────
// AGENT IDENTITIES
// ─────────────────────────────────────────────────────────────

export type AgentId =
  | 'orchestrator'
  | 'diagnostic-agent'
  | 'revision-agent'
  | 'customer-agent'
  | 'stock-agent'
  | 'invoice-agent'
  | 'chat-agent'
  | 'analytics-agent'
  | 'security-agent'
  | 'notification-agent'

export type AgentStatus = 'idle' | 'thinking' | 'executing' | 'waiting' | 'done' | 'error'
export type MessageRole = 'user' | 'assistant' | 'system' | 'tool'
export type ToolCallStatus = 'pending' | 'running' | 'success' | 'error'
export type TaskPriority = 'low' | 'normal' | 'high' | 'critical'

// ─────────────────────────────────────────────────────────────
// AGENT DEFINITIONS
// ─────────────────────────────────────────────────────────────

export interface AgentDefinition {
  id: AgentId
  name: string
  description: string
  systemPrompt: string
  tools: ToolDefinition[]
  model: string
  maxTokens: number
  temperature: number
  capabilities: string[]
  icon: string
  color: string
}

export interface AgentState {
  id: AgentId
  status: AgentStatus
  currentTask?: string
  messages: AgentMessage[]
  toolCalls: ToolCallRecord[]
  tokensUsed: number
  startedAt?: Date
  completedAt?: Date
  error?: string
}

// ─────────────────────────────────────────────────────────────
// MESSAGES
// ─────────────────────────────────────────────────────────────

export interface AgentMessage {
  id: string
  role: MessageRole
  content: string | ContentBlock[]
  agentId?: AgentId
  timestamp: Date
  tokens?: number
}

export type ContentBlock =
  | { type: 'text'; text: string }
  | { type: 'tool_use'; id: string; name: string; input: Record<string, unknown> }
  | { type: 'tool_result'; tool_use_id: string; content: string; is_error?: boolean }

// ─────────────────────────────────────────────────────────────
// TOOLS
// ─────────────────────────────────────────────────────────────

export interface ToolDefinition {
  name: string
  description: string
  input_schema: {
    type: 'object'
    properties: Record<string, ToolProperty>
    required?: string[]
  }
}

export interface ToolProperty {
  type: 'string' | 'number' | 'boolean' | 'array' | 'object'
  description: string
  enum?: string[]
  items?: ToolProperty
  properties?: Record<string, ToolProperty>
}

export interface ToolCallRecord {
  id: string
  toolName: string
  input: Record<string, unknown>
  output?: string
  status: ToolCallStatus
  durationMs?: number
  error?: string
}

// ─────────────────────────────────────────────────────────────
// TASKS & ORCHESTRATION
// ─────────────────────────────────────────────────────────────

export interface AgentTask {
  id: string
  description: string
  priority: TaskPriority
  assignedTo?: AgentId
  dependsOn?: string[]
  status: 'pending' | 'assigned' | 'running' | 'done' | 'failed'
  result?: string
  createdAt: Date
  completedAt?: Date
  context?: Record<string, unknown>
}

export interface OrchestratorPlan {
  goal: string
  tasks: AgentTask[]
  parallelGroups: string[][]
  estimatedTokens: number
  estimatedDurationMs: number
}

export interface AgentEvent {
  type:
    | 'agent_started'
    | 'agent_thinking'
    | 'tool_called'
    | 'tool_result'
    | 'agent_response'
    | 'agent_done'
    | 'agent_error'
    | 'task_delegated'
    | 'plan_created'
  agentId: AgentId
  taskId?: string
  data: Record<string, unknown>
  timestamp: Date
}

// ─────────────────────────────────────────────────────────────
// ORCHESTRATOR REQUEST/RESPONSE
// ─────────────────────────────────────────────────────────────

export interface OrchestratorRequest {
  userMessage: string
  context?: {
    vehicleId?: string
    customerId?: string
    garageId?: string
    userId?: string
    currentPage?: string
  }
  sessionId: string
  stream?: boolean
}

export interface OrchestratorResponse {
  sessionId: string
  finalAnswer: string
  plan: OrchestratorPlan
  agentResults: Record<AgentId, AgentResult>
  events: AgentEvent[]
  totalTokens: number
  durationMs: number
  success: boolean
  error?: string
}

export interface AgentResult {
  agentId: AgentId
  success: boolean
  output: string
  toolCallsCount: number
  tokensUsed: number
  durationMs: number
}

// ─────────────────────────────────────────────────────────────
// STREAM EVENTS (SSE)
// ─────────────────────────────────────────────────────────────

export type StreamEvent =
  | { event: 'plan'; data: OrchestratorPlan }
  | { event: 'agent_start'; data: { agentId: AgentId; task: string } }
  | { event: 'thinking'; data: { agentId: AgentId; text: string } }
  | { event: 'tool_call'; data: { agentId: AgentId; toolName: string; input: Record<string, unknown> } }
  | { event: 'tool_result'; data: { agentId: AgentId; toolName: string; result: string; success: boolean } }
  | { event: 'agent_done'; data: { agentId: AgentId; output: string; tokens: number } }
  | { event: 'final_answer'; data: { text: string } }
  | { event: 'error'; data: { agentId?: AgentId; message: string } }
  | { event: 'done'; data: { totalTokens: number; durationMs: number } }

// ─────────────────────────────────────────────────────────────
// MECAPRO DOMAIN TYPES (used by tools)
// ─────────────────────────────────────────────────────────────

export interface VehicleContext {
  id: string
  plate: string
  make: string
  model: string
  year: number
  mileage: number
  customerId: string
  customerName: string
  activeDiagnostics: number
  nextRevision?: string
}

export interface DiagnosticInsight {
  faultCode: string
  description: string
  severity: 'Critical' | 'Major' | 'Minor' | 'Info'
  probableCauses: string[]
  recommendedActions: string[]
  estimatedCost?: number
  urgency: 'immediate' | 'soon' | 'routine'
}

export interface RevisionRecommendation {
  type: string
  reason: string
  urgency: 'overdue' | 'due_soon' | 'upcoming'
  estimatedCost: number
  estimatedDuration: number
  partsNeeded: string[]
}

export interface StockAlert {
  partId: string
  partName: string
  reference: string
  currentStock: number
  minStock: number
  suggestedOrderQty: number
  supplierName?: string
  leadTimeDays?: number
}

export interface CustomerInsight {
  customerId: string
  customerName: string
  loyaltyLevel: string
  totalSpent: number
  visitFrequency: string
  churnRisk: 'low' | 'medium' | 'high'
  upsellOpportunities: string[]
  lastVisit?: string
}
