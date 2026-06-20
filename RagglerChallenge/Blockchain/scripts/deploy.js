const { ethers } = require("hardhat");
const fs = require("fs");
require("dotenv").config();

/**
 * Deployment script for BSC Testnet.
 *
 * Run:
 *   npx hardhat run scripts/deploy.js --network bscTestnet
 *
 * After deployment, addresses are written to:
 *   deployed-addresses.json   (for this script's reference)
 *   ../Assets/Resources/ContractConfig.json   (loaded by Unity at runtime)
 */
async function main() {
  const [deployer] = await ethers.getSigners();
  console.log("Deploying with:", deployer.address);
  console.log("Balance:", ethers.formatEther(await ethers.provider.getBalance(deployer.address)), "BNB");

  // 1. Deploy RaggToken
  console.log("\nDeploying RaggToken...");
  const RaggToken = await ethers.getContractFactory("RaggToken");
  const token = await RaggToken.deploy();
  await token.waitForDeployment();
  const tokenAddr = await token.getAddress();
  console.log("RaggToken deployed at:", tokenAddr);

  // 2. Derive game signer address from env key
  const signerWallet = process.env.GAME_SIGNER_PRIVATE_KEY
    ? new ethers.Wallet(process.env.GAME_SIGNER_PRIVATE_KEY)
    : deployer; // fallback: use deployer as signer (dev only)
  console.log("Game signer address:", signerWallet.address);

  // 3. Deploy GameRewards
  console.log("\nDeploying GameRewards...");
  const GameRewards = await ethers.getContractFactory("GameRewards");
  const rewards = await GameRewards.deploy(tokenAddr, signerWallet.address);
  await rewards.waitForDeployment();
  const rewardsAddr = await rewards.getAddress();
  console.log("GameRewards deployed at:", rewardsAddr);

  // 4. Grant GameRewards minting rights on the token
  console.log("\nSetting GameRewards as minter...");
  const tx = await token.setMinter(rewardsAddr);
  await tx.wait();
  console.log("Minter set.");

  // 5. Write addresses for Unity
  const config = {
    network:          "BSC Testnet",
    chainId:          97,
    rpcUrl:           "https://data-seed-prebsc-1-s1.binance.org:8545/",
    tokenAddress:     tokenAddr,
    rewardsAddress:   rewardsAddr,
    tokensPerStar:    "10000000000000000000",   // 10 * 10^18
    explorerBase:     "https://testnet.bscscan.com"
  };

  fs.writeFileSync("deployed-addresses.json", JSON.stringify(config, null, 2));

  const unityResourcesDir = "../Assets/Resources";
  if (!fs.existsSync(unityResourcesDir)) fs.mkdirSync(unityResourcesDir, { recursive: true });
  fs.writeFileSync(`${unityResourcesDir}/ContractConfig.json`, JSON.stringify(config, null, 2));

  console.log("\n✅ Deployment complete!");
  console.log("   RaggToken  :", tokenAddr);
  console.log("   GameRewards:", rewardsAddr);
  console.log("   Config written to Assets/Resources/ContractConfig.json");

  // 6. Verify on BscScan (optional, requires API key)
  if (process.env.BSCSCAN_API_KEY) {
    console.log("\nVerifying on BscScan...");
    try {
      await hre.run("verify:verify", { address: tokenAddr, constructorArguments: [] });
      await hre.run("verify:verify", {
        address: rewardsAddr,
        constructorArguments: [tokenAddr, signerWallet.address],
      });
    } catch (e) {
      console.warn("Verification error (may already be verified):", e.message);
    }
  }
}

main().catch((err) => { console.error(err); process.exit(1); });
