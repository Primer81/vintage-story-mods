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

public class BlockEntityClayForm : BlockEntity
{
	private ItemStack workItemStack;

	private int selectedRecipeId = -1;

	private ClayFormingRecipe selectedRecipe;

	public int AvailableVoxels;

	public bool[,,] Voxels = new bool[16, 16, 16];

	private ItemStack baseMaterial;

	private Cuboidf[] selectionBoxes = new Cuboidf[0];

	private ClayFormRenderer workitemRenderer;

	private GuiDialog dlg;

	public ClayFormingRecipe SelectedRecipe => selectedRecipe;

	public bool CanWorkCurrent
	{
		get
		{
			if (workItemStack != null)
			{
				return CanWork(workItemStack);
			}
			return false;
		}
	}

	public ItemStack BaseMaterial => baseMaterial;

	static BlockEntityClayForm()
	{
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		setSelectedRecipe(selectedRecipeId);
		if (workItemStack != null)
		{
			workItemStack.ResolveBlockOrItem(api.World);
			if (baseMaterial == null)
			{
				baseMaterial = new ItemStack(api.World.GetItem(new AssetLocation("clay-" + workItemStack.Collectible.LastCodePart())));
			}
			else
			{
				baseMaterial.ResolveBlockOrItem(api.World);
			}
		}
		if (api is ICoreClientAPI capi)
		{
			capi.Event.RegisterRenderer(workitemRenderer = new ClayFormRenderer(Pos, capi), EnumRenderStage.Opaque);
			capi.Event.RegisterRenderer(workitemRenderer, EnumRenderStage.AfterFinalComposition);
			RegenMeshForNextLayer();
			capi.Event.ColorsPresetChanged += RegenMeshForNextLayer;
		}
	}

	public bool CanWork(ItemStack stack)
	{
		return true;
	}

	internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	public void PutClay(ItemSlot slot)
	{
		if (workItemStack == null)
		{
			if (Api.World is IClientWorldAccessor)
			{
				OpenDialog(Api.World as IClientWorldAccessor, Pos, slot.Itemstack);
			}
			CreateInitialWorkItem();
			workItemStack = new ItemStack(Api.World.GetItem(new AssetLocation("clayworkitem-" + slot.Itemstack.Collectible.LastCodePart())));
			baseMaterial = slot.Itemstack.Clone();
			baseMaterial.StackSize = 1;
		}
		AvailableVoxels += 25;
		slot.TakeOut(1);
		slot.MarkDirty();
		RegenMeshForNextLayer();
		MarkDirty();
	}

	public void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (SelectedRecipe == null && Api.Side == EnumAppSide.Client)
		{
			OpenDialog(Api.World as IClientWorldAccessor, Pos, byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
		}
	}

	public void OnUseOver(IPlayer byPlayer, int selectionBoxIndex, BlockFacing facing, bool mouseBreakMode)
	{
		if (selectionBoxIndex >= 0 && selectionBoxIndex < selectionBoxes.Length)
		{
			Cuboidf box = selectionBoxes[selectionBoxIndex];
			Vec3i voxelPos = new Vec3i((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1));
			Api.World.FrameProfiler.Enter("clayforming");
			OnUseOver(byPlayer, voxelPos, facing, mouseBreakMode);
			Api.World.FrameProfiler.Leave();
		}
	}

