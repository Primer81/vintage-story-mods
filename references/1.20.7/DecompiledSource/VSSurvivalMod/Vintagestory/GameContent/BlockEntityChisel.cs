using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VSSurvivalMod.Systems.ChiselModes;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityChisel : BlockEntityMicroBlock
{
	public static bool ForceDetailingMode = false;

	public static ChiselMode defaultMode = new OneByChiselMode();

	public ushort[] AvailMaterialQuantities;

	protected byte nowmaterialIndex;

	public static bool ConstrainToAvailableMaterialQuantity = true;

	public bool DetailingMode
	{
		get
		{
			ItemStack stack = ((Api.World as IClientWorldAccessor).Player?.InventoryManager?.ActiveHotbarSlot)?.Itemstack;
			if (Api.Side == EnumAppSide.Client)
			{
				if (stack == null || (stack.Collectible?.Tool).GetValueOrDefault() != EnumTool.Chisel)
				{
					return ForceDetailingMode;
				}
				return true;
			}
			return false;
		}
	}

	public override void WasPlaced(Block block, string blockName)
	{
		base.WasPlaced(block, blockName);
		AvailMaterialQuantities = new ushort[1];
		CuboidWithMaterial cwm = new CuboidWithMaterial();
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			BlockEntityMicroBlock.FromUint(VoxelCuboids[i], cwm);
			AvailMaterialQuantities[0] = (ushort)(AvailMaterialQuantities[0] + cwm.SizeXYZ);
		}
	}

	public SkillItem GetChiselMode(IPlayer player)
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return null;
		}
		ICoreClientAPI clientApi = (ICoreClientAPI)Api;
		ItemSlot slot = player?.InventoryManager?.ActiveHotbarSlot;
		ItemChisel chisel = (ItemChisel)(slot?.Itemstack.Collectible);
		int? mode = chisel.GetToolMode(slot, player, new BlockSelection
		{
			Position = Pos
		});
		if (!mode.HasValue)
		{
			return null;
		}
		return chisel.GetToolModes(slot, clientApi.World.Player, new BlockSelection
		{
			Position = Pos
		})[mode.Value];
	}

	public ChiselMode GetChiselModeData(IPlayer player)
	{
		ItemSlot slot = player?.InventoryManager?.ActiveHotbarSlot;
		if (!(slot?.Itemstack?.Collectible is ItemChisel itemChisel))
		{
			return defaultMode;
		}
		int? mode = itemChisel.GetToolMode(slot, player, new BlockSelection
		{
			Position = Pos
		});
		if (!mode.HasValue)
		{
			return null;
		}
		return (ChiselMode)itemChisel.ToolModes[mode.Value].Data;
	}

	public int GetChiselSize(IPlayer player)
	{
		return GetChiselModeData(player)?.ChiselSize ?? 0;
	}

	public Vec3i GetVoxelPos(BlockSelection blockSel, int chiselSize)
	{
		RegenSelectionVoxelBoxes(mustLoad: true, chiselSize);
		Cuboidf[] boxes = selectionBoxesVoxels;
		if (blockSel.SelectionBoxIndex >= boxes.Length)
		{
			return null;
		}
		Cuboidf box = boxes[blockSel.SelectionBoxIndex];
		return new Vec3i((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1));
	}

	internal void OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel, bool isBreak)
	{
		if (Api.World.Side == EnumAppSide.Client && DetailingMode)
		{
			Cuboidf[] boxes = GetOrCreateVoxelSelectionBoxes(byPlayer);
			if (blockSel.SelectionBoxIndex < boxes.Length)
			{
				Cuboidf box = boxes[blockSel.SelectionBoxIndex];
				Vec3i voxelPos = new Vec3i((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1));
				UpdateVoxel(byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, voxelPos, blockSel.Face, isBreak);
			}
		}
	}

	public bool Interact(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer != null)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (activeHotbarSlot != null && activeHotbarSlot.Itemstack?.Collectible.Tool == EnumTool.Knife)
			{
				BlockFacing face = blockSel.Face;
				int rotfaceindex = (face.IsVertical ? face.Index : BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(face.HorizontalAngleIndex + rotationY / 90, 4)].Index);
				if (DecorIds != null && DecorIds[rotfaceindex] != 0)
				{
					Block block = Api.World.Blocks[DecorIds[rotfaceindex]];
					Api.World.SpawnItemEntity(block.OnPickBlock(Api.World, Pos), Pos);
					DecorIds[rotfaceindex] = 0;
					MarkDirty(redrawOnClient: true, byPlayer);
				}
				return true;
			}
		}
		return false;
	}

	public void SetNowMaterialId(int materialId)
	{
		nowmaterialIndex = (byte)Math.Max(0, BlockIds.IndexOf(materialId));
	}

	internal void UpdateVoxel(IPlayer byPlayer, ItemSlot itemslot, Vec3i voxelPos, BlockFacing facing, bool isBreak)
	{
		if (!Api.World.Claims.TryAccess(byPlayer, Pos, EnumBlockAccessFlags.Use))
		{
			MarkDirty(redrawOnClient: true, byPlayer);
		}
		else if (GetChiselModeData(byPlayer).Apply(this, byPlayer, voxelPos, facing, isBreak, nowmaterialIndex))
		{
			if (Api.Side == EnumAppSide.Client)
			{
				MarkMeshDirty();
				BlockEntityMicroBlock.UpdateNeighbors(this);
			}
			RegenSelectionBoxes(Api.World, byPlayer);
			MarkDirty(redrawOnClient: true, byPlayer);
			if (Api.Side == EnumAppSide.Client)
			{
				SendUseOverPacket(voxelPos, facing, isBreak);
			}
			double posx = (float)Pos.X + (float)voxelPos.X / 16f;
			double posy = (float)Pos.InternalY + (float)voxelPos.Y / 16f;
			double posz = (float)Pos.Z + (float)voxelPos.Z / 16f;
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/knap" + ((Api.World.Rand.Next(2) > 0) ? 1 : 2)), posx, posy, posz, byPlayer, randomizePitch: true, 12f);
			if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative && Api.World.Rand.Next(3) == 0)
			{
				itemslot.Itemstack?.Collectible.DamageItem(Api.World, byPlayer.Entity, itemslot);
			}
			if (VoxelCuboids.Count == 0)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
				Api.World.BlockAccessor.RemoveBlockLight(GetLightHsv(Api.World.BlockAccessor), Pos);
			}
		}
	}

	public void SendUseOverPacket(Vec3i voxelPos, BlockFacing facing, bool isBreak)
	{
		byte[] data;
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(ms);
			binaryWriter.Write(voxelPos.X);
			binaryWriter.Write(voxelPos.Y);
			binaryWriter.Write(voxelPos.Z);
			binaryWriter.Write(isBreak);
			binaryWriter.Write((ushort)facing.Index);
			binaryWriter.Write(nowmaterialIndex);
			data = ms.ToArray();
		}
		((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, 1010, data);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (!Api.World.Claims.TryAccess(player, Pos, EnumBlockAccessFlags.BuildOrBreak))
		{
			player.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return;
		}
		if (packetid == 1002)
		{
			EditSignPacket packet = SerializerUtil.Deserialize<EditSignPacket>(data);
			base.BlockName = packet.Text;
			MarkDirty(redrawOnClient: true, player);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		if (packetid == 1010)
		{
			Vec3i voxelPos;
			bool isBreak;
			BlockFacing facing;
			using (MemoryStream ms = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(ms);
				voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
				isBreak = reader.ReadBoolean();
				facing = BlockFacing.ALLFACES[reader.ReadInt16()];
				nowmaterialIndex = (byte)Math.Clamp(reader.ReadByte(), 0, BlockIds.Length - 1);
			}
			UpdateVoxel(player, player.InventoryManager.ActiveHotbarSlot, voxelPos, facing, isBreak);
		}
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos, IPlayer forPlayer = null)
	{
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && DetailingMode)
		{
			if (forPlayer == null)
			{
				forPlayer = (Api.World as IClientWorldAccessor).Player;
			}
			int nowSize = GetChiselSize(forPlayer);
			if (prevSize > 0 && prevSize != nowSize)
			{
				selectionBoxesVoxels = null;
			}
			prevSize = nowSize;
			return GetOrCreateVoxelSelectionBoxes(forPlayer);
		}
		return base.GetSelectionBoxes(world, pos, forPlayer);
	}

	private Cuboidf[] GetOrCreateVoxelSelectionBoxes(IPlayer byPlayer)
	{
		if (selectionBoxesVoxels == null)
		{
			GenerateSelectionVoxelBoxes(byPlayer);
		}
		return selectionBoxesVoxels;
	}

	public bool SetVoxel(Vec3i voxelPos, bool add, IPlayer byPlayer, byte materialId)
	{
		int size = GetChiselSize(byPlayer);
		if (add && ConstrainToAvailableMaterialQuantity && AvailMaterialQuantities != null)
		{
			int availableSumMaterial = AvailMaterialQuantities[materialId];
			CuboidWithMaterial cwm = new CuboidWithMaterial();
			int usedSumMaterial = 0;
			foreach (uint voxelCuboid in VoxelCuboids)
			{
				BlockEntityMicroBlock.FromUint(voxelCuboid, cwm);
				if (cwm.Material == materialId)
				{
					usedSumMaterial += cwm.SizeXYZ;
				}
			}
			usedSumMaterial += size * size * size;
			if (usedSumMaterial > availableSumMaterial)
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "outofmaterial", Lang.Get("Out of material, add more material to continue adding voxels"));
				return false;
			}
		}
		if (!SetVoxel(voxelPos, add, materialId, size))
		{
			return false;
		}
		if (Api.Side == EnumAppSide.Client && !add)
		{
			Vec3d basepos = Pos.ToVec3d().Add((double)voxelPos.X / 16.0, (double)voxelPos.Y / 16.0, (double)voxelPos.Z / 16.0).Add((double)((float)size / 4f) / 16.0, (double)((float)size / 4f) / 16.0, (double)((float)size / 4f) / 16.0);
			int q = size * 5 - 2 + Api.World.Rand.Next(5);
			Block block = Api.World.GetBlock(BlockIds[materialId]);
			while (q-- > 0)
			{
				Api.World.SpawnParticles(1f, block.GetRandomColor(Api as ICoreClientAPI, Pos, BlockFacing.UP) | -16777216, basepos, basepos.Clone().Add((double)((float)size / 4f) / 16.0, (double)((float)size / 4f) / 16.0, (double)((float)size / 4f) / 16.0), new Vec3f(-1f, -0.5f, -1f), new Vec3f(1f, 1f + (float)size / 3f, 1f), 1f, 1f, (float)size / 30f + 0.1f + (float)Api.World.Rand.NextDouble() * 0.25f, EnumParticleModel.Cube);
			}
		}
		return true;
	}

	public override void RegenSelectionBoxes(IWorldAccessor worldForResolve, IPlayer byPlayer)
	{
		base.RegenSelectionBoxes(worldForResolve, byPlayer);
		if (byPlayer != null)
		{
			int size = GetChiselSize(byPlayer);
			RegenSelectionVoxelBoxes(mustLoad: false, size);
		}
		else
		{
			selectionBoxesVoxels = null;
		}
	}

	public void GenerateSelectionVoxelBoxes(IPlayer byPlayer)
	{
		int size = GetChiselSize(byPlayer);
		RegenSelectionVoxelBoxes(mustLoad: true, size);
	}

	public void RegenSelectionVoxelBoxes(bool mustLoad, int chiselSize)
	{
		if (selectionBoxesVoxels == null && !mustLoad)
		{
			return;
		}
		HashSet<Cuboidf> boxes = new HashSet<Cuboidf>();
		if (chiselSize <= 0)
		{
			chiselSize = 16;
		}
		float sx = (float)chiselSize / 16f;
		float sy = (float)chiselSize / 16f;
		float sz = (float)chiselSize / 16f;
		CuboidWithMaterial cwm = BlockEntityMicroBlock.tmpCuboids[0];
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			BlockEntityMicroBlock.FromUint(VoxelCuboids[i], cwm);
			for (int x1 = cwm.X1; x1 < cwm.X2; x1 += chiselSize)
			{
				for (int y1 = cwm.Y1; y1 < cwm.Y2; y1 += chiselSize)
				{
					for (int z1 = cwm.Z1; z1 < cwm.Z2; z1 += chiselSize)
					{
						float px = (float)Math.Floor((float)x1 / (float)chiselSize) * sx;
						float py = (float)Math.Floor((float)y1 / (float)chiselSize) * sy;
						float pz = (float)Math.Floor((float)z1 / (float)chiselSize) * sz;
						if (!(px + sx > 1f) && !(py + sy > 1f) && !(pz + sz > 1f))
						{
							boxes.Add(new Cuboidf(px, py, pz, px + sx, py + sy, pz + sz));
						}
					}
				}
			}
		}
		selectionBoxesVoxels = boxes.ToArray();
	}

	public int AddMaterial(Block addblock, out bool isFull, bool compareToPickBlock = true)
	{
		Cuboidf[] collboxes = addblock.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
		int sum = 0;
		if (collboxes == null)
		{
			collboxes = new Cuboidf[1] { Cuboidf.Default() };
		}
		foreach (Cuboidf box in collboxes)
		{
			sum += new Cuboidi((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1), (int)(16f * box.X2), (int)(16f * box.Y2), (int)(16f * box.Z2)).SizeXYZ;
		}
		if (compareToPickBlock && !BlockIds.Contains(addblock.Id))
		{
			int[] blockIds = BlockIds;
			foreach (int blockid in blockIds)
			{
				Block matblock = Api.World.Blocks[blockid];
				if (matblock.OnPickBlock(Api.World, Pos).Block?.Id == addblock.Id)
				{
					addblock = matblock;
				}
			}
		}
		if (!BlockIds.Contains(addblock.Id))
		{
			isFull = false;
			BlockIds = BlockIds.Append(addblock.Id);
			if (AvailMaterialQuantities != null)
			{
				AvailMaterialQuantities = AvailMaterialQuantities.Append((ushort)sum);
			}
			return BlockIds.Length - 1;
		}
		int index = BlockIds.IndexOf(addblock.Id);
		isFull = AvailMaterialQuantities[index] >= 4096;
		if (AvailMaterialQuantities != null)
		{
			AvailMaterialQuantities[index] = (ushort)Math.Min(65535, AvailMaterialQuantities[index] + sum);
		}
		return index;
	}

	public int AddMaterial(Block block)
	{
		bool isFull;
		return AddMaterial(block, out isFull);
	}

	public override bool RemoveMaterial(Block block)
	{
		int index = BlockIds.IndexOf(block.Id);
		if (AvailMaterialQuantities != null && index >= 0)
		{
			AvailMaterialQuantities = AvailMaterialQuantities.RemoveEntry(index);
		}
		return base.RemoveMaterial(block);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		if (tree["availMaterialQuantities"] is IntArrayAttribute intarrattr)
		{
			AvailMaterialQuantities = new ushort[intarrattr.value.Length];
			for (int i = 0; i < intarrattr.value.Length; i++)
			{
				AvailMaterialQuantities[i] = (ushort)intarrattr.value[i];
			}
			while (BlockIds.Length > AvailMaterialQuantities.Length)
			{
				AvailMaterialQuantities = AvailMaterialQuantities.Append((ushort)4096);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (AvailMaterialQuantities != null)
		{
			IntArrayAttribute attr = new IntArrayAttribute();
			attr.value = new int[AvailMaterialQuantities.Length];
			for (int i = 0; i < AvailMaterialQuantities.Length; i++)
			{
				attr.value[i] = AvailMaterialQuantities[i];
			}
			tree["availMaterialQuantities"] = attr;
		}
	}
}
