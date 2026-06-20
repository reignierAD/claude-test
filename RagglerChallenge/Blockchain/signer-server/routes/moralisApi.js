/**
 * Moralis Token API + Wallet API proxy routes.
 * Unity calls these endpoints; the server adds the Moralis API key
 * so it never reaches the client.
 *
 * All routes require a valid JWT in Authorization: Bearer <token>
 * (issued by /auth/verify) so only authenticated players can query.
 */

const express = require("express");
const Moralis  = require("moralis").default;
const { verifyJwt } = require("../middleware/verifyJwt");
const router   = express.Router();

const RAGG_TOKEN_ADDR = process.env.RAGG_TOKEN_ADDRESS || "";
const BSC_TESTNET     = "0x61";

// ── GET /moralis/balance/:address ─────────────────────────────────────────────
// Returns RAGG token balance + USD value for the player.
router.get("/balance/:address", verifyJwt, async (req, res) => {
    try {
        const { address } = req.params;

        const response = await Moralis.EvmApi.token.getWalletTokenBalances({
            chain:             BSC_TESTNET,
            address,
            tokenAddresses:    [RAGG_TOKEN_ADDR],
        });

        const tokens = response.toJSON();
        const ragg   = tokens.find(t => t.token_address?.toLowerCase() === RAGG_TOKEN_ADDR.toLowerCase());

        res.json({
            balance:       ragg?.balance         || "0",
            balanceFormatted: ragg?.balance_formatted || "0",
            usdValue:      ragg?.usd_value        || null,
            symbol:        ragg?.symbol           || "RAGG",
            decimals:      ragg?.decimals         || 18,
        });
    } catch (err) {
        console.error("[Moralis] balance error:", err);
        res.status(500).json({ error: err.message });
    }
});

// ── GET /moralis/history/:address ─────────────────────────────────────────────
// Returns RAGG transfer history (minted rewards) for the player.
// Sorted newest first; limited to 50 entries.
router.get("/history/:address", verifyJwt, async (req, res) => {
    try {
        const { address } = req.params;
        const limit = Math.min(parseInt(req.query.limit) || 20, 50);

        const response = await Moralis.EvmApi.token.getWalletTokenTransfers({
            chain:            BSC_TESTNET,
            address,
            contractAddresses: [RAGG_TOKEN_ADDR],
            limit,
        });

        const transfers = response.toJSON().result.map(tx => ({
            txHash:    tx.transaction_hash,
            from:      tx.from_address,
            to:        tx.to_address,
            value:     tx.value,
            valueFormatted: tx.value_decimal,
            blockTimestamp: tx.block_timestamp,
        }));

        res.json({ transfers });
    } catch (err) {
        console.error("[Moralis] history error:", err);
        res.status(500).json({ error: err.message });
    }
});

// ── GET /moralis/nft/:address ─────────────────────────────────────────────────
// Placeholder for future NFT power-up checks.
router.get("/nft/:address", verifyJwt, async (req, res) => {
    try {
        const { address } = req.params;

        const response = await Moralis.EvmApi.nft.getWalletNFTs({
            chain:   BSC_TESTNET,
            address,
            limit:   100,
        });

        res.json({ nfts: response.toJSON().result });
    } catch (err) {
        console.error("[Moralis] nft error:", err);
        res.status(500).json({ error: err.message });
    }
});

module.exports = router;
