using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public delegate void HitEntityDelegate(IServerPlayer byPlayer, int chunkx, int chunky, int chunkz, int id);
