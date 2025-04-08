namespace Vintagestory.API.Common;

/// <summary>
/// Custom transition speed handler
/// </summary>
/// <param name="transType"></param>
/// <param name="stack"></param>
/// <param name="mulByConfig">Multiplier set by other configuration, if any, otherwise 1</param>
/// <returns></returns>
public delegate float CustomGetTransitionSpeedMulDelegate(EnumTransitionType transType, ItemStack stack, float mulByConfig);
