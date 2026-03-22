import React, { useState, useEffect } from 'react';
import { AgentId, AgentState, StreamEvent } from '../types/agents';
import { agentDefinitions } from '../agents/definitions';

const AgentPanel = () => {
  const [prompt, setPrompt] = useState("");
  const [logs, setLogs] = useState<{ id: number, text: string }[]>([]);
  const [agentStates, setAgentStates] = useState<Partial<Record<AgentId, AgentState>>>({});

  const handleSend = async () => {
    if (!prompt) return;
    setLogs(prev => [...prev, { id: Date.now(), text: `User: ${prompt}` }]);
    
    // Initialise l'orchestrateur
    setAgentStates(prev => ({ ...prev, orchestrator: 'thinking' }));

    // Dans une vraie application, cela appellerait la route /api/ai-agent via EventSource ou fetch streaming
    // Exemple d'intégration SSE :
    /*
    const response = await fetch('/api/ai-agent', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ prompt })
    });
    
    const reader = response.body?.getReader();
    // Decode stream...
    */
    
    // Simulation du comportement attendu :
    setTimeout(() => {
        setLogs(prev => [...prev, { id: Date.now(), text: `Orchestrateur: J'analyse la requête... Délégation à 'diagnostic'.` }]);
        setAgentStates(prev => ({ ...prev, orchestrator: 'done', diagnostic: 'thinking' }));
        
        setTimeout(() => {
           setLogs(prev => [...prev, { id: Date.now(), text: `Tool Call (diagnostic): 'analyze_fault_code' (P0300)` }]);
           setAgentStates(prev => ({ ...prev, diagnostic: 'tool_call' }));
           
           setTimeout(() => {
               setLogs(prev => [...prev, { id: Date.now(), text: `Diagnostic IA: Le défaut P0300 correspond à des ratés d'allumage multiples.` }]);
               setAgentStates(prev => ({ ...prev, diagnostic: 'done' }));
           }, 1500);
        }, 1500);
    }, 1000);
    
    setPrompt("");
  };

  return (
    <div className="flex h-screen bg-[#07080a] text-[#e2e2e8] font-['DM_Sans']">
      
      {/* Sidebar - État des agents */}
      <div className="w-64 border-r border-[#524534]/30 bg-[#111318] p-4 flex flex-col gap-2 overflow-y-auto">
        <h2 className="text-[#f5a623] font-black uppercase tracking-widest text-lg mb-4 font-['Bebas_Neue']">Système Agentique</h2>
        
        {Object.values(agentDefinitions).map(a => (
          <div key={a.id} className="p-3 bg-[#1e2024] rounded-lg border border-[#333539] flex items-center justify-between">
            <span className="text-sm font-semibold">{a.name}</span>
            <div className={`w-3 h-3 rounded-full ${
              agentStates[a.id] === 'thinking' ? 'bg-[#ffb955] animate-pulse shadow-[0_0_8px_#ffb955]' :
              agentStates[a.id] === 'tool_call' ? 'bg-[#ffb596] animate-pulse' :
              agentStates[a.id] === 'done' ? 'bg-green-500' :
              'bg-[#37393e]'
            }`} />
          </div>
        ))}
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col">
        {/* Chat Logs */}
        <div className="flex-1 p-6 overflow-y-auto space-y-4">
          {logs.map(log => (
            <div key={log.id} className={`p-4 rounded-xl max-w-3xl ${log.text.startsWith('User:') ? 'bg-[#f5a623]/10 ml-auto border border-[#f5a623]/30 text-[#ffddb4]' : 'bg-[#1a1c20] border border-[#333539] text-[#d0d2db]'}`}>
              <div className="font-['JetBrains_Mono'] text-sm break-words">{log.text}</div>
            </div>
          ))}
          {logs.length === 0 && (
            <div className="flex items-center justify-center h-full text-[#45474f]">
              <div className="text-center">
                <span className="material-symbols-outlined text-4xl mb-2 text-[#45474f]">robot_2</span>
                <p>Posez une question technique ou demandez une analyse.</p>
              </div>
            </div>
          )}
        </div>

        {/* Input Area */}
        <div className="p-4 bg-[#111318] border-t border-[#524534]/30">
          <div className="flex gap-2 max-w-4xl mx-auto">
            <input 
              type="text" 
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSend()}
              placeholder="Ex: Analyse le code OBD P0300 du client A..."
              className="flex-1 bg-[#1a1c20] border border-[#333539] p-3 rounded-lg focus:outline-none focus:border-[#f5a623] text-sm"
            />
            <button 
              onClick={handleSend}
              className="px-6 py-3 bg-[#f5a623] text-[#452b00] font-bold rounded-lg hover:bg-[#ffb955] transition-colors"
            >
              Envoyer
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AgentPanel;
