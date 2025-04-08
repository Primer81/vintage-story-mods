using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorDoor : StrongBlockBehavior, IMultiBlockColSelBoxes, IMultiBlockBlockProperties
{
	public AssetLocation OpenSound;

	public AssetLocation CloseSound;

	public int width;

	public int height;

	public bool handopenable;

	public bool airtight;

	private ICoreAPI api;

	public MeshData animatableOrigMesh;

	public Shape animatableShape;

	public string animatableDictKey;

	public BlockBehaviorDoor(Block block)
		: base(block)
	{
		airtight = block.Attributes["airtight"].AsBool(defaultValue: true);
		width = block.Attributes["width"].AsInt(1);
		height = block.Attributes["height"].AsInt(1);
		handopenable = block.Attributes["handopenable"].AsBool(defaultValue: true);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		this.api = api;
		OpenSound = (CloseSound = AssetLocation.Create(block.Attributes["triggerSound"].AsString("sounds/block/door")));
		if (block.Attributes["openSound"].Exists)
		{
			OpenSound = AssetLocation.Create(block.Attributes["openSound"].AsString("sounds/block/door"));
		}
		if (block.Attributes["closeSound"].Exists)
		{
			CloseSound = AssetLocation.Create(block.Attributes["closeSound"].AsString("sounds/block/door"));
		}
		base.OnLoaded(api);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
	{
		BEBehaviorDoor beh = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorDoor>();
		bool opened = !beh.Opened;
		if (activationArgs != null)
		{
			opened = activationArgs.GetBool("opened", opened);
		}
		if (beh.Opened != opened)
		{
			beh.ToggleDoorState(null, opened);
		}
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorDoor beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		if (beh != null)
		{
			decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, beh.RotateYRad, 0f);
		}
	}

	public static BEBehaviorDoor getDoorAt(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorDoor door = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		if (door != null)
		{
			return door;
		}
		if (world.BlockAccessor.GetBlock(pos) is BlockMultiblock blockMb)
		{
			return world.BlockAccessor.GetBlockEntity(pos.AddCopy(blockMb.OffsetInv))?.GetBehavior<BEBehaviorDoor>();
		}
		return null;
	}

	protected bool hasCombinableLeftDoor(IWorldAccessor world, float RotateYRad, BlockPos pos, int doorWidth)
	{
		BlockPos leftPos = pos.AddCopy((int)Math.Round(Math.Sin(RotateYRad - (float)Math.PI / 2f)), 0, (int)Math.Round(Math.Cos(RotateYRad - (float)Math.PI / 2f)));
		BEBehaviorDoor leftDoor = getDoorAt(world, leftPos);
		if (leftDoor != null && !leftDoor.InvertHandles && leftDoor.facingWhenClosed == BlockFacing.HorizontalFromYaw(RotateYRad))
		{
			return true;
		}
		return false;
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		float rotRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
		bool blocked = false;
		bool invertHandle = hasCombinableLeftDoor(world, rotRad, blockSel.Position, width);
		IterateOverEach(blockSel.Position, rotRad, invertHandle, delegate(BlockPos mpos)
		{
			if (mpos == blockSel.Position)
			{
				return true;
			}
			if (!world.BlockAccessor.GetBlock(mpos, 1).IsReplacableBy(block))
			{
				blocked = true;
				return false;
			}
			return true;
		});
		if (blocked)
		{
			handling = EnumHandling.PreventDefault;
			failureCode = "notenoughspace";
			return false;
		}
		return base.CanPlaceBlock(world, byPlayer, blockSel, ref handling, ref failureCode);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		BlockPos pos = blockSel.Position;
		IBlockAccessor ba = world.BlockAccessor;
		if (ba.GetBlock(pos, 1).Id == 0 && block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return placeDoor(world, byPlayer, itemstack, blockSel, pos, ba);
		}
		return false;
	}

	public bool placeDoor(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, BlockPos pos, IBlockAccessor ba)
	{
		ba.SetBlock(block.BlockId, pos);
		(ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()).OnBlockPlaced(itemstack, byPlayer, blockSel);
		if (world.Side == EnumAppSide.Server)
		{
			placeMultiblockParts(world, pos);
		}
		return true;
	}

	public void placeMultiblockParts(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorDoor beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		float rotRad = beh?.RotateYRad ?? 0f;
		IterateOverEach(pos, rotRad, beh?.InvertHandles ?? false, delegate(BlockPos mpos)
		{
			if (mpos == pos)
			{
				return true;
			}
			int num = mpos.X - pos.X;
			int num2 = mpos.Y - pos.Y;
			int num3 = mpos.Z - pos.Z;
			string text = ((num < 0) ? "n" : ((num > 0) ? "p" : "")) + Math.Abs(num);
			string text2 = ((num2 < 0) ? "n" : ((num2 > 0) ? "p" : "")) + Math.Abs(num2);
			string text3 = ((num3 < 0) ? "n" : ((num3 > 0) ? "p" : "")) + Math.Abs(num3);
			AssetLocation blockCode = new AssetLocation("multiblock-monolithic-" + text + "-" + text2 + "-" + text3);
			Block block = world.GetBlock(blockCode);
			world.BlockAccessor.SetBlock(block.Id, mpos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(mpos);
			}
			return true;
		});
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		if (world.Side == EnumAppSide.Client)
		{
			return;
		}
		BEBehaviorDoor beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		float rotRad = beh?.RotateYRad ?? 0f;
		IterateOverEach(pos, rotRad, beh?.InvertHandles ?? false, delegate(BlockPos mpos)
		{
			if (mpos == pos)
			{
				return true;
			}
			if (world.BlockAccessor.GetBlock(mpos) is BlockMultiblock)
			{
				world.BlockAccessor.SetBlock(0, mpos);
				if (world.Side == EnumAppSide.Server)
				{
					world.BlockAccessor.TriggerNeighbourBlockUpdate(mpos);
				}
			}
			return true;
		});
		base.OnBlockRemoved(world, pos, ref handling);
	}

	public void IterateOverEach(BlockPos pos, float yRotRad, bool invertHandle, ActionConsumable<BlockPos> onBlock)
	{
		BlockPos tmpPos = new BlockPos(pos.dimension);
		for (int dx = 0; dx < width; dx++)
		{
			for (int dy = 0; dy < height; dy++)
			{
				for (int dz = 0; dz < width; dz++)
				{
					Vec3i offset = BEBehaviorDoor.getAdjacentOffset(dx, dz, dy, yRotRad, invertHandle);
					tmpPos.Set(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);
					if (!onBlock(tmpPos))
					{
						return;
					}
				}
			}
		}
	}

	public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		return getColSelBoxes(blockAccessor, pos, offset);
	}

	public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		return getColSelBoxes(blockAccessor, pos, offset);
	}

	private static Cuboidf[] getColSelBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		BEBehaviorDoor beh = blockAccessor.GetBlockEntity(pos.AddCopy(offset.X, offset.Y, offset.Z))?.GetBehavior<BEBehaviorDoor>();
		if (beh == null)
		{
			return null;
		}
		Vec3i rightBackOffset = beh.getAdjacentOffset(-1, -1);
		if (offset.X == rightBackOffset.X && offset.Z == rightBackOffset.Z)
		{
			return null;
		}
		if (beh.Opened)
		{
			Vec3i rightOffset = beh.getAdjacentOffset(-1);
			if (offset.X == rightOffset.X && offset.Z == rightOffset.Z)
			{
				return null;
			}
		}
		else
		{
			Vec3i backOffset = beh.getAdjacentOffset(0, -1);
			if (offset.X == backOffset.X && offset.Z == backOffset.Z)
			{
				return null;
			}
		}
		return beh.ColSelBoxes;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>()?.ColSelBoxes ?? null;
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing, ref EnumHandling handled)
	{
		return base.GetParticleBreakBox(blockAccess, pos, facing, ref handled);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData, ref EnumHandling handled)
	{
		BEBehaviorDoor beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		if (beh.Opened)
		{
			float rot = (beh.InvertHandles ? 90 : (-90));
			decalModelData = decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, rot * ((float)Math.PI / 180f), 0f);
			if (!beh.InvertHandles)
			{
				decalModelData = decalModelData.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 1f, 1f, -1f);
			}
		}
		base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData, ref handled);
	}

	public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
	{
		doorNameWithMaterial(sb);
	}

	public override void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
	{
	}

	private void doorNameWithMaterial(StringBuilder sb)
	{
		if (block.Variant.ContainsKey("wood"))
		{
			string doorname = sb.ToString();
			sb.Clear();
			sb.Append(Lang.Get("doorname-with-material", doorname, Lang.Get("material-" + block.Variant["wood"])));
		}
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorDoor beh = block.GetBEBehavior<BEBehaviorDoor>(pos);
		if (beh == null)
		{
			return 0f;
		}
		if (!beh.IsSideSolid(face))
		{
			return 0f;
		}
		if (block.Variant["style"] == "sleek-windowed")
		{
			return 1f;
		}
		if (!airtight)
		{
			return 0f;
		}
		return 1f;
	}

	public float MBGetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, Vec3i offset)
	{
		BEBehaviorDoor beh = block.GetBEBehavior<BEBehaviorDoor>(pos.AddCopy(offset.X, offset.Y, offset.Z));
		if (beh == null)
		{
			return 0f;
		}
		if (!beh.IsSideSolid(face))
		{
			return 0f;
		}
		if (block.Variant["style"] == "sleek-windowed")
		{
			if (offset.Y != -1)
			{
				return 1f;
			}
			return 0f;
		}
		if (!airtight)
		{
			return 0f;
		}
		return 1f;
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BEBehaviorDoor beh = block.GetBEBehavior<BEBehaviorDoor>(pos);
		if (beh == null)
		{
			return 0;
		}
		if (type == EnumRetentionType.Sound)
		{
			if (!beh.IsSideSolid(facing))
			{
				return 0;
			}
			return 3;
		}
		if (!airtight)
		{
			return 0;
		}
		if (api.World.Config.GetBool("openDoorsNotSolid"))
		{
			if (!beh.IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!beh.IsSideSolid(facing) && !beh.IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	public int MBGetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, Vec3i offset)
	{
		BEBehaviorDoor beh = block.GetBEBehavior<BEBehaviorDoor>(pos.AddCopy(offset.X, offset.Y, offset.Z));
		if (beh == null)
		{
			return 0;
		}
		if (type == EnumRetentionType.Sound)
		{
			if (!beh.IsSideSolid(facing))
			{
				return 0;
			}
			return 3;
		}
		if (!airtight)
		{
			return 0;
		}
		if (api.World.Config.GetBool("openDoorsNotSolid"))
		{
			if (!beh.IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!beh.IsSideSolid(facing) && !beh.IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	private int getInsulation(BlockPos pos)
	{
		EnumBlockMaterial mat = block.GetBlockMaterial(api.World.BlockAccessor, pos);
		if (mat == EnumBlockMaterial.Ore || mat == EnumBlockMaterial.Stone || mat == EnumBlockMaterial.Soil || mat == EnumBlockMaterial.Ceramic)
		{
			return -1;
		}
		return 1;
	}

	public bool MBCanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea, Vec3i offsetInv)
	{
		return false;
	}

	public JsonObject MBGetAttributes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return null;
	}
}
