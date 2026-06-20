/**
 * Moralis Auth API routes
 * Replaces raw MetaMask eth_requestAccounts with a challenge-response
 * login that proves the player owns the wallet without exposing a secret.
 *
 * Flow:
 *  1. Unity calls POST /auth/request  → server returns a challenge message
 *  2. Unity asks MetaMask to personal_sign the message
 *  3. Unity calls POST /auth/verify   → server verifies with Moralis and
 *     issues a short-lived JWT the signer server trusts for reward requests
 */

const express  = require("express");
const Moralis  = require("moralis").default;
const jwt      = require("jsonwebtoken");
const router   = express.Router();

const JWT_SECRET  = process.env.JWT_SECRET  || "change-me-in-production";
const JWT_EXPIRES = process.env.JWT_EXPIRES || "2h";

// POST /auth/request
// Body: { address, chain }   e.g. { address: "0x...", chain: "0x61" }
// Returns: { message }  — the EIP-4361 challenge string to sign
router.post("/request", async (req, res) => {
    try {
        const { address, chain } = req.body;
        if (!address || !chain)
            return res.status(400).json({ error: "address and chain required" });

        const response = await Moralis.Auth.requestMessage({
            address,
            chain,
            networkType: "evm",
            domain:      process.env.AUTH_DOMAIN    || "raggler.game",
            uri:         process.env.AUTH_URI        || "https://raggler.game",
            statement:   "Sign in to Raggler's Challenge to earn RAGG tokens.",
            timeout:     120,
        });

        res.json({ message: response.raw.message, id: response.raw.id });
    } catch (err) {
        console.error("[Auth] request error:", err);
        res.status(500).json({ error: err.message });
    }
});

// POST /auth/verify
// Body: { message, signature }
// Returns: { token, address }  — JWT for subsequent reward requests
router.post("/verify", async (req, res) => {
    try {
        const { message, signature } = req.body;
        if (!message || !signature)
            return res.status(400).json({ error: "message and signature required" });

        const response = await Moralis.Auth.verify({
            networkType: "evm",
            message,
            signature,
        });

        const { address, profileId } = response.raw;
        const token = jwt.sign({ address, profileId }, JWT_SECRET, { expiresIn: JWT_EXPIRES });

        console.log(`[Auth] Verified login: ${address}`);
        res.json({ token, address });
    } catch (err) {
        console.error("[Auth] verify error:", err);
        res.status(401).json({ error: "Verification failed: " + err.message });
    }
});

module.exports = router;