	public void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseBreakMode)
	{
		if (SelectedRecipe == null || voxelPos == null)
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			SendUseOverPacket(byPlayer, voxelPos, facing, mouseBreakMode);
		}
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (slot.Itemstack == null || !CanWorkCurrent)
		{
			return;
		}
		int toolMode = slot.Itemstack.Collectible.GetToolMode(slot, byPlayer, new BlockSelection
		{
			Position = Pos
		});
		bool didmodify = false;
		Api.World.FrameProfiler.Mark("clayform-modified1");
		int layer = NextNotMatchingRecipeLayer();
		Api.World.FrameProfiler.Mark("clayform-modified2");
		if (toolMode == 3)
		{
			if (!mouseBreakMode)
			{
				didmodify = OnCopyLayer(layer);
			}
			else
			{
				toolMode = 1;
			}
		}
		if (toolMode != 3)
		{
			didmodify = (mouseBreakMode ? OnRemove(layer, voxelPos, facing, toolMode) : OnAdd(layer, voxelPos, facing, toolMode));
		}
		Api.World.FrameProfiler.Mark("clayform-modified3");
		if (didmodify)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/clayform.ogg"), byPlayer, byPlayer, randomizePitch: true, 8f);
			Api.World.FrameProfiler.Mark("clayform-playsound");
		}
		layer = NextNotMatchingRecipeLayer(layer);
		RegenMeshAndSelectionBoxes(layer);
		Api.World.FrameProfiler.Mark("clayform-regenmesh");
		Api.World.BlockAccessor.MarkBlockDirty(Pos);
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
		if (!HasAnyVoxel())
		{
			AvailableVoxels = 0;
			workItemStack = null;
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		else
		{
			CheckIfFinished(byPlayer, layer);
			Api.World.FrameProfiler.Mark("clayform-checkfinished");
			MarkDirty();
		}
	}

	public void CheckIfFinished(IPlayer byPlayer, int layer)
	{
		if (!MatchesRecipe(layer) || !(Api.World is IServerWorldAccessor))
		{
			return;
		}
		workItemStack = null;
		Voxels = new bool[16, 16, 16];
		AvailableVoxels = 0;
		ItemStack outstack = SelectedRecipe.Output.ResolvedItemstack.Clone();
		selectedRecipeId = -1;
		selectedRecipe = null;
		if (outstack.StackSize == 1 && outstack.Class == EnumItemClass.Block)
		{
			Api.World.BlockAccessor.SetBlock(outstack.Block.BlockId, Pos);
			return;
		}
		int tries = 500;
		while (outstack.StackSize > 0 && tries-- > 0)
		{
			ItemStack dropStack = outstack.Clone();
			dropStack.StackSize = Math.Min(outstack.StackSize, outstack.Collectible.MaxStackSize);
			outstack.StackSize -= dropStack.StackSize;
			TreeAttribute tree = new TreeAttribute();
			tree["itemstack"] = new ItemstackAttribute(dropStack);
			tree["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
			Api.Event.PushEvent("onitemclayformed", tree);
			if (byPlayer.InventoryManager.TryGiveItemstack(dropStack))
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
			}
			else
			{
				Api.World.SpawnItemEntity(dropStack, Pos);
			}
		}
		if (tries <= 1)
		{
			Api.World.Logger.Error("Tried to drop finished clay forming item but failed after 500 times?! Gave up doing so. Out stack was " + outstack);
		}
		Api.World.BlockAccessor.SetBlock(0, Pos);
	}

	private bool MatchesRecipe(int layer)
	{
		if (SelectedRecipe == null)
		{
			return false;
		}
		return NextNotMatchingRecipeLayer(layer) >= SelectedRecipe.Pattern.Length;
	}

	private int NextNotMatchingRecipeLayer(int layerStart = 0)
	{
		if (SelectedRecipe == null)
		{
			return 0;
		}
		if (layerStart < 0)
		{
			return 0;
		}
		bool[,,] selectedRecipeVoxels = SelectedRecipe.Voxels;
		for (int layer = layerStart; layer < 16; layer++)
		{
			for (int x = 0; x < 16; x++)
			{
				for (int z = 0; z < 16; z++)
				{
					if (Voxels[x, layer, z] != selectedRecipeVoxels[x, layer, z])
					{
						return layer;
					}
				}
			}
		}
		return 16;
	}

	private Cuboidi LayerBounds(int layer)
	{
		Cuboidi bounds = new Cuboidi(8, 8, 8, 8, 8, 8);
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				if (SelectedRecipe.Voxels[x, layer, z])
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
			for (int y = 0; y < 16; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					if (Voxels[x, y, z])
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	[Obsolete("retained only for mod compatibility, for performance please cache the bounds and use the other overload")]
	public bool InBounds(Vec3i voxelPos, int layer)
	{
		if (layer < 0 || layer >= 16)
		{
			return false;
		}
		Cuboidi bounds = LayerBounds(layer);
		return InBounds(voxelPos, bounds);
	}

	public bool InBounds(Vec3i voxelPos, Cuboidi bounds)
	{
		if (voxelPos.X >= bounds.X1 && voxelPos.X <= bounds.X2 && voxelPos.Y >= 0 && voxelPos.Y < 16 && voxelPos.Z >= bounds.Z1)
		{
			return voxelPos.Z <= bounds.Z2;
		}
		return false;
	}

	private bool OnRemove(int layer, Vec3i voxelPos, BlockFacing facing, int radius)
	{
		bool didremove = false;
		if (voxelPos.Y != layer)
		{
			return didremove;
		}
		if (layer < 0 || layer >= 16)
		{
			return didremove;
		}
		Vec3i offPos = voxelPos.Clone();
		for (int dx = -(int)Math.Ceiling((float)radius / 2f); dx <= radius / 2; dx++)
		{
			offPos.X = voxelPos.X + dx;
			if (offPos.X < 0 || offPos.X >= 16)
			{
				continue;
			}
			for (int dz = -(int)Math.Ceiling((float)radius / 2f); dz <= radius / 2; dz++)
			{
				offPos.Z = voxelPos.Z + dz;
				if (offPos.Z >= 0 && offPos.Z < 16 && Voxels[offPos.X, offPos.Y, offPos.Z])
				{
					didremove = true;
					Voxels[offPos.X, offPos.Y, offPos.Z] = false;
					AvailableVoxels++;
				}
			}
		}
		return didremove;
	}

	private bool OnCopyLayer(int layer)
	{
		if (layer <= 0 || layer > 15)
		{
			return false;
		}
		bool didplace = false;
		int quantity = 4;
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				if (Voxels[x, layer - 1, z] && !Voxels[x, layer, z])
				{
					quantity--;
					Voxels[x, layer, z] = true;
					AvailableVoxels--;
					didplace = true;
				}
				if (quantity == 0)
				{
					return didplace;
				}
			}
		}
		return didplace;
	}

	private bool OnAdd(int layer, Vec3i voxelPos, BlockFacing facing, int radius)
	{
		if (voxelPos.Y == layer && facing.IsVertical)
		{
			return OnAdd(layer, voxelPos, radius);
		}
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z])
		{
			Vec3i offPoss = voxelPos.AddCopy(facing);
			if (layer >= 0 && layer < 16 && InBounds(offPoss, LayerBounds(layer)))
			{
				return OnAdd(layer, offPoss, radius);
			}
			return false;
		}
		return OnAdd(layer, voxelPos, radius);
	}

	private bool OnAdd(int layer, Vec3i voxelPos, int radius)
	{
		bool didadd = false;
		if (voxelPos.Y != layer)
		{
			return didadd;
		}
		if (layer < 0 || layer >= 16)
		{
			return didadd;
		}
		Cuboidi bounds = LayerBounds(layer);
		Vec3i offPos = voxelPos.Clone();
		for (int dx = -(int)Math.Ceiling((float)radius / 2f); dx <= radius / 2; dx++)
		{
			offPos.X = voxelPos.X + dx;
			for (int dz = -(int)Math.Ceiling((float)radius / 2f); dz <= radius / 2; dz++)
			{
				offPos.Z = voxelPos.Z + dz;
				if (InBounds(offPos, bounds) && !Voxels[offPos.X, offPos.Y, offPos.Z])
				{
					AvailableVoxels--;
					didadd = true;
					Voxels[offPos.X, offPos.Y, offPos.Z] = true;
				}
			}
		}
		return didadd;
	}

	private void RegenMeshAndSelectionBoxes(int layer)
	{
		if (workitemRenderer != null && layer != 16)
		{
			workitemRenderer.RegenMesh(workItemStack, Voxels, SelectedRecipe, layer);
		}
		List<Cuboidf> boxes = new List<Cuboidf>();
		bool[,,] recipeVoxels = SelectedRecipe?.Voxels;
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 16; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					if (y == 0 || Voxels[x, y, z] || (recipeVoxels != null && y == layer && recipeVoxels[x, y, z]))
					{
						boxes.Add(new Cuboidf((float)x / 16f, (float)y / 16f, (float)z / 16f, (float)x / 16f + 0.0625f, (float)y / 16f + 0.0625f, (float)z / 16f + 0.0625f));
					}
				}
			}
		}
		selectionBoxes = boxes.ToArray();
	}

	public void CreateInitialWorkItem()
	{
		Voxels = new bool[16, 16, 16];
		for (int x = 4; x < 12; x++)
		{
			for (int z = 4; z < 12; z++)
			{
				Voxels[x, 0, z] = true;
			}
		}
	}

	private void RegenMeshForNextLayer()
	{
		int layer = NextNotMatchingRecipeLayer();
		RegenMeshAndSelectionBoxes(layer);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		dlg?.TryClose();
		if (workitemRenderer != null)
		{
			workitemRenderer.Dispose();
			workitemRenderer = null;
		}
		if (Api is ICoreClientAPI capi)
		{
			capi.Event.ColorsPresetChanged -= RegenMeshForNextLayer;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		bool modified = deserializeVoxels(tree.GetBytes("voxels"));
		workItemStack = tree.GetItemstack("workItemStack");
		baseMaterial = tree.GetItemstack("baseMaterial");
		AvailableVoxels = tree.GetInt("availableVoxels");
		setSelectedRecipe(tree.GetInt("selectedRecipeId", -1));
		if (Api != null && workItemStack != null)
		{
			workItemStack.ResolveBlockOrItem(Api.World);
			Item item = Api.World.GetItem(new AssetLocation("clay-" + workItemStack.Collectible.LastCodePart()));
			if (item == null)
			{
				Api.World.Logger.Notification("Clay form base mat is null! Clay form @ {0}/{1}/{2} corrupt. Will reset to blue clay", Pos.X, Pos.Y, Pos.Z);
				item = Api.World.GetItem(new AssetLocation("clay-blue"));
			}
			baseMaterial = new ItemStack(item);
		}
		if (modified)
		{
			RegenMeshForNextLayer();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBytes("voxels", serializeVoxels());
		tree.SetItemstack("workItemStack", workItemStack);
		tree.SetItemstack("baseMaterial", baseMaterial);
		tree.SetInt("availableVoxels", AvailableVoxels);
		tree.SetInt("selectedRecipeId", selectedRecipeId);
	}

	private byte[] serializeVoxels()
	{
		byte[] data = new byte[512];
		int pos = 0;
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 16; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					int bitpos = pos % 8;
					data[pos / 8] |= (byte)((Voxels[x, y, z] ? 1u : 0u) << bitpos);
					pos++;
				}
			}
		}
		return data;
	}

	private bool deserializeVoxels(byte[] data)
	{
		if (data == null || data.Length < 512)
		{
			Voxels = new bool[16, 16, 16];
			return true;
		}
		if (Voxels == null)
		{
			Voxels = new bool[16, 16, 16];
		}
		int pos = 0;
		bool modified = false;
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 16; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					int bitpos = pos % 8;
					bool voxel = (data[pos / 8] & (1 << bitpos)) > 0;
					modified |= Voxels[x, y, z] != voxel;
					Voxels[x, y, z] = voxel;
					pos++;
				}
			}
		}
		return modified;
	}

	protected void setSelectedRecipe(int newId)
	{
		if (selectedRecipeId == newId && (selectedRecipe != null || newId < 0))
		{
			return;
		}
		if (newId == -1)
		{
			selectedRecipe = null;
		}
		else
		{
			selectedRecipe = ((Api != null) ? Api.GetClayformingRecipes().FirstOrDefault((ClayFormingRecipe r) => r.RecipeId == newId) : null);
		}
		selectedRecipeId = newId;
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
			if (baseMaterial != null)
			{
				Api.World.SpawnItemEntity(baseMaterial, Pos);
			}
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		if (packetid == 1001)
		{
			int recipeid = SerializerUtil.Deserialize<int>(data);
			ClayFormingRecipe recipe = Api.GetClayformingRecipes().FirstOrDefault((ClayFormingRecipe r) => r.RecipeId == recipeid);
			if (recipe == null)
			{
				Api.World.Logger.Error("Client tried to selected clayforming recipe with id {0}, but no such recipe exists!");
				selectedRecipe = null;
				selectedRecipeId = -1;
				return;
			}
			selectedRecipe = recipe;
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
			Api.World.FrameProfiler.Enter("clayforming");
			OnUseOver(player, voxelPos, facing, mouseMode);
			Api.World.FrameProfiler.Leave();
		}
	}

	public void OpenDialog(IClientWorldAccessor world, BlockPos pos, ItemStack ingredient)
	{
		if (dlg == null || !dlg.IsOpened())
		{
			if (ingredient.Collectible is ItemWorkItem)
			{
				ingredient = new ItemStack(world.GetItem(new AssetLocation("clay-" + ingredient.Collectible.LastCodePart())));
			}
			List<ClayFormingRecipe> recipes = (from r in Api.GetClayformingRecipes()
				where r.Ingredient.SatisfiesAsIngredient(ingredient)
				orderby r.Output.ResolvedItemstack.Collectible.Code
				select r).ToList();
			List<ItemStack> stacks = recipes.Select((ClayFormingRecipe r) => r.Output.ResolvedItemstack).ToList();
			ICoreClientAPI capi = Api as ICoreClientAPI;
			dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select recipe"), stacks.ToArray(), delegate(int selectedIndex)
			{
				capi.Logger.VerboseDebug("Select clay from recipe {0}, have {1} recipes.", selectedIndex, recipes.Count);
				selectedRecipe = recipes[selectedIndex];
				selectedRecipeId = selectedRecipe.RecipeId;
				capi.Network.SendBlockEntityPacket(pos, 1001, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
				RegenMeshForNextLayer();
			}, delegate
			{
				capi.Network.SendBlockEntityPacket(pos, 1003);
			}, pos, Api as ICoreClientAPI);
			dlg.OnClosed += dlg.Dispose;
			dlg.TryOpen();
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (workItemStack != null && SelectedRecipe != null)
		{
			dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe?.Output?.ResolvedItemstack?.GetName()));
			dsc.AppendLine(Lang.Get("Available Voxels: {0}", AvailableVoxels));
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		workitemRenderer?.Dispose();
		if (Api is ICoreClientAPI capi)
		{
			capi.Event.ColorsPresetChanged -= RegenMeshForNextLayer;
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		workItemStack?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(workItemStack), blockIdMapping, itemIdMapping);
		baseMaterial?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(baseMaterial), blockIdMapping, itemIdMapping);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack itemStack = workItemStack;
		if (itemStack != null && !itemStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			workItemStack = null;
		}
		ItemStack itemStack2 = baseMaterial;
		if (itemStack2 != null && !itemStack2.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			baseMaterial = null;
		}
		workItemStack?.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(workItemStack), oldBlockIdMapping, oldItemIdMapping, resolveImports);
		baseMaterial?.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(baseMaterial), oldBlockIdMapping, oldItemIdMapping, resolveImports);
	}
}
