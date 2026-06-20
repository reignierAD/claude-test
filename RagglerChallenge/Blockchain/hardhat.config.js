require("@nomicfoundation/hardhat-toolbox");
require("dotenv").config();

/**
 * Hardhat config for BSC Testnet deployment.
 *
 * Required .env variables:
 *   DEPLOYER_PRIVATE_KEY   — wallet that deploys + owns contracts
 *   GAME_SIGNER_PRIVATE_KEY — backend signer key (signs game results)
 *   BSCSCAN_API_KEY        — for contract verification (optional)
 */
module.exports = {
  solidity: {
    version: "0.8.20",
    settings: { optimizer: { enabled: true, runs: 200 } },
  },
  networks: {
    bscTestnet: {
      url: "https://data-seed-prebsc-1-s1.binance.org:8545/",
      chainId: 97,
      accounts: process.env.DEPLOYER_PRIVATE_KEY
        ? [process.env.DEPLOYER_PRIVATE_KEY]
        : [],
      gasPrice: 10_000_000_000, // 10 gwei
    },
    // Local fork for testing without real tBNB
    hardhat: {
      forking: {
        url: "https://data-seed-prebsc-1-s1.binance.org:8545/",
        enabled: false,   // set true to fork BSC testnet locally
      },
    },
  },
  etherscan: {
    apiKey: {
      bscTestnet: process.env.BSCSCAN_API_KEY || "",
    },
    customChains: [
      {
        network: "bscTestnet",
        chainId: 97,
        urls: {
          apiURL: "https://api-testnet.bscscan.com/api",
          browserURL: "https://testnet.bscscan.com",
        },
      },
    ],
  },
};
