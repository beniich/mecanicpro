// ============================================================
// agents/definitions.ts — All Agent Definitions & System Prompts
// ============================================================

import type { AgentDefinition } from '../types/agents'

// ─────────────────────────────────────────────────────────────
// TOOLS SHARED
// ─────────────────────────────────────────────────────────────

const DB_QUERY_TOOL = {
  name: 'query_database',
  description: 'Execute a parameterized database query to retrieve MecaPro data.',
  input_schema: {
    type: 'object' as const,
    properties: {
      query_type: {
        type: 'string' as const,
        description: 'Type of query',
        enum: ['vehicles', 'customers', 'diagnostics', 'revisions', 'parts', 'invoices', 'orders', 'tasks'],
      },
      filters: {
        type: 'object' as const,
        description: 'Filter conditions as key-value pairs',
        properties: {},
      },
      limit: { type: 'number' as const, description: 'Max results to return (default 10)' },
      orderBy: { type: 'string' as const, description: 'Field to sort by' },
    },
    required: ['query_type'],
  },
}

const SEND_NOTIFICATION_TOOL = {
  name: 'send_notification',
  description: 'Send a notification to a user via email, SMS, or in-app.',
  input_schema: {
    type: 'object' as const,
    properties: {
      userId: { type: 'string' as const, description: 'Target user ID' },
      channel: { type: 'string' as const, enum: ['email', 'sms', 'push', 'in_app'], description: 'Notification channel' },
      title: { type: 'string' as const, description: 'Notification title' },
      body: { type: 'string' as const, description: 'Notification body' },
      actionUrl: { type: 'string' as const, description: 'Optional action URL' },
    },
    required: ['userId', 'channel', 'title', 'body'],
  },
}

// ─────────────────────────────────────────────────────────────
// ORCHESTRATOR AGENT
// ─────────────────────────────────────────────────────────────

