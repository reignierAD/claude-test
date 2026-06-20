/**
 * MoralisBridge.jslib
 * Extends Web3Bridge with:
 *  1. Moralis Auth — challenge/sign/verify wallet login
 *  2. Moralis Token API  — RAGG balance (via our server proxy)
 *  3. Moralis Wallet API — RAGG transfer history
 *  4. WebSocket client   — receives real-time reward confirmations from server
 *
 * Callbacks fire via SendMessage("MoralisManager", "On<Event>", payload).
 */
mergeInto(LibraryManager.library, {

    // ── Moralis Auth — Step 1: request challenge ──────────────────────────────

    JS_MoralisAuthRequest: async function (serverUrlPtr, addressPtr, chainPtr) {
        try {
            const serverUrl = UTF8ToString(serverUrlPtr);
            const address   = UTF8ToString(addressPtr);
            const chain     = UTF8ToString(chainPtr);

            const resp = await fetch(`${serverUrl}/auth/request`, {
                method:  "POST",
                headers: { "Content-Type": "application/json" },
                body:    JSON.stringify({ address, chain }),
            });
            const data = await resp.json();
            if (data.error) throw new Error(data.error);

            // Store message so Step 2 can retrieve it
            window._moralisAuthMessage = data.message;
            SendMessage("MoralisManager", "OnAuthChallengeReceived", data.message);
        } catch (err) {
            SendMessage("MoralisManager", "OnAuthError", err.message || String(err));
        }
    },

    // ── Moralis Auth — Step 2: sign challenge with MetaMask ──────────────────

    JS_MoralisAuthSign: async function (serverUrlPtr, messagePtr) {
        try {
            const serverUrl = UTF8ToString(serverUrlPtr);
            const message   = UTF8ToString(messagePtr);

            const provider  = new ethers.BrowserProvider(window.ethereum);
            const signer    = await provider.getSigner();
            const signature = await signer.signMessage(message);

            // Step 3: verify signature with backend
            const resp = await fetch(`${serverUrl}/auth/verify`, {
                method:  "POST",
                headers: { "Content-Type": "application/json" },
                body:    JSON.stringify({ message, signature }),
            });
            const data = await resp.json();
            if (data.error) throw new Error(data.error);

            // Cache JWT for subsequent API calls
            window._moralisJwt     = data.token;
            window._moralisAddress = data.address;

            SendMessage("MoralisManager", "OnAuthVerified",
                JSON.stringify({ token: data.token, address: data.address }));
        } catch (err) {
            SendMessage("MoralisManager", "OnAuthError", err.message || String(err));
        }
    },

    // ── Token Balance (via server proxy → Moralis Token API) ─────────────────

    JS_MoralisGetBalance: async function (serverUrlPtr, addressPtr) {
        try {
            const serverUrl = UTF8ToString(serverUrlPtr);
            const address   = UTF8ToString(addressPtr);
            const jwt       = window._moralisJwt || "";

            const resp = await fetch(`${serverUrl}/moralis/balance/${address}`, {
                headers: { Authorization: `Bearer ${jwt}` },
            });
            const data = await resp.json();
            if (data.error) throw new Error(data.error);

            SendMessage("MoralisManager", "OnBalanceReceived", JSON.stringify(data));
        } catch (err) {
            SendMessage("MoralisManager", "OnMoralisError", "Balance: " + (err.message || String(err)));
        }
    },

    // ── Transfer History (via server proxy → Moralis Wallet API) ─────────────

    JS_MoralisGetHistory: async function (serverUrlPtr, addressPtr, limitInt) {
        try {
            const serverUrl = UTF8ToString(serverUrlPtr);
            const address   = UTF8ToString(addressPtr);
            const jwt       = window._moralisJwt || "";

            const resp = await fetch(`${serverUrl}/moralis/history/${address}?limit=${limitInt}`, {
                headers: { Authorization: `Bearer ${jwt}` },
            });
            const data = await resp.json();
            if (data.error) throw new Error(data.error);

            SendMessage("MoralisManager", "OnHistoryReceived", JSON.stringify(data.transfers));
        } catch (err) {
            SendMessage("MoralisManager", "OnMoralisError", "History: " + (err.message || String(err)));
        }
    },

    // ── WebSocket — connect and register wallet address ───────────────────────

    JS_WsConnect: function (wsUrlPtr, addressPtr) {
        const wsUrl  = UTF8ToString(wsUrlPtr);
        const address = UTF8ToString(addressPtr);

        if (window._gameWs) {
            window._gameWs.close();
            window._gameWs = null;
        }

        const ws = new WebSocket(wsUrl);
        window._gameWs = ws;

        ws.onopen = () => {
            ws.send(JSON.stringify({ type: "register", address }));
            SendMessage("MoralisManager", "OnWsConnected", "");
        };

        ws.onmessage = (evt) => {
            SendMessage("MoralisManager", "OnWsMessage", evt.data);
        };

        ws.onerror = (err) => {
            SendMessage("MoralisManager", "OnWsError", "WebSocket error");
        };

        ws.onclose = () => {
            SendMessage("MoralisManager", "OnWsDisconnected", "");
        };
    },

    JS_WsDisconnect: function () {
        if (window._gameWs) {
            window._gameWs.close();
            window._gameWs = null;
        }
    },

    JS_WsSend: function (msgPtr) {
        const msg = UTF8ToString(msgPtr);
        if (window._gameWs && window._gameWs.readyState === WebSocket.OPEN)
            window._gameWs.send(msg);
    },

});
