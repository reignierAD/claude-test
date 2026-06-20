// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

/**
 * @title RaggToken
 * @dev BEP-20 / ERC-20 compatible token on BSC.
 *      Only the GameRewards contract (set as minter) can mint tokens.
 *      Total supply is uncapped — minting is gated by game logic.
 */
contract RaggToken is ERC20, Ownable {
    address public minter;

    event MinterUpdated(address indexed oldMinter, address indexed newMinter);

    constructor() ERC20("Raggler Token", "RAGG") Ownable(msg.sender) {}

    modifier onlyMinter() {
        require(msg.sender == minter, "RaggToken: caller is not minter");
        _;
    }

    /// @notice Owner sets the GameRewards contract as the sole minter.
    function setMinter(address _minter) external onlyOwner {
        emit MinterUpdated(minter, _minter);
        minter = _minter;
    }

    /// @notice Called by GameRewards to reward a player.
    function mint(address to, uint256 amount) external onlyMinter {
        _mint(to, amount);
    }
}
