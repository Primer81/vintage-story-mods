using System;
using System.Security.Cryptography;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public abstract class ModStdWorldGen : ModSystem
{
	public static int SkipStructuresgHashCode;

	public static int SkipPatchesgHashCode;

	public static int SkipCavesgHashCode;

	public static int SkipTreesgHashCode;

	public static int SkipShurbsgHashCode;

	public static int SkipStalagHashCode;

	public static int SkipHotSpringsgHashCode;

	public static int SkipRivuletsgHashCode;

	public static int SkipPondgHashCode;

	public static int SkipCreaturesgHashCode;

	public GlobalConfig GlobalConfig;

	protected const int chunksize = 32;

	internal GenStoryStructures modSys;

	static ModStdWorldGen()
	{
		SkipStructuresgHashCode = BitConverter.ToInt32(SHA256.HashData("structures"u8.ToArray()));
		SkipPatchesgHashCode = BitConverter.ToInt32(SHA256.HashData("patches"u8.ToArray()));
		SkipCavesgHashCode = BitConverter.ToInt32(SHA256.HashData("caves"u8.ToArray()));
		SkipTreesgHashCode = BitConverter.ToInt32(SHA256.HashData("trees"u8.ToArray()));
		SkipShurbsgHashCode = BitConverter.ToInt32(SHA256.HashData("shrubs"u8.ToArray()));
		SkipHotSpringsgHashCode = BitConverter.ToInt32(SHA256.HashData("hotsprings"u8.ToArray()));
		SkipRivuletsgHashCode = BitConverter.ToInt32(SHA256.HashData("rivulets"u8.ToArray()));
		SkipStalagHashCode = BitConverter.ToInt32(SHA256.HashData("stalag"u8.ToArray()));
		SkipPondgHashCode = BitConverter.ToInt32(SHA256.HashData("pond"u8.ToArray()));
		SkipCreaturesgHashCode = BitConverter.ToInt32(SHA256.HashData("creatures"u8.ToArray()));
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public void LoadGlobalConfig(ICoreServerAPI api)
	{
		modSys = api.ModLoader.GetModSystem<GenStoryStructures>();
		GlobalConfig = GlobalConfig.GetInstance(api);
	}

	public string GetIntersectingStructure(Vec3d position, int category)
	{
		return modSys.GetStoryStructureCodeAt(position, category);
	}

	public string GetIntersectingStructure(int x, int z, int category)
	{
		return modSys.GetStoryStructureCodeAt(x, z, category);
	}

	public StoryStructureLocation GetIntersectingStructure(int x, int z)
	{
		return modSys.GetStoryStructureAt(x, z);
	}

	public string GetIntersectingStructure(BlockPos position, int category)
	{
		return modSys.GetStoryStructureCodeAt(position, category);
	}
}
