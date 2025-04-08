using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common.Entities;

/// <summary>
/// A base class for entities spawning conditions.
/// </summary>
[DocumentAsJson]
public class BaseSpawnConditions : ClimateSpawnCondition
{
	/// <summary>
	/// <!--<jsonoptional>Recommended</jsonoptional><jsondefault>None</jsondefault>-->
	/// The group of the spawn conditions. Vanilla groups are:<br />
	/// - hostile<br />
	/// - neutral<br />
	/// - passive<br />
	/// Hostile creatures should be defined as such here.
	/// This will automatically stop them spawning with a grace timer,
	///     and in locations where hostiles should not spawn.
	/// </summary>
	[DocumentAsJson]
	public string Group;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The minimum light level for an object to spawn.
	/// </summary>
	[DocumentAsJson]
	public int MinLightLevel;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>32</jsondefault>-->
	/// The maximum light level for an object to spawn.
	/// </summary>
	[DocumentAsJson]
	public int MaxLightLevel = 32;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>MaxLight</jsondefault>-->
	/// The type of light counted for spawning purposes.
	/// </summary>
	[DocumentAsJson]
	public EnumLightLevelType LightLevelType = EnumLightLevelType.MaxLight;

	/// <summary>
	/// <!--<jsonoptional>Recommended</jsonoptional><jsondefault>1</jsondefault>-->
	/// the group size for the spawn.
	/// </summary>
	[DocumentAsJson]
	public NatFloat HerdSize = NatFloat.createUniform(1f, 0f);

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// Additional companions for the spawn.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation[] Companions = new AssetLocation[0];

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>"air"</jsondefault>-->
	/// The blocks that the object will spawn in.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation[] InsideBlockCodes = new AssetLocation[1]
	{
		new AssetLocation("air")
	};

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>true</jsondefault>-->
	/// Checks to see if the object requires solid ground.
	/// </summary>
	[DocumentAsJson]
	public bool RequireSolidGround = true;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// checks to see if the object can only spawn in the surface.
	/// </summary>
	[DocumentAsJson]
	public bool TryOnlySurface;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>WorldGenValues</jsondefault>-->
	/// Whether the rain and temperature values are referring to the worldgen values (i.e. yearly averages) or the current values at the moment of spawning.
	/// </summary>
	[DocumentAsJson]
	public EnumGetClimateMode ClimateValueMode;

	protected HashSet<Block> InsideBlockCodesResolved;

	protected string[] InsideBlockCodesBeginsWith;

	protected string[] InsideBlockCodesExact;

	protected string InsideBlockFirstLetters = "";

	/// <summary>
	/// <!--<jsonoptional>Obsolete</jsonoptional>-->
	/// Obsolete. Use <see cref="F:Vintagestory.API.Common.Entities.BaseSpawnConditions.HerdSize" /> instead.
	/// </summary>
	[DocumentAsJson]
	[Obsolete("Use HerdSize instead")]
	public NatFloat GroupSize
	{
		get
		{
			return HerdSize;
		}
		set
		{
			HerdSize = value;
		}
	}

	public bool CanSpawnInside(Block testBlock)
	{
		string testPath = testBlock.Code.Path;
		if (testPath.Length < 1)
		{
			return false;
		}
		if (InsideBlockFirstLetters.IndexOf(testPath[0]) < 0)
		{
			return false;
		}
		if (PathMatchesInsideBlockCodes(testPath))
		{
			return InsideBlockCodesResolved.Contains(testBlock);
		}
		return false;
	}

	private bool PathMatchesInsideBlockCodes(string testPath)
	{
		for (int j = 0; j < InsideBlockCodesExact.Length; j++)
		{
			if (testPath == InsideBlockCodesExact[j])
			{
				return true;
			}
		}
		for (int i = 0; i < InsideBlockCodesBeginsWith.Length; i++)
		{
			if (testPath.StartsWithOrdinal(InsideBlockCodesBeginsWith[i]))
			{
				return true;
			}
		}
		return false;
	}

	public void Initialise(IServerWorldAccessor server, string entityName, Dictionary<AssetLocation, Block[]> searchCache)
	{
		if (InsideBlockCodes == null || InsideBlockCodes.Length == 0)
		{
			return;
		}
		bool anyBlockOk = false;
		AssetLocation[] insideBlockCodes = InsideBlockCodes;
		foreach (AssetLocation val in insideBlockCodes)
		{
			if (!searchCache.TryGetValue(val, out var foundBlocks))
			{
				foundBlocks = (searchCache[val] = server.SearchBlocks(val));
			}
			Block[] array2 = foundBlocks;
			foreach (Block b in array2)
			{
				if (InsideBlockCodesResolved == null)
				{
					InsideBlockCodesResolved = new HashSet<Block>();
				}
				InsideBlockCodesResolved.Add(b);
			}
			anyBlockOk |= foundBlocks.Length != 0;
		}
		if (!anyBlockOk)
		{
			server.Logger.Warning("Entity with code {0} has defined InsideBlockCodes for its spawn conditions, but none of these blocks exists, entity is unlikely to spawn.", entityName);
		}
		List<string> targetEntityCodesList = new List<string>();
		List<string> beginswith = new List<string>();
		AssetLocation[] codes = InsideBlockCodes;
		for (int i = 0; i < codes.Length; i++)
		{
			string code3 = codes[i].Path;
			if (code3.EndsWith('*'))
			{
				beginswith.Add(code3.Substring(0, code3.Length - 1));
			}
			else
			{
				targetEntityCodesList.Add(code3);
			}
		}
		InsideBlockCodesBeginsWith = beginswith.ToArray();
		InsideBlockCodesExact = new string[targetEntityCodesList.Count];
		int j = 0;
		foreach (string code2 in targetEntityCodesList)
		{
			if (code2.Length != 0)
			{
				InsideBlockCodesExact[j++] = code2;
				char c2 = code2[0];
				if (InsideBlockFirstLetters.IndexOf(c2) < 0)
				{
					ReadOnlySpan<char> readOnlySpan = InsideBlockFirstLetters;
					char reference = c2;
					InsideBlockFirstLetters = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
				}
			}
		}
		string[] insideBlockCodesBeginsWith = InsideBlockCodesBeginsWith;
		foreach (string code in insideBlockCodesBeginsWith)
		{
			if (code.Length != 0)
			{
				char c = code[0];
				if (InsideBlockFirstLetters.IndexOf(c) < 0)
				{
					ReadOnlySpan<char> readOnlySpan2 = InsideBlockFirstLetters;
					char reference = c;
					InsideBlockFirstLetters = string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference));
				}
			}
		}
	}
}
