using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Triggered when the server loaded a map region from disk or generated a new one
/// </summary>
/// <param name="mapCoord">regionX and regionZ (multiply with region size to get block position)</param>
/// <param name="region"></param>
public delegate void MapRegionLoadedDelegate(Vec2i mapCoord, IMapRegion region);