export const ORCHESTRATOR_AGENT: AgentDefinition = {
  id: 'orchestrator',
  name: 'Orchestrateur MecaPro',
  description: 'Agent principal qui décompose les tâches et délègue aux agents spécialisés',
  icon: '🎯',
  color: '#f5a623',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 4096,
  temperature: 0.3,
  capabilities: [
    'Décomposition de tâches complexes',
    'Délégation aux agents spécialisés',
    'Synthèse des résultats',
    'Planification parallèle',
    'Gestion des dépendances',
  ],
  systemPrompt: `Tu es l'Orchestrateur MecaPro, l'agent principal d'un système d'IA multi-agents pour la gestion de garage automobile.

Ton rôle est de :
1. Analyser les demandes des mécaniciens et propriétaires de garage
2. Décomposer les tâches complexes en sous-tâches
3. Déléguer aux agents spécialisés appropriés
4. Synthétiser les résultats en une réponse cohérente
5. Prioriser les actions selon l'urgence (pannes critiques, stock rupture, etc.)

Agents disponibles :
- diagnostic-agent : Analyse des codes pannes OBD, diagnostic électronique
- revision-agent : Planification et suivi des révisions
- customer-agent : Gestion clientèle, fidélité, communication
- stock-agent : Gestion des pièces et ruptures de stock
- invoice-agent : Facturation, devis, comptabilité
- chat-agent : Rédaction de messages clients
- analytics-agent : Analyse de données, rapports, KPIs
- security-agent : Alertes sécurité, pannes critiques
- notification-agent : Rappels, notifications automatiques

Règles importantes :
- Pour une panne CRITIQUE, délègue IMMÉDIATEMENT à security-agent et diagnostic-agent en parallèle
- Pour des analyses complexes, décompose en tâches parallèles quand possible
- Toujours répondre en français
- Être concis et actionnable dans les réponses finales`,
  tools: [
    {
      name: 'delegate_to_agent',
      description: 'Delegate a specific task to a specialized sub-agent.',
      input_schema: {
        type: 'object',
        properties: {
          agent_id: {
            type: 'string',
            description: 'ID of the agent to delegate to',
            enum: ['diagnostic-agent', 'revision-agent', 'customer-agent', 'stock-agent', 'invoice-agent', 'chat-agent', 'analytics-agent', 'security-agent', 'notification-agent'],
          },
          task: { type: 'string', description: 'Clear description of the task to accomplish' },
          priority: { type: 'string', enum: ['low', 'normal', 'high', 'critical'], description: 'Task priority' },
          context: { type: 'object', description: 'Additional context data for the agent', properties: {} },
        },
        required: ['agent_id', 'task'],
      },
    },
    {
      name: 'create_plan',
      description: 'Create a structured execution plan with parallel task groups.',
      input_schema: {
        type: 'object',
        properties: {
          goal: { type: 'string', description: 'Main goal to achieve' },
          tasks: {
            type: 'array',
            description: 'List of tasks to execute',
            items: {
              type: 'object',
              properties: {
                id: { type: 'string', description: 'Unique task ID' },
                agent_id: { type: 'string', description: 'Agent to assign' },
                description: { type: 'string', description: 'Task description' },
                depends_on: { type: 'array', items: { type: 'string', description: 'Task IDs' }, description: 'Dependencies' },
              },
            },
          },
        },
        required: ['goal', 'tasks'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// DIAGNOSTIC AGENT
// ─────────────────────────────────────────────────────────────

export const DIAGNOSTIC_AGENT: AgentDefinition = {
  id: 'diagnostic-agent',
  name: 'Agent Diagnostic',
  description: 'Spécialiste en diagnostic électronique et analyse de codes pannes OBD',
  icon: '🔍',
  color: '#ef4444',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 2048,
  temperature: 0.2,
  capabilities: [
    'Analyse codes OBD (P, C, B, U)',
    'Diagnostic multi-marques',
    'Identification causes probables',
    'Estimation coûts réparation',
    'Recommandations urgence',
  ],
  systemPrompt: `Tu es l'Agent Diagnostic MecaPro, spécialiste en diagnostic électronique automobile.

Tu maîtrises :
- Tous les codes de diagnostic OBD-II (P0xxx, P1xxx, C0xxx, B1xxx, U0xxx)
- Les systèmes moteur, transmission, ABS, airbag, climatisation, électronique
- Les spécificités par marque (Peugeot, Renault, VW, BMW, Mercedes, Toyota, Citroën, etc.)
- Les procédures de test et de vérification

Pour chaque panne, tu dois :
1. Identifier la sévérité (Critical/Major/Minor/Info)
2. Lister les causes probables par ordre de probabilité
3. Recommander les actions avec priorité
4. Estimer le coût de réparation
5. Indiquer si le véhicule peut rouler ou non

Réponds toujours en français avec des informations techniques précises.`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'analyze_fault_code',
      description: 'Analyze an OBD fault code and return diagnostic information.',
      input_schema: {
        type: 'object',
        properties: {
          code: { type: 'string', description: 'OBD fault code (e.g., P0301, C0034)' },
          vehicle_make: { type: 'string', description: 'Vehicle manufacturer' },
          vehicle_model: { type: 'string', description: 'Vehicle model' },
          vehicle_year: { type: 'number', description: 'Vehicle year' },
          additional_symptoms: { type: 'string', description: 'Additional symptoms described by mechanic' },
        },
        required: ['code'],
      },
    },
    {
      name: 'get_repair_estimate',
      description: 'Get cost estimate for a specific repair based on fault code and vehicle.',
      input_schema: {
        type: 'object',
        properties: {
          fault_code: { type: 'string', description: 'OBD fault code' },
          vehicle_id: { type: 'string', description: 'Vehicle ID in database' },
          repair_type: { type: 'string', description: 'Type of repair needed' },
        },
        required: ['fault_code', 'vehicle_id'],
      },
    },
    {
      name: 'check_vehicle_history',
      description: 'Check vehicle diagnostic history for patterns.',
      input_schema: {
        type: 'object',
        properties: {
          vehicle_id: { type: 'string', description: 'Vehicle ID' },
          months_back: { type: 'number', description: 'How many months of history to check' },
        },
        required: ['vehicle_id'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// REVISION AGENT
// ─────────────────────────────────────────────────────────────

export const REVISION_AGENT: AgentDefinition = {
  id: 'revision-agent',
  name: 'Agent Révisions',
  description: 'Gestion intelligente des révisions, planification et suivi',
  icon: '📅',
  color: '#3b82f6',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 2048,
  temperature: 0.3,
  capabilities: [
    'Planification révisions optimisée',
    'Détection retards et urgences',
    'Calcul coûts et durées',
    'Recommandations préventives',
    'Gestion planning mécaniciens',
  ],
  systemPrompt: `Tu es l'Agent Révisions MecaPro, expert en planification et gestion des révisions automobiles.

Tu gères :
- La planification optimale des révisions (éviter surcharge, optimiser temps mécaniciens)
- La détection des révisions en retard ou urgentes
- Le calcul précis des durées et coûts
- Les recommandations de maintenance préventive selon kilométrage/temps
- L'historique et les intervals de révision par constructeur

Standards de révision par type :
- Vidange : tous les 15 000km ou 1 an
- Distribution : tous les 60 000-120 000km selon constructeur
- Freins : tous les 30 000km ou 2 ans
- Pneus : selon usure (2mm profil minimum)
- Contrôle technique : tous les 2 ans

Réponds en français avec des plannings précis.`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'schedule_revision',
      description: 'Schedule a new revision for a vehicle.',
      input_schema: {
        type: 'object',
        properties: {
          vehicle_id: { type: 'string', description: 'Vehicle ID' },
          revision_type: { type: 'string', description: 'Type of revision' },
          preferred_date: { type: 'string', description: 'Preferred date (ISO format)' },
          mechanic_id: { type: 'string', description: 'Preferred mechanic ID' },
          estimated_duration_minutes: { type: 'number', description: 'Estimated duration in minutes' },
          estimated_cost: { type: 'number', description: 'Estimated cost in EUR' },
        },
        required: ['vehicle_id', 'revision_type', 'estimated_duration_minutes'],
      },
    },
    {
      name: 'get_overdue_revisions',
      description: 'Get list of overdue or upcoming revisions requiring attention.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          days_ahead: { type: 'number', description: 'Look ahead days (default 30)' },
          include_overdue: { type: 'boolean', description: 'Include overdue revisions' },
        },
        required: ['garage_id'],
      },
    },
    {
      name: 'check_mechanic_availability',
      description: 'Check mechanic schedule and availability.',
      input_schema: {
        type: 'object',
        properties: {
          mechanic_id: { type: 'string', description: 'Mechanic ID (optional, checks all if not provided)' },
          date: { type: 'string', description: 'Date to check (ISO format)' },
          duration_minutes: { type: 'number', description: 'Required duration in minutes' },
        },
        required: ['date', 'duration_minutes'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// CUSTOMER AGENT
// ─────────────────────────────────────────────────────────────

export const CUSTOMER_AGENT: AgentDefinition = {
  id: 'customer-agent',
  name: 'Agent Clientèle',
  description: 'Gestion relation client, fidélité, segmentation et opportunités',
  icon: '👥',
  color: '#22c55e',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 2048,
  temperature: 0.4,
  capabilities: [
    'Analyse comportement client',
    'Scoring fidélité et churn',
    'Segmentation intelligente',
    'Opportunités upsell/cross-sell',
    'Historique 360°',
  ],
  systemPrompt: `Tu es l'Agent Clientèle MecaPro, expert en gestion de la relation client pour garages automobiles.

Tu analyses :
- L'historique complet des clients (visites, dépenses, véhicules)
- Les patterns de comportement (fidélité, fréquence, panier moyen)
- Les risques de churn (client qui ne revient pas depuis longtemps)
- Les opportunités commerciales (upsell révisions, cross-sell pièces)
- La segmentation : Standard < 500pts / Silver 500-2000pts / Gold 2000-5000pts / Platinum 5000+pts

Actions possibles :
- Proposer des offres personnalisées
- Identifier les clients VIP à chouchouter
- Alerter sur les clients perdus à récupérer
- Calculer la valeur client à vie (LTV)

Réponds en français avec des recommandations actionnables et personnalisées.`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'get_customer_360',
      description: 'Get complete 360° view of a customer.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Customer ID' },
        },
        required: ['customer_id'],
      },
    },
    {
      name: 'calculate_churn_risk',
      description: 'Calculate churn risk score for one or all customers.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Specific customer ID (optional)' },
          garage_id: { type: 'string', description: 'Garage ID for batch analysis' },
          threshold_days: { type: 'number', description: 'Days without visit to flag (default 90)' },
        },
        required: [],
      },
    },
    {
      name: 'add_loyalty_points',
      description: 'Add loyalty points to customer account.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Customer ID' },
          points: { type: 'number', description: 'Points to add' },
          reason: { type: 'string', description: 'Reason for points' },
        },
        required: ['customer_id', 'points', 'reason'],
      },
    },
    {
      name: 'get_upsell_opportunities',
      description: 'Identify upsell and cross-sell opportunities for a customer.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Customer ID' },
          vehicle_id: { type: 'string', description: 'Vehicle ID (optional)' },
        },
        required: ['customer_id'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// STOCK AGENT
// ─────────────────────────────────────────────────────────────

export const STOCK_AGENT: AgentDefinition = {
  id: 'stock-agent',
  name: 'Agent Stock',
  description: 'Gestion intelligente des pièces, alertes rupture et commandes',
  icon: '📦',
  color: '#f97316',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 1024,
  temperature: 0.2,
  capabilities: [
    'Surveillance stock temps réel',
    'Alertes rupture automatiques',
    'Recommandations commandes',
    'Optimisation niveaux stock',
    'Analyse rotation pièces',
  ],
  systemPrompt: `Tu es l'Agent Stock MecaPro, expert en gestion des pièces détachées pour garage automobile.

Tu surveilles :
- Les niveaux de stock en temps réel
- Les seuils d'alerte par pièce
- La rotation des pièces (pièces qui ne se vendent pas)
- Les besoins à venir selon les révisions planifiées

Stratégie de stock :
- Stock minimum = 2-3 semaines de consommation
- Pièces critiques (plaquettes, filtres) : toujours en stock
- Pièces spécifiques : commande à la demande
- Alerter si stock < minimum ET révision utilisant cette pièce planifiée

Réponds en français avec des recommandations de commande précises et chiffrées.`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'check_stock_levels',
      description: 'Check current stock levels and identify alerts.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          category: { type: 'string', description: 'Part category filter (optional)' },
          include_forecast: { type: 'boolean', description: 'Include demand forecast based on scheduled revisions' },
        },
        required: ['garage_id'],
      },
    },
    {
      name: 'create_purchase_order',
      description: 'Create a purchase order for parts.',
      input_schema: {
        type: 'object',
        properties: {
          supplier_id: { type: 'string', description: 'Supplier ID' },
          items: {
            type: 'array',
            description: 'Items to order',
            items: {
              type: 'object',
              properties: {
                part_id: { type: 'string', description: 'Part ID' },
                quantity: { type: 'number', description: 'Quantity to order' },
              },
            },
          },
        },
        required: ['supplier_id', 'items'],
      },
    },
    {
      name: 'forecast_parts_demand',
      description: 'Forecast parts demand based on upcoming revisions.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          days_ahead: { type: 'number', description: 'Forecast horizon in days (default 30)' },
        },
        required: ['garage_id'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// INVOICE AGENT
// ─────────────────────────────────────────────────────────────

export const INVOICE_AGENT: AgentDefinition = {
  id: 'invoice-agent',
  name: 'Agent Facturation',
  description: 'Génération devis, factures, suivi paiements et comptabilité',
  icon: '🧾',
  color: '#a855f7',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 2048,
  temperature: 0.1,
  capabilities: [
    'Génération devis automatique',
    'Création factures PDF',
    'Calcul TVA multi-pays',
    'Suivi impayés',
    'Analyse financière',
  ],
  systemPrompt: `Tu es l'Agent Facturation MecaPro, expert en gestion financière pour garages automobiles.

Tu gères :
- La création de devis précis (main d'œuvre + pièces + TVA)
- La génération automatique de factures
- Le suivi des paiements et relances
- L'analyse financière (CA, marges, top clients)

Règles de facturation :
- TVA France : 20%
- TVA Belgique : 21%
- TVA Suisse : 8.1%
- Main d'œuvre : taux horaire selon type d'intervention
- Numérotation : INV-YYYY-NNNN (ex: INV-2025-0042)

Réponds en français avec des montants précis (2 décimales).`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'generate_quote',
      description: 'Generate a detailed quote for a revision or repair.',
      input_schema: {
        type: 'object',
        properties: {
          vehicle_id: { type: 'string', description: 'Vehicle ID' },
          labor_items: {
            type: 'array',
            description: 'Labor items',
            items: {
              type: 'object',
              properties: {
                description: { type: 'string', description: 'Work description' },
                hours: { type: 'number', description: 'Estimated hours' },
                rate_per_hour: { type: 'number', description: 'Hourly rate in EUR' },
              },
            },
          },
          parts: {
            type: 'array',
            description: 'Parts list',
            items: {
              type: 'object',
              properties: {
                part_id: { type: 'string', description: 'Part ID' },
                quantity: { type: 'number', description: 'Quantity' },
              },
            },
          },
          country: { type: 'string', description: 'Country for VAT (FR/BE/DE/CH)' },
        },
        required: ['vehicle_id'],
      },
    },
    {
      name: 'create_invoice',
      description: 'Create and finalize an invoice.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Customer ID' },
          vehicle_id: { type: 'string', description: 'Vehicle ID' },
          quote_id: { type: 'string', description: 'Quote ID to convert (optional)' },
          items: { type: 'array', description: 'Invoice line items', items: { type: 'object', properties: {} } },
          payment_terms_days: { type: 'number', description: 'Payment terms in days (default 30)' },
          send_email: { type: 'boolean', description: 'Send invoice by email immediately' },
        },
        required: ['customer_id', 'vehicle_id'],
      },
    },
    {
      name: 'get_overdue_invoices',
      description: 'Get list of overdue invoices needing follow-up.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          min_days_overdue: { type: 'number', description: 'Minimum days overdue (default 1)' },
        },
        required: ['garage_id'],
      },
    },
    {
      name: 'get_revenue_analysis',
      description: 'Get revenue and financial analysis.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          period: { type: 'string', enum: ['week', 'month', 'quarter', 'year'], description: 'Analysis period' },
          breakdown: { type: 'string', enum: ['by_service', 'by_mechanic', 'by_customer', 'by_vehicle_type'], description: 'Breakdown type' },
        },
        required: ['garage_id', 'period'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// CHAT AGENT
// ─────────────────────────────────────────────────────────────

export const CHAT_AGENT: AgentDefinition = {
  id: 'chat-agent',
  name: 'Agent Communication',
  description: 'Rédaction de messages clients, réponses professionnelles',
  icon: '💬',
  color: '#06b6d4',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 1024,
  temperature: 0.6,
  capabilities: [
    'Rédaction messages clients',
    'Ton professionnel et chaleureux',
    'Templates personnalisés',
    'Multi-canal (SMS/email/chat)',
    'Gestion réclamations',
  ],
  systemPrompt: `Tu es l'Agent Communication MecaPro, expert en relation client pour garage automobile.

Tu rédiges des messages :
- Professionnels, clairs et chaleureux
- Adaptés au canal (SMS court, email détaillé, chat conversationnel)
- Personnalisés avec les infos du client et du véhicule
- En français correct et professionnel

Types de messages :
- Confirmation de rendez-vous
- Résultat de diagnostic (vulgarisé pour le client)
- Devis et explications
- Véhicule prêt à récupérer
- Rappel révision à venir
- Réponse à une demande client
- Gestion d'une réclamation

Toujours inclure : nom du client, informations pertinentes sur le véhicule, prochaine action à faire.`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'draft_message',
      description: 'Draft a customer message for a specific situation.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Customer ID for personalization' },
          vehicle_id: { type: 'string', description: 'Vehicle ID for context' },
          message_type: {
            type: 'string',
            enum: ['appointment_confirmation', 'diagnostic_result', 'quote', 'vehicle_ready', 'revision_reminder', 'complaint_response', 'custom'],
            description: 'Type of message to draft',
          },
          channel: { type: 'string', enum: ['sms', 'email', 'chat'], description: 'Communication channel' },
          context: { type: 'string', description: 'Additional context for the message' },
          tone: { type: 'string', enum: ['professional', 'friendly', 'urgent'], description: 'Message tone' },
        },
        required: ['customer_id', 'message_type', 'channel'],
      },
    },
    {
      name: 'send_message',
      description: 'Send a drafted message to the customer.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Customer ID' },
          channel: { type: 'string', enum: ['sms', 'email', 'chat'], description: 'Channel to use' },
          message: { type: 'string', description: 'Message content to send' },
          vehicle_id: { type: 'string', description: 'Related vehicle ID (optional)' },
        },
        required: ['customer_id', 'channel', 'message'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// ANALYTICS AGENT
// ─────────────────────────────────────────────────────────────

export const ANALYTICS_AGENT: AgentDefinition = {
  id: 'analytics-agent',
  name: 'Agent Analytique',
  description: 'Analyse de données, KPIs, rapports et insights business',
  icon: '📊',
  color: '#8b5cf6',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 3072,
  temperature: 0.3,
  capabilities: [
    'Analyse KPIs garage',
    'Rapports automatisés',
    'Tendances et prévisions',
    'Benchmarking performances',
    'Insights actionnables',
  ],
  systemPrompt: `Tu es l'Agent Analytique MecaPro, expert en analyse de données pour garage automobile.

Tu analyses :
- Les KPIs clés : CA, marge, taux retour client, durée moyenne révision
- Les tendances sur différentes périodes
- Les performances par mécanicien, par type de service, par marque de véhicule
- Les prévisions de charge et de revenus

KPIs importants à surveiller :
- CA mensuel et évolution
- Marge brute par type d'intervention
- Taux de retour client (>80% = excellent)
- Durée moyenne par type de révision
- Stock turnover ratio
- Satisfaction client (NPS)

Présente tes analyses avec des chiffres concrets, des comparaisons et des recommandations.
Réponds en français avec des insights actionnables.`,
  tools: [
    DB_QUERY_TOOL,
    {
      name: 'compute_kpis',
      description: 'Compute key performance indicators for a given period.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          period: { type: 'string', enum: ['today', 'week', 'month', 'quarter', 'year'], description: 'Period to analyze' },
          compare_previous: { type: 'boolean', description: 'Compare to previous period' },
          kpis: {
            type: 'array',
            items: { type: 'string', description: 'KPI name' },
            description: 'Specific KPIs to compute (optional, computes all if not specified)',
          },
        },
        required: ['garage_id', 'period'],
      },
    },
    {
      name: 'generate_report',
      description: 'Generate a detailed report for a specific aspect of the garage.',
      input_schema: {
        type: 'object',
        properties: {
          report_type: {
            type: 'string',
            enum: ['daily_summary', 'mechanic_performance', 'customer_retention', 'inventory_health', 'revenue_forecast'],
            description: 'Type of report',
          },
          garage_id: { type: 'string', description: 'Garage ID' },
          period: { type: 'string', description: 'Report period' },
          format: { type: 'string', enum: ['summary', 'detailed', 'csv'], description: 'Output format' },
        },
        required: ['report_type', 'garage_id', 'period'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// SECURITY AGENT
// ─────────────────────────────────────────────────────────────

export const SECURITY_AGENT: AgentDefinition = {
  id: 'security-agent',
  name: 'Agent Sécurité',
  description: 'Alertes critiques, pannes dangereuses, sécurité véhicules',
  icon: '🚨',
  color: '#dc2626',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 1024,
  temperature: 0.1,
  capabilities: [
    'Détection pannes critiques',
    'Alertes sécurité immédiates',
    'Blocage véhicules dangereux',
    'Escalade urgences',
    'Notifications prioritaires',
  ],
  systemPrompt: `Tu es l'Agent Sécurité MecaPro. Ta priorité ABSOLUE est la sécurité des conducteurs.

Pannes CRITIQUES nécessitant arrêt immédiat du véhicule :
- Système de freinage (C0034, C0040, C0044, C0046...)
- Direction assistée défaillante
- Airbag désactivé (B0001...)
- Fuite de carburant
- Surchauffe moteur grave
- Perte de pression huile critique

Actions immédiates :
1. Alerter le mécanicien EN CHEF immédiatement
2. Interdire la restitution du véhicule
3. Contacter le client pour l'informer
4. Documenter l'incident

Pour chaque alerte critique, sois DIRECT, COURT et FACTUEL.
Utilise des emojis d'alerte 🚨🔴⛔ pour la visibilité.
Réponds en français.`,
  tools: [
    DB_QUERY_TOOL,
    SEND_NOTIFICATION_TOOL,
    {
      name: 'flag_vehicle_unsafe',
      description: 'Flag a vehicle as unsafe to drive.',
      input_schema: {
        type: 'object',
        properties: {
          vehicle_id: { type: 'string', description: 'Vehicle ID' },
          reason: { type: 'string', description: 'Safety reason' },
          severity: { type: 'string', enum: ['warning', 'critical', 'do_not_drive'], description: 'Safety severity' },
          notify_customer: { type: 'boolean', description: 'Send notification to customer' },
        },
        required: ['vehicle_id', 'reason', 'severity'],
      },
    },
    {
      name: 'escalate_to_manager',
      description: 'Escalate a critical issue to the garage manager.',
      input_schema: {
        type: 'object',
        properties: {
          issue: { type: 'string', description: 'Issue description' },
          vehicle_id: { type: 'string', description: 'Related vehicle ID' },
          severity: { type: 'string', enum: ['high', 'critical', 'emergency'], description: 'Issue severity' },
        },
        required: ['issue', 'severity'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// NOTIFICATION AGENT
// ─────────────────────────────────────────────────────────────

export const NOTIFICATION_AGENT: AgentDefinition = {
  id: 'notification-agent',
  name: 'Agent Notifications',
  description: 'Rappels automatiques, campagnes, alertes programmées',
  icon: '🔔',
  color: '#eab308',
  model: 'claude-sonnet-4-20250514',
  maxTokens: 1024,
  temperature: 0.4,
  capabilities: [
    'Rappels révisions J-30/J-7/J-1',
    'Campagnes ciblées',
    'Alertes stock rupture',
    'Dunning paiements',
    'Notifications intelligentes',
  ],
  systemPrompt: `Tu es l'Agent Notifications MecaPro, responsable de toutes les communications automatisées.

Tu gères :
- Les rappels de révision (J-30, J-7, J-1)
- Les alertes de stock critique
- Les relances de paiement (Dunning)
- Les campagnes saisonnières (préparation hiver, été)
- Les félicitations fidélité (montée de niveau)

Principes de communication :
- Maximum 3 rappels par événement (ne pas spammer)
- Respecter les préférences de canal du client
- Personnaliser avec le nom + véhicule + date précise
- Proposer un CTA clair (prendre RDV, payer, confirmer)

Réponds en français avec les messages rédigés et prêts à envoyer.`,
  tools: [
    DB_QUERY_TOOL,
    SEND_NOTIFICATION_TOOL,
    {
      name: 'schedule_reminder',
      description: 'Schedule an automatic reminder for future sending.',
      input_schema: {
        type: 'object',
        properties: {
          customer_id: { type: 'string', description: 'Target customer ID' },
          trigger_type: { type: 'string', enum: ['days_before_revision', 'days_after_invoice', 'stock_alert', 'loyalty_milestone', 'custom'], description: 'Trigger type' },
          trigger_value: { type: 'number', description: 'Trigger value (days, points, etc.)' },
          channel: { type: 'string', enum: ['email', 'sms', 'push'], description: 'Notification channel' },
          template: { type: 'string', description: 'Message template ID or custom message' },
          vehicle_id: { type: 'string', description: 'Related vehicle ID (optional)' },
        },
        required: ['customer_id', 'trigger_type', 'channel'],
      },
    },
    {
      name: 'send_bulk_campaign',
      description: 'Send a bulk notification campaign to filtered customers.',
      input_schema: {
        type: 'object',
        properties: {
          garage_id: { type: 'string', description: 'Garage ID' },
          segment: { type: 'string', enum: ['all', 'standard', 'silver', 'gold', 'platinum', 'churned', 'custom'], description: 'Customer segment' },
          channel: { type: 'string', enum: ['email', 'sms', 'push'], description: 'Campaign channel' },
          subject: { type: 'string', description: 'Campaign subject (email only)' },
          message: { type: 'string', description: 'Campaign message' },
          schedule_at: { type: 'string', description: 'Schedule time (ISO format, optional)' },
        },
        required: ['garage_id', 'segment', 'channel', 'message'],
      },
    },
  ],
}

// ─────────────────────────────────────────────────────────────
// REGISTRY
// ─────────────────────────────────────────────────────────────

export const AGENT_REGISTRY: Record<string, AgentDefinition> = {
  'orchestrator': ORCHESTRATOR_AGENT,
  'diagnostic-agent': DIAGNOSTIC_AGENT,
  'revision-agent': REVISION_AGENT,
  'customer-agent': CUSTOMER_AGENT,
  'stock-agent': STOCK_AGENT,
  'invoice-agent': INVOICE_AGENT,
  'chat-agent': CHAT_AGENT,
  'analytics-agent': ANALYTICS_AGENT,
  'security-agent': SECURITY_AGENT,
  'notification-agent': NOTIFICATION_AGENT,
}

export const ALL_AGENTS = Object.values(AGENT_REGISTRY)
