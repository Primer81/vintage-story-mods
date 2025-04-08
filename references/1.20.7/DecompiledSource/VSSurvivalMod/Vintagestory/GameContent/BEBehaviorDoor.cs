using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorDoor : BEBehaviorAnimatable, IInteractable, IRotatable
{
	public float RotateYRad;

	protected bool opened;

	protected bool invertHandles;

	protected MeshData mesh;

	protected Cuboidf[] boxesClosed;

	protected Cuboidf[] boxesOpened;

	protected Vec3i leftDoorOffset;

	protected Vec3i rightDoorOffset;

	protected BlockBehaviorDoor doorBh;

	public BlockFacing facingWhenClosed => BlockFacing.HorizontalFromYaw(RotateYRad);

	public BlockFacing facingWhenOpened
	{
		get
		{
			if (!invertHandles)
			{
				return facingWhenClosed.GetCW();
			}
			return facingWhenClosed.GetCCW();
		}
	}

	private BEBehaviorDoor leftDoor
	{
		get
		{
			if (!(leftDoorOffset == null))
			{
				return BlockBehaviorDoor.getDoorAt(Api.World, base.Pos.AddCopy(leftDoorOffset));
			}
			return null;
		}
		set
		{
			leftDoorOffset = value?.Pos.SubCopy(base.Pos).ToVec3i();
		}
	}

	private BEBehaviorDoor rightDoor
	{
		get
		{
			if (!(rightDoorOffset == null))
			{
				return BlockBehaviorDoor.getDoorAt(Api.World, base.Pos.AddCopy(rightDoorOffset));
			}
			return null;
		}
		set
		{
			rightDoorOffset = value?.Pos.SubCopy(base.Pos).ToVec3i();
		}
	}

	public Cuboidf[] ColSelBoxes
	{
		get
		{
			if (!opened)
			{
				return boxesClosed;
			}
			return boxesOpened;
		}
	}

	public bool Opened => opened;

	public bool InvertHandles => invertHandles;

	public BEBehaviorDoor(BlockEntity blockentity)
		: base(blockentity)
	{
		boxesClosed = blockentity.Block.CollisionBoxes;
		doorBh = blockentity.Block.GetBehavior<BlockBehaviorDoor>();
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		SetupRotationsAndColSelBoxes(initalSetup: false);
		if (opened && animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("opened"))
		{
			ToggleDoorWing(opened: true);
		}
	}

	public BlockPos getAdjacentPosition(int right, int back = 0, int up = 0)
	{
		return Blockentity.Pos.AddCopy(getAdjacentOffset(right, back, up, RotateYRad, invertHandles));
	}

	public Vec3i getAdjacentOffset(int right, int back = 0, int up = 0)
	{
		return getAdjacentOffset(right, back, up, RotateYRad, invertHandles);
	}

	public static Vec3i getAdjacentOffset(int right, int back, int up, float rotateYRad, bool invertHandles)
	{
		if (invertHandles)
		{
			right = -right;
		}
		return new Vec3i(right * (int)Math.Round(Math.Sin(rotateYRad + (float)Math.PI / 2f)) - back * (int)Math.Round(Math.Sin(rotateYRad)), up, right * (int)Math.Round(Math.Cos(rotateYRad + (float)Math.PI / 2f)) - back * (int)Math.Round(Math.Cos(rotateYRad)));
	}

	internal void SetupRotationsAndColSelBoxes(bool initalSetup)
	{
		int width = doorBh.width;
		if (initalSetup)
		{
			BlockPos leftPos = Blockentity.Pos.AddCopy(width * (int)Math.Round(Math.Sin(RotateYRad - (float)Math.PI / 2f)), 0, width * (int)Math.Round(Math.Cos(RotateYRad - (float)Math.PI / 2f)));
			leftDoor = BlockBehaviorDoor.getDoorAt(Api.World, leftPos);
		}
		if (leftDoor != null && !leftDoor.invertHandles && invertHandles)
		{
			leftDoor.rightDoor = this;
		}
		if (initalSetup)
		{
			if (leftDoor != null && !leftDoor.invertHandles && leftDoor.facingWhenClosed == facingWhenClosed)
			{
				invertHandles = true;
				leftDoor.rightDoor = this;
				Blockentity.MarkDirty(redrawOnClient: true);
			}
			BlockPos rightPos = Blockentity.Pos.AddCopy(width * (int)Math.Round(Math.Sin(RotateYRad + (float)Math.PI / 2f)), 0, width * (int)Math.Round(Math.Cos(RotateYRad + (float)Math.PI / 2f)));
			BEBehaviorDoor rightDoor = BlockBehaviorDoor.getDoorAt(Api.World, rightPos);
			if (leftDoor == null && rightDoor != null && !rightDoor.invertHandles)
			{
				BEBehaviorDoor bEBehaviorDoor = rightDoor.rightDoor;
				if ((bEBehaviorDoor == null || !bEBehaviorDoor.invertHandles) && rightDoor.facingWhenClosed == facingWhenClosed && Api.Side == EnumAppSide.Server)
				{
					if (rightDoor.doorBh.width > 1)
					{
						Api.World.BlockAccessor.SetBlock(0, rightDoor.Blockentity.Pos);
						BlockPos rightDoorPos = Blockentity.Pos.AddCopy((rightDoor.doorBh.width + width - 1) * (int)Math.Round(Math.Sin(RotateYRad + (float)Math.PI / 2f)), 0, (rightDoor.doorBh.width + width - 1) * (int)Math.Round(Math.Cos(RotateYRad + (float)Math.PI / 2f)));
						Api.World.BlockAccessor.SetBlock(rightDoor.Block.Id, rightDoorPos);
						rightDoor = base.Block.GetBEBehavior<BEBehaviorDoor>(rightDoorPos);
						rightDoor.RotateYRad = RotateYRad;
						rightDoor.invertHandles = true;
						rightDoor.doorBh.placeMultiblockParts(Api.World, rightDoorPos);
						this.rightDoor = rightDoor;
						rightDoor.SetupRotationsAndColSelBoxes(initalSetup: true);
						rightDoor.leftDoor = this;
						rightDoor.Blockentity.MarkDirty(redrawOnClient: true);
					}
					else
					{
						rightDoor.invertHandles = true;
						this.rightDoor = rightDoor;
						rightDoor.leftDoor = this;
						rightDoor.Blockentity.MarkDirty(redrawOnClient: true);
					}
				}
			}
		}
		if (Api.Side == EnumAppSide.Client)
		{
			if (doorBh.animatableOrigMesh == null)
			{
				string animkey = "door-" + Blockentity.Block.Variant["style"];
				doorBh.animatableOrigMesh = animUtil.CreateMesh(animkey, null, out var shape, null);
				doorBh.animatableShape = shape;
				doorBh.animatableDictKey = animkey;
			}
			if (doorBh.animatableOrigMesh != null)
			{
				animUtil.InitializeAnimator(doorBh.animatableDictKey, doorBh.animatableOrigMesh, doorBh.animatableShape, null);
				UpdateMeshAndAnimations();
			}
		}
		UpdateHitBoxes();
	}

	protected virtual void UpdateMeshAndAnimations()
	{
		mesh = doorBh.animatableOrigMesh.Clone();
		if (RotateYRad != 0f)
		{
			float rot = (invertHandles ? (0f - RotateYRad) : RotateYRad);
			mesh = mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, rot, 0f);
			animUtil.renderer.rotationDeg.Y = rot * (180f / (float)Math.PI);
		}
		if (invertHandles)
		{
			Matrixf matf = new Matrixf();
			matf.Translate(0.5f, 0.5f, 0.5f).Scale(-1f, 1f, 1f).Translate(-0.5f, -0.5f, -0.5f);
			mesh.MatrixTransform(matf.Values);
			animUtil.renderer.backfaceCulling = false;
			animUtil.renderer.ScaleX = -1f;
		}
	}

	protected virtual void UpdateHitBoxes()
	{
		if (RotateYRad != 0f)
		{
			boxesClosed = Blockentity.Block.CollisionBoxes;
			Cuboidf[] boxes = new Cuboidf[boxesClosed.Length];
			for (int j = 0; j < boxesClosed.Length; j++)
			{
				boxes[j] = boxesClosed[j].RotatedCopy(0f, RotateYRad * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5));
			}
			boxesClosed = boxes;
		}
		Cuboidf[] boxesopened = new Cuboidf[boxesClosed.Length];
		for (int i = 0; i < boxesClosed.Length; i++)
		{
			boxesopened[i] = boxesClosed[i].RotatedCopy(0f, invertHandles ? 90 : (-90), 0f, new Vec3d(0.5, 0.5, 0.5));
		}
		boxesOpened = boxesopened;
	}

	public virtual void OnBlockPlaced(ItemStack byItemStack, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byItemStack != null)
		{
			RotateYRad = getRotateYRad(byPlayer, blockSel);
			SetupRotationsAndColSelBoxes(initalSetup: true);
		}
	}

	public static float getRotateYRad(IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
		double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
		double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
		float num = (float)Math.Atan2(y, dz);
		float deg90 = (float)Math.PI / 2f;
		return (float)(int)Math.Round(num / deg90) * deg90;
	}

	public bool IsSideSolid(BlockFacing facing)
	{
		if (opened || facing != facingWhenClosed)
		{
			if (opened)
			{
				return facing == facingWhenOpened;
			}
			return false;
		}
		return true;
	}

	public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (!doorBh.handopenable && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			(Api as ICoreClientAPI).TriggerIngameError(this, "nothandopenable", Lang.Get("This door cannot be opened by hand."));
			return true;
		}
		ToggleDoorState(byPlayer, !opened);
		handling = EnumHandling.PreventDefault;
		return true;
	}

	public void ToggleDoorState(IPlayer byPlayer, bool opened)
	{
		float breakChance = base.Block.Attributes["breakOnTriggerChance"].AsFloat();
		if (Api.Side == EnumAppSide.Server && Api.World.Rand.NextDouble() < (double)breakChance && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			Api.World.BlockAccessor.BreakBlock(base.Pos, byPlayer);
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), base.Pos, 0.0);
			return;
		}
		this.opened = opened;
		ToggleDoorWing(opened);
		BlockEntity be = Blockentity;
		float pitch = (opened ? 1.1f : 0.9f);
		BlockBehaviorDoor bh = Blockentity.Block.GetBehavior<BlockBehaviorDoor>();
		AssetLocation sound = ((!opened) ? bh?.CloseSound : bh?.OpenSound);
		Api.World.PlaySoundAt(sound, (float)be.Pos.X + 0.5f, (float)be.Pos.InternalY + 0.5f, (float)be.Pos.Z + 0.5f, byPlayer, EnumSoundType.Sound, pitch);
		if (leftDoor != null && invertHandles)
		{
			leftDoor.ToggleDoorWing(opened);
			leftDoor.UpdateNeighbors();
		}
		else if (rightDoor != null)
		{
			rightDoor.ToggleDoorWing(opened);
			rightDoor.UpdateNeighbors();
		}
		be.MarkDirty(redrawOnClient: true);
		UpdateNeighbors();
	}

	private void UpdateNeighbors()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		BlockPos tempPos = new BlockPos();
		tempPos.dimension = base.Pos.dimension;
		for (int y = 0; y < doorBh.height; y++)
		{
			tempPos.Set(base.Pos).Add(0, y, 0);
			BlockFacing sideMove = BlockFacing.ALLFACES[Opened ? facingWhenClosed.HorizontalAngleIndex : facingWhenOpened.HorizontalAngleIndex];
			for (int x = 0; x < doorBh.width; x++)
			{
				Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(tempPos);
				tempPos.Add(sideMove);
			}
		}
	}

	private void ToggleDoorWing(bool opened)
	{
		this.opened = opened;
		if (!opened)
		{
			animUtil.StopAnimation("opened");
		}
		else
		{
			float easingSpeed = Blockentity.Block.Attributes?["easingSpeed"].AsFloat(10f) ?? 10f;
			animUtil.StartAnimation(new AnimationMetaData
			{
				Animation = "opened",
				Code = "opened",
				EaseInSpeed = easingSpeed,
				EaseOutSpeed = easingSpeed
			});
		}
		Blockentity.MarkDirty();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (!base.OnTesselation(mesher, tessThreadTesselator))
		{
			mesher.AddMeshData(mesh);
		}
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		bool beforeOpened = opened;
		RotateYRad = tree.GetFloat("rotateYRad");
		opened = tree.GetBool("opened");
		invertHandles = tree.GetBool("invertHandles");
		leftDoorOffset = tree.GetVec3i("leftDoorPos");
		rightDoorOffset = tree.GetVec3i("rightDoorPos");
		if (opened != beforeOpened && animUtil != null)
		{
			ToggleDoorWing(opened);
		}
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			UpdateMeshAndAnimations();
			if (opened && !beforeOpened && animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("opened"))
			{
				ToggleDoorWing(opened: true);
			}
			UpdateHitBoxes();
			Api.World.BlockAccessor.MarkBlockDirty(base.Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("rotateYRad", RotateYRad);
		tree.SetBool("opened", opened);
		tree.SetBool("invertHandles", invertHandles);
		if (leftDoorOffset != null)
		{
			tree.SetVec3i("leftDoorPos", leftDoorOffset);
		}
		if (rightDoorOffset != null)
		{
			tree.SetVec3i("rightDoorPos", rightDoorOffset);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine(facingWhenClosed?.ToString() + (invertHandles ? "-inv " : " ") + (opened ? "open" : "closed"));
			dsc.AppendLine(doorBh.height + "x" + doorBh.width + ((leftDoorOffset != null) ? (" leftdoor at:" + leftDoorOffset) : " ") + ((rightDoorOffset != null) ? (" rightdoor at:" + rightDoorOffset) : " "));
			EnumHandling h = EnumHandling.PassThrough;
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.NORTH, base.Pos, ref h) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: North");
			}
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.EAST, base.Pos, ref h) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: East");
			}
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.SOUTH, base.Pos, ref h) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: South");
			}
			if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.WEST, base.Pos, ref h) > 0f)
			{
				dsc.AppendLine("Barrier to liquid on side: West");
			}
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		RotateYRad = tree.GetFloat("rotateYRad");
		RotateYRad = (RotateYRad - (float)degreeRotation * ((float)Math.PI / 180f)) % ((float)Math.PI * 2f);
		tree.SetFloat("rotateYRad", RotateYRad);
	}
}
