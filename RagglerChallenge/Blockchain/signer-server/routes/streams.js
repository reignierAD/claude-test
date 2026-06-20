/**
 * Moralis Streams API webhook receiver.
 *
 * Moralis calls POST /streams/webhook every time our GameRewards contract
 * emits StageRewardClaimed or EndlessRewardClaimed on BSC testnet.
 *
 * We verify the Moralis signature on each webhook, decode the event,
 * then push a real-time update to the correct Unity WebGL client
 * via the WebSocket server.
 */

const express   = require("express");
const Moralis   = require("moralis").default;
const wsServer  = require("../websocketServer");
const router    = express.Router();

// Moralis sends webhooks as raw JSON — must parse before signature check
router.post("/webhook", express.raw({ type: "application/json" }), async (req, res) => {
    try {
        // Moralis signature verification (prevents fake webhook calls)
        const signature = req.headers["x-signature"];
        Moralis.Streams.verifySignature({
            body: req.body,
            signature,
        });

        const body = JSON.parse(req.body.toString());

        // Moralis sends a test ping when the stream is first created
        if (body.confirmed === false && body.txs?.length === 0) {
            console.log("[Streams] Received test ping from Moralis.");
            return res.status(200).json({ received: true });
        }

        // Only process confirmed blocks (not unconfirmed)
        if (!body.confirmed) return res.status(200).json({ received: true });

        const logs = body.logs || [];
        const abi  = body.abis  || {};

        for (const log of logs) {
            const decoded = decodeLog(log, abi);
            if (!decoded) continue;

            const player = decoded.player?.toLowerCase();
            if (!player) continue;

            console.log(`[Streams] Event: ${decoded.event} player=${player} tokens=${decoded.tokensEarned}`);

            // Push to the connected Unity client for this wallet address
            wsServer.pushToPlayer(player, {
                type:         "reward",
                event:        decoded.event,
                stageId:      decoded.stageId   || 0,
                stars:        decoded.newStars   || decoded.stars || 0,
                tokensEarned: decoded.tokensEarned,
                txHash:       log.transactionHash,
            });
        }

        res.status(200).json({ received: true });
    } catch (err) {
        console.error("[Streams] Webhook error:", err.message);
        // Return 200 so Moralis doesn't retry indefinitely on our own errors
        res.status(200).json({ error: err.message });
    }
});

// ── Event decoder ─────────────────────────────────────────────────────────────

// Topic hashes for our two events (pre-computed from ABI):
//  StageRewardClaimed(address indexed player, uint8 stageId, uint8 newStars, uint8 prevBest, uint256 tokensEarned)
//  EndlessRewardClaimed(address indexed player, uint8 stars, uint256 tokensEarned)
const TOPICS = {
    "0x" + require("../utils/keccakTopic")("StageRewardClaimed(address,uint8,uint8,uint8,uint256)"):
        "StageRewardClaimed",
    "0x" + require("../utils/keccakTopic")("EndlessRewardClaimed(address,uint8,uint256)"):
        "EndlessRewardClaimed",
};

function decodeLog(log, _abi) {
    const eventName = TOPICS[log.topic0];
    if (!eventName) return null;

    // Moralis decodes the log data for us if ABI is registered on the stream.
    // If not, we fall back to raw hex parsing.
    if (log.decoded) {
        const d = log.decoded;
        return {
            event:        eventName,
            player:       d.player,
            stageId:      Number(d.stageId  || 0),
            newStars:     Number(d.newStars  || d.stars || 0),
            tokensEarned: d.tokensEarned,
        };
    }

    // Fallback raw decode (topics[1] = indexed player address)
    return {
        event:        eventName,
        player:       "0x" + log.topic1?.slice(-40),
        tokensEarned: "unknown",
    };
}

module.exports = router;
