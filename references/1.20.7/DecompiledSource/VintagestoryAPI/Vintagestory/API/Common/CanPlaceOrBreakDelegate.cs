using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

/// <summary>
/// Test if a player has the privilege to modify a block at given block selection
/// </summary>
/// <param name="byPlayer"></param>
/// <param name="blockSel"></param>
/// <param name="claimant">Needs to be set when false is returned. Is used to display the reason why the placement was denied. Either it needs to be the name of a player/npc who owns this block, or it needs to be prefixed with custommessage- for a custom error message, e.g. "custommessage-nobuildprivilege": "No build privilege" </param>
/// <returns>False to deny placement</returns>
public delegate bool CanPlaceOrBreakDelegate(IServerPlayer byPlayer, BlockSelection blockSel, out string claimant);
