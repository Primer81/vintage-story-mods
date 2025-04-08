using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemRoller : Item
{
	public static List<BlockPos> emptyList = new List<BlockPos>();

	public static List<List<BlockPos>> siteListByFacing = new List<List<BlockPos>>();

	public static List<List<BlockPos>> waterEdgeByFacing = new List<List<BlockPos>>();

	public static List<BlockPos> siteListN = new List<BlockPos>
	{
		new BlockPos(-5, -1, -2),
		new BlockPos(3, 2, 2)
	};

	public static List<BlockPos> waterEdgeListN = new List<BlockPos>
	{
		new BlockPos(3, -1, -2),
		new BlockPos(6, 0, 2)
	};

	public SkillItem[] skillItems;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		siteListByFacing.Add(siteListN);
		waterEdgeByFacing.Add(waterEdgeListN);
		for (int i = 1; i < 4; i++)
		{
			siteListByFacing.Add(rotateList(siteListN, i));
			waterEdgeByFacing.Add(rotateList(waterEdgeListN, i));
		}
		skillItems = new SkillItem[4]
		{
			new SkillItem
			{
				Code = new AssetLocation("east"),
				Name = Lang.Get("facing-east")
			},
			new SkillItem
			{
				Code = new AssetLocation("north"),
				Name = Lang.Get("facing-north")
			},
			new SkillItem
			{
				Code = new AssetLocation("west"),
				Name = Lang.Get("facing-west")
			},
			new SkillItem
			{
				Code = new AssetLocation("south"),
				Name = Lang.Get("facing-south")
			}
		};
		if (api is ICoreClientAPI capi)
		{
			skillItems[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointeast.svg"), 48, 48, 5, -1));
			skillItems[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointnorth.svg"), 48, 48, 5, -1));
			skillItems[2].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointwest.svg"), 48, 48, 5, -1));
			skillItems[3].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointsouth.svg"), 48, 48, 5, -1));
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (skillItems != null)
		{
			SkillItem[] array = skillItems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
		}
	}

	private static List<BlockPos> rotateList(List<BlockPos> startlist, int i)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.RotateY((float)i * ((float)Math.PI / 2f));
		if (i == 2)
		{
			matrixf.Translate(0f, 0f, -1f);
		}
		if (i == 3)
		{
			matrixf.Translate(1f, 0f, -1f);
		}
		List<BlockPos> list = new List<BlockPos>();
		Vec4f vec1 = matrixf.TransformVector(new Vec4f(startlist[0].X, startlist[0].Y, startlist[0].Z, 1f));
		Vec4f vec2 = matrixf.TransformVector(new Vec4f(startlist[1].X, startlist[1].Y, startlist[1].Z, 1f));
		BlockPos minpos = new BlockPos((int)Math.Round(Math.Min(vec1.X, vec2.X)), (int)Math.Round(Math.Min(vec1.Y, vec2.Y)), (int)Math.Round(Math.Min(vec1.Z, vec2.Z)));
		BlockPos maxpos = new BlockPos((int)Math.Round(Math.Max(vec1.X, vec2.X)), (int)Math.Round(Math.Max(vec1.Y, vec2.Y)), (int)Math.Round(Math.Max(vec1.Z, vec2.Z)));
		list.Add(minpos);
		list.Add(maxpos);
		return list;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return GetOrient(byPlayer);
	}

	public static int GetOrient(IPlayer byPlayer)
	{
		siteListN = new List<BlockPos>
		{
			new BlockPos(-5, -1, -1),
			new BlockPos(3, 2, 2)
		};
		waterEdgeListN = new List<BlockPos>
		{
			new BlockPos(3, -1, -1),
			new BlockPos(6, 0, 2)
		};
		siteListByFacing.Clear();
		waterEdgeByFacing.Clear();
		siteListByFacing.Add(siteListN);
		waterEdgeByFacing.Add(waterEdgeListN);
		for (int i = 1; i < 4; i++)
		{
			siteListByFacing.Add(rotateList(siteListN, i));
			waterEdgeByFacing.Add(rotateList(waterEdgeListN, i));
		}
		return ObjectCacheUtil.GetOrCreate(byPlayer.Entity.Api, "rollerOrient-" + byPlayer.PlayerUID, () => 0);
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return skillItems;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		api.ObjectCache["rollerOrient-" + byPlayer.PlayerUID] = toolMode;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (slot.StackSize < 5)
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "need5", Lang.Get("Need 5 rolles to place a boat construction site"));
			return;
		}
		if (!suitableLocation(player, blockSel))
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "unsuitableLocation", Lang.Get("Requires a suitable location near water to place a boat construction site. Boat will roll towards the blue highlighted area. Use tool mode to rotate"));
			return;
		}
		slot.TakeOut(5);
		slot.MarkDirty();
		string material = "oak";
		int orient = GetOrient(player);
		EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("boatconstruction-sailed-" + material));
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(type);
		entity.ServerPos.SetPos(blockSel.Position.ToVec3d().AddCopy(0.5, 1.0, 0.5));
		entity.ServerPos.Yaw = -(float)Math.PI / 2f + (float)orient * ((float)Math.PI / 2f);
		if (orient == 1)
		{
			entity.ServerPos.Z -= 1.0;
		}
		if (orient == 2)
		{
			entity.ServerPos.X -= 1.0;
		}
		if (orient == 3)
		{
			entity.ServerPos.Z += 1.0;
		}
		entity.Pos.SetFrom(entity.ServerPos);
		byEntity.World.SpawnEntity(entity);
		api.World.PlaySoundAt(new AssetLocation("sounds/block/planks"), byEntity, player);
		handling = EnumHandHandling.PreventDefault;
	}

	private bool suitableLocation(IPlayer forPlayer, BlockSelection blockSel)
	{
		int orient = GetOrient(forPlayer);
		List<BlockPos> siteList = siteListByFacing[orient];
		List<BlockPos> waterEdgeList = waterEdgeByFacing[orient];
		IBlockAccessor ba = api.World.BlockAccessor;
		bool placeable = true;
		BlockPos cpos = blockSel.Position;
		BlockPos mingPos = siteList[0].AddCopy(0, 1, 0).Add(cpos);
		BlockPos maxgPos = siteList[1].AddCopy(-1, 0, -1).Add(cpos);
		maxgPos.Y = mingPos.Y;
		api.World.BlockAccessor.WalkBlocks(mingPos, maxgPos, delegate(Block block, int x, int y, int z)
		{
			if (!block.SideIsSolid(new BlockPos(x, y, z), BlockFacing.UP.Index))
			{
				placeable = false;
			}
		});
		if (!placeable)
		{
			return false;
		}
		BlockPos minPos = siteList[0].AddCopy(0, 2, 0).Add(cpos);
		BlockPos maxPos = siteList[1].AddCopy(-1, 1, -1).Add(cpos);
		api.World.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			Cuboidf[] collisionBoxes = block.GetCollisionBoxes(ba, new BlockPos(x, y, z));
			if (collisionBoxes != null && collisionBoxes.Length != 0)
			{
				placeable = false;
			}
		});
		BlockPos minlPos = waterEdgeList[0].AddCopy(0, 1, 0).Add(cpos);
		BlockPos maxlPos = waterEdgeList[1].AddCopy(-1, 0, -1).Add(cpos);
		WalkBlocks(minlPos, maxlPos, delegate(Block block, int x, int y, int z)
		{
			if (!block.IsLiquid())
			{
				placeable = false;
			}
		}, 2);
		return placeable;
	}

	public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, int layer)
	{
		IBlockAccessor ba = api.World.BlockAccessor;
		int x2 = minPos.X;
		int miny = minPos.InternalY;
		int minz = minPos.Z;
		int maxx = maxPos.X;
		int maxy = maxPos.InternalY;
		int maxz = maxPos.Z;
		for (int x = x2; x <= maxx; x++)
		{
			for (int y = miny; y <= maxy; y++)
			{
				for (int z = minz; z <= maxz; z++)
				{
					Block block = ba.GetBlock(x, y, z);
					onBlock(block, x, y, z);
				}
			}
		}
	}
}
