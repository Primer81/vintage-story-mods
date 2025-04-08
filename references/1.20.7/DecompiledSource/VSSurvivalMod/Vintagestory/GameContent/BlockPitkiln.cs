using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPitkiln : BlockGroundStorage, IIgnitable, ISmokeEmitter
{
	public Dictionary<string, BuildStage[]> BuildStagesByBlock = new Dictionary<string, BuildStage[]>();

	public Dictionary<string, Shape> ShapesByBlock = new Dictionary<string, Shape>();

	public byte[] litKilnLightHsv = new byte[3] { 4, 7, 14 };

	private WorldInteraction[] ingiteInteraction;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Dictionary<string, BuildStageMaterial[]> resolvedMats = new Dictionary<string, BuildStageMaterial[]>();
		List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
		ingiteInteraction = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-firepit-ignite",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift",
				Itemstacks = canIgniteStacks.ToArray(),
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					BlockEntityPitKiln blockEntityPitKiln = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPitKiln;
					return ((blockEntityPitKiln == null || !blockEntityPitKiln.Lit) && blockEntityPitKiln != null && blockEntityPitKiln.CanIgnite()) ? wi.Itemstacks : null;
				}
			}
		};
		Dictionary<string, PitKilnModelConfig> modelConfigs = Attributes["modelConfigs"].AsObject<Dictionary<string, PitKilnModelConfig>>();
		foreach (KeyValuePair<string, JsonItemStackBuildStage[]> val2 in Attributes["buildMats"].AsObject<Dictionary<string, JsonItemStackBuildStage[]>>())
		{
			resolvedMats[val2.Key] = new BuildStageMaterial[val2.Value.Length];
			int j = 0;
			JsonItemStackBuildStage[] value = val2.Value;
			foreach (JsonItemStackBuildStage stack in value)
			{
				if (stack.Resolve(api.World, "pit kiln build material"))
				{
					resolvedMats[val2.Key][j++] = new BuildStageMaterial
					{
						ItemStack = stack.ResolvedItemstack,
						EleCode = stack.EleCode,
						TextureCodeReplace = stack.TextureCodeReplace,
						BurnTimeHours = stack.BurnTimeHours
					};
				}
			}
		}
		foreach (KeyValuePair<string, PitKilnModelConfig> val in modelConfigs)
		{
			if (val.Value?.BuildStages == null || val.Value.BuildMatCodes == null || val.Value.Shape?.Base == null)
			{
				api.World.Logger.Error("Pit kiln model configs: Build stage array, build mat array or composite shape is null. Will ignore this config.");
				continue;
			}
			if (val.Value.BuildStages.Length != val.Value.BuildMatCodes.Length)
			{
				api.World.Logger.Error("Pit kiln model configs: Build stage array and build mat array not the same length, please fix. Will ignore this config.");
				continue;
			}
			AssetLocation loc = val.Value.Shape.Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
			Shape shape = Vintagestory.API.Common.Shape.TryGet(api, loc);
			if (shape == null)
			{
				api.World.Logger.Error("Pit kiln model configs: Shape file {0} not found. Will ignore this config.", val.Value.Shape.Base);
				continue;
			}
			string[] stages = val.Value.BuildStages;
			string[] matcodes = val.Value.BuildMatCodes;
			BuildStage[] resostages = new BuildStage[stages.Length];
			for (int i = 0; i < stages.Length; i++)
			{
				if (!resolvedMats.TryGetValue(matcodes[i], out var stacks))
				{
					api.World.Logger.Error("Pit kiln model configs: No such mat code " + matcodes[i] + ". Please fix. Will ignore all configs.");
					return;
				}
				float miny2 = 0f;
				if (val.Value.MinHitboxY2 != null)
				{
					miny2 = val.Value.MinHitboxY2[GameMath.Clamp(i, 0, val.Value.MinHitboxY2.Length - 1)];
				}
				resostages[i] = new BuildStage
				{
					ElementName = stages[i],
					Materials = stacks,
					MinHitboxY2 = miny2,
					MatCode = matcodes[i]
				};
			}
			BuildStagesByBlock[val.Key] = resostages;
			ShapesByBlock[val.Key] = shape;
		}
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos != null && blockAccessor.GetBlockEntity(pos) is BlockEntityPitKiln { Lit: not false })
		{
			return litKilnLightHsv;
		}
		return base.GetLightHsv(blockAccessor, pos, stack);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage beg)
		{
			beg.OnPlayerInteractStart(byPlayer, blockSel);
			return true;
		}
		return true;
	}

	public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		return base.GetBlockMaterial(blockAccessor, pos, stack);
	}

	public bool TryCreateKiln(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (hotbarSlot.Empty)
		{
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage beg)
		{
			if (!beg.OnTryCreateKiln())
			{
				return false;
			}
			ICoreClientAPI capi = api as ICoreClientAPI;
			bool ok = true;
			BlockFacing[] array = BlockFacing.HORIZONTALS.Append(BlockFacing.DOWN);
			foreach (BlockFacing face in array)
			{
				BlockPos npos = pos.AddCopy(face);
				Block block = world.BlockAccessor.GetBlock(npos);
				if (!block.CanAttachBlockAt(world.BlockAccessor, this, npos, face.Opposite))
				{
					capi?.TriggerIngameError(this, "notsolid", Lang.Get("Pit kilns need to be surrounded by solid, non-flammable blocks"));
					ok = false;
					break;
				}
				if (block.CombustibleProps != null)
				{
					capi?.TriggerIngameError(this, "notsolid", Lang.Get("Pit kilns need to be surrounded by solid, non-flammable blocks"));
					ok = false;
					break;
				}
			}
			if (!ok)
			{
				return false;
			}
			if (world.BlockAccessor.GetBlock(pos.UpCopy()).Replaceable < 6000)
			{
				ok = false;
				capi?.TriggerIngameError(this, "notairspace", Lang.Get("Pit kilns need one block of air space above them"));
			}
			if (!ok)
			{
				return false;
			}
			BuildStage[] buildStages = null;
			bool found = false;
			foreach (KeyValuePair<string, BuildStage[]> val in BuildStagesByBlock)
			{
				if (!beg.Inventory[0].Empty && WildcardUtil.Match(new AssetLocation(val.Key), beg.Inventory[0].Itemstack.Collectible.Code))
				{
					buildStages = val.Value;
					found = true;
					break;
				}
			}
			if (!found)
			{
				BuildStagesByBlock.TryGetValue("*", out buildStages);
			}
			if (buildStages == null)
			{
				return false;
			}
			if (!hotbarSlot.Itemstack.Equals(world, buildStages[0].Materials[0].ItemStack, GlobalConstants.IgnoredStackAttributes) || hotbarSlot.StackSize < buildStages[0].Materials[0].ItemStack.StackSize)
			{
				return false;
			}
			InventoryBase prevInv = beg.Inventory;
			world.BlockAccessor.SetBlock(Id, pos);
			BlockEntityPitKiln begs = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln;
			for (int i = 0; i < prevInv.Count; i++)
			{
				begs.Inventory[i] = prevInv[i];
			}
			begs.MeshAngle = beg.MeshAngle;
			begs.OnCreated(byPlayer);
			begs.updateMeshes();
			begs.MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public new EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln).CanIgnite())
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 4f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public new void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln)?.TryIgnite((byEntity as EntityPlayer).Player);
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln).Lit)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityPitKiln begs)
		{
			if (!begs.IsComplete)
			{
				ItemStack[] stacks = begs.NextBuildStage.Materials.Select((BuildStageMaterial bsm) => bsm.ItemStack).ToArray();
				return new WorldInteraction[1]
				{
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-pitkiln-build",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "shift",
						Itemstacks = stacks.ToArray(),
						GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => stacks
					}
				};
			}
			return ingiteInteraction;
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(this).GetName();
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityPitKiln blockEntity = GetBlockEntity<BlockEntityPitKiln>(pos);
			if (blockEntity == null || !blockEntity.IsBurning)
			{
				return 1f;
			}
			return 10000f;
		}
		return base.GetTraversalCost(pos, creatureType);
	}

	public bool EmitsSmoke(BlockPos pos)
	{
		return (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPitKiln)?.IsBurning ?? false;
	}
}
