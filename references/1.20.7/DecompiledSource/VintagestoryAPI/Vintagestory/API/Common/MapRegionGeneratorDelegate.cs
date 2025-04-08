using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public delegate void MapRegionGeneratorDelegate(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null);
