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
  inputSchema: any; // JSON schema de l'input
}

export interface AgentMessage {
  role: 'user' | 'assistant' | 'system' | 'tool';
  content: string;
  name?: string;
  toolCallId?: string;
}

export interface OrchestratorPlan {
  tasks: AgentTask[];
}

export interface AgentTask {
  id: string;
  agentId: AgentId;
  instruction: string;
  dependsOn?: string[];
  status: 'pending' | 'in_progress' | 'completed' | 'failed';
  result?: any;
}

export interface StreamEvent {
  type: 'token' | 'tool_call_start' | 'tool_call_result' | 'agent_state_change' | 'done' | 'error';
  agentId: AgentId;
  content?: string;
  toolName?: string;
  toolInput?: any;
  toolResult?: any;
  state?: AgentState;
  error?: string;
}

export interface AgentEvent {
  agentId: AgentId;
  timestamp: Date;
  details: string;
}

// Types Domaine MecaPro
export interface VehicleContext {
  id: string;
  licensePlate: string;
  make: string;
  model: string;
  mileage: number;
}

export interface DiagnosticInsight {
  faultCode: string;
  description: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  recommendedAction: string;
  estimatedTime?: number;
}

export interface StockAlert {
  partId: string;
  reference: string;
  currentQuantity: number;
  minimumThreshold: number;
}

export interface CustomerContext {
  id: string;
  name: string;
  loyaltyPoints: number;
  churnRisk: number;
}
