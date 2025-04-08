using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public delegate bool TrySpawnEntityDelegate(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId);
