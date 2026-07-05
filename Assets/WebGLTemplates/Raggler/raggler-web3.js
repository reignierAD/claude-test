// ============================================================
// Raggler's Challenge — browser Web3 layer
// MetaMask login + BSC Testnet + RagglerToken claims, with an
// optional Moralis Web3 Data API path for reading balances.
// Talks to Unity via SendMessage -> the "Web3Bridge" GameObject.
// ============================================================
const RagglerWeb3 = {
  provider: null,
  signer: null,
  address: null,
  levelCount: 10,

  send: function (method, msg) {
    if (window.ragglerUnityInstance)
      window.ragglerUnityInstance.SendMessage('Web3Bridge', method, msg == null ? '' : String(msg));
  },

  connect: async function () {
    try {
      if (!window.ethereum) {
        this.send('OnWalletError', 'MetaMask not detected — install it from metamask.io and reload.');
        return;
      }
      await this.ensureBscTestnet();

      const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
      this.address = accounts[0];
      this.provider = new ethers.providers.Web3Provider(window.ethereum, 'any');
      this.signer = this.provider.getSigner();

      // MetaMask "login": sign a message proving ownership of the address.
      try {
        await this.signer.signMessage(RAGGLER_CONFIG.loginMessage || "Login to Raggler's Challenge");
      } catch (e) {
        this.send('OnWalletError', 'Login signature rejected.');
        return;
      }

      this.send('OnWalletConnected', this.address);
      this.refreshState(this.levelCount);
    } catch (e) {
      this.send('OnWalletError', (e && e.message) ? e.message : String(e));
    }
  },

  ensureBscTestnet: async function () {
    const target = RAGGLER_CONFIG.chain;
    try {
      await window.ethereum.request({
        method: 'wallet_switchEthereumChain',
        params: [{ chainId: target.chainId }]
      });
    } catch (switchError) {
      // 4902 = chain not added to MetaMask yet
      if (switchError && (switchError.code === 4902 || switchError.code === -32603)) {
        await window.ethereum.request({
          method: 'wallet_addEthereumChain',
          params: [target]
        });
      } else {
        throw switchError;
      }
    }
  },

  contract: function (withSigner) {
    if (!RAGGLER_CONFIG.contractAddress) return null;
    return new ethers.Contract(
      RAGGLER_CONFIG.contractAddress,
      RAGGLER_ABI,
      withSigner ? this.signer : this.provider
    );
  },

  // Reads balance (Moralis first, chain fallback) + per-level claimed flags,
  // then pushes everything to Unity in one message: "balance|claimedBits".
  refreshState: async function (levelCount) {
    if (levelCount) this.levelCount = levelCount;
    if (!this.address) return;
    if (!RAGGLER_CONFIG.contractAddress) {
      this.send('OnState', '0|' + '0'.repeat(this.levelCount));
      this.send('OnWalletError', 'No contract address configured (web3config.js).');
      return;
    }
    try {
      let balance = null;
      if (RAGGLER_CONFIG.moralisApiKey) balance = await this.moralisBalance();
      if (balance === null) {
        const raw = await this.contract(false).balanceOf(this.address);
        balance = ethers.utils.formatUnits(raw, 18);
      }

      const bits = [];
      for (let i = 1; i <= this.levelCount; i++) {
        const claimed = await this.contract(false).levelClaimed(this.address, i);
        bits.push(claimed ? '1' : '0');
      }
      this.send('OnState', balance + '|' + bits.join(''));
    } catch (e) {
      this.send('OnWalletError', 'Could not read chain state: ' + ((e && e.message) ? e.message : e));
    }
  },

  // Moralis Web3 Data API: ERC20 balances for the connected wallet.
  moralisBalance: async function () {
    try {
      const url = 'https://deep-index.moralis.io/api/v2.2/' + this.address +
        '/erc20?chain=' + RAGGLER_CONFIG.chain.chainId +
        '&token_addresses%5B0%5D=' + RAGGLER_CONFIG.contractAddress;
      const res = await fetch(url, {
        headers: { 'X-API-Key': RAGGLER_CONFIG.moralisApiKey, 'accept': 'application/json' }
      });
      if (!res.ok) return null;
      const arr = await res.json();
      if (Array.isArray(arr) && arr.length > 0)
        return ethers.utils.formatUnits(arr[0].balance, arr[0].decimals || 18);
      return '0';
    } catch (e) {
      return null; // fall back to direct chain read
    }
  },

  claimLevel: function (level, stars) {
    this._claim(() => this.contract(true).claimLevel(level, stars), 'level ' + level);
  },

  claimEndless: function (stars) {
    this._claim(() => this.contract(true).claimEndless(stars), 'endless');
  },

  _claim: async function (txFactory, label) {
    try {
      if (!this.signer) {
        this.send('OnClaimError', 'Connect your wallet first.');
        return;
      }
      if (!RAGGLER_CONFIG.contractAddress) {
        this.send('OnClaimError', 'No contract address configured (web3config.js).');
        return;
      }
      const tx = await txFactory();
      this.send('OnClaimPending', tx.hash);
      await tx.wait();
      this.send('OnClaimOk', label + '|' + tx.hash);
      this.refreshState(this.levelCount);
    } catch (e) {
      let msg = (e && (e.reason || (e.data && e.data.message) || e.message)) || String(e);
      this.send('OnClaimError', msg.substring(0, 140));
    }
  }
};

// keep Unity in sync if the user switches accounts in MetaMask
if (window.ethereum && window.ethereum.on) {
  window.ethereum.on('accountsChanged', function (accounts) {
    if (accounts && accounts.length > 0 && RagglerWeb3.address) {
      RagglerWeb3.address = accounts[0];
      RagglerWeb3.send('OnWalletConnected', accounts[0]);
      RagglerWeb3.refreshState(RagglerWeb3.levelCount);
    }
  });
}
