namespace Vintagestory.API.Common;

public enum EnumWorldAccessResponse
{
	/// <summary>
	/// Access ok or was called client side
	/// </summary>
	Granted,
	/// <summary>
	/// Players in spectator mode may not place blocks
	/// </summary>
	InSpectatorMode,
	/// <summary>
	/// Player tries to place/break blocks but is in guest mode
	/// </summary>
	InGuestMode,
	/// <summary>
	/// Dead players may not place blocks
	/// </summary>
	PlayerDead,
	/// <summary>
	/// This player was not granted the block build or use privilege
	/// </summary>
	NoPrivilege,
	/// <summary>
	/// Player does not have the build/use blocks every privilege and the position is claimed by another player
	/// </summary>
	LandClaimed,
	/// <summary>
	/// A mod denied use/placement
	/// </summary>
	DeniedByMod
}
