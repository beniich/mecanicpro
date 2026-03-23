// ============================================================
// orchestrator/engine.ts — Core Orchestration Engine
// ============================================================

import Anthropic from '@anthropic-ai/sdk'
import Redis from 'ioredis'
import type {
  AgentId, AgentTask, AgentEvent, AgentState, AgentMessage,
  OrchestratorRequest, OrchestratorResponse, StreamEvent,
  ContentBlock, ToolCallRecord
} from './types/agents'
import { AGENT_REGISTRY, ORCHESTRATOR_AGENT } from './agents/definitions'

const client = new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY })

// ─── Singleton Redis — connexion réutilisée entre les requêtes ────────────────
const redis = new Redis(process.env.REDIS_URL || 'redis://localhost:6379', {
  lazyConnect: true,          // Ne pas bloquer si Redis est indisponible
  enableOfflineQueue: false,  // Rejeter immédiatement si hors-ligne (pas de queue)
  maxRetriesPerRequest: 1,    // Ne pas ré-essayer indéfiniment
  connectTimeout: 2000,       // Timeout de connexion : 2 secondes
})
redis.on('error', (e) => { /* ignore — Redis optionnel (caching seulement) */ })

// ─────────────────────────────────────────────────────────────
// SUB-AGENT RUNNER
// ─────────────────────────────────────────────────────────────

export class SubAgentRunner {
  private toolExecutor: ToolExecutor
  private onEvent?: (event: AgentEvent) => void

  constructor(toolExecutor: ToolExecutor, onEvent?: (event: AgentEvent) => void) {
    this.toolExecutor = toolExecutor
    this.onEvent = onEvent
  }

  async run(
    agentId: AgentId,
    task: string,
    context: Record<string, unknown> = {},
    taskId?: string
  ): Promise<{ output: string; tokensUsed: number; toolCalls: ToolCallRecord[] }> {
    const agentDef = AGENT_REGISTRY[agentId]
    if (!agentDef) throw new Error(`Agent '${agentId}' not found`)

    const startTime = Date.now()
    const toolCalls: ToolCallRecord[] = []
    let totalTokens = 0

    this.emit({ type: 'agent_started', agentId, taskId, data: { task }, timestamp: new Date() })

    const messages: Anthropic.MessageParam[] = [
      {
        role: 'user',
        content: `${task}\n\nContexte: ${JSON.stringify(context, null, 2)}`,
      },
    ]

    // Agentic loop — continue until no more tool calls
    while (true) {
      const response = await client.messages.create({
        model: agentDef.model,
        max_tokens: agentDef.maxTokens,
        temperature: agentDef.temperature,
        system: agentDef.systemPrompt,
        tools: agentDef.tools.map(t => ({
          name: t.name,
          description: t.description,
          input_schema: t.inputSchema
        })) as Anthropic.Tool[],
        messages,
      })

      totalTokens += (response.usage.input_tokens + response.usage.output_tokens)

      // Emit thinking event for text blocks
      for (const block of response.content) {
        if (block.type === 'text') {
          this.emit({
            type: 'agent_thinking',
            agentId,
            taskId,
            data: { text: block.text },
            timestamp: new Date(),
          })
        }
      }

      // If stop reason is end_turn or no tool calls, we're done
      if (response.stop_reason === 'end_turn') {
        const text = response.content
          .filter((b) => b.type === 'text')
          .map((b) => (b as { type: 'text'; text: string }).text)
          .join('\n')

        this.emit({
          type: 'agent_done',
          agentId,
          taskId,
          data: { output: text, tokens: totalTokens, durationMs: Date.now() - startTime },
          timestamp: new Date(),
        })

        return { output: text, tokensUsed: totalTokens, toolCalls }
      }

      // Process tool calls
      const toolUseBlocks = response.content.filter((b) => b.type === 'tool_use') as Anthropic.ToolUseBlock[]

      if (toolUseBlocks.length === 0) {
        // Fallback: extract text
        const text = response.content
          .filter((b) => b.type === 'text')
          .map((b) => (b as { type: 'text'; text: string }).text)
          .join('\n')
        return { output: text, tokensUsed: totalTokens, toolCalls }
      }

      // Add assistant message
      messages.push({ role: 'assistant', content: response.content })

      // Execute all tool calls in parallel
      const toolResults = await Promise.all(
        toolUseBlocks.map(async (toolUse) => {
          const callRecord: ToolCallRecord = {
            id: toolUse.id,
            toolName: toolUse.name,
            input: toolUse.input as Record<string, unknown>,
            status: 'running',
          }
          toolCalls.push(callRecord)

          this.emit({
            type: 'tool_called',
            agentId,
            taskId,
            data: { toolName: toolUse.name, input: toolUse.input },
            timestamp: new Date(),
          })

          const start = Date.now()
          try {
            const result = await this.toolExecutor.execute(toolUse.name, toolUse.input as Record<string, unknown>)
            callRecord.output = JSON.stringify(result)
            callRecord.status = 'success'
            callRecord.durationMs = Date.now() - start

            this.emit({
              type: 'tool_result',
              agentId,
              taskId,
              data: { toolName: toolUse.name, result: callRecord.output, success: true },
              timestamp: new Date(),
            })

            return {
              type: 'tool_result' as const,
              tool_use_id: toolUse.id,
              content: callRecord.output,
            } as Anthropic.ToolResultBlockParam
          } catch (err) {
            const errMsg = err instanceof Error ? err.message : 'Unknown error'
            callRecord.status = 'error'
            callRecord.error = errMsg
            callRecord.durationMs = Date.now() - start

            this.emit({
              type: 'tool_result',
              agentId,
              taskId,
              data: { toolName: toolUse.name, result: errMsg, success: false },
              timestamp: new Date(),
            })

            return {
              type: 'tool_result' as const,
              tool_use_id: toolUse.id,
              content: `Error: ${errMsg}`,
              is_error: true,
            } as Anthropic.ToolResultBlockParam
          }
        })
      )

      // Add tool results to messages
      messages.push({ role: 'user', content: toolResults })
    }
  }

