using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

internal class TreeGenTool : ToolBase
{
	private readonly IRandom _rand;

	private TreeGeneratorsUtil _treeGenerators;

	public float MinTreeSize
	{
		get
		{
			return workspace.FloatValues["std.treeToolMinTreeSize"];
		}
		set
		{
			workspace.FloatValues["std.treeToolMinTreeSize"] = value;
		}
	}

	public float MaxTreeSize
	{
		get
		{
			return workspace.FloatValues["std.treeToolMaxTreeSize"];
		}
		set
		{
			workspace.FloatValues["std.treeToolMaxTreeSize"] = value;
		}
	}

	public string TreeVariant
	{
		get
		{
			return workspace.StringValues["std.treeToolTreeVariant"];
		}
		set
		{
			workspace.StringValues["std.treeToolTreeVariant"] = value;
		}
	}

	public int WithForestFloor
	{
		get
		{
			return workspace.IntValues["std.treeToolWithForestFloor"];
		}
		set
		{
			workspace.IntValues["std.treeToolWithForestFloor"] = value;
		}
	}

	public float VinesGrowthChance
	{
		get
		{
			return workspace.FloatValues["std.treeToolVinesGrowthChance"];
		}
		set
		{
			workspace.FloatValues["std.treeToolVinesGrowthChance"] = value;
		}
	}

	public override Vec3i Size => new Vec3i(0, 0, 0);

	public TreeGenTool()
	{
	}

	public TreeGenTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		_rand = new NormalRandom();
		if (!workspace.FloatValues.ContainsKey("std.treeToolMinTreeSize"))
		{
			MinTreeSize = 0.7f;
		}
		if (!workspace.FloatValues.ContainsKey("std.treeToolMaxTreeSize"))
		{
			MaxTreeSize = 1.3f;
		}
		if (!workspace.StringValues.ContainsKey("std.treeToolTreeVariant"))
		{
			TreeVariant = null;
		}
		if (!workspace.FloatValues.ContainsKey("std.treeToolVinesGrowthChance"))
		{
			VinesGrowthChance = 0f;
		}
		if (!workspace.IntValues.ContainsKey("std.treeToolWithForestFloor"))
		{
			WithForestFloor = 0;
		}
	}

	public override bool OnWorldEditCommand(Vintagestory.ServerMods.WorldEdit.WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		if (_treeGenerators == null)
		{
			_treeGenerators = new TreeGeneratorsUtil(worldEdit.sapi);
		}
		switch (args.PopWord())
		{
		case "tsizemin":
		{
			float size2 = 0.7f;
			if (args.Length > 0)
			{
				float.TryParse(args[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out size2);
			}
			MinTreeSize = size2;
			Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, "Tree Min Size=" + size2 + " set.");
			return true;
		}
		case "tsizemax":
		{
			float size = 0.7f;
			if (args.Length > 0)
			{
				float.TryParse(args[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out size);
			}
			MaxTreeSize = size;
			Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, "Tree Max Size=" + size + " set.");
			return true;
		}
		case "tsize":
		{
			float min = 0.7f;
			if (args.Length > 0)
			{
				float.TryParse(args[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out min);
			}
			MinTreeSize = min;
			float max = 1.3f;
			if (args.Length > 1)
			{
				float.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out max);
			}
			MaxTreeSize = max;
			Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, "Tree Min Size=" + min + ", max size =" + MaxTreeSize + " set.");
			return true;
		}
		case "trnd":
			return true;
		case "tforestfloor":
		{
			bool? on = args.PopBool(false);
			WithForestFloor = (on.GetValueOrDefault() ? 1 : 0);
			Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, "Forest floor generation now {0}.", on.GetValueOrDefault() ? "on" : "off");
			return true;
		}
		case "tvines":
		{
			float chance = (VinesGrowthChance = args.PopFloat(0f).Value);
			Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, "Vines growth chance now at {0}.", chance);
			return true;
		}
		case "tv":
		{
			string variant = args.PopWord();
			int index;
			bool num = int.TryParse(variant, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out index);
			_treeGenerators.ReloadTreeGenerators();
			if (num)
			{
				KeyValuePair<AssetLocation, ITreeGenerator> val = _treeGenerators.GetGenerator(index);
				if (val.Key == null)
				{
					Vintagestory.ServerMods.WorldEdit.WorldEdit.Bad(player, "No such tree variant found.");
					return true;
				}
				TreeVariant = val.Key.ToShortString();
				Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, string.Concat("Tree variant ", val.Key, " set."));
			}
			else if (variant != null && _treeGenerators.GetGenerator(new AssetLocation(variant)) != null)
			{
				TreeVariant = variant;
				Vintagestory.ServerMods.WorldEdit.WorldEdit.Good(player, "Tree variant " + variant + " set.");
			}
			else
			{
				Vintagestory.ServerMods.WorldEdit.WorldEdit.Bad(player, "No such tree variant found.");
			}
			return true;
		}
		default:
			return false;
		}
	}

	public override void OnInteractStart(Vintagestory.ServerMods.WorldEdit.WorldEdit worldEdit, BlockSelection blockSelection)
	{
		if (_treeGenerators == null)
		{
			_treeGenerators = new TreeGeneratorsUtil(worldEdit.sapi);
		}
		if (TreeVariant == null)
		{
			Vintagestory.ServerMods.WorldEdit.WorldEdit.Bad((IServerPlayer)worldEdit.sapi.World.PlayerByUid(workspace.PlayerUID), "Please select a tree variant first.");
			return;
		}
		blockSelection.Position.Add(blockSelection.Face.Opposite);
		ba.ReadFromStagedByDefault = true;
		_treeGenerators.ReloadTreeGenerators();
		_treeGenerators.GetGenerator(new AssetLocation(TreeVariant)).GrowTree(treeGenParams: new TreeGenParams
		{
			skipForestFloor = (WithForestFloor == 0),
			size = MinTreeSize + (float)_rand.NextDouble() * (MaxTreeSize - MinTreeSize),
			vinesGrowthChance = VinesGrowthChance
		}, blockAccessor: ba, pos: blockSelection.Position, random: _rand);
		ba.Commit();
	}
}
