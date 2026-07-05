// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

/**
 * RagglerToken (RST) — star-reward token for the Raggler's Challenge game.
 *
 * A self-contained ERC-20 (no external imports, deploys directly in Remix)
 * with two game-specific mint functions:
 *
 *  - claimLevel(level, stars): converts the stars earned on a level into
 *    tokens (1 star = 1 RST). Each level can be claimed ONCE per wallet —
 *    after that, finishing the level again never yields more tokens.
 *
 *  - claimEndless(stars): converts the stars still lit when an Endless run
 *    ends (timer ran out or the clearing zone overflowed). Repeatable, but
 *    rate-limited by a cooldown to stop trivial farming.
 *
 * NOTE (school-project honesty): the star amounts are reported by the game
 * client, so a technically savvy player could call these functions directly.
 * A production version would have a backend verify each run and sign the
 * claim (see BLOCKCHAIN_SETUP.md). For a testnet demo this is fine.
 */
contract RagglerToken {
    string public constant name = "Raggler Star Token";
    string public constant symbol = "RST";
    uint8 public constant decimals = 18;

    uint256 public totalSupply;
    mapping(address => uint256) public balanceOf;
    mapping(address => mapping(address => uint256)) public allowance;

    /// levelClaimed[player][level] — level rewards are one-shot per wallet.
    mapping(address => mapping(uint8 => bool)) public levelClaimed;

    /// Endless claims are repeatable but throttled.
    mapping(address => uint256) public lastEndlessClaim;
    uint256 public endlessCooldown = 10 minutes;

    address public immutable owner;

    event Transfer(address indexed from, address indexed to, uint256 value);
    event Approval(address indexed owner, address indexed spender, uint256 value);
    event LevelClaimed(address indexed player, uint8 indexed level, uint8 stars);
    event EndlessClaimed(address indexed player, uint8 stars);

    constructor() {
        owner = msg.sender;
    }

    // ---------------------------------------------------------------
    // game claims
    // ---------------------------------------------------------------

    function claimLevel(uint8 level, uint8 stars) external {
        require(level >= 1, "invalid level");
        require(stars >= 1 && stars <= 3, "stars must be 1-3");
        require(!levelClaimed[msg.sender][level], "level already claimed");

        levelClaimed[msg.sender][level] = true;
        _mint(msg.sender, uint256(stars) * 1e18);
        emit LevelClaimed(msg.sender, level, stars);
    }

    function claimEndless(uint8 stars) external {
        require(stars >= 1 && stars <= 3, "stars must be 1-3");
        require(
            block.timestamp >= lastEndlessClaim[msg.sender] + endlessCooldown,
            "endless cooldown active"
        );

        lastEndlessClaim[msg.sender] = block.timestamp;
        _mint(msg.sender, uint256(stars) * 1e18);
        emit EndlessClaimed(msg.sender, stars);
    }

    function setEndlessCooldown(uint256 seconds_) external {
        require(msg.sender == owner, "owner only");
        endlessCooldown = seconds_;
    }

    // ---------------------------------------------------------------
    // standard ERC-20
    // ---------------------------------------------------------------

    function transfer(address to, uint256 value) external returns (bool) {
        _transfer(msg.sender, to, value);
        return true;
    }

    function approve(address spender, uint256 value) external returns (bool) {
        allowance[msg.sender][spender] = value;
        emit Approval(msg.sender, spender, value);
        return true;
    }

    function transferFrom(address from, address to, uint256 value) external returns (bool) {
        uint256 allowed = allowance[from][msg.sender];
        require(allowed >= value, "allowance exceeded");
        if (allowed != type(uint256).max) {
            allowance[from][msg.sender] = allowed - value;
        }
        _transfer(from, to, value);
        return true;
    }

    function _transfer(address from, address to, uint256 value) internal {
        require(to != address(0), "transfer to zero address");
        uint256 fromBalance = balanceOf[from];
        require(fromBalance >= value, "balance too low");
        unchecked {
            balanceOf[from] = fromBalance - value;
            balanceOf[to] += value;
        }
        emit Transfer(from, to, value);
    }

    function _mint(address to, uint256 value) internal {
        totalSupply += value;
        unchecked {
            balanceOf[to] += value;
        }
        emit Transfer(address(0), to, value);
    }
}