  private emit(event: AgentEvent) {
    this.onEvent?.(event)
  }
}

// ─────────────────────────────────────────────────────────────
// ORCHESTRATOR ENGINE
// ─────────────────────────────────────────────────────────────

export class OrchestratorEngine {
  private toolExecutor: ToolExecutor
  private events: AgentEvent[] = []

  constructor() {
    this.toolExecutor = new ToolExecutor()
  }

  async process(
    request: OrchestratorRequest,
    onStream?: (event: StreamEvent) => void
  ): Promise<OrchestratorResponse> {
    const startTime = Date.now()
    this.events = []

    const agentResults: Record<string, {
      output: string; tokensUsed: number; toolCalls: ToolCallRecord[]
    }> = {}

    let totalTokens = 0

    // Emit handler
    const emit = (event: AgentEvent) => {
      this.events.push(event)
      if (onStream) {
        this.forwardEventToStream(event, onStream)
      }
    }

    const runner = new SubAgentRunner(this.toolExecutor, emit)

    // ── ORCHESTRATOR LOOP ──────────────────────────────────
    const orchestratorMessages: Anthropic.MessageParam[] = [
      {
        role: 'user',
        content: `Demande utilisateur: "${request.userMessage}"\n\nContexte: ${JSON.stringify(request.context ?? {}, null, 2)}`,
      },
    ]

    let finalAnswer = ''
    let iterations = 0
    const MAX_ITERATIONS = 10

    if (!process.env.ANTHROPIC_API_KEY || process.env.ANTHROPIC_API_KEY === 'YOUR_API_KEY') {
        // MOCK MODE
        onStream?.({ event: 'thinking', data: { agentId: 'orchestrator', text: "Analyse des codes OBD en cours (Mode Simulation)..." } });
        await new Promise(r => setTimeout(r, 1000));
        
        onStream?.({ event: 'agent_start', data: { agentId: 'diagnostic', task: "Analyse technique détaillée" } });
        await new Promise(r => setTimeout(r, 1500));
        
        const mockOutput = "Basé sur les codes P0301 (Raté cylindre 1) et P0171 (Mélange pauvre), je recommande de vérifier d'abord les bougies et bobines d'allumage. Une fuite d'air à l'admission est également probable.";
        onStream?.({ event: 'agent_done', data: { agentId: 'diagnostic', output: mockOutput, tokens: 150 } });
        
        onStream?.({ event: 'final_answer', data: { text: mockOutput } });
        onStream?.({ event: 'done', data: { totalTokens: 200, durationMs: 3000 } });
        
        return {
            sessionId: request.sessionId,
            finalAnswer: mockOutput,
            plan: { goal: request.userMessage, tasks: [], parallelGroups: [], estimatedTokens: 200, estimatedDurationMs: 3000 },
            agentResults: {} as any,
            events: this.events,
            totalTokens: 200,
            durationMs: 3000,
            success: true,
        };
    }

    while (iterations < MAX_ITERATIONS) {
      iterations++

      const response = await client.messages.create({
        model: ORCHESTRATOR_AGENT.model,
        max_tokens: ORCHESTRATOR_AGENT.maxTokens,
        temperature: ORCHESTRATOR_AGENT.temperature,
        system: ORCHESTRATOR_AGENT.systemPrompt,
        tools: ORCHESTRATOR_AGENT.tools.map(t => ({
          name: t.name,
          description: t.description,
          input_schema: t.inputSchema
        })) as Anthropic.Tool[],
        messages: orchestratorMessages,
      })

      totalTokens += response.usage.input_tokens + response.usage.output_tokens

      // Extract text blocks
      for (const block of response.content) {
        if (block.type === 'text') {
          emit({
            type: 'agent_thinking',
            agentId: 'orchestrator',
            data: { text: block.type === 'text' ? block.text : '' },
            timestamp: new Date(),
          })
        }
      }

      if (response.stop_reason === 'end_turn') {
        finalAnswer = response.content
        .filter((b): b is Anthropic.TextBlock => b.type === 'text')
        .map((b) => b.text)
        .join('\n')
        break
      }

      const toolUseBlocks = response.content.filter((b): b is Anthropic.ToolUseBlock => b.type === 'tool_use')
      if (toolUseBlocks.length === 0) {
        finalAnswer = response.content
        .filter((b): b is Anthropic.TextBlock => b.type === 'text')
        .map((b) => b.text)
        .join('\n')
        break
      }

      orchestratorMessages.push({ role: 'assistant', content: response.content })

      // Execute orchestrator tools
      const toolResults = await Promise.all(
        toolUseBlocks.map(async (toolUse) => {
          let result: string

          if (toolUse.name === 'delegate_to_agent') {
            const { agentId: agent_id, task, priority = 'normal', context = {} } = toolUse.input as {
              agentId: AgentId; task: string; priority?: string; context?: Record<string, unknown>
            }

            emit({
              type: 'task_delegated',
              agentId: 'orchestrator',
              data: { agentId: agent_id, task, priority },
              timestamp: new Date(),
            })

            onStream?.({ event: 'agent_start', data: { agentId: agent_id, task } })

            const agentResult = await runner.run(agent_id, task, {
              ...context,
              ...request.context,
            })

            agentResults[agent_id] = agentResult
            totalTokens += agentResult.tokensUsed

            onStream?.({
              event: 'agent_done',
              data: { agentId: agent_id, output: agentResult.output, tokens: agentResult.tokensUsed },
            })

            result = agentResult.output

          } else if (toolUse.name === 'create_plan') {
            const { goal, tasks } = toolUse.input as { goal: string; tasks: AgentTask[] }
            const plan = { goal, tasks, parallelGroups: [], estimatedTokens: 2000, estimatedDurationMs: 5000 }
            onStream?.({ event: 'plan', data: plan })
            result = `Plan créé avec ${tasks.length} tâche(s)`
          } else {
            result = await this.toolExecutor.execute(toolUse.name, toolUse.input as Record<string, unknown>)
              .then(r => JSON.stringify(r))
              .catch(e => `Error: ${e.message}`)
          }

          return {
            type: 'tool_result' as const,
            tool_use_id: toolUse.id,
            content: result,
          } as Anthropic.ToolResultBlockParam
        })
      )

      orchestratorMessages.push({ role: 'user', content: toolResults })
    }

    onStream?.({ event: 'final_answer', data: { text: finalAnswer } })
    onStream?.({ event: 'done', data: { totalTokens, durationMs: Date.now() - startTime } })

    return {
      sessionId: request.sessionId,
      finalAnswer,
      plan: { goal: request.userMessage, tasks: [], parallelGroups: [], estimatedTokens: totalTokens, estimatedDurationMs: Date.now() - startTime },
      agentResults: agentResults as any,
      events: this.events,
      totalTokens,
      durationMs: Date.now() - startTime,
      success: true,
    }
  }

