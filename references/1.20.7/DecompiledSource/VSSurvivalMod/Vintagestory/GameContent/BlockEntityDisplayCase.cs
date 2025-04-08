using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityDisplayCase : BlockEntityDisplay, IRotatable
{
	protected InventoryGeneric inventory;

	private bool haveCenterPlacement;

	private float[] rotations = new float[4];

	public override string InventoryClassName => "displaycase";

	public override InventoryBase Inventory => inventory;

	public BlockEntityDisplayCase()
	{
		inventory = new InventoryDisplayed(this, 4, "displaycase-0", null);
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (slot.Empty)
		{
			if (TryTake(byPlayer, blockSel))
			{
				return true;
			}
			return false;
		}
		CollectibleObject colObj = slot.Itemstack.Collectible;
		if (colObj.Attributes != null && colObj.Attributes["displaycaseable"].AsBool())
		{
			AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
			if (TryPut(slot, blockSel, byPlayer))
			{
				Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				int index = blockSel.SelectionBoxIndex;
				Api.World.Logger.Audit("{0} Put 1x{1} into DisplayCase slotid {2} at {3}.", byPlayer.PlayerName, inventory[index].Itemstack?.Collectible.Code, index, Pos);
				return true;
			}
			return false;
		}
		(Api as ICoreClientAPI)?.TriggerIngameError(this, "doesnotfit", Lang.Get("This item does not fit into the display case."));
		return true;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel, IPlayer player)
	{
		int index = blockSel.SelectionBoxIndex;
		bool nowCenterPlacement = inventory.Empty && Math.Abs(blockSel.HitPosition.X - 0.5) < 0.1 && Math.Abs(blockSel.HitPosition.Z - 0.5) < 0.1;
		if ((slot.Itemstack.ItemAttributes?["displaycase"]["minHeight"]?.AsFloat(0.25f)).GetValueOrDefault() > (base.Block as BlockDisplayCase)?.height)
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "tootall", Lang.Get("This item is too tall to fit in this display case."));
			return false;
		}
		haveCenterPlacement = nowCenterPlacement;
		if (inventory[index].Empty)
		{
			int num = slot.TryPutInto(Api.World, inventory[index]);
			if (num > 0)
			{
				BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
				double y = player.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
				double dz = (double)(float)player.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
				float angleHor = (float)Math.Atan2(y, dz);
				float deg90 = (float)Math.PI / 2f;
				rotations[index] = (float)(int)Math.Round(angleHor / deg90) * deg90;
				MarkDirty();
			}
			return num > 0;
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int index = blockSel.SelectionBoxIndex;
		if (haveCenterPlacement)
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				if (!inventory[i].Empty)
				{
					index = i;
				}
			}
		}
		if (!inventory[index].Empty)
		{
			ItemStack stack = inventory[index].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(stack))
			{
				AssetLocation sound = stack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				Api.World.Logger.Audit("{0} Took 1x{1} from DisplayCase slotid {2} at {3}.", byPlayer.PlayerName, stack.Collectible.Code, index, Pos);
			}
			if (stack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(stack, Pos);
			}
			updateMesh(index);
			MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		sb.AppendLine();
		if (forPlayer?.CurrentBlockSelection != null)
		{
			int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
			if (index < inventory.Count && !inventory[index].Empty)
			{
				sb.AppendLine(inventory[index].Itemstack.GetName());
			}
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] tfMatrices = new float[4][];
		for (int index = 0; index < 4; index++)
		{
			float x = ((index % 2 == 0) ? 0.3125f : 0.6875f);
			float y = 0.063125f;
			float z = ((index > 1) ? 0.6875f : 0.3125f);
			int rnd = GameMath.MurmurHash3Mod(Pos.X, Pos.Y + index * 50, Pos.Z, 30) - 15;
			JsonObject collObjAttr = inventory[index]?.Itemstack?.Collectible?.Attributes;
			if (collObjAttr != null && !collObjAttr["randomizeInDisplayCase"].AsBool(defaultValue: true))
			{
				rnd = 0;
			}
			float degY = rotations[index] * (180f / (float)Math.PI) + 45f + (float)rnd;
			if (haveCenterPlacement)
			{
				x = 0.5f;
				z = 0.5f;
			}
			tfMatrices[index] = new Matrixf().Translate(0.5f, 0f, 0.5f).Translate(x - 0.5f, y, z - 0.5f).RotateYDeg(degY)
				.Scale(0.75f, 0.75f, 0.75f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		haveCenterPlacement = tree.GetBool("haveCenterPlacement");
		rotations = new float[4]
		{
			tree.GetFloat("rotation0"),
			tree.GetFloat("rotation1"),
			tree.GetFloat("rotation2"),
			tree.GetFloat("rotation3")
		};
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("haveCenterPlacement", haveCenterPlacement);
		tree.SetFloat("rotation0", rotations[0]);
		tree.SetFloat("rotation1", rotations[1]);
		tree.SetFloat("rotation2", rotations[2]);
		tree.SetFloat("rotation3", rotations[3]);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		int[] rot = new int[4] { 0, 1, 3, 2 };
		float[] rots = new float[4];
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("inventory");
		inventory.FromTreeAttributes(treeAttribute);
		ItemSlot[] inv = new ItemSlot[4];
		int start = degreeRotation / 90 % 4;
		for (int j = 0; j < 4; j++)
		{
			rots[j] = tree.GetFloat("rotation" + j);
			inv[j] = inventory[j];
		}
		for (int i = 0; i < 4; i++)
		{
			int index = GameMath.Mod(i - start, 4);
			rotations[rot[i]] = rots[rot[index]] - (float)degreeRotation * ((float)Math.PI / 180f);
			inventory[rot[i]] = inv[rot[index]];
			tree.SetFloat("rotation" + rot[i], rotations[rot[i]]);
		}
		inventory.ToTreeAttributes(treeAttribute);
		tree["inventory"] = treeAttribute;
	}
}
