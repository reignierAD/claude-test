/**
 * Returns the keccak256 hex (without 0x) of an event signature string.
 * Used to match Moralis stream log topic0 values to our event names.
 */
const { keccak256, toUtf8Bytes } = require("ethers");

module.exports = function keccakTopic(sig) {
    return keccak256(toUtf8Bytes(sig)).slice(2); // remove "0x"
};
