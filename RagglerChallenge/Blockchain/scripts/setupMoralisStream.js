/**
 * One-time script: registers a Moralis Stream that watches GameRewards
 * on BSC Testnet and POSTs webhooks to your signer server.
 *
 * Run AFTER deploying contracts:
 *   node scripts/setupMoralisStream.js
 *
 * Requires in .env:
 *   MORALIS_API_KEY
 *   GAME_REWARDS_ADDRESS
 *   WEBHOOK_URL   e.g. https://your-server.com/streams/webhook
 */

require("dotenv").config({ path: "../signer-server/.env" });
const Moralis = require("moralis").default;

const GAME_REWARDS_ABI = [
    {
        "anonymous": false,
        "inputs": [
            { "indexed": true,  "name": "player",      "type": "address" },
            { "indexed": false, "name": "stageId",     "type": "uint8"   },
            { "indexed": false, "name": "newStars",    "type": "uint8"   },
            { "indexed": false, "name": "prevBest",    "type": "uint8"   },
            { "indexed": false, "name": "tokensEarned","type": "uint256" }
        ],
        "name": "StageRewardClaimed",
        "type": "event"
    },
    {
        "anonymous": false,
        "inputs": [
            { "indexed": true,  "name": "player",      "type": "address" },
            { "indexed": false, "name": "stars",       "type": "uint8"   },
            { "indexed": false, "name": "tokensEarned","type": "uint256" }
        ],
        "name": "EndlessRewardClaimed",
        "type": "event"
    }
];

async function main() {
    const apiKey      = process.env.MORALIS_API_KEY;
    const contractAddr = process.env.GAME_REWARDS_ADDRESS;
    const webhookUrl  = process.env.WEBHOOK_URL;

    if (!apiKey || !contractAddr || !webhookUrl) {
        console.error("Missing: MORALIS_API_KEY, GAME_REWARDS_ADDRESS, or WEBHOOK_URL in .env");
        process.exit(1);
    }

    await Moralis.start({ apiKey });

    console.log("Creating Moralis Stream...");
    console.log("  Contract:", contractAddr);
    console.log("  Webhook: ", webhookUrl);

    const stream = await Moralis.Streams.add({
        webhookUrl,
        description:     "Raggler's Challenge — reward events",
        tag:             "raggler-rewards",
        chains:          [{ chainId: "0x61" }],  // BSC Testnet
        includeNativeTxs: false,
        abi:             GAME_REWARDS_ABI,
        topic0:          [
            "StageRewardClaimed(address,uint8,uint8,uint8,uint256)",
            "EndlessRewardClaimed(address,uint8,uint256)",
        ],
        advancedOptions: [
            { topic0: "StageRewardClaimed(address,uint8,uint8,uint8,uint256)", filter: {}, includeAllTxLogs: false },
            { topic0: "EndlessRewardClaimed(address,uint8,uint256)",           filter: {}, includeAllTxLogs: false },
        ],
    });

    const streamId = stream.toJSON().id;
    console.log("Stream created! ID:", streamId);

    // Attach the GameRewards contract address to the stream
    await Moralis.Streams.addAddress({
        id:      streamId,
        address: [contractAddr],
    });

    console.log("Contract address attached to stream.");
    console.log("\nAdd MORALIS_STREAM_ID to your .env:");
    console.log(`MORALIS_STREAM_ID=${streamId}`);
}

main().catch(err => { console.error(err); process.exit(1); });
