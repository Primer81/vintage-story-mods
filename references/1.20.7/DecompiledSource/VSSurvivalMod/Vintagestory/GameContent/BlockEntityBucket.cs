using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityBucket : BlockEntityLiquidContainer, IRotatable
{
	private MeshData currentMesh;

	private BlockBucket ownBlock;

	public float MeshAngle;

	public override string InventoryClassName => "bucket";

	public BlockEntityBucket()
	{
		inventory = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockBucket;
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	internal MeshData GenMesh()
	{
		if (ownBlock == null)
		{
			return null;
		}
		MeshData mesh = ownBlock.GenMesh(Api as ICoreClientAPI, GetContent(), Pos);
		if (mesh.CustomInts != null)
		{
			for (int i = 0; i < mesh.CustomInts.Count; i++)
			{
				mesh.CustomInts.Values[i] |= 134217728;
				mesh.CustomInts.Values[i] |= 67108864;
			}
		}
		return mesh;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (currentMesh != null)
		{
			mesher.AddMeshData(currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f));
		}
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemSlot slot = inventory[0];
		if (slot.Empty)
		{
			dsc.AppendLine(Lang.Get("Empty"));
			return;
		}
		dsc.AppendLine(Lang.Get("Contents: {0}x{1}", slot.Itemstack.StackSize, slot.Itemstack.GetName()));
	}
}
