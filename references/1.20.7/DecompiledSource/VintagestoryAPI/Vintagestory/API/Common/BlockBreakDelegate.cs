using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public delegate void BlockBreakDelegate(IServerPlayer byPlayer, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling);
