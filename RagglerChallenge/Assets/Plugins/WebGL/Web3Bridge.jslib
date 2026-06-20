/**
 * Web3Bridge.jslib
 * Unity WebGL ↔ MetaMask / ethers.js bridge.
 *
 * Unity calls these functions via [DllImport("__Internal")].
 * Results are returned asynchronously by calling back into Unity via
 * SendMessage("Web3Manager", "On<Event>", payload).
 *
 * Requires ethers.js v6 loaded in the WebGL template HTML:
 *   <script src="https://cdn.jsdelivr.net/npm/ethers@6/dist/ethers.umd.min.js"></script>
 */
mergeInto(LibraryManager.library, {

  // ── Wallet connection ───────────────────────────────────────────────────────

  JS_ConnectWallet: async function () {
    try {
      if (typeof window.ethereum === "undefined") {
        SendMessage("Web3Manager", "OnWalletError", "MetaMask not found. Please install it.");
        return;
      }

      const accounts = await window.ethereum.request({ method: "eth_requestAccounts" });
      const address  = accounts[0];

      // Switch to / add BSC Testnet (chainId 0x61 = 97)
      try {
        await window.ethereum.request({
          method: "wallet_switchEthereumChain",
          params: [{ chainId: "0x61" }],
        });
      } catch (switchErr) {
        if (switchErr.code === 4902) {
          await window.ethereum.request({
            method: "wallet_addEthereumChain",
            params: [{
              chainId:         "0x61",
              chainName:       "BSC Testnet",
              nativeCurrency:  { name: "tBNB", symbol: "tBNB", decimals: 18 },
              rpcUrls:         ["https://data-seed-prebsc-1-s1.binance.org:8545/"],
              blockExplorerUrls: ["https://testnet.bscscan.com"],
            }],
          });
        } else {
          throw switchErr;
        }
      }

      SendMessage("Web3Manager", "OnWalletConnected", address);
    } catch (err) {
      SendMessage("Web3Manager", "OnWalletError", err.message || String(err));
    }
  },

  // ── Token balance ───────────────────────────────────────────────────────────

  JS_GetTokenBalance: async function (tokenAddrPtr, playerAddrPtr) {
    try {
      const tokenAddr  = UTF8ToString(tokenAddrPtr);
      const playerAddr = UTF8ToString(playerAddrPtr);
      const provider   = new ethers.BrowserProvider(window.ethereum);

      const abi      = ["function balanceOf(address) view returns (uint256)"];
      const contract = new ethers.Contract(tokenAddr, abi, provider);
      const balance  = await contract.balanceOf(playerAddr);

      // Return as a decimal string (Unity formats it)
      SendMessage("Web3Manager", "OnBalanceReceived", balance.toString());
    } catch (err) {
      SendMessage("Web3Manager", "OnWeb3Error", "GetBalance: " + (err.message || String(err)));
    }
  },

  // ── Player nonce (to build claim payload) ──────────────────────────────────

  JS_GetPlayerNonce: async function (rewardsAddrPtr, playerAddrPtr) {
    try {
      const rewardsAddr = UTF8ToString(rewardsAddrPtr);
      const playerAddr  = UTF8ToString(playerAddrPtr);
      const provider    = new ethers.BrowserProvider(window.ethereum);

      const abi      = ["function nonces(address) view returns (uint256)"];
      const contract = new ethers.Contract(rewardsAddr, abi, provider);
      const nonce    = await contract.nonces(playerAddr);

      SendMessage("Web3Manager", "OnNonceReceived", nonce.toString());
    } catch (err) {
      SendMessage("Web3Manager", "OnWeb3Error", "GetNonce: " + (err.message || String(err)));
    }
  },

  // ── Stage reward claim ──────────────────────────────────────────────────────

  /**
   * @param rewardsAddrPtr  GameRewards contract address
   * @param payloadPtr      JSON string: { stageId, stars, nonce, sig }
   *                        'sig' is produced by the game backend signer
   */
  JS_ClaimStageReward: async function (rewardsAddrPtr, payloadPtr) {
    try {
      const rewardsAddr = UTF8ToString(rewardsAddrPtr);
      const payload     = JSON.parse(UTF8ToString(payloadPtr));
      const provider    = new ethers.BrowserProvider(window.ethereum);
      const signer      = await provider.getSigner();

      const abi = [
        "function claimStageReward(uint8 stageId, uint8 stars, uint256 nonce, bytes calldata sig) external"
      ];
      const contract = new ethers.Contract(rewardsAddr, abi, signer);
      const tx = await contract.claimStageReward(
        payload.stageId,
        payload.stars,
        payload.nonce,
        payload.sig
      );

      SendMessage("Web3Manager", "OnTxPending", tx.hash);
      const receipt = await tx.wait();
      SendMessage("Web3Manager", "OnClaimSuccess", JSON.stringify({
        type: "stage",
        stageId: payload.stageId,
        stars:   payload.stars,
        txHash:  receipt.hash,
      }));
    } catch (err) {
      // User rejected or on-chain revert
      const msg = err?.reason || err?.message || String(err);
      SendMessage("Web3Manager", "OnClaimFailed", msg);
    }
  },

  // ── Endless reward claim ────────────────────────────────────────────────────

  JS_ClaimEndlessReward: async function (rewardsAddrPtr, payloadPtr) {
    try {
      const rewardsAddr = UTF8ToString(rewardsAddrPtr);
      const payload     = JSON.parse(UTF8ToString(payloadPtr));
      const provider    = new ethers.BrowserProvider(window.ethereum);
      const signer      = await provider.getSigner();

      const abi = [
        "function claimEndlessReward(uint8 stars, uint256 nonce, bytes calldata sig) external"
      ];
      const contract = new ethers.Contract(rewardsAddr, abi, signer);
      const tx = await contract.claimEndlessReward(payload.stars, payload.nonce, payload.sig);

      SendMessage("Web3Manager", "OnTxPending", tx.hash);
      const receipt = await tx.wait();
      SendMessage("Web3Manager", "OnClaimSuccess", JSON.stringify({
        type:   "endless",
        stars:  payload.stars,
        txHash: receipt.hash,
      }));
    } catch (err) {
      const msg = err?.reason || err?.message || String(err);
      SendMessage("Web3Manager", "OnClaimFailed", msg);
    }
  },

  // ── Best stars query ────────────────────────────────────────────────────────

  JS_GetBestStars: async function (rewardsAddrPtr, playerAddrPtr, stageId) {
    try {
      const rewardsAddr = UTF8ToString(rewardsAddrPtr);
      const playerAddr  = UTF8ToString(playerAddrPtr);
      const provider    = new ethers.BrowserProvider(window.ethereum);

      const abi      = ["function bestStars(address, uint8) view returns (uint8)"];
      const contract = new ethers.Contract(rewardsAddr, abi, provider);
      const stars    = await contract.bestStars(playerAddr, stageId);

      SendMessage("Web3Manager", "OnBestStarsReceived",
        JSON.stringify({ stageId: stageId, stars: Number(stars) }));
    } catch (err) {
      SendMessage("Web3Manager", "OnWeb3Error", "GetBestStars: " + (err.message || String(err)));
    }
  },

  // ── Account change listener ─────────────────────────────────────────────────

  JS_ListenAccountChanges: function () {
    if (typeof window.ethereum === "undefined") return;
    window.ethereum.on("accountsChanged", function (accounts) {
      const addr = accounts.length > 0 ? accounts[0] : "";
      SendMessage("Web3Manager", "OnAccountChanged", addr);
    });
    window.ethereum.on("chainChanged", function (chainId) {
      SendMessage("Web3Manager", "OnChainChanged", chainId.toString());
    });
  },

});