  private forwardEventToStream(event: AgentEvent, onStream: (e: StreamEvent) => void) {
    switch (event.type) {
      case 'agent_thinking':
        onStream({ event: 'thinking', data: { agentId: event.agentId, text: event.data.text as string } })
        break
      case 'tool_called':
        onStream({ event: 'tool_call', data: { agentId: event.agentId, toolName: event.data.toolName as string, input: event.data.input as Record<string, unknown> } })
        break
      case 'tool_result':
        onStream({ event: 'tool_result', data: { agentId: event.agentId, toolName: event.data.toolName as string, result: event.data.result as string, success: event.data.success as boolean } })
        break
    }
  }
}


// ============================================================
// orchestrator/tools.ts — Tool Executor (DB, APIs, Services)
// ============================================================

export class ToolExecutor {
  private handlers: Record<string, (input: Record<string, unknown>) => Promise<unknown>> = {}

  constructor() {
    this.registerDefaultHandlers()
  }

  register(toolName: string, handler: (input: Record<string, unknown>) => Promise<unknown>) {
    this.handlers[toolName] = handler
  }

  async execute(toolName: string, input: Record<string, unknown>): Promise<unknown> {
    const handler = this.handlers[toolName]
    if (!handler) {
      console.warn(`Tool '${toolName}' not implemented, returning mock data`)
      return this.mockTool(toolName, input)
    }
    return handler(input)
  }

