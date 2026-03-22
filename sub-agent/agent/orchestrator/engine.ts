import Anthropic from '@anthropic-ai/sdk';
import { AgentId, AgentMessage, StreamEvent, OrchestratorPlan, AgentTask } from '../types/agents';
import { agentDefinitions } from '../agents/definitions';

const anthropic = new Anthropic({
  apiKey: process.env.ANTHROPIC_API_KEY,
});

class ToolExecutor {
  async execute(toolName: string, input: any): Promise<any> {
    // Dans une vraie app, on appelle la BD, Stripe, Sendgrid, etc.
    console.log(`Exécution outil ${toolName} avec args:`, input);
    return { success: true, timestamp: new Date().toISOString(), data: `Mock result for ${toolName}` };
  }
}

export class SubAgentRunner {
  private exucutor = new ToolExecutor();

  async runAgent(agentId: AgentId, userMessage: string, onEvent: (e: StreamEvent) => void): Promise<string> {
    const agent = agentDefinitions[agentId];
    if (!agent) throw new Error(`Agent ${agentId} inconnu`);

    onEvent({ type: 'agent_state_change', agentId, state: 'thinking' });

    // Build the request
    const messages: Anthropic.MessageParam[] = [{ role: 'user', content: userMessage }];

    // Simple runner without fully recursive tool loops for demonstration
    // Real loop would check response.stop_reason === 'tool_use'
    try {
      const response = await anthropic.messages.create({
        model: 'claude-3-opus-20240229',
        system: agent.systemPrompt,
        max_tokens: 1500,
        messages: messages,
        tools: agent.tools.map(t => ({
          name: t.name,
          description: t.description,
          input_schema: t.inputSchema
        }))
      });

      let finalContent = "";

      for (const content of response.content) {
        if (content.type === 'text') {
          finalContent += content.text + "\n";
          onEvent({ type: 'token', agentId, content: content.text });
        } else if (content.type === 'tool_use') {
          onEvent({ type: 'tool_call_start', agentId, toolName: content.name, toolInput: content.input });
          const result = await this.exucutor.execute(content.name, content.input);
          onEvent({ type: 'tool_call_result', agentId, toolName: content.name, toolResult: result });
        }
      }

      onEvent({ type: 'agent_state_change', agentId, state: 'done' });
      onEvent({ type: 'done', agentId });
      
      return finalContent;
    } catch (err: any) {
      onEvent({ type: 'error', agentId, error: err.message, state: 'error' });
      throw err;
    }
  }
}

export class OrchestratorEngine {
  private runner = new SubAgentRunner();

  async handleUserRequest(request: string, onEvent: (e: StreamEvent) => void) {
    onEvent({ type: 'agent_state_change', agentId: 'orchestrator', state: 'thinking' });

    // Step 1: Orchestrator analyzes
    const orchTask = await this.runner.runAgent('orchestrator', `Veuillez analyser cette requête et utiliser vos outils pour déléguer si besoin : ${request}`, onEvent);
    
    // In a full implementation, you'd parse Orchestrator's plan and run agents in parallel.
    // For this boilerplate, the runner handles basic dispatching.
    
    // Example pseudo-execution:
    /*
    const plan: OrchestratorPlan = parsePlan(orchTask);
    await Promise.all(plan.tasks.map(task => 
      this.runner.runAgent(task.agentId, task.instruction, onEvent)
    ));
    */
  }
}
