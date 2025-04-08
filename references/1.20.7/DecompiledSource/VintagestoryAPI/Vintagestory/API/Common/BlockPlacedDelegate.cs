using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public delegate void BlockPlacedDelegate(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack);