  private registerDefaultHandlers() {
    const internalFetch = async (path: string, options: RequestInit = {}) => {
      const baseUrl = process.env.INTERNAL_API_URL || 'http://localhost:5000'
      const url = `${baseUrl}${path.startsWith('/') ? '' : '/'}${path}`
      const controller = new AbortController()
      const timeout = setTimeout(() => controller.abort(), 10_000) // 10s timeout
      try {
        const response = await fetch(url, {
          ...options,
          signal: controller.signal,
          headers: {
            'Authorization': `Bearer ${process.env.INTERNAL_API_KEY || 'AI_INTERNAL_TOKEN'}`,
            'Content-Type': 'application/json',
            ...options.headers,
          },
        })
        if (!response.ok) {
          const text = await response.text()
          throw new Error(`API ${path} failed (${response.status}): ${text}`)
        }
        return response.json()
      } finally {
        clearTimeout(timeout)
      }
    }

    // ── Database queries ──────────────────────────────────
    this.register('query_database', async (input) => {
      const { query_type, filters = {}, limit = 10 } = input as { query_type: string; filters?: any; limit?: number }
      // Map query_type to real endpoints
      const endpointMap: Record<string, string> = {
        'customers': '/api/v1/customers',
        'vehicles': '/api/v1/vehicles',
        'parts': '/api/v1/parts',
        'revisions': '/api/v1/revisions',
        'invoices': '/api/v1/billing/invoices'
      }
      const path = endpointMap[query_type] || `/api/v1/${query_type}`
      return internalFetch(`${path}?pageSize=${limit}`)
    })

    // ── Diagnostic analysis (With Redis Caching) ─────────────────────────

    this.register('analyze_fault_code', async (input) => {
      const { code } = input as { code: string }
      const cacheKey = `diag_cache_${code}`

      try {
        const cached = await redis.get(cacheKey)
        if (cached) return JSON.parse(cached)
      } catch (e) { /* ignore cache error */ }

      // Real analysis (would be AI or logic)
      const mockResult = {
        code,
        description: `Analyse systémique du code ${code}`,
        severity: code.startsWith('P03') ? 'Critical' : 'Major',
        probableCauses: ['Bobine d\'allumage cylindre 1 HS', 'Bougie encrassée', 'Injection défaillante'],
        recommendedActions: ['Permutation bobine 1 & 2 pour test', 'Contrôle compression', 'Nettoyage injecteurs'],
        estimatedCost: 245.50,
        laborHours: 1.5
      }

      try { await redis.set(cacheKey, JSON.stringify(mockResult), 'EX', 3600) } catch (e) {}

      return mockResult
    })

    this.register('analyze_part_image', async (input) => {
      const { imageUrl, partType = 'pièce mécanique' } = input as { imageUrl: string; partType?: string }
      
      console.log(`[VISION] Analyzing ${partType} from image: ${imageUrl.substring(0, 50)}...`)
      
      // In a real scenario, we'd send the image to Claude 3.5's vision blocks
      // For this version, we return a high-quality simulated analysis based on the part type
      return {
        part: partType,
        diagnostics: [
          { type: 'Structural', observation: 'Fissure de fatigue détectée sur le bras de suspension', severity: 'Critical' },
          { type: 'Condition', observation: 'Corrosion galvanique modérée sur les points de fixation', severity: 'Medium' }
        ],
        aiConfidence: 0.94,
        recommendation: 'Arrêt immédiat du véhicule conseillé. Remplacement prioritaire de la pièce.',
        estimatedReplacementTime: '2.5 hours'
      }
    })

    // ── Send notification ─────────────────────────────────
    this.register('send_notification', async (input) => {
      const { userId, channel, title, body } = input as {
        userId: string; channel: string; title: string; body: string
      }
      // In production: call notification service (SendGrid, Twilio, etc.)
      console.log(`[Notification] ${channel} → User ${userId}: ${title}`)
      return { success: true, messageId: `msg_${Date.now()}`, sentAt: new Date().toISOString() }
    })

    // ── Flag vehicle unsafe ───────────────────────────────
    this.register('flag_vehicle_unsafe', async (input) => {
      const { vehicle_id, reason } = input as { vehicle_id: string; reason: string }
      return internalFetch(`/api/v1/vehicles/${vehicle_id}/safety-flag`, {
        method: 'POST',
        body: JSON.stringify({ reason, severity: 'Critical' })
      })
    })

    // ── Schedule revision ─────────────────────────────────
    this.register('schedule_revision', async (input) => {
      return internalFetch('/api/v1/revisions', {
        method: 'POST',
        body: JSON.stringify(input)
      })
    })

    // ── Generate quote ────────────────────────────────────
    this.register('generate_quote', async (input) => {
      const { vehicle_id, labor_items = [], parts = [], country = 'FR' } = input as {
        vehicle_id: string; labor_items?: Array<{ description: string; hours: number; rate_per_hour: number }>
        parts?: Array<{ part_id: string; quantity: number }>; country?: string
      }
      const vatRate: Record<string, number> = { FR: 0.20, BE: 0.21, DE: 0.19, CH: 0.081 }
      const vat = vatRate[country] ?? 0.20
      const laborTotal = labor_items.reduce((s, l) => s + l.hours * l.rate_per_hour, 0)
      const partsTotal = parts.reduce((s) => s + 50, 0) // mock parts cost
      const totalHT = laborTotal + partsTotal
      return {
        quoteNumber: `QT-${Date.now()}`,
        vehicleId: vehicle_id,
        laborTotal,
        partsTotal,
        totalHT,
        vatAmount: totalHT * vat,
        totalTTC: totalHT * (1 + vat),
        currency: 'EUR',
        validUntil: new Date(Date.now() + 30 * 86400000).toISOString(),
      }
    })

    // ── Stock levels ──────────────────────────────────────
    this.register('check_stock_levels', async (input) => {
      return internalFetch('/api/v1/parts/categories') // Fallback to categories or similar query
    })

    // ── Customer 360 ──────────────────────────────────────
    this.register('get_customer_360', async (input: any) => {
      return internalFetch(`/api/v1/customers/${input.customer_id}`)
    })

    // ── KPIs ──────────────────────────────────────────────
    this.register('compute_kpis', async () => {
      return internalFetch('/api/v1/dashboard/stats')
    })
  }

  private mockTool(toolName: string, input: Record<string, unknown>): unknown {
    return {
      tool: toolName,
      input,
      result: `Mock result for ${toolName}`,
      timestamp: new Date().toISOString(),
    }
  }
}



