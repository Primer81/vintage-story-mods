using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorTrapDoor : BEBehaviorAnimatable, IInteractable, IRotatable
{
	protected bool opened;

	protected MeshData mesh;

	protected Cuboidf[] boxesClosed;

	protected Cuboidf[] boxesOpened;

	public int AttachedFace;

	public int RotDeg;

	protected BlockBehaviorTrapDoor doorBh;

	public float RotRad => (float)RotDeg * ((float)Math.PI / 180f);

	public BlockFacing facingWhenClosed
	{
		get
		{
			if (BlockFacing.ALLFACES[AttachedFace].IsVertical)
			{
				return BlockFacing.ALLFACES[AttachedFace].Opposite;
			}
			return BlockFacing.DOWN.FaceWhenRotatedBy(0f, (float)BlockFacing.ALLFACES[AttachedFace].HorizontalAngleIndex * 90f * ((float)Math.PI / 180f) + (float)Math.PI / 2f, RotRad);
		}
	}

	public BlockFacing facingWhenOpened
	{
		get
		{
			if (BlockFacing.ALLFACES[AttachedFace].IsVertical)
			{
				return BlockFacing.ALLFACES[AttachedFace].Opposite.FaceWhenRotatedBy((BlockFacing.ALLFACES[AttachedFace].Negative ? (-90f) : 90f) * ((float)Math.PI / 180f), 0f, 0f).FaceWhenRotatedBy(0f, RotRad, 0f);
			}
			return BlockFacing.ALLFACES[AttachedFace].Opposite;
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

	public BEBehaviorTrapDoor(BlockEntity blockentity)
		: base(blockentity)
	{
		boxesClosed = blockentity.Block.CollisionBoxes;
		doorBh = blockentity.Block.GetBehavior<BlockBehaviorTrapDoor>();
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

	protected void SetupRotationsAndColSelBoxes(bool initalSetup)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			if (doorBh.animatableOrigMesh == null)
			{
				string animkey = "trapdoor-" + Blockentity.Block.Variant["style"];
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
		Matrixf mat = getTfMatrix();
		mesh.MatrixTransform(mat.Values);
		animUtil.renderer.CustomTransform = mat.Values;
	}

	private Matrixf getTfMatrix(float rotz = 0f)
	{
		if (BlockFacing.ALLFACES[AttachedFace].IsVertical)
		{
			return new Matrixf().Translate(0.5f, 0.5f, 0.5f).RotateYDeg(RotDeg).RotateZDeg(BlockFacing.ALLFACES[AttachedFace].Negative ? 180 : 0)
				.Translate(-0.5f, -0.5f, -0.5f);
		}
		int hai = BlockFacing.ALLFACES[AttachedFace].HorizontalAngleIndex;
		Matrixf matrixf = new Matrixf();
		matrixf.Translate(0.5f, 0.5f, 0.5f).RotateYDeg(hai * 90).RotateYDeg(90f)
			.RotateZDeg(RotDeg)
			.Translate(-0.5f, -0.5f, -0.5f);
		return matrixf;
	}

	protected virtual void UpdateHitBoxes()
	{
		Matrixf mat = getTfMatrix();
		boxesClosed = Blockentity.Block.CollisionBoxes;
		Cuboidf[] boxes = new Cuboidf[boxesClosed.Length];
		for (int j = 0; j < boxesClosed.Length; j++)
		{
			boxes[j] = boxesClosed[j].TransformedCopy(mat.Values);
		}
		Cuboidf[] boxesopened = new Cuboidf[boxesClosed.Length];
		for (int i = 0; i < boxesClosed.Length; i++)
		{
			boxesopened[i] = boxesClosed[i].RotatedCopy(90f, 0f, 0f, new Vec3d(0.5, 0.5, 0.5)).TransformedCopy(mat.Values);
		}
		boxesOpened = boxesopened;
		boxesClosed = boxes;
	}

	public virtual void OnBlockPlaced(ItemStack byItemStack, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byItemStack != null)
		{
			AttachedFace = blockSel.Face.Index;
			Vec2f center = blockSel.Face.ToAB(blockSel.Face.PlaneCenter);
			Vec2f hitpos = blockSel.Face.ToAB(blockSel.HitPosition.ToVec3f());
			RotDeg = (int)Math.Round(180f / (float)Math.PI * (float)Math.Atan2(center.A - hitpos.A, center.B - hitpos.B) / 90f) * 90;
			if (blockSel.Face == BlockFacing.WEST || blockSel.Face == BlockFacing.SOUTH)
			{
				RotDeg *= -1;
			}
			SetupRotationsAndColSelBoxes(initalSetup: true);
		}
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
		this.opened = opened;
		ToggleDoorWing(opened);
		BlockEntity be = Blockentity;
		float pitch = (opened ? 1.1f : 0.9f);
		BlockBehaviorTrapDoor bh = Blockentity.Block.GetBehavior<BlockBehaviorTrapDoor>();
		AssetLocation sound = ((!opened) ? bh?.CloseSound : bh?.OpenSound);
		Api.World.PlaySoundAt(sound, (float)be.Pos.X + 0.5f, (float)be.Pos.Y + 0.5f, (float)be.Pos.Z + 0.5f, byPlayer, EnumSoundType.Sound, pitch);
		be.MarkDirty(redrawOnClient: true);
		if (Api.Side == EnumAppSide.Server)
		{
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(base.Pos);
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
		AttachedFace = tree.GetInt("attachedFace");
		RotDeg = tree.GetInt("rotDeg");
		opened = tree.GetBool("opened");
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
		tree.SetInt("attachedFace", AttachedFace);
		tree.SetInt("rotDeg", RotDeg);
		tree.SetBool("opened", opened);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		AttachedFace = tree.GetInt("attachedFace");
		BlockFacing face = BlockFacing.ALLFACES[AttachedFace];
		if (face.IsVertical)
		{
			RotDeg = tree.GetInt("rotDeg");
			RotDeg = GameMath.Mod(RotDeg - degreeRotation, 360);
			tree.SetInt("rotDeg", RotDeg);
		}
		else
		{
			int rIndex = degreeRotation / 90;
			int horizontalAngleIndex = GameMath.Mod(face.HorizontalAngleIndex - rIndex, 4);
			BlockFacing newFace = BlockFacing.HORIZONTALS_ANGLEORDER[horizontalAngleIndex];
			AttachedFace = newFace.Index;
			tree.SetInt("attachedFace", AttachedFace);
		}
	}
}
