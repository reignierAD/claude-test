// Unity <-> browser bridge. The real Web3 logic lives in raggler-web3.js
// (shipped with the WebGL template); these thin wrappers just forward calls.
mergeInto(LibraryManager.library, {
  JS_Web3_Connect: function () {
    if (typeof RagglerWeb3 !== 'undefined') RagglerWeb3.connect();
  },
  JS_Web3_ClaimLevel: function (level, stars) {
    if (typeof RagglerWeb3 !== 'undefined') RagglerWeb3.claimLevel(level, stars);
  },
  JS_Web3_ClaimEndless: function (stars) {
    if (typeof RagglerWeb3 !== 'undefined') RagglerWeb3.claimEndless(stars);
  },
  JS_Web3_RefreshState: function (levelCount) {
    if (typeof RagglerWeb3 !== 'undefined') RagglerWeb3.refreshState(levelCount);
  }
});
