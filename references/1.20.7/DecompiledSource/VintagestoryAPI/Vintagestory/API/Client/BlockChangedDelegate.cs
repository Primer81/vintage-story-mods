using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// OldBlock param may be null!
/// </summary>
/// <param name="pos"></param>
/// <param name="oldBlock"></param>
public delegate void BlockChangedDelegate(BlockPos pos, Block oldBlock);
