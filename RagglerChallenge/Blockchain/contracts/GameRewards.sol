// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import "@openzeppelin/contracts/utils/cryptography/MessageHashUtils.sol";
import "./RaggToken.sol";

/**
 * @title GameRewards
 * @dev Manages star-based token rewards for Raggler's Challenge.
 *
 * Rules (matching the game):
 *  - Stages 1-10: player earns tokens only for star upgrades.
 *    e.g. previously earned 2★ on stage 3 → completing stage 3 with 3★ pays 1★ worth of tokens.
 *    Once all stages are 3★, no more stage tokens can be earned.
 *  - Endless mode: every round pays tokens freely (stars × TOKENS_PER_STAR).
 *
 * Security: results are signed by the game's backend signer key so the
 * contract can verify the claim was actually issued by the game server,
 * preventing players from submitting fake results.
 */
contract GameRewards is Ownable {
    using ECDSA for bytes32;
    using MessageHashUtils for bytes32;

    RaggToken public immutable token;

    uint256 public constant TOKENS_PER_STAR = 10 * 10 ** 18; // 10 RAGG per star
    uint8   public constant TOTAL_STAGES    = 10;

    /// @dev Address whose private key signs game results (game backend server).
    address public gameSigner;

    /// @dev player => stageId (1-indexed) => best star rating achieved
    mapping(address => mapping(uint8 => uint8)) public bestStars;

    /// @dev Nonces prevent replay of signed messages.
    mapping(address => uint256) public nonces;

    event StageRewardClaimed(address indexed player, uint8 stageId, uint8 newStars, uint8 prevBest, uint256 tokensEarned);
    event EndlessRewardClaimed(address indexed player, uint8 stars, uint256 tokensEarned);
    event SignerUpdated(address indexed oldSigner, address indexed newSigner);

    constructor(address _token, address _gameSigner) Ownable(msg.sender) {
        token     = RaggToken(_token);
        gameSigner = _gameSigner;
    }

    // ── Owner admin ───────────────────────────────────────────────────────────

    function setGameSigner(address _signer) external onlyOwner {
        emit SignerUpdated(gameSigner, _signer);
        gameSigner = _signer;
    }

    // ── Stage reward claim ────────────────────────────────────────────────────

    /**
     * @notice Claim tokens for completing a numbered stage.
     * @param stageId  Stage number 1–10.
     * @param stars    Stars achieved this run (1–3).
     * @param nonce    Must equal player's current nonce.
     * @param sig      ECDSA signature from gameSigner over (player, stageId, stars, nonce, chainId).
     */
    function claimStageReward(
        uint8   stageId,
        uint8   stars,
        uint256 nonce,
        bytes calldata sig
    ) external {
        require(stageId >= 1 && stageId <= TOTAL_STAGES, "Invalid stageId");
        require(stars >= 1 && stars <= 3, "Invalid stars");

        address player = msg.sender;
        _verifyNonce(player, nonce);
        _verifySig(player, stageId, stars, nonce, false, sig);

        uint8 prev = bestStars[player][stageId];
        require(stars > prev, "No improvement: stars not higher than best");

        uint8 newStarsDelta = stars - prev;
        uint256 tokensEarned = uint256(newStarsDelta) * TOKENS_PER_STAR;

        bestStars[player][stageId] = stars;
        nonces[player]++;

        token.mint(player, tokensEarned);
        emit StageRewardClaimed(player, stageId, stars, prev, tokensEarned);
    }

    /**
     * @notice Claim tokens for an Endless mode round.
     * @param stars  Stars achieved this Endless round (1–3).
     * @param nonce  Must equal player's current nonce.
     * @param sig    ECDSA signature from gameSigner.
     */
    function claimEndlessReward(
        uint8   stars,
        uint256 nonce,
        bytes calldata sig
    ) external {
        require(stars >= 1 && stars <= 3, "Invalid stars");

        address player = msg.sender;
        _verifyNonce(player, nonce);
        _verifySig(player, 0, stars, nonce, true, sig);

        uint256 tokensEarned = uint256(stars) * TOKENS_PER_STAR;
        nonces[player]++;

        token.mint(player, tokensEarned);
        emit EndlessRewardClaimed(player, stars, tokensEarned);
    }

    // ── View helpers ──────────────────────────────────────────────────────────

    /// @notice Returns true when all 10 stages are 3★ for a player (no more stage tokens).
    function allStagesMaxed(address player) external view returns (bool) {
        for (uint8 i = 1; i <= TOTAL_STAGES; i++)
            if (bestStars[player][i] < 3) return false;
        return true;
    }

    /// @notice Returns how many more stars (and thus tokens) a player can earn across stages.
    function remainingStageStars(address player) external view returns (uint256 total) {
        for (uint8 i = 1; i <= TOTAL_STAGES; i++)
            total += 3 - uint256(bestStars[player][i]);
    }

    // ── Signature verification ────────────────────────────────────────────────

    function _verifyNonce(address player, uint256 nonce) internal view {
        require(nonce == nonces[player], "Invalid nonce");
    }

    function _verifySig(
        address player,
        uint8   stageId,
        uint8   stars,
        uint256 nonce,
        bool    isEndless,
        bytes calldata sig
    ) internal view {
        bytes32 msgHash = keccak256(abi.encodePacked(
            player, stageId, stars, nonce, isEndless, block.chainid
        )).toEthSignedMessageHash();

        address recovered = msgHash.recover(sig);
        require(recovered == gameSigner, "Invalid signature");
    }
}
