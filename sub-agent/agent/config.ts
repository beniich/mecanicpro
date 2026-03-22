// ============================================================
// package.json
// ============================================================
{
  "name": "mecapro-ai-agents",
  "version": "1.0.0",
  "private": true,
  "description": "MecaPro AI Sub-Agent System — Multi-agent orchestration for garage management",
  "scripts": {
    "dev": "next dev --turbo",
    "build": "next build",
    "start": "next start",
    "type-check": "tsc --noEmit",
    "lint": "next lint",
    "test:agents": "tsx scripts/test-agents.ts"
  },
  "dependencies": {
    "next": "14.2.5",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "@anthropic-ai/sdk": "^0.27.3",
    "ai": "^3.4.7",
    "zod": "^3.23.8"
  },
  "devDependencies": {
    "typescript": "^5.5.4",
    "@types/node": "^20.14.12",
    "@types/react": "^18.3.3",
    "@types/react-dom": "^18.3.0",
    "tsx": "^4.16.2",
    "tailwindcss": "^3.4.7",
    "postcss": "^8.4.40",
    "autoprefixer": "^10.4.19"
  }
}

// ============================================================
// .env.local.example
// ============================================================
/*
# Anthropic API Key (required)
ANTHROPIC_API_KEY=sk-ant-api03-...

# Internal MecaPro API (for tool calls)
INTERNAL_API_URL=http://localhost:5000
INTERNAL_API_KEY=your-internal-api-key

# Model configuration
ORCHESTRATOR_MODEL=claude-sonnet-4-20250514
AGENT_MODEL=claude-sonnet-4-20250514
MAX_TOKENS_ORCHESTRATOR=4096
MAX_TOKENS_AGENTS=2048

# Feature flags
ENABLE_PARALLEL_AGENTS=true
ENABLE_STREAMING=true
MAX_AGENT_ITERATIONS=10
*/

// ============================================================
// tsconfig.json
// ============================================================
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["dom", "dom.iterable", "esnext"],
    "allowJs": true,
    "skipLibCheck": true,
    "strict": true,
    "noEmit": true,
    "esModuleInterop": true,
    "module": "esnext",
    "moduleResolution": "bundler",
    "resolveJsonModule": true,
    "isolatedModules": true,
    "jsx": "preserve",
    "incremental": true,
    "plugins": [{ "name": "next" }],
    "paths": { "@/*": ["./*"] }
  },
  "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx", ".next/types/**/*.ts"],
  "exclude": ["node_modules"]
}

// ============================================================
// scripts/test-agents.ts — CLI test script
// ============================================================
import { OrchestratorEngine } from '../orchestrator/engine'

const TEST_CASES = [
  {
    name: 'Analyse panne critique',
    message: "Le véhicule MN-012-OP a une panne C0034 (circuit frein). Que faut-il faire ?",
  },
  {
    name: 'Rapport journalier',
    message: "Génère le rapport de performance d'aujourd'hui pour le garage.",
  },
  {
    name: 'Gestion stock',
    message: "Vérifie les stocks et alerte sur les ruptures critiques.",
  },
]

async function runTests() {
  const engine = new OrchestratorEngine()

  for (const test of TEST_CASES) {
    console.log(`\n${'='.repeat(60)}`)
    console.log(`TEST: ${test.name}`)
    console.log(`${'='.repeat(60)}`)
    console.log(`User: ${test.message}\n`)

    const startTime = Date.now()

    const result = await engine.process(
      { userMessage: test.message, sessionId: `test_${Date.now()}` },
      (event) => {
        const { event: type, data } = event
        if (type === 'thinking') {
          const d = data as { agentId: string; text: string }
          console.log(`  [${d.agentId}] 💭 ${d.text.slice(0, 80)}...`)
        } else if (type === 'tool_call') {
          const d = data as { agentId: string; toolName: string }
          console.log(`  [${d.agentId}] 🔧 ${d.toolName}`)
        } else if (type === 'agent_done') {
          const d = data as { agentId: string; tokens: number }
          console.log(`  [${d.agentId}] ✅ Done (${d.tokens} tokens)`)
        }
      }
    )

    console.log(`\n📝 Réponse finale:\n${result.finalAnswer}`)
    console.log(`\n📊 Stats: ${result.totalTokens} tokens, ${((Date.now() - startTime) / 1000).toFixed(1)}s`)
  }
}

runTests().catch(console.error)


