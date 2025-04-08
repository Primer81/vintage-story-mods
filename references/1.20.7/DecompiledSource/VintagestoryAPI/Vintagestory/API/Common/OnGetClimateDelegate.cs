using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public delegate void OnGetClimateDelegate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0);
