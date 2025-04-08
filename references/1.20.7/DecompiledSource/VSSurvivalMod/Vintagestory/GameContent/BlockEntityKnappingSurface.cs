using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityKnappingSurface : BlockEntity
{
	private int selectedRecipeId = -1;

	public bool[,] Voxels = new bool[16, 16];

	public ItemStack BaseMaterial;

	private Cuboidf[] selectionBoxes = new Cuboidf[0];

	private KnappingRenderer workitemRenderer;

	private Vec3d lastRemovedLocalPos = new Vec3d();

	private GuiDialog dlg;

	public KnappingRecipe SelectedRecipe => Api.GetKnappingRecipes().FirstOrDefault((KnappingRecipe r) => r.RecipeId == selectedRecipeId);

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		CreateInitialWorkItem();
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (BaseMaterial != null)
		{
			BaseMaterial.ResolveBlockOrItem(api.World);
		}
		if (api is ICoreClientAPI capi)
		{
			workitemRenderer = new KnappingRenderer(Pos, capi);
			RegenMeshAndSelectionBoxes();
			capi.Event.ColorsPresetChanged += RegenMeshAndSelectionBoxes;
		}
	}

	internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	internal void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (SelectedRecipe == null && Api.Side == EnumAppSide.Client)
		{
			OpenDialog(Api.World as IClientWorldAccessor, Pos, byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
		}
	}

	internal void OnUseOver(IPlayer byPlayer, int selectionBoxIndex, BlockFacing facing, bool mouseMode)
	{
		if (selectionBoxIndex >= 0 && selectionBoxIndex < selectionBoxes.Length)
		{
			Cuboidf box = selectionBoxes[selectionBoxIndex];
			Vec3i voxelPos = new Vec3i((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1));
			OnUseOver(byPlayer, voxelPos, facing, mouseMode);
		}
	}

	internal void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseMode)
	{
		if (voxelPos == null)
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			SendUseOverPacket(byPlayer, voxelPos, facing, mouseMode);
		}
		if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null)
		{
			return;
		}
		bool didRemove = mouseMode && OnRemove(voxelPos, 0);
		if (didRemove)
		{
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing face = BlockFacing.HORIZONTALS[i];
				Vec3i nnode = voxelPos.AddCopy(face);
				if (Voxels[nnode.X, nnode.Z] && !SelectedRecipe.Voxels[nnode.X, 0, nnode.Z])
				{
					tryBfsRemove(nnode.X, nnode.Z);
				}
			}
		}
		if (mouseMode && (didRemove || Voxels[voxelPos.X, voxelPos.Z]))
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/knap" + ((Api.World.Rand.Next(2) > 0) ? 1 : 2)), lastRemovedLocalPos.X, lastRemovedLocalPos.Y, lastRemovedLocalPos.Z, byPlayer, randomizePitch: true, 12f);
		}
		if (didRemove && Api.Side == EnumAppSide.Client)
		{
			spawnParticles(lastRemovedLocalPos);
		}
		RegenMeshAndSelectionBoxes();
		Api.World.BlockAccessor.MarkBlockDirty(Pos);
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
		if (!HasAnyVoxel())
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
			return;
		}
		CheckIfFinished(byPlayer);
		MarkDirty();
	}

	public void CheckIfFinished(IPlayer byPlayer)
	{
		if (!MatchesRecipe() || !(Api.World is IServerWorldAccessor))
		{
			return;
		}
		Voxels = new bool[16, 16];
		ItemStack outstack = SelectedRecipe.Output.ResolvedItemstack.Clone();
		selectedRecipeId = -1;
		if (outstack.StackSize == 1 && outstack.Class == EnumItemClass.Block)
		{
			Api.World.BlockAccessor.SetBlock(outstack.Block.BlockId, Pos);
			return;
		}
		int tries = 0;
		while (outstack.StackSize > 0)
		{
			ItemStack dropStack = outstack.Clone();
			dropStack.StackSize = Math.Min(outstack.StackSize, outstack.Collectible.MaxStackSize);
			outstack.StackSize -= dropStack.StackSize;
			TreeAttribute tree = new TreeAttribute();
			tree["itemstack"] = new ItemstackAttribute(dropStack);
			tree["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
			Api.Event.PushEvent("onitemknapped", tree);
			if (byPlayer.InventoryManager.TryGiveItemstack(dropStack))
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
			}
			else
			{
				Api.World.SpawnItemEntity(dropStack, Pos);
			}
			if (tries++ > 1000)
			{
				throw new Exception("Endless loop prevention triggered. Something seems broken with a matching knapping recipe with number " + selectedRecipeId + ". Tried 1000 times to drop the resulting stack " + outstack.ToString());
			}
		}
		Api.World.BlockAccessor.SetBlock(0, Pos);
	}

	private void spawnParticles(Vec3d pos)
	{
		Random rnd = Api.World.Rand;
		for (int i = 0; i < 3; i++)
		{
			Api.World.SpawnParticles(new SimpleParticleProperties
			{
				MinQuantity = 1f,
				AddQuantity = 2f,
				Color = BaseMaterial.Collectible.GetRandomColor(Api as ICoreClientAPI, BaseMaterial),
				MinPos = new Vec3d(pos.X, pos.Y + 0.0625 + 0.009999999776482582, pos.Z),
				AddPos = new Vec3d(0.0625, 0.009999999776482582, 0.0625),
				MinVelocity = new Vec3f(0f, 1f, 0f),
				AddVelocity = new Vec3f(4f * ((float)rnd.NextDouble() - 0.5f), 1f * ((float)rnd.NextDouble() - 0.5f), 4f * ((float)rnd.NextDouble() - 0.5f)),
				LifeLength = 0.2f,
				GravityEffect = 1f,
				MinSize = 0.1f,
				MaxSize = 0.4f,
				ParticleModel = EnumParticleModel.Cube,
				SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.15f)
			});
		}
	}

	private bool MatchesRecipe()
	{
		if (SelectedRecipe == null)
		{
			return false;
		}
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				if (Voxels[x, z] != SelectedRecipe.Voxels[x, 0, z])
				{
					return false;
				}
			}
		}
		return true;
	}

	private Cuboidi LayerBounds()
	{
		Cuboidi bounds = new Cuboidi(8, 8, 8, 8, 8, 8);
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				if (SelectedRecipe.Voxels[x, 0, z])
				{
					bounds.X1 = Math.Min(bounds.X1, x);
					bounds.X2 = Math.Max(bounds.X2, x);
					bounds.Z1 = Math.Min(bounds.Z1, z);
					bounds.Z2 = Math.Max(bounds.Z2, z);
				}
			}
		}
		return bounds;
	}

	private bool HasAnyVoxel()
	{
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				if (Voxels[x, z])
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool InBounds(Vec3i voxelPos)
	{
		Cuboidi bounds = LayerBounds();
		if (voxelPos.X >= bounds.X1 && voxelPos.X <= bounds.X2 && voxelPos.Y >= 0 && voxelPos.Y < 16 && voxelPos.Z >= bounds.Z1)
		{
			return voxelPos.Z <= bounds.Z2;
		}
		return false;
	}

	private bool OnRemove(Vec3i voxelPos, int radius)
	{
		if (SelectedRecipe == null || SelectedRecipe.Voxels[voxelPos.X, 0, voxelPos.Z])
		{
			return false;
		}
		for (int dx = -(int)Math.Ceiling((float)radius / 2f); dx <= radius / 2; dx++)
		{
			for (int dz = -(int)Math.Ceiling((float)radius / 2f); dz <= radius / 2; dz++)
			{
				Vec3i offPos = voxelPos.AddCopy(dx, 0, dz);
				if (offPos.X >= 0 && offPos.X < 16 && offPos.Z >= 0 && offPos.Z < 16 && Voxels[offPos.X, offPos.Z])
				{
					Voxels[offPos.X, offPos.Z] = false;
					lastRemovedLocalPos.Set((float)Pos.X + (float)voxelPos.X / 16f, (float)Pos.Y + (float)voxelPos.Y / 16f, (float)Pos.Z + (float)voxelPos.Z / 16f);
					return true;
				}
			}
		}
		return false;
	}

	private void tryBfsRemove(int x, int z)
	{
		Queue<Vec2i> nodesToVisit = new Queue<Vec2i>();
		HashSet<Vec2i> nodesVisited = new HashSet<Vec2i>();
		nodesToVisit.Enqueue(new Vec2i(x, z));
		List<Vec2i> foundPieces = new List<Vec2i>();
		while (nodesToVisit.Count > 0)
		{
			Vec2i node = nodesToVisit.Dequeue();
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing face = BlockFacing.HORIZONTALS[i];
				Vec2i nnode = node.Copy().Add(face.Normali.X, face.Normali.Z);
				if (nnode.X >= 0 && nnode.X < 16 && nnode.Y >= 0 && nnode.Y < 16 && Voxels[nnode.X, nnode.Y] && !nodesVisited.Contains(nnode))
				{
					nodesVisited.Add(nnode);
					foundPieces.Add(nnode);
					if (SelectedRecipe.Voxels[nnode.X, 0, nnode.Y])
					{
						return;
					}
					nodesToVisit.Enqueue(nnode);
				}
			}
		}
		if (nodesVisited.Count == 0 && foundPieces.Count == 0)
		{
			foundPieces.Add(new Vec2i(x, z));
		}
		Vec3d tmp = new Vec3d();
		foreach (Vec2i val in foundPieces)
		{
			Voxels[val.X, val.Y] = false;
			if (Api.Side == EnumAppSide.Client)
			{
				tmp.Set((float)Pos.X + (float)val.X / 16f, Pos.Y, (float)Pos.Z + (float)val.Y / 16f);
				spawnParticles(tmp);
			}
		}
	}

	public void RegenMeshAndSelectionBoxes()
	{
		if (workitemRenderer != null && BaseMaterial != null)
		{
			BaseMaterial.ResolveBlockOrItem(Api.World);
			workitemRenderer.Material = BaseMaterial.Collectible.FirstCodePart(1);
			if (workitemRenderer.Material == null)
			{
				workitemRenderer.Material = BaseMaterial.Collectible.FirstCodePart();
			}
			workitemRenderer.RegenMesh(Voxels, SelectedRecipe);
		}
		List<Cuboidf> boxes = new List<Cuboidf>();
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				boxes.Add(new Cuboidf((float)x / 16f, 0f, (float)z / 16f, (float)x / 16f + 0.0625f, 0.0625f, (float)z / 16f + 0.0625f));
			}
		}
		selectionBoxes = boxes.ToArray();
	}

	public void CreateInitialWorkItem()
	{
		Voxels = new bool[16, 16];
		for (int x = 3; x <= 12; x++)
		{
			for (int z = 3; z <= 12; z++)
			{
				Voxels[x, z] = true;
			}
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		workitemRenderer?.Dispose();
		workitemRenderer = null;
		dlg?.TryClose();
		dlg?.Dispose();
		if (Api is ICoreClientAPI capi)
		{
			capi.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		deserializeVoxels(tree.GetBytes("voxels"));
		selectedRecipeId = tree.GetInt("selectedRecipeId", -1);
		BaseMaterial = tree.GetItemstack("baseMaterial");
		if (Api?.World != null)
		{
			BaseMaterial?.ResolveBlockOrItem(Api.World);
		}
		RegenMeshAndSelectionBoxes();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBytes("voxels", serializeVoxels());
		tree.SetInt("selectedRecipeId", selectedRecipeId);
		tree.SetItemstack("baseMaterial", BaseMaterial);
	}

	private byte[] serializeVoxels()
	{
		byte[] data = new byte[32];
		int pos = 0;
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				int bitpos = pos % 8;
				data[pos / 8] |= (byte)((Voxels[x, z] ? 1u : 0u) << bitpos);
				pos++;
			}
		}
		return data;
	}

	private void deserializeVoxels(byte[] data)
	{
		Voxels = new bool[16, 16];
		if (data == null || data.Length < 32)
		{
			return;
		}
		int pos = 0;
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				int bitpos = pos % 8;
				Voxels[x, z] = (data[pos / 8] & (1 << bitpos)) > 0;
				pos++;
			}
		}
	}

	public void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseMode)
	{
		byte[] data;
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(ms);
			binaryWriter.Write(voxelPos.X);
			binaryWriter.Write(voxelPos.Y);
			binaryWriter.Write(voxelPos.Z);
			binaryWriter.Write(mouseMode);
			binaryWriter.Write((ushort)facing.Index);
			data = ms.ToArray();
		}
		((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, 1002, data);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1003)
		{
			if (BaseMaterial != null)
			{
				Api.World.SpawnItemEntity(BaseMaterial, Pos);
			}
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
		if (packetid == 1001)
		{
			int recipeid = SerializerUtil.Deserialize<int>(data);
			KnappingRecipe recipe = Api.GetKnappingRecipes().FirstOrDefault((KnappingRecipe r) => r.RecipeId == recipeid);
			if (recipe == null)
			{
				Api.World.Logger.Error("Client tried to selected knapping recipe with id {0}, but no such recipe exists!");
				return;
			}
			selectedRecipeId = recipe.RecipeId;
			MarkDirty();
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		if (packetid == 1002)
		{
			Vec3i voxelPos;
			bool mouseMode;
			BlockFacing facing;
			using (MemoryStream ms = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(ms);
				voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
				mouseMode = reader.ReadBoolean();
				facing = BlockFacing.ALLFACES[reader.ReadInt16()];
			}
			OnUseOver(player, voxelPos, facing, mouseMode);
		}
	}

	public void OpenDialog(IClientWorldAccessor world, BlockPos pos, ItemStack baseMaterial)
	{
		List<KnappingRecipe> recipes = (from r in Api.GetKnappingRecipes()
			where r.Ingredient.SatisfiesAsIngredient(baseMaterial)
			orderby r.Output.ResolvedItemstack.Collectible.Code
			select r).ToList();
		List<ItemStack> stacks = recipes.Select((KnappingRecipe r) => r.Output.ResolvedItemstack).ToList();
		ICoreClientAPI capi = Api as ICoreClientAPI;
		dlg?.Dispose();
		dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select recipe"), stacks.ToArray(), delegate(int selectedIndex)
		{
			selectedRecipeId = recipes[selectedIndex].RecipeId;
			capi.Network.SendBlockEntityPacket(pos, 1001, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
		}, delegate
		{
			capi.Network.SendBlockEntityPacket(pos, 1003);
		}, pos, Api as ICoreClientAPI);
		dlg.TryOpen();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (BaseMaterial != null && SelectedRecipe != null)
		{
			dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe.Output?.ResolvedItemstack?.GetName()));
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		workitemRenderer?.Dispose();
		if (Api is ICoreClientAPI capi)
		{
			capi.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}
}
