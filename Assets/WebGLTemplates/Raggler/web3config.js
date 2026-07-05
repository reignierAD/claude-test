// ============================================================
// Raggler's Challenge — Web3 configuration
// Fill these in after deploying contracts/RagglerToken.sol
// (see BLOCKCHAIN_SETUP.md in the repository root).
// ============================================================
const RAGGLER_CONFIG = {
  // Address of the deployed RagglerToken contract on BSC Testnet.
  contractAddress: "",

  // Optional: Moralis Web3 Data API key (moralis.com > Web3 APIs).
  // Used to read your RST balance through Moralis. If left empty the
  // game falls back to reading the balance directly from the chain.
  // NOTE: for a real product this key belongs on a backend, not in
  // client code — fine for a school/testnet demo.
  moralisApiKey: "",

  // Message signed by MetaMask on login.
  loginMessage: "Login to Raggler's Challenge",

  // BSC Testnet chain parameters.
  chain: {
    chainId: "0x61", // 97
    chainName: "BNB Smart Chain Testnet",
    nativeCurrency: { name: "tBNB", symbol: "tBNB", decimals: 18 },
    rpcUrls: ["https://data-seed-prebsc-1-s1.bnbchain.org:8545"],
    blockExplorerUrls: ["https://testnet.bscscan.com"]
  }
};

// Minimal ABI of RagglerToken (ethers human-readable format).
const RAGGLER_ABI = [
  "function balanceOf(address) view returns (uint256)",
  "function levelClaimed(address, uint8) view returns (bool)",
  "function claimLevel(uint8 level, uint8 stars)",
  "function claimEndless(uint8 stars)",
  "function symbol() view returns (string)"
];
