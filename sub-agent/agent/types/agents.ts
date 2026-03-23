export type AgentId = 
  | 'orchestrator'
  | 'diagnostic'
  | 'revisions'
  | 'clientele'
  | 'stock'
  | 'facturation'
  | 'communication'
  | 'analytique'
  | 'securite'
  | 'notifications';

export type AgentState = 'idle' | 'thinking' | 'tool_call' | 'done' | 'error';

export interface ToolDefinition {
  name: string;
  description: string;
  inputSchema: any;
}

export interface AgentMessage {
  role: 'user' | 'assistant' | 'system' | 'tool';
  content: string | any[];
  name?: string;
  tool_use_id?: string;
}

export interface AgentTask {
  id: string;
  agentId: AgentId;
  instruction: string;
  dependsOn?: string[];
  status: 'pending' | 'in_progress' | 'completed' | 'failed';
  result?: any;
}

export interface AgentEvent {
  type: 'agent_started' | 'agent_thinking' | 'agent_done' | 'tool_called' | 'tool_result' | 'task_delegated';
  agentId: AgentId;
  taskId?: string;
  data: Record<string, unknown>;
  timestamp: Date;
}

export interface StreamEvent {
  event: 'plan' | 'agent_start' | 'thinking' | 'tool_call' | 'tool_result' | 'agent_done' | 'final_answer' | 'done' | 'error';
  data: any;
}

export interface OrchestratorRequest {
  sessionId: string;
  userMessage: string;
  context?: Record<string, unknown>;
}

export interface OrchestratorResponse {
  sessionId: string;
  finalAnswer: string;
  plan: any;
  agentResults: Record<AgentId, any>;
  events: AgentEvent[];
  totalTokens: number;
  durationMs: number;
  success: boolean;
}

export interface ContentBlock {
  type: 'text' | 'tool_use';
  text?: string;
  id?: string;
  name?: string;
  input?: any;
}

export interface ToolCallRecord {
  id: string;
  toolName: string;
  input: Record<string, unknown>;
  output?: string;
  status: 'running' | 'success' | 'error';
  error?: string;
  durationMs?: number;
}
