using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Triggered just before a map region gets unloaded
/// </summary>
/// <param name="mapCoord">regionX and regionZ (multiply with region size to get block position)</param>
/// <param name="region"></param>
public delegate void MapRegionUnloadDelegate(Vec2i mapCoord, IMapRegion region);
