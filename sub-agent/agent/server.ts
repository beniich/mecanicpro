import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { OrchestratorEngine } from './engine';

dotenv.config();

const app = express();
const port = 3001;

app.use(cors());
app.use(express.json());

// Main invocation endpoint used by the C# proxy
// Main streaming invocation endpoint (SSE)
app.post('/api/invoke', async (req, res) => {
    const { message, sessionId, context } = req.body;
    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');

    const encoder = new TextEncoder();
    const sendEvent = (data: any) => res.write(`data: ${JSON.stringify(data)}\n\n`);

    try {
        const engine = new OrchestratorEngine();
        await engine.process(
            { userMessage: message, sessionId: sessionId || `session_${Date.now()}`, context: context || {} },
            (event) => sendEvent(event)
        );
    } catch (err) {
        sendEvent({ event: 'error', data: { message: (err as Error).message } });
    } finally {
        res.end();
    }
});

// Synchronous invocation for tools/vision
app.post('/api/invoke-sync', async (req, res) => {
    const { message, sessionId, context } = req.body;
    try {
        const engine = new OrchestratorEngine();
        // Catch final events to return as JSON
        let finalOutput = '';
        await engine.process(
            { userMessage: message, sessionId: sessionId || `sync_${Date.now()}`, context: context || {} },
            (event) => { if (event.event === 'final_answer') finalOutput = event.data.text; }
        );
        res.json({ output: finalOutput, success: true });
    } catch (err) {
        res.status(500).json({ error: (err as Error).message });
    }
});

app.get('/health', (req, res) => {
    res.json({ status: 'MecaPro AI Agent Online', port });
});

app.listen(port, () => {
    console.log(`[AI-AGENT] Running on http://localhost:${port}`);
});
