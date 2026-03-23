import { AgentId, ToolDefinition } from '../types/agents';

export interface AgentDefinition {
  id: AgentId;
  name: string;
  icon: string;
  color: string;
  model: string;
  maxTokens: number;
  temperature: number;
  systemPrompt: string;
  tools: ToolDefinition[];
}

export const AGENT_REGISTRY: Record<AgentId, AgentDefinition> = {
  orchestrator: {
    id: 'orchestrator',
    name: 'Orchestrateur',
    icon: '🎯',
    color: '#f59e0b',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 4096,
    temperature: 0,
    systemPrompt: "Vous êtes l'Orchestrateur IA de MecaPro. Votre rôle est d'analyser la requête utilisateur, de créer un plan et de déléguer les tâches aux agents spécialisés appropriés.",
    tools: [
      {
        name: 'delegate_to_agent',
        description: 'Délègue une tâche spécifique à un autre agent.',
        inputSchema: { type: 'object', properties: { agentId: { type: 'string' }, task: { type: 'string' }, context: { type: 'object' } }, required: ['agentId', 'task'] }
      },
      {
        name: 'create_plan',
        description: 'Crée un plan d\'exécution avec plusieurs tâches dépendantes.',
        inputSchema: { type: 'object', properties: { goal: { type: 'string' }, tasks: { type: 'array' } }, required: ['goal', 'tasks'] }
      }
    ]
  },
  diagnostic: {
    id: 'diagnostic',
    name: 'Diagnostic IA',
    icon: '🔍',
    color: '#3b82f6',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Spécialiste en diagnostic automobile. Vous décodez les codes défauts OBD-II et proposez des solutions techniques.",
    tools: [
      {
        name: 'analyze_fault_code',
        description: 'Analyse un code défaut OBD-II.',
        inputSchema: { type: 'object', properties: { code: { type: 'string' } }, required: ['code'] }
      },
      {
        name: 'analyze_part_image',
        description: 'Analyse une image d\'une pièce mécanique pour détecter des dommages ou de l\'usure.',
        inputSchema: { type: 'object', properties: { imageUrl: { type: 'string' }, partType: { type: 'string' } }, required: ['imageUrl'] }
      }
    ]
  },
  revisions: {
    id: 'revisions',
    name: 'Planning Révisions',
    icon: '📅',
    color: '#8b5cf6',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Vous gérez le planning de l'atelier et les rendez-vous d'entretien.",
    tools: [
      {
        name: 'schedule_revision',
        description: 'Planifie une nouvelle révision.',
        inputSchema: { type: 'object', properties: { vehicle_id: { type: 'string' }, date: { type: 'string' } }, required: ['vehicle_id', 'date'] }
      }
    ]
  },
  clientele: {
    id: 'clientele',
    name: 'Relation Client',
    icon: '👥',
    color: '#ec4899',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Chef du CRM et de la fidélité, vous analysez le comportement des clients.",
    tools: [
      {
        name: 'get_customer_360',
        description: 'Récupère une vue à 360 degrés d\'un client.',
        inputSchema: { type: 'object', properties: { customer_id: { type: 'string' } }, required: ['customer_id'] }
      }
    ]
  },
  stock: {
    id: 'stock',
    name: 'Gestionnaire Stock',
    icon: '📦',
    color: '#10b981',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Agent en charge de l'inventaire des pièces automobiles.",
    tools: [
      {
        name: 'check_stock_levels',
        description: 'Vérifie les niveaux de stock.',
        inputSchema: { type: 'object', properties: { garage_id: { type: 'string' } }, required: ['garage_id'] }
      }
    ]
  },
  facturation: {
    id: 'facturation',
    name: 'Agent Facturation',
    icon: '🧾',
    color: '#06b6d4',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Gère la finance : vous créez des devis et facturez les clients.",
    tools: [
      {
        name: 'generate_quote',
        description: 'Génère un devis pour un client.',
        inputSchema: { type: 'object', properties: { vehicle_id: { type: 'string' } }, required: ['vehicle_id'] }
      }
    ]
  },
  communication: {
    id: 'communication',
    name: 'Secrétariat Auto',
    icon: '💬',
    color: '#64748b',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Rédige et expédie les e-mails et SMS.",
    tools: []
  },
  analytique: {
    id: 'analytique',
    name: 'Business Analyst',
    icon: '📊',
    color: '#3b82f6',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Agrège les données et génère des KPIs pour le manager du garage.",
    tools: [
      {
        name: 'compute_kpis',
        description: 'Calcule divers KPIs sur une période donnée.',
        inputSchema: { type: 'object', properties: { period: { type: 'string' } } }
      }
    ]
  },
  securite: {
    id: 'securite',
    name: 'Agent Sécurité',
    icon: '🚨',
    color: '#ef4444',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Supervise la conformité et lance des alertes critiques lors de problèmes graves.",
    tools: [
      {
        name: 'flag_vehicle_unsafe',
        description: 'Signale un véhicule comme dangereux ou non réparable.',
        inputSchema: { type: 'object', properties: { vehicle_id: { type: 'string' }, reason: { type: 'string' } }, required: ['vehicle_id', 'reason'] }
      }
    ]
  },
  notifications: {
    id: 'notifications',
    name: 'Notification Hub',
    icon: '🔔',
    color: '#f59e0b',
    model: 'claude-3-5-sonnet-20240620',
    maxTokens: 2048,
    temperature: 0,
    systemPrompt: "Gère les campagnes de masse et les rappels automatiques.",
    tools: []
  }
};

export const ALL_AGENTS = Object.values(AGENT_REGISTRY);
export const ORCHESTRATOR_AGENT = AGENT_REGISTRY.orchestrator;