// ============================================================
// ARCHITECTURE OVERVIEW (README)
// ============================================================
/*
# MecaPro AI Sub-Agent System

## Architecture

```
User Request
     │
     ▼
┌─────────────────────────────────────────┐
│         ORCHESTRATEUR (Claude)          │
│  - Analyse la demande                   │
│  - Crée un plan d'exécution             │
│  - Délègue aux agents spécialisés       │
│  - Synthétise les résultats             │
└──────────────┬──────────────────────────┘
               │  delegate_to_agent()
       ┌───────┴────────┐
       │                │
       ▼                ▼
┌──────────────┐  ┌──────────────────┐
│  diagnostic  │  │   revision       │
│   -agent     │  │    -agent        │
│ (Claude)     │  │  (Claude)        │
│ Tools:       │  │ Tools:           │
│ - analyze_   │  │ - schedule_      │
│   fault_code │  │   revision       │
│ - get_repair │  │ - check_         │
│   _estimate  │  │   availability   │
└──────┬───────┘  └────────┬─────────┘
       │                   │
       └─────────┬─────────┘
                 │
          ┌──────▼──────┐
          │ Tool Executor│
          │  (Real APIs) │
          │ - DB queries │
          │ - Send notif │
          │ - External   │
          │   services   │
          └─────────────┘
```

## Agents disponibles

| Agent | Spécialité | Outils |
|-------|-----------|--------|
| 🎯 Orchestrateur | Coordination, planification | delegate_to_agent, create_plan |
| 🔍 Diagnostic | Codes OBD, pannes | analyze_fault_code, get_repair_estimate |
| 📅 Révisions | Planification, suivi | schedule_revision, check_availability |
| 👥 Clientèle | CRM, fidélité | get_customer_360, calculate_churn_risk |
| 📦 Stock | Pièces, ruptures | check_stock_levels, create_purchase_order |
| 🧾 Facturation | Devis, factures | generate_quote, create_invoice |
| 💬 Communication | Messages clients | draft_message, send_message |
| 📊 Analytique | KPIs, rapports | compute_kpis, generate_report |
| 🚨 Sécurité | Alertes critiques | flag_vehicle_unsafe, escalate_to_manager |
| 🔔 Notifications | Rappels, campagnes | schedule_reminder, send_bulk_campaign |

## Utilisation

### 1. Installation
```bash
npm install @anthropic-ai/sdk
cp .env.local.example .env.local
# Remplir ANTHROPIC_API_KEY
```

### 2. API Route (Next.js)
L'endpoint SSE est disponible à : POST /api/ai-agent

### 3. Intégration UI
```tsx
import AgentPanel from '@/ui/AgentPanel'

export default function AiPage() {
  return <AgentPanel />
}
```

### 4. Utilisation directe
```typescript
import { OrchestratorEngine } from './orchestrator/engine'

const engine = new OrchestratorEngine()
const result = await engine.process({
  userMessage: "Analyse la panne P0301 sur le véhicule AB-123",
  sessionId: "session_001"
})
console.log(result.finalAnswer)
```

### 5. Ajouter un outil custom
```typescript
const engine = new OrchestratorEngine()
engine.toolExecutor.register('my_custom_tool', async (input) => {
  // Votre logique ici
  return { result: 'données' }
})
```

## Exemples de requêtes

1. **Panne critique** : "Le véhicule MN-012 a un code C0034 (circuit frein). Que faire ?"
   → security-agent + diagnostic-agent + customer-agent (en parallèle)

2. **Analyse clientèle** : "Identifie les clients à risque de churn et prépare une campagne"
   → customer-agent + notification-agent

3. **Rapport complet** : "Génère le bilan de la semaine avec recommandations"
   → analytics-agent + stock-agent + invoice-agent (parallèle)

4. **Gestion stock** : "Vérifie les stocks avant les révisions de demain"
   → stock-agent + revision-agent

## Flux SSE (Streaming)

```
data: {"event":"plan","data":{"goal":"...","tasks":[...]}}
data: {"event":"agent_start","data":{"agentId":"diagnostic-agent","task":"..."}}
data: {"event":"thinking","data":{"agentId":"diagnostic-agent","text":"..."}}
data: {"event":"tool_call","data":{"toolName":"analyze_fault_code","input":{...}}}
data: {"event":"tool_result","data":{"toolName":"analyze_fault_code","result":"..."}}
data: {"event":"agent_done","data":{"agentId":"diagnostic-agent","tokens":342}}
data: {"event":"final_answer","data":{"text":"Voici l'analyse..."}}
data: {"event":"done","data":{"totalTokens":1247,"durationMs":4821}}
```
*/
