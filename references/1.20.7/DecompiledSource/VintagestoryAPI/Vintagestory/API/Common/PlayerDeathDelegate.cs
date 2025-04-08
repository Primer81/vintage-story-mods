using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

/// <summary>
/// When the player died, this delegate will fire.
/// </summary>
/// <param name="byPlayer">The player that died.</param>
/// <param name="damageSource">The source of the damage.</param>
public delegate void PlayerDeathDelegate(IServerPlayer byPlayer, DamageSource damageSource);
