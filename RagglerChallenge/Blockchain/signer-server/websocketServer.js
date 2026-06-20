/**
 * WebSocket server — pushes real-time on-chain events to Unity WebGL clients.
 *
 * Unity connects on load: ws://your-server/ws
 * First message sent by Unity must be: { type: "register", address: "0x..." }
 * After that, the server pushes reward confirmations whenever Moralis
 * Streams fires a webhook for that wallet.
 */

const { WebSocketServer } = require("ws");

// Map: lowercased wallet address → WebSocket client
const clients = new Map();

let wss = null;

function init(server) {
    wss = new WebSocketServer({ server, path: "/ws" });

    wss.on("connection", (ws, req) => {
        let registeredAddress = null;

        console.log("[WS] Client connected from", req.socket.remoteAddress);

        ws.on("message", (data) => {
            try {
                const msg = JSON.parse(data.toString());

                if (msg.type === "register" && msg.address) {
                    registeredAddress = msg.address.toLowerCase();
                    clients.set(registeredAddress, ws);
                    console.log(`[WS] Registered wallet: ${registeredAddress}`);
                    ws.send(JSON.stringify({ type: "registered", address: registeredAddress }));
                }

                if (msg.type === "ping") ws.send(JSON.stringify({ type: "pong" }));
            } catch {
                // ignore malformed messages
            }
        });

        ws.on("close", () => {
            if (registeredAddress) {
                clients.delete(registeredAddress);
                console.log(`[WS] Disconnected: ${registeredAddress}`);
            }
        });

        ws.on("error", (err) => console.error("[WS] Error:", err.message));
    });

    console.log("[WS] WebSocket server initialised on /ws");
}

/**
 * Push a JSON payload to the Unity client registered under `playerAddress`.
 * Called by the Streams webhook handler.
 */
function pushToPlayer(playerAddress, payload) {
    const ws = clients.get(playerAddress.toLowerCase());
    if (!ws || ws.readyState !== ws.OPEN) {
        console.log(`[WS] No connected client for ${playerAddress}`);
        return false;
    }
    ws.send(JSON.stringify(payload));
    return true;
}

/**
 * Broadcast to all connected clients (e.g. global event announcements).
 */
function broadcast(payload) {
    const msg = JSON.stringify(payload);
    for (const ws of clients.values())
        if (ws.readyState === ws.OPEN) ws.send(msg);
}

module.exports = { init, pushToPlayer, broadcast };
