using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityScrollRack : BlockEntityDisplay, IRotatable
{
	private InventoryGeneric inv;

	private Block block;

	private MeshData mesh;

	private string type;

	private string material;

	private float[] mat;

	private int[] UsableSlots;

	private Cuboidf[] UsableSelectionBoxes;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "scrollrack";

	public override string AttributeTransformCode => "onscrollrackTransform";

	public float MeshAngleRad { get; set; }

	public string Type => type;

	public string Material => material;

	public BlockEntityScrollRack()
	{
		inv = new InventoryGeneric(12, "scrollrack-0", null);
	}

	public int[] getOrCreateUsableSlots()
	{
		if (UsableSlots == null)
		{
			genUsableSlots();
		}
		return UsableSlots;
	}

	public Cuboidf[] getOrCreateSelectionBoxes()
	{
		getOrCreateUsableSlots();
		return UsableSelectionBoxes;
	}

	private void genUsableSlots()
	{
		bool num = isRack(BEBehaviorDoor.getAdjacentOffset(-1, 0, 0, MeshAngleRad, invertHandles: false));
		Dictionary<string, int[]> slotsBySide = (base.Block as BlockScrollRack).slotsBySide;
		List<int> usableSlots = new List<int>();
		usableSlots.AddRange(slotsBySide["mid"]);
		usableSlots.AddRange(slotsBySide["top"]);
		if (num)
		{
			usableSlots.AddRange(slotsBySide["left"]);
		}
		UsableSlots = usableSlots.ToArray();
		Cuboidf[] hitboxes = (base.Block as BlockScrollRack).slotsHitBoxes;
		UsableSelectionBoxes = new Cuboidf[hitboxes.Length];
		for (int i = 0; i < hitboxes.Length; i++)
		{
			UsableSelectionBoxes[i] = hitboxes[i].RotatedCopy(0f, MeshAngleRad * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5));
		}
	}

	private bool isRack(Vec3i offset)
	{
		BlockEntityScrollRack be = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(Pos.AddCopy(offset));
		if (be != null)
		{
			return be.MeshAngleRad == MeshAngleRad;
		}
		return false;
	}

	private void initShelf()
	{
		if (Api != null && type != null && base.Block is BlockScrollRack rack)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				mesh = rack.GetOrCreateMesh(type, material);
				mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad)
					.Translate(-0.5f, -0.5f, -0.5f)
					.Values;
			}
			type = "normal";
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		block = api.World.BlockAccessor.GetBlock(Pos);
		base.Initialize(api);
		if (mesh == null && type != null)
		{
			initShelf();
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (type == null)
		{
			type = byItemStack?.Attributes.GetString("type");
		}
		if (material == null)
		{
			material = byItemStack?.Attributes.GetString("material");
		}
		initShelf();
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		getOrCreateUsableSlots();
		string[] slotSide = (base.Block as BlockScrollRack).slotSide;
		int[] oppositeSlotIndex = (base.Block as BlockScrollRack).oppositeSlotIndex;
		string slotside = slotSide[blockSel.SelectionBoxIndex];
		if (slotside == "bot" || slotside == "right")
		{
			BlockPos npos = ((slotside == "bot") ? Pos.DownCopy() : Pos.AddCopy(BEBehaviorDoor.getAdjacentOffset(1, 0, 0, MeshAngleRad, invertHandles: false)));
			BlockEntityScrollRack be = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(npos);
			if (be == null)
			{
				return false;
			}
			float theirAngle = GameMath.NormaliseAngleRad(be.MeshAngleRad);
			float ourAngle = GameMath.NormaliseAngleRad(MeshAngleRad);
			if (theirAngle % (float)Math.PI == ourAngle % (float)Math.PI)
			{
				if (theirAngle != ourAngle && slotside == "right")
				{
					return false;
				}
				BlockSelection blockSelDown = blockSel.Clone();
				blockSelDown.SelectionBoxIndex = oppositeSlotIndex[(theirAngle == ourAngle) ? blockSelDown.SelectionBoxIndex : (blockSelDown.SelectionBoxIndex ^ 1)];
				return be.OnInteract(byPlayer, blockSelDown);
			}
			return false;
		}
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		CollectibleObject colObj = slot.Itemstack?.Collectible;
		bool shelvable = colObj?.Attributes != null && colObj.Attributes["scrollrackable"].AsBool();
		if (slot.Empty || !shelvable)
		{
			if (TryTake(byPlayer, blockSel))
			{
				return true;
			}
			return false;
		}
		if (shelvable)
		{
			AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
			AssetLocation stackName = slot.Itemstack?.Collectible.Code;
			if (TryPut(slot, blockSel))
			{
				Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				Api.World.Logger.Audit("{0} Put 1x{1} into Scroll rack at {2}.", byPlayer.PlayerName, stackName, Pos);
				return true;
			}
			return false;
		}
		return false;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel)
	{
		int boxIndex = blockSel.SelectionBoxIndex;
		if (boxIndex < 0 || boxIndex >= inv.Count)
		{
			return false;
		}
		if (!UsableSlots.Contains(boxIndex))
		{
			return false;
		}
		int invIndex = boxIndex;
		if (inv[invIndex].Empty)
		{
			int num = slot.TryPutInto(Api.World, inv[invIndex]);
			MarkDirty();
			return num > 0;
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int boxIndex = blockSel.SelectionBoxIndex;
		if (boxIndex < 0 || boxIndex >= inv.Count)
		{
			return false;
		}
		int invIndex = boxIndex;
		if (!inv[invIndex].Empty)
		{
			ItemStack stack = inv[invIndex].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(stack))
			{
				AssetLocation sound = stack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
			}
			if (stack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(stack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Scroll rack at {2}.", byPlayer.PlayerName, stack.Collectible.Code, Pos);
			MarkDirty();
			return true;
		}
		return false;
	}

	protected override float[][] genTransformationMatrices()
	{
		tfMatrices = new float[Inventory.Count][];
		Cuboidf[] hitboxes = (base.Block as BlockScrollRack).slotsHitBoxes;
		for (int i = 0; i < Inventory.Count; i++)
		{
			Cuboidf obj = hitboxes[i];
			float x = obj.MidX;
			float y = obj.MidY;
			float z = obj.MidZ;
			Vec3f off = new Vec3f(x, y, z);
			off = new Matrixf().RotateY(MeshAngleRad).TransformVector(off.ToVec4f(0f)).XYZ;
			tfMatrices[i] = new Matrixf().Translate(off.X, off.Y, off.Z).Translate(0.5f, 0f, 0.5f).RotateY(MeshAngleRad - (float)Math.PI / 2f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("type", type);
		tree.SetString("material", material);
		tree.SetFloat("meshAngleRad", MeshAngleRad);
		tree.SetBool("usableSlotsDirty", UsableSlots == null);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		type = tree.GetString("type");
		material = tree.GetString("material");
		MeshAngleRad = tree.GetFloat("meshAngleRad");
		if (tree.GetBool("usableSlotsDirty"))
		{
			UsableSlots = null;
		}
		initShelf();
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(mesh, mat);
		base.OnTesselation(mesher, tessThreadTesselator);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (forPlayer.CurrentBlockSelection == null)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
		if (index < 0 || index >= inv.Count)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		ItemSlot slot = inv[index];
		if (slot.Empty)
		{
			string slotside = (base.Block as BlockScrollRack).slotSide[index];
			if (slotside == "bot")
			{
				BlockEntityScrollRack be = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(Pos.DownCopy());
				if (be != null)
				{
					float theirAngle = GameMath.NormaliseAngleRad(be.MeshAngleRad);
					float ourAngle = GameMath.NormaliseAngleRad(MeshAngleRad);
					if (theirAngle % (float)Math.PI == ourAngle % (float)Math.PI)
					{
						slot = be.inv[(theirAngle == ourAngle) ? (index + 10) : (11 - index)];
					}
				}
			}
			else if (slotside == "right")
			{
				BlockEntityScrollRack be2 = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(Pos.AddCopy(BEBehaviorDoor.getAdjacentOffset(1, 0, 0, MeshAngleRad, invertHandles: false)));
				if (be2 != null)
				{
					float num = GameMath.NormaliseAngleRad(be2.MeshAngleRad);
					float ourAngle2 = GameMath.NormaliseAngleRad(MeshAngleRad);
					if (num == ourAngle2)
					{
						slot = be2.inv[index - 2];
					}
				}
			}
			sb.AppendLine(slot.Empty ? Lang.Get("Empty") : slot.Itemstack.GetName());
		}
		else
		{
			sb.AppendLine(slot.Itemstack.GetName());
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngleRad = tree.GetFloat("meshAngleRad");
		MeshAngleRad -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngleRad", MeshAngleRad);
	}

	internal void clearUsableSlots()
	{
		genUsableSlots();
		for (int i = 0; i < inv.Count; i++)
		{
			if (!UsableSlots.Contains(i))
			{
				ItemSlot slot = inv[i];
				if (!slot.Empty)
				{
					Vec3d vec = Pos.ToVec3d();
					vec.Add(0.5 - (double)GameMath.Cos(MeshAngleRad) * 0.6, 0.15, 0.5 + (double)GameMath.Sin(MeshAngleRad) * 0.6);
					Api.World.SpawnItemEntity(slot.Itemstack, vec);
					slot.Itemstack = null;
				}
			}
		}
		MarkDirty(redrawOnClient: true);
	}
}
