# Blockchain Setup — BSC Testnet + MetaMask + Moralis

This guide takes you from a fresh clone to a WebGL build where players log
in with MetaMask and convert their stars into **RST** tokens (1 star = 1
token) on the **BNB Smart Chain (BSC) Testnet**.

## How the token rules work

| Mode | Reward | Repeatable? |
|---|---|---|
| Levels 1..N | Stars earned on the clear (1–3) → RST | **No** — the contract records the claim; a finished level never pays again |
| Endless | Stars banked across the run (each cleared board banks the stars still lit, then timer + stars reset) → RST | Yes, with an on-chain cooldown (default 10 min) |

## 1. MetaMask + test BNB

1. Install [MetaMask](https://metamask.io) in your browser.
2. Get free test BNB from the official faucet:
   <https://www.bnbchain.org/en/testnet-faucet>
   (You need a little tBNB to pay gas for deployment and claims.)
3. The game adds/switches to BSC Testnet automatically on login, but you can
   also add it manually: RPC `https://data-seed-prebsc-1-s1.bnbchain.org:8545`,
   chain ID `97`, symbol `tBNB`, explorer `https://testnet.bscscan.com`.

## 2. Deploy the token contract

1. Open [Remix](https://remix.ethereum.org) and create a new file with the
   contents of [`contracts/RagglerToken.sol`](contracts/RagglerToken.sol).
2. Compile with Solidity **0.8.20+**.
3. In *Deploy & Run*, set **Environment = Injected Provider – MetaMask**
   (make sure MetaMask is on BSC Testnet), then **Deploy**.
4. Copy the deployed contract address.

## 3. Configure the game

Open `Assets/WebGLTemplates/Raggler/web3config.js` and set:

```js
contractAddress: "0xYourDeployedAddress",
moralisApiKey: "...", // optional, see below
```

### Moralis (optional but recommended)

1. Create a free account at [moralis.com](https://moralis.com/) and grab an
   API key from the **Web3 Data API** dashboard.
2. Paste it into `moralisApiKey`. The game then reads your RST balance
   through Moralis; without a key it falls back to reading the chain
   directly through MetaMask's provider.

> ⚠️ An API key in client-side JS is visible to anyone. That's acceptable
> for a school/testnet demo; a production app would proxy Moralis calls
> through a small backend. The same goes for claims: the star counts are
> reported by the game client, so production would verify runs server-side
> and have the backend sign each claim before the contract accepts it.

## 4. Build for WebGL

1. Unity: **File > Build Profiles** (or Build Settings), switch platform to
   **Web / WebGL**.
2. **Player Settings > Resolution and Presentation > WebGL Template**:
   pick **Raggler** (this ships the MetaMask/Moralis JavaScript with the build).
3. **Player Settings > Publishing Settings > Compression Format**: choose
   **Disabled** for painless static hosting (or Gzip + "Decompression
   Fallback" if your host doesn't set Content-Encoding headers).
4. Build. The output folder contains `index.html`, `web3config.js`,
   `raggler-web3.js` and the `Build/` folder — host all of it together.
   You can still edit `web3config.js` inside the build output afterwards
   (e.g. to swap the contract address without rebuilding).

## 5. Test locally

```bash
cd <your-build-folder>
python3 -m http.server 8080
```

Open `http://localhost:8080` in the browser that has MetaMask. Click
**Connect Wallet** on the menu, approve the connection, sign the login
message, play a level and hit **Claim N RST** on the victory screen. The
transaction appears in MetaMask; once mined, your balance updates (check it
on [testnet.bscscan.com](https://testnet.bscscan.com) too).

To see RST inside MetaMask: *Import tokens* → paste the contract address.

## In-editor testing without a wallet

Outside WebGL builds (Unity editor / desktop) the whole flow runs in a
**local simulation** — "Connect Wallet" instantly connects a fake address,
claims add to a locally stored balance, and level claims are remembered —
so you can test the UX without MetaMask. The wallet corner is labeled
`(sim)` in that mode.

## Troubleshooting

- **"MetaMask not detected"** — use a desktop browser with the extension;
  it isn't injected into `file://` pages, so serve over http(s).
- **"No contract address configured"** — fill `contractAddress` in
  `web3config.js` (in the template folder before building, or in the build
  output afterwards).
- **Claim reverts with "level already claimed"** — that wallet already
  converted that level's stars; this is by design.
- **Claim reverts with "endless cooldown active"** — wait out the cooldown
  (default 10 minutes) or lower it via `setEndlessCooldown` as the
  contract owner.
- **Balance not updating** — Moralis indexes testnets with a small delay;
  the direct chain fallback is instant, so try removing the API key if it
  bothers you during demos.
