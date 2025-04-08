using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class ModBasicBlocksLoader : ModSystem
{
	private ICoreServerAPI api;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.1;
	}

	public override void Start(ICoreAPI manager)
	{
		if (manager is ICoreServerAPI sapi)
		{
			api = sapi;
			Block block2 = new Block();
			block2.Code = new AssetLocation("mantle");
			block2.Textures = new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("block/mantle")));
			block2.DrawType = EnumDrawType.Cube;
			block2.MatterState = EnumMatterState.Solid;
			block2.BlockMaterial = EnumBlockMaterial.Mantle;
			block2.Replaceable = 0;
			block2.Resistance = 31337f;
			block2.RequiredMiningTier = 196;
			block2.Sounds = new BlockSounds
			{
				Walk = new AssetLocation("sounds/walk/stone"),
				ByTool = new Dictionary<EnumTool, BlockSounds> { 
				{
					EnumTool.Pickaxe,
					new BlockSounds
					{
						Hit = new AssetLocation("sounds/block/rock-hit-pickaxe"),
						Break = new AssetLocation("sounds/block/rock-hit-pickaxe")
					}
				} }
			};
			block2.CreativeInventoryTabs = new string[1] { "general" };
			Block block = block2;
			api.RegisterBlock(block);
		}
	}
}
