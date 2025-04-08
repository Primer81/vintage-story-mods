using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public delegate bool TryGenerateHandler(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, string locationCode);
