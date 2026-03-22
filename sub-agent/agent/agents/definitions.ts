import { AgentId, ToolDefinition } from '../types/agents';

export interface AgentDefinition {
  id: AgentId;
  name: string;
  systemPrompt: string;
  tools: ToolDefinition[];
}

export const agentDefinitions: Record<AgentId, AgentDefinition> = {
  orchestrator: {
    id: 'orchestrator',
    name: 'Orchestrateur',
    systemPrompt: "Vous êtes l'Orchestrateur IA de MecaPro. Votre rôle est d'analyser la requête utilisateur, de créer un plan et de déléguer les tâches aux agents spécialisés appropriés.",
    tools: [
      {
        name: 'delegate_to_agent',
        description: 'Délègue une tâche spécifique à un autre agent.',
        inputSchema: { type: 'object', properties: { agentId: { type: 'string' }, instruction: { type: 'string' } }, required: ['agentId', 'instruction'] }
      },
      {
        name: 'create_plan',
        description: 'Crée un plan d\'exécution avec plusieurs tâches dépendantes.',
        inputSchema: { type: 'object', properties: { tasks: { type: 'array' } }, required: ['tasks'] }
      }
    ]
  },
  diagnostic: {
    id: 'diagnostic',
    name: 'Diagnostic IA',
    systemPrompt: "Spécialiste en diagnostic automobile. Vous décodez les codes défauts OBD-II et proposez des solutions techniques.",
    tools: [
      {
        name: 'analyze_fault_code',
        description: 'Analyse un code défaut OBD-II.',
        inputSchema: { type: 'object', properties: { code: { type: 'string' }, vehicleId: { type: 'string' } }, required: ['code'] }
      },
      {
        name: 'get_repair_estimate',
        description: 'Estime le temps et le coût d\'une réparation.',
        inputSchema: { type: 'object', properties: { operation: { type: 'string' } }, required: ['operation'] }
      }
    ]
  },
  revisions: {
    id: 'revisions',
    name: 'Planning Révisions',
    systemPrompt: "Vous gérez le planning de l'atelier et les rendez-vous d'entretien.",
    tools: [
      {
        name: 'schedule_revision',
        description: 'Planifie une nouvelle révision.',
        inputSchema: { type: 'object', properties: { vehicleId: { type: 'string' }, date: { type: 'string' } }, required: ['vehicleId', 'date'] }
      },
      {
        name: 'check_availability',
        description: 'Vérifie la disponibilité de l\'atelier.',
        inputSchema: { type: 'object', properties: { date: { type: 'string' } }, required: ['date'] }
      }
    ]
  },
  clientele: {
    id: 'clientele',
    name: 'Relation Client',
    systemPrompt: "Chef du CRM et de la fidélité, vous analysez le comportement des clients.",
    tools: [
      {
        name: 'get_customer_360',
        description: 'Récupère une vue à 360 degrés d\'un client.',
        inputSchema: { type: 'object', properties: { customerId: { type: 'string' } }, required: ['customerId'] }
      },
      {
        name: 'calculate_churn_risk',
        description: 'Calcule le risque de départ d\'un client.',
        inputSchema: { type: 'object', properties: { customerId: { type: 'string' } }, required: ['customerId'] }
      }
    ]
  },
  stock: {
    id: 'stock',
    name: 'Gestionnaire Stock',
    systemPrompt: "Agent en charge de l'inventaire des pièces automobiles.",
    tools: [
      {
        name: 'check_stock_levels',
        description: 'Vérifie les niveaux de stock.',
        inputSchema: { type: 'object', properties: { partReference: { type: 'string' } }, required: ['partReference'] }
      },
      {
        name: 'create_purchase_order',
        description: 'Génère un bon de commande pour le réapprovisionnement.',
        inputSchema: { type: 'object', properties: { partId: { type: 'string' }, quantity: { type: 'number' } }, required: ['partId', 'quantity'] }
      }
    ]
  },
  facturation: {
    id: 'facturation',
    name: 'Agent Facturation',
    systemPrompt: "Gère la finance : vous créez des devis et facturez les clients.",
    tools: [
      {
        name: 'generate_quote',
        description: 'Génère un devis pour un client.',
        inputSchema: { type: 'object', properties: { customerId: { type: 'string' }, items: { type: 'array' } }, required: ['customerId', 'items'] }
      },
      {
        name: 'create_invoice',
        description: 'Convertit un diagnostic ou un devis en facture finale.',
        inputSchema: { type: 'object', properties: { quoteId: { type: 'string' } }, required: ['quoteId'] }
      }
    ]
  },
  communication: {
    id: 'communication',
    name: 'Secrétariat Auto',
    systemPrompt: "Rédige et expédie les e-mails et SMS.",
    tools: [
      {
        name: 'draft_message',
        description: 'Rédige un brouillon de message.',
        inputSchema: { type: 'object', properties: { context: { type: 'string' }, tone: { type: 'string' } }, required: ['context'] }
      },
      {
        name: 'send_message',
        description: 'Envoie un message via Email ou SMS.',
        inputSchema: { type: 'object', properties: { address: { type: 'string' }, body: { type: 'string' }, channel: { type: 'string' } }, required: ['address', 'body', 'channel'] }
      }
    ]
  },
  analytique: {
    id: 'analytique',
    name: 'Business Analyst',
    systemPrompt: "Agrège les données et génère des KPIs pour le manager du garage.",
    tools: [
      {
        name: 'compute_kpis',
        description: 'Calcule divers KPIs sur une période donnée.',
        inputSchema: { type: 'object', properties: { period: { type: 'string' } }, required: ['period'] }
      },
      {
        name: 'generate_report',
        description: 'Crée un rapport d\'activité complet.',
        inputSchema: { type: 'object', properties: { parameters: { type: 'object' } }, required: ['parameters'] }
      }
    ]
  },
  securite: {
    id: 'securite',
    name: 'Agent Sécurité',
    systemPrompt: "Supervise la conformité et lance des alertes critiques lors de problèmes graves.",
    tools: [
      {
        name: 'flag_vehicle_unsafe',
        description: 'Signale un véhicule comme dangereux ou non réparable.',
        inputSchema: { type: 'object', properties: { vehicleId: { type: 'string' }, reason: { type: 'string' } }, required: ['vehicleId', 'reason'] }
      },
      {
        name: 'escalate_to_manager',
        description: 'Fait remonter un problème critique au responsable.',
        inputSchema: { type: 'object', properties: { issueId: { type: 'string' }, priority: { type: 'string' } }, required: ['issueId', 'priority'] }
      }
    ]
  },
  notifications: {
    id: 'notifications',
    name: 'Notification Hub',
    systemPrompt: "Gère les campagnes de masse et les rappels automatiques (CT, entretien).",
    tools: [
      {
        name: 'schedule_reminder',
        description: 'Programme un rappel pour le futur.',
        inputSchema: { type: 'object', properties: { customerId: { type: 'string' }, date: { type: 'string' }, eventType: { type: 'string' } }, required: ['customerId', 'date', 'eventType'] }
      },
      {
        name: 'send_bulk_campaign',
        description: 'Envoie une campagne de notification en masse.',
        inputSchema: { type: 'object', properties: { segmentId: { type: 'string' }, content: { type: 'string' } }, required: ['segmentId', 'content'] }
      }
    ]
  }
};
