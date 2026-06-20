/**
 * Game Signer Backend
 * Minimal Express server that holds the GAME_SIGNER_PRIVATE_KEY
 * and signs verified game results for on-chain claim submission.
 *
 * Deploy this on your own server (Render, Railway, VPS, etc.)
 * NEVER expose GAME_SIGNER_PRIVATE_KEY to the client.
 *
 * Install: npm install
 * Run:     node index.js
 */

require("dotenv").config();
const express = require("express");
const { ethers }  = require("ethers");
const cors    = require("cors");

const app = express();
app.use(express.json());
app.use(cors({ origin: process.env.ALLOWED_ORIGIN || "*" }));

const signerWallet = new ethers.Wallet(process.env.GAME_SIGNER_PRIVATE_KEY);
console.log("Signer address:", signerWallet.address);

const TOTAL_STAGES  = 10;
const BSC_TESTNET   = 97;

/**
 * POST /sign-reward
 * Body: { player, stageId, stars, nonce, isEndless, chainId }
 * Response: { sig }
 *
 * In production add rate limiting, session validation, and
 * verify the game result against your own authoritative game state.
 */
app.post("/sign-reward", async (req, res) => {
    try {
        const { player, stageId, stars, nonce, isEndless, chainId } = req.body;

        // ── Basic validation ─────────────────────────────────────────────────
        if (!ethers.isAddress(player))
            return res.status(400).json({ error: "Invalid player address" });
        if (chainId !== BSC_TESTNET)
            return res.status(400).json({ error: "Wrong chain — BSC Testnet only" });
        if (stars < 1 || stars > 3)
            return res.status(400).json({ error: "Invalid stars" });
        if (!isEndless && (stageId < 1 || stageId > TOTAL_STAGES))
            return res.status(400).json({ error: "Invalid stageId" });

        // ── Build the same hash the contract will verify ─────────────────────
        // Must match _verifySig() in GameRewards.sol exactly:
        // keccak256(abi.encodePacked(player, stageId, stars, nonce, isEndless, chainId))
        const packedHash = ethers.keccak256(
            ethers.solidityPacked(
                ["address", "uint8", "uint8", "uint256", "bool", "uint256"],
                [player, stageId, stars, BigInt(nonce), isEndless, BigInt(chainId)]
            )
        );

        // ethSignedMessageHash = "\x19Ethereum Signed Message:\n32" + hash
        const sig = await signerWallet.signMessage(ethers.getBytes(packedHash));

        console.log(`Signed: player=${player} stage=${stageId} stars=${stars} nonce=${nonce} endless=${isEndless}`);
        res.json({ sig });

    } catch (err) {
        console.error("Sign error:", err);
        res.status(500).json({ error: "Internal server error" });
    }
});

app.get("/health", (_, res) => res.json({ status: "ok", signer: signerWallet.address }));

const PORT = process.env.PORT || 3001;
app.listen(PORT, () => console.log(`Signer server listening on :${PORT}`));
