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
app.post('/api/invoke', async (req, res) => {
    const { message, sessionId, context } = req.body;
    
    // Set headers for Server-Sent Events (SSE)
    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');

    const encoder = new TextEncoder();
    const sendEvent = (data: any) => {
        res.write(`data: ${JSON.stringify(data)}\n\n`);
    };

    try {
        const engine = new OrchestratorEngine();
        await engine.process(
            {
                userMessage: message,
                sessionId: sessionId || `session_${Date.now()}`,
                context: context || {},
            },
            (event) => {
                sendEvent(event);
            }
        );
    } catch (err) {
        console.error('AI Engine Error:', err);
        sendEvent({ event: 'error', data: { message: err instanceof Error ? err.message : 'Unknown error' } });
    } finally {
        res.end();
    }
});

app.get('/health', (req, res) => {
    res.json({ status: 'MecaPro AI Agent Online', port });
});

app.listen(port, () => {
    console.log(`[AI-AGENT] Running on http://localhost:${port}`);
});
