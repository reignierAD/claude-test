/**
 * Raggler's Challenge — Game Backend Server
 *
 * Responsibilities:
 *  1. Sign game results with GAME_SIGNER_PRIVATE_KEY (on-chain reward claims)
 *  2. Moralis Auth API  — challenge/verify wallet login, issue JWTs
 *  3. Moralis Token API — RAGG balance + USD value proxy
 *  4. Moralis Wallet API — RAGG transfer history proxy
 *  5. Moralis Streams   — receive on-chain event webhooks, push to Unity via WS
 *  6. WebSocket server  — real-time push channel to Unity WebGL clients
 */

require("dotenv").config();
const http    = require("http");
const express = require("express");
const cors    = require("cors");
const { ethers } = require("ethers");
const Moralis    = require("moralis").default;

const wsServer   = require("./websocketServer");
const authRoutes = require("./routes/auth");
const apiRoutes  = require("./routes/moralisApi");
const streamRoutes = require("./routes/streams");
const { verifyJwt } = require("./middleware/verifyJwt");

const app    = express();
const server = http.createServer(app);

// ── Middleware ────────────────────────────────────────────────────────────────

app.use(cors({ origin: process.env.ALLOWED_ORIGIN || "*" }));

// NOTE: /streams/webhook uses express.raw() internally — do NOT put
// express.json() before it globally or signature verification will break.
app.use((req, res, next) => {
    if (req.path === "/streams/webhook") return next();
    express.json()(req, res, next);
});

// ── Signer wallet ─────────────────────────────────────────────────────────────

const signerWallet = new ethers.Wallet(process.env.GAME_SIGNER_PRIVATE_KEY);
const TOTAL_STAGES = 10;
const BSC_TESTNET  = 97;

// ── Sign-reward endpoint (protected by JWT after Moralis Auth) ────────────────

app.post("/sign-reward", verifyJwt, async (req, res) => {
    try {
        const { stageId, stars, nonce, isEndless, chainId } = req.body;
        const player = req.player.address;   // pulled from verified JWT

        if (chainId !== BSC_TESTNET)
            return res.status(400).json({ error: "Wrong chain — BSC Testnet only" });
        if (stars < 1 || stars > 3)
            return res.status(400).json({ error: "Invalid stars" });
        if (!isEndless && (stageId < 1 || stageId > TOTAL_STAGES))
            return res.status(400).json({ error: "Invalid stageId" });

        const packedHash = ethers.keccak256(
            ethers.solidityPacked(
                ["address", "uint8", "uint8", "uint256", "bool", "uint256"],
                [player, stageId, stars, BigInt(nonce), isEndless, BigInt(chainId)]
            )
        );
        const sig = await signerWallet.signMessage(ethers.getBytes(packedHash));

        console.log(`[Sign] player=${player} stage=${stageId} stars=${stars} nonce=${nonce} endless=${isEndless}`);
        res.json({ sig });
    } catch (err) {
        console.error("[Sign] error:", err);
        res.status(500).json({ error: "Internal server error" });
    }
});

// ── Route modules ─────────────────────────────────────────────────────────────

app.use("/auth",    authRoutes);
app.use("/moralis", apiRoutes);
app.use("/streams", streamRoutes);

// ── Health check ──────────────────────────────────────────────────────────────

app.get("/health", (_, res) => res.json({
    status: "ok",
    signer: signerWallet.address,
    moralis: !!process.env.MORALIS_API_KEY,
}));

// ── Boot ──────────────────────────────────────────────────────────────────────

async function start() {
    await Moralis.start({ apiKey: process.env.MORALIS_API_KEY });
    console.log("[Moralis] SDK initialised");

    wsServer.init(server);

    const PORT = process.env.PORT || 3001;
    server.listen(PORT, () => {
        console.log(`Server listening on :${PORT}`);
        console.log(`  Signer : ${signerWallet.address}`);
        console.log(`  WS     : ws://localhost:${PORT}/ws`);
    });
}

start().catch(err => { console.error("Startup error:", err); process.exit(1); });
