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

public class BlockEntityAnvil : BlockEntity, IRotatable, ITemperatureSensitive
{
	public static SimpleParticleProperties bigMetalSparks;

	public static SimpleParticleProperties smallMetalSparks;

	public static SimpleParticleProperties slagPieces;

	private ItemStack workItemStack;

	public int SelectedRecipeId = -1;

	public byte[,,] Voxels = new byte[16, 6, 16];

	private float voxYOff = 0.625f;

	private Cuboidf[] selectionBoxes = new Cuboidf[1];

	public int OwnMetalTier;

	private AnvilWorkItemRenderer workitemRenderer;

	public int rotation;

	public float MeshAngle;

	private MeshData currentMesh;

	private GuiDialog dlg;

	private ItemStack returnOnCancelStack;

	private static int bitsPerByte;

	private static int partsPerByte;

	public bool[,,] recipeVoxels
	{
		get
		{
			if (SelectedRecipe == null)
			{
				return null;
			}
			bool[,,] origVoxels = SelectedRecipe.Voxels;
			bool[,,] rotVoxels = new bool[origVoxels.GetLength(0), origVoxels.GetLength(1), origVoxels.GetLength(2)];
			if (rotation == 0)
			{
				return origVoxels;
			}
			for (int i = 0; i < rotation / 90; i++)
			{
				for (int x = 0; x < origVoxels.GetLength(0); x++)
				{
					for (int y = 0; y < origVoxels.GetLength(1); y++)
					{
						for (int z = 0; z < origVoxels.GetLength(2); z++)
						{
							rotVoxels[z, y, x] = origVoxels[16 - x - 1, y, z];
						}
					}
				}
				origVoxels = (bool[,,])rotVoxels.Clone();
			}
			return rotVoxels;
		}
	}

	public SmithingRecipe SelectedRecipe => Api.GetSmithingRecipes().FirstOrDefault((SmithingRecipe r) => r.RecipeId == SelectedRecipeId);

	public bool CanWorkCurrent
	{
		get
		{
			if (workItemStack != null)
			{
				return (workItemStack.Collectible as IAnvilWorkable).CanWork(WorkItemStack);
			}
			return false;
		}
	}

	public ItemStack WorkItemStack => workItemStack;

	public bool IsHot => (workItemStack?.Collectible.GetTemperature(Api.World, workItemStack) ?? 0f) > 20f;

	static BlockEntityAnvil()
	{
		bitsPerByte = 2;
		partsPerByte = 8 / bitsPerByte;
		smallMetalSparks = new SimpleParticleProperties(2f, 5f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-3f, 8f, -3f), new Vec3f(3f, 12f, 3f), 0.1f, 1f, 0.25f, 0.25f, EnumParticleModel.Quad);
		smallMetalSparks.VertexFlags = 128;
		smallMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
		smallMetalSparks.ParticleModel = EnumParticleModel.Quad;
		smallMetalSparks.LifeLength = 0.03f;
		smallMetalSparks.MinVelocity = new Vec3f(-2f, 1f, -2f);
		smallMetalSparks.AddVelocity = new Vec3f(4f, 2f, 4f);
		smallMetalSparks.MinQuantity = 6f;
		smallMetalSparks.AddQuantity = 12f;
		smallMetalSparks.MinSize = 0.1f;
		smallMetalSparks.MaxSize = 0.1f;
		smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f);
		bigMetalSparks = new SimpleParticleProperties(4f, 8f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-1f, 1f, -1f), new Vec3f(2f, 4f, 2f), 0.5f, 1f, 0.25f, 0.25f);
		bigMetalSparks.VertexFlags = 128;
		bigMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
		bigMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		bigMetalSparks.Bounciness = 1f;
		bigMetalSparks.addLifeLength = 2f;
		bigMetalSparks.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -233f);
		bigMetalSparks.BlueEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -83f);
		slagPieces = new SimpleParticleProperties(2f, 12f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-1f, 0.5f, -1f), new Vec3f(2f, 1.5f, 2f), 0.5f, 1f, 0.25f, 0.5f);
		slagPieces.AddPos.Set(0.0625, 0.0, 0.0625);
		slagPieces.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		workItemStack?.ResolveBlockOrItem(api.World);
		if (api is ICoreClientAPI capi)
		{
			capi.Event.RegisterRenderer(workitemRenderer = new AnvilWorkItemRenderer(this, Pos, capi), EnumRenderStage.Opaque);
			capi.Event.RegisterRenderer(workitemRenderer, EnumRenderStage.AfterFinalComposition);
			RegenMeshAndSelectionBoxes();
			capi.Tesselator.TesselateBlock(base.Block, out currentMesh);
			capi.Event.ColorsPresetChanged += RegenMeshAndSelectionBoxes;
		}
		string metalType = base.Block.Variant["metal"];
		if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metalType, out var var))
		{
			OwnMetalTier = var.Tier;
		}
	}

	internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (itemstack != null && itemstack.Collectible.Tool.GetValueOrDefault() == EnumTool.Hammer)
		{
			return RotateWorkItem(byPlayer.Entity.Controls.ShiftKey);
		}
		if (byPlayer.Entity.Controls.ShiftKey)
		{
			return TryPut(world, byPlayer, blockSel);
		}
		return TryTake(world, byPlayer, blockSel);
	}

	private bool RotateWorkItem(bool ccw)
	{
		byte[,,] rotVoxels = new byte[16, 6, 16];
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					if (ccw)
					{
						rotVoxels[z, y, x] = Voxels[x, y, 16 - z - 1];
					}
					else
					{
						rotVoxels[z, y, x] = Voxels[16 - x - 1, y, z];
					}
				}
			}
		}
		rotation = (rotation + 90) % 360;
		Voxels = rotVoxels;
		RegenMeshAndSelectionBoxes();
		MarkDirty();
		return true;
	}

	private bool TryTake(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (workItemStack == null)
		{
			return false;
		}
		ditchWorkItemStack(byPlayer);
		return true;
	}

	private bool TryPut(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (slot.Itemstack == null)
		{
			return false;
		}
		ItemStack stack = slot.Itemstack;
		if (!(stack.Collectible is IAnvilWorkable workableobj))
		{
			return false;
		}
		int requiredTier = workableobj.GetRequiredAnvilTier(stack);
		if (requiredTier > OwnMetalTier)
		{
			if (world.Side == EnumAppSide.Client)
			{
				(Api as ICoreClientAPI).TriggerIngameError(this, "toolowtier", Lang.Get("Working this metal needs a tier {0} anvil", requiredTier));
			}
			return false;
		}
		ItemStack newWorkItemStack = workableobj.TryPlaceOn(stack, this);
		if (newWorkItemStack != null)
		{
			if (workItemStack == null)
			{
				workItemStack = newWorkItemStack;
				rotation = workItemStack.Attributes.GetInt("rotation");
			}
			else if (workItemStack.Collectible is ItemWorkItem { isBlisterSteel: not false })
			{
				return false;
			}
			if (SelectedRecipeId < 0)
			{
				List<SmithingRecipe> list = workableobj.GetMatchingRecipes(stack);
				if (list.Count == 1)
				{
					SelectedRecipeId = list[0].RecipeId;
				}
				else if (world.Side == EnumAppSide.Client)
				{
					OpenDialog(stack);
				}
			}
			returnOnCancelStack = slot.TakeOut(1);
			slot.MarkDirty();
			Api.World.Logger.Audit("{0} Put 1x{1} on to Anvil at {2}.", byPlayer?.PlayerName, newWorkItemStack.Collectible.Code, Pos);
			if (Api.Side == EnumAppSide.Server)
			{
				RegenMeshAndSelectionBoxes();
			}
			CheckIfFinished(byPlayer);
			MarkDirty();
			return true;
		}
		return false;
	}

	internal void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel)
	{
	}

	internal void OnUseOver(IPlayer byPlayer, int selectionBoxIndex)
	{
		if (selectionBoxIndex > 0 && selectionBoxIndex < selectionBoxes.Length)
		{
			Cuboidf box = selectionBoxes[selectionBoxIndex];
			Vec3i voxelPos = new Vec3i((int)(16f * box.X1), (int)(16f * box.Y1) - 10, (int)(16f * box.Z1));
			OnUseOver(byPlayer, voxelPos, new BlockSelection
			{
				Position = Pos,
				SelectionBoxIndex = selectionBoxIndex
			});
		}
	}

	internal void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockSelection blockSel)
	{
		if (voxelPos == null)
		{
			return;
		}
		if (SelectedRecipe == null)
		{
			ditchWorkItemStack();
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			SendUseOverPacket(byPlayer, voxelPos);
		}
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (slot.Itemstack == null || !CanWorkCurrent)
		{
			return;
		}
		int toolMode = slot.Itemstack.Collectible.GetToolMode(slot, byPlayer, blockSel);
		float yaw = GameMath.Mod(byPlayer.Entity.Pos.Yaw, (float)Math.PI * 2f);
		EnumVoxelMaterial voxelMat = (EnumVoxelMaterial)Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z];
		if (voxelMat != 0)
		{
			spawnParticles(voxelPos, voxelMat, byPlayer);
			switch (toolMode)
			{
			case 0:
				OnHit(voxelPos);
				break;
			case 1:
				OnUpset(voxelPos, BlockFacing.NORTH.FaceWhenRotatedBy(0f, yaw - (float)Math.PI, 0f));
				break;
			case 2:
				OnUpset(voxelPos, BlockFacing.EAST.FaceWhenRotatedBy(0f, yaw - (float)Math.PI, 0f));
				break;
			case 3:
				OnUpset(voxelPos, BlockFacing.SOUTH.FaceWhenRotatedBy(0f, yaw - (float)Math.PI, 0f));
				break;
			case 4:
				OnUpset(voxelPos, BlockFacing.WEST.FaceWhenRotatedBy(0f, yaw - (float)Math.PI, 0f));
				break;
			case 5:
				OnSplit(voxelPos);
				break;
			}
			RegenMeshAndSelectionBoxes();
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
			Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
			slot.Itemstack.Collectible.DamageItem(Api.World, byPlayer.Entity, slot);
			if (!HasAnyMetalVoxel())
			{
				clearWorkSpace();
				return;
			}
		}
		CheckIfFinished(byPlayer);
		MarkDirty();
	}

	private void spawnParticles(Vec3i voxelPos, EnumVoxelMaterial voxelMat, IPlayer byPlayer)
	{
		float temp = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
		if (voxelMat == EnumVoxelMaterial.Metal && temp > 800f)
		{
			bigMetalSparks.MinPos = Pos.ToVec3d().AddCopy((float)voxelPos.X / 16f, voxYOff + (float)voxelPos.Y / 16f + 0.0625f, (float)voxelPos.Z / 16f);
			bigMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
			bigMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)(temp - 700f) / 2, 32, 128);
			Api.World.SpawnParticles(bigMetalSparks, byPlayer);
			smallMetalSparks.MinPos = Pos.ToVec3d().AddCopy((float)voxelPos.X / 16f, voxYOff + (float)voxelPos.Y / 16f + 0.0625f, (float)voxelPos.Z / 16f);
			smallMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)(temp - 770f) / 3, 32, 128);
			smallMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
			Api.World.SpawnParticles(smallMetalSparks, byPlayer);
		}
		if (voxelMat == EnumVoxelMaterial.Slag)
		{
			slagPieces.Color = workItemStack.Collectible.GetRandomColor(Api as ICoreClientAPI, workItemStack);
			slagPieces.MinPos = Pos.ToVec3d().AddCopy((float)voxelPos.X / 16f, voxYOff + (float)voxelPos.Y / 16f + 0.0625f, (float)voxelPos.Z / 16f);
			Api.World.SpawnParticles(slagPieces, byPlayer);
		}
	}

	internal string PrintDebugText()
	{
		SmithingRecipe recipe = SelectedRecipe;
		EnumHelveWorkableMode? mode = (workItemStack?.Collectible as IAnvilWorkable)?.GetHelveWorkableMode(workItemStack, this);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Workitem: " + workItemStack);
		stringBuilder.AppendLine("Recipe: " + recipe?.Name);
		stringBuilder.AppendLine("Matches recipe: " + MatchesRecipe());
		EnumHelveWorkableMode? enumHelveWorkableMode = mode;
		stringBuilder.AppendLine("Helve Workable: " + enumHelveWorkableMode.ToString());
		return stringBuilder.ToString();
	}

	public virtual void OnHelveHammerHit()
	{
		if (workItemStack == null || !CanWorkCurrent)
		{
			return;
		}
		SmithingRecipe recipe = SelectedRecipe;
		if (recipe == null)
		{
			return;
		}
		EnumHelveWorkableMode mode = (workItemStack.Collectible as IAnvilWorkable).GetHelveWorkableMode(workItemStack, this);
		if (mode == EnumHelveWorkableMode.NotWorkable)
		{
			return;
		}
		rotation = 0;
		int ymax = recipe.QuantityLayers;
		if (mode == EnumHelveWorkableMode.TestSufficientVoxelsWorkable)
		{
			Vec3i usableMetalVoxel = findFreeMetalVoxel();
			for (int x = 0; x < 16; x++)
			{
				for (int z = 0; z < 16; z++)
				{
					for (int y = 0; y < 6; y++)
					{
						bool requireMetalHere = y < ymax && recipe.Voxels[x, y, z];
						EnumVoxelMaterial mat = (EnumVoxelMaterial)Voxels[x, y, z];
						if (mat == EnumVoxelMaterial.Slag)
						{
							Voxels[x, y, z] = 0;
							onHelveHitSuccess(mat, null, x, y, z);
							return;
						}
						if (requireMetalHere && usableMetalVoxel != null && mat == EnumVoxelMaterial.Empty)
						{
							Voxels[x, y, z] = 1;
							Voxels[usableMetalVoxel.X, usableMetalVoxel.Y, usableMetalVoxel.Z] = 0;
							onHelveHitSuccess(mat, usableMetalVoxel, x, y, z);
							return;
						}
					}
				}
			}
			if (usableMetalVoxel != null)
			{
				Voxels[usableMetalVoxel.X, usableMetalVoxel.Y, usableMetalVoxel.Z] = 0;
				onHelveHitSuccess(EnumVoxelMaterial.Metal, null, usableMetalVoxel.X, usableMetalVoxel.Y, usableMetalVoxel.Z);
			}
			return;
		}
		for (int y2 = 5; y2 >= 0; y2--)
		{
			for (int z2 = 0; z2 < 16; z2++)
			{
				for (int x2 = 0; x2 < 16; x2++)
				{
					bool requireMetalHere2 = y2 < ymax && recipe.Voxels[x2, y2, z2];
					EnumVoxelMaterial mat2 = (EnumVoxelMaterial)Voxels[x2, y2, z2];
					if ((!requireMetalHere2 || mat2 != EnumVoxelMaterial.Metal) && (requireMetalHere2 || mat2 != 0))
					{
						if (requireMetalHere2 && mat2 == EnumVoxelMaterial.Empty)
						{
							Voxels[x2, y2, z2] = 1;
						}
						else
						{
							Voxels[x2, y2, z2] = 0;
						}
						onHelveHitSuccess((mat2 == EnumVoxelMaterial.Empty) ? EnumVoxelMaterial.Metal : mat2, null, x2, y2, z2);
						return;
					}
				}
			}
		}
	}

	private void onHelveHitSuccess(EnumVoxelMaterial mat, Vec3i usableMetalVoxel, int x, int y, int z)
	{
		if (Api.World.Side == EnumAppSide.Client)
		{
			spawnParticles(new Vec3i(x, y, z), (mat == EnumVoxelMaterial.Empty) ? EnumVoxelMaterial.Metal : mat, null);
			if (usableMetalVoxel != null)
			{
				spawnParticles(usableMetalVoxel, EnumVoxelMaterial.Metal, null);
			}
		}
		RegenMeshAndSelectionBoxes();
		CheckIfFinished(null);
	}

	private Vec3i findFreeMetalVoxel()
	{
		SmithingRecipe recipe = SelectedRecipe;
		int ymax = recipe.QuantityLayers;
		for (int y = 5; y >= 0; y--)
		{
			for (int z = 0; z < 16; z++)
			{
				for (int x = 0; x < 16; x++)
				{
					bool num = y < ymax && recipe.Voxels[x, y, z];
					EnumVoxelMaterial mat = (EnumVoxelMaterial)Voxels[x, y, z];
					if (!num && mat == EnumVoxelMaterial.Metal)
					{
						return new Vec3i(x, y, z);
					}
				}
			}
		}
		return null;
	}

	public virtual void CheckIfFinished(IPlayer byPlayer)
	{
		if (SelectedRecipe != null && MatchesRecipe() && Api.World is IServerWorldAccessor)
		{
			Voxels = new byte[16, 6, 16];
			ItemStack outstack = SelectedRecipe.Output.ResolvedItemstack.Clone();
			outstack.Collectible.SetTemperature(Api.World, outstack, workItemStack.Collectible.GetTemperature(Api.World, workItemStack));
			workItemStack = null;
			SelectedRecipeId = -1;
			if (byPlayer != null && byPlayer.InventoryManager.TryGiveItemstack(outstack))
			{
				Api.World.PlaySoundFor(new AssetLocation("sounds/player/collect"), byPlayer, randomizePitch: false, 24f);
			}
			else
			{
				Api.World.SpawnItemEntity(outstack, Pos.ToVec3d().Add(0.5, 0.626, 0.5));
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Anvil at {2}.", byPlayer?.PlayerName, outstack.Collectible.Code, Pos);
			RegenMeshAndSelectionBoxes();
			MarkDirty();
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
			rotation = 0;
		}
	}

	public void ditchWorkItemStack(IPlayer byPlayer = null)
	{
		if (workItemStack == null)
		{
			return;
		}
		ItemStack ditchedStack;
		if (SelectedRecipe == null)
		{
			ditchedStack = returnOnCancelStack ?? (workItemStack.Collectible as IAnvilWorkable).GetBaseMaterial(workItemStack);
			float temp = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
			ditchedStack.Collectible.SetTemperature(Api.World, ditchedStack, temp);
		}
		else
		{
			workItemStack.Attributes.SetBytes("voxels", serializeVoxels(Voxels));
			workItemStack.Attributes.SetInt("selectedRecipeId", SelectedRecipeId);
			workItemStack.Attributes.SetInt("rotation", rotation);
			if (workItemStack.Collectible is ItemIronBloom bloomItem)
			{
				workItemStack.Attributes.SetInt("hashCode", bloomItem.GetWorkItemHashCode(workItemStack));
			}
			ditchedStack = workItemStack;
		}
		if (byPlayer == null || !byPlayer.InventoryManager.TryGiveItemstack(ditchedStack))
		{
			Api.World.SpawnItemEntity(ditchedStack, Pos);
		}
		Api.World.Logger.Audit("{0} Took 1x{1} from Anvil at {2}.", byPlayer?.PlayerName, ditchedStack.Collectible.Code, Pos);
		clearWorkSpace();
	}

	protected void clearWorkSpace()
	{
		workItemStack = null;
		Voxels = new byte[16, 6, 16];
		RegenMeshAndSelectionBoxes();
		MarkDirty();
		rotation = 0;
		SelectedRecipeId = -1;
	}

	private bool MatchesRecipe()
	{
		if (SelectedRecipe == null)
		{
			return false;
		}
		int ymax = Math.Min(6, SelectedRecipe.QuantityLayers);
		bool[,,] recipeVoxels = this.recipeVoxels;
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < ymax; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					byte desiredMat = (byte)(recipeVoxels[x, y, z] ? 1u : 0u);
					if (Voxels[x, y, z] != desiredMat)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private bool HasAnyMetalVoxel()
	{
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					if (Voxels[x, y, z] == 1)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public virtual void OnSplit(Vec3i voxelPos)
	{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == 2)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dz = -1; dz <= 1; dz++)
				{
					int x = voxelPos.X + dx;
					int z = voxelPos.Z + dz;
					if (x >= 0 && z >= 0 && x < 16 && z < 16 && Voxels[x, voxelPos.Y, z] == 2)
					{
						Voxels[x, voxelPos.Y, z] = 0;
					}
				}
			}
		}
		Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
	}

	public virtual void OnUpset(Vec3i voxelPos, BlockFacing towardsFace)
	{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != 1 || (voxelPos.Y < 5 && Voxels[voxelPos.X, voxelPos.Y + 1, voxelPos.Z] != 0))
		{
			return;
		}
		Vec3i npos = voxelPos.Clone().Add(towardsFace);
		Vec3i opFaceDir = towardsFace.Opposite.Normali;
		if (npos.X < 0 || npos.X >= 16 || npos.Y < 0 || npos.Y >= 6 || npos.Z < 0 || npos.Z >= 16)
		{
			return;
		}
		if (voxelPos.Y > 0)
		{
			if (Voxels[npos.X, npos.Y, npos.Z] == 0 && Voxels[npos.X, npos.Y - 1, npos.Z] != 0)
			{
				if (npos.X >= 0 && npos.X < 16 && npos.Y >= 0 && npos.Y < 6 && npos.Z >= 0 && npos.Z < 16)
				{
					Voxels[npos.X, npos.Y, npos.Z] = 1;
					Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
				}
				return;
			}
			npos.Y++;
			if (voxelPos.X + opFaceDir.X >= 0 && voxelPos.X + opFaceDir.X < 16 && voxelPos.Z + opFaceDir.Z >= 0 && voxelPos.Z + opFaceDir.Z < 16)
			{
				if (npos.Y < 6 && Voxels[npos.X, npos.Y, npos.Z] == 0 && Voxels[npos.X, npos.Y - 1, npos.Z] != 0 && Voxels[voxelPos.X + opFaceDir.X, voxelPos.Y, voxelPos.Z + opFaceDir.Z] == 0)
				{
					Voxels[npos.X, npos.Y, npos.Z] = 1;
					Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
				}
				else if (!moveVoxelDownwards(voxelPos.Clone(), towardsFace, 1))
				{
					moveVoxelDownwards(voxelPos.Clone(), towardsFace, 2);
				}
			}
		}
		else
		{
			npos.Y++;
			if (npos.X >= 0 && npos.X < 16 && npos.Y >= 0 && npos.Y < 6 && npos.Z >= 0 && npos.Z < 16 && voxelPos.X + opFaceDir.X >= 0 && voxelPos.X + opFaceDir.X < 16 && voxelPos.Z + opFaceDir.Z >= 0 && voxelPos.Z + opFaceDir.Z < 16 && npos.Y < 6 && Voxels[npos.X, npos.Y, npos.Z] == 0 && Voxels[npos.X, npos.Y - 1, npos.Z] != 0 && Voxels[voxelPos.X + opFaceDir.X, voxelPos.Y, voxelPos.Z + opFaceDir.Z] == 0)
			{
				Voxels[npos.X, npos.Y, npos.Z] = 1;
				Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
			}
		}
	}

	private Vec3i getClosestBfs(Vec3i voxelPos, BlockFacing towardsFace, int maxDist)
	{
		Queue<Vec3i> nodesToVisit = new Queue<Vec3i>();
		HashSet<Vec3i> nodesVisited = new HashSet<Vec3i>();
		nodesToVisit.Enqueue(voxelPos);
		while (nodesToVisit.Count > 0)
		{
			Vec3i node = nodesToVisit.Dequeue();
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing face = BlockFacing.HORIZONTALS[i];
				Vec3i nnode = node.Clone().Add(face);
				if (nnode.X < 0 || nnode.X >= 16 || nnode.Y < 0 || nnode.Y >= 6 || nnode.Z < 0 || nnode.Z >= 16 || nodesVisited.Contains(nnode))
				{
					continue;
				}
				nodesVisited.Add(nnode);
				double x = nnode.X - voxelPos.X;
				double z = nnode.Z - voxelPos.Z;
				double len = GameMath.Sqrt(x * x + z * z);
				if (!(len > (double)maxDist))
				{
					x /= len;
					z /= len;
					if ((towardsFace == null || Math.Abs((float)Math.Acos((double)towardsFace.Normalf.X * x + (double)towardsFace.Normalf.Z * z)) < 0.43633232f) && Voxels[nnode.X, nnode.Y, nnode.Z] == 0)
					{
						return nnode;
					}
					if (Voxels[nnode.X, nnode.Y, nnode.Z] == 1)
					{
						nodesToVisit.Enqueue(nnode);
					}
				}
			}
		}
		return null;
	}

	public virtual void OnHit(Vec3i voxelPos)
	{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != 1 || voxelPos.Y <= 0)
		{
			return;
		}
		int voxelsMoved = 0;
		for (int dx2 = -1; dx2 <= 1; dx2++)
		{
			for (int dz2 = -1; dz2 <= 1; dz2++)
			{
				if ((dx2 != 0 || dz2 != 0) && voxelPos.X + dx2 >= 0 && voxelPos.X + dx2 < 16 && voxelPos.Z + dz2 >= 0 && voxelPos.Z + dz2 < 16 && Voxels[voxelPos.X + dx2, voxelPos.Y, voxelPos.Z + dz2] == 1)
				{
					voxelsMoved += (moveVoxelDownwards(voxelPos.Clone().Add(dx2, 0, dz2), null, 1) ? 1 : 0);
				}
			}
		}
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == 1)
		{
			voxelsMoved += (moveVoxelDownwards(voxelPos.Clone(), null, 1) ? 1 : 0);
		}
		if (voxelsMoved != 0)
		{
			return;
		}
		Vec3i emptySpot = null;
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				if ((dx == 0 && dz == 0) || voxelPos.X + 2 * dx < 0 || voxelPos.X + 2 * dx >= 16 || voxelPos.Z + 2 * dz < 0 || voxelPos.Z + 2 * dz >= 16)
				{
					continue;
				}
				bool spotEmpty = Voxels[voxelPos.X + 2 * dx, voxelPos.Y, voxelPos.Z + 2 * dz] == 0;
				if (Voxels[voxelPos.X + dx, voxelPos.Y, voxelPos.Z + dz] == 1 && spotEmpty)
				{
					Voxels[voxelPos.X + dx, voxelPos.Y, voxelPos.Z + dz] = 0;
					if (Voxels[voxelPos.X + 2 * dx, voxelPos.Y - 1, voxelPos.Z + 2 * dz] == 0)
					{
						Voxels[voxelPos.X + 2 * dx, voxelPos.Y - 1, voxelPos.Z + 2 * dz] = 1;
					}
					else
					{
						Voxels[voxelPos.X + 2 * dx, voxelPos.Y, voxelPos.Z + 2 * dz] = 1;
					}
				}
				else if (spotEmpty)
				{
					emptySpot = voxelPos.Clone().Add(dx, 0, dz);
				}
			}
		}
		if (emptySpot != null && Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == 1)
		{
			Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
			if (Voxels[emptySpot.X, emptySpot.Y - 1, emptySpot.Z] == 0)
			{
				Voxels[emptySpot.X, emptySpot.Y - 1, emptySpot.Z] = 1;
			}
			else
			{
				Voxels[emptySpot.X, emptySpot.Y, emptySpot.Z] = 1;
			}
		}
	}

	protected bool moveVoxelDownwards(Vec3i voxelPos, BlockFacing towardsFace, int maxDist)
	{
		int origy = voxelPos.Y;
		while (voxelPos.Y > 0)
		{
			voxelPos.Y--;
			Vec3i spos = getClosestBfs(voxelPos, towardsFace, maxDist);
			if (spos == null)
			{
				continue;
			}
			Voxels[voxelPos.X, origy, voxelPos.Z] = 0;
			for (int y = 0; y <= spos.Y; y++)
			{
				if (Voxels[spos.X, y, spos.Z] == 0)
				{
					Voxels[spos.X, y, spos.Z] = 1;
					return true;
				}
			}
			return true;
		}
		return false;
	}

	protected void RegenMeshAndSelectionBoxes()
	{
		if (workitemRenderer != null)
		{
			workitemRenderer.RegenMesh(workItemStack, Voxels, recipeVoxels);
		}
		List<Cuboidf> boxes = new List<Cuboidf>();
		boxes.Add(null);
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					if (Voxels[x, y, z] != 0)
					{
						float py = y + 10;
						boxes.Add(new Cuboidf((float)x / 16f, py / 16f, (float)z / 16f, (float)x / 16f + 0.0625f, py / 16f + 0.0625f, (float)z / 16f + 0.0625f));
					}
				}
			}
		}
		selectionBoxes = boxes.ToArray();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		workitemRenderer?.Dispose();
		workitemRenderer = null;
		if (Api is ICoreClientAPI capi)
		{
			capi.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (workItemStack != null)
		{
			workItemStack.Attributes.SetBytes("voxels", serializeVoxels(Voxels));
			workItemStack.Attributes.SetInt("selectedRecipeId", SelectedRecipeId);
			Api.World.SpawnItemEntity(workItemStack, Pos);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		Voxels = deserializeVoxels(tree.GetBytes("voxels"));
		workItemStack = tree.GetItemstack("workItemStack");
		SelectedRecipeId = tree.GetInt("selectedRecipeId", -1);
		rotation = tree.GetInt("rotation");
		if (Api != null && workItemStack != null)
		{
			workItemStack.ResolveBlockOrItem(Api.World);
		}
		RegenMeshAndSelectionBoxes();
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			((ICoreClientAPI)Api).Tesselator.TesselateBlock(base.Block, out var newMesh);
			currentMesh = newMesh;
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBytes("voxels", serializeVoxels(Voxels));
		tree.SetItemstack("workItemStack", workItemStack);
		tree.SetInt("selectedRecipeId", SelectedRecipeId);
		tree.SetInt("rotation", rotation);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public static byte[] serializeVoxels(byte[,,] voxels)
	{
		byte[] data = new byte[1536 / partsPerByte];
		int pos = 0;
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					int bitpos = bitsPerByte * (pos % partsPerByte);
					data[pos / partsPerByte] |= (byte)((voxels[x, y, z] & 3) << bitpos);
					pos++;
				}
			}
		}
		return data;
	}

	public static byte[,,] deserializeVoxels(byte[] data)
	{
		byte[,,] voxels = new byte[16, 6, 16];
		if (data == null || data.Length < 1536 / partsPerByte)
		{
			return voxels;
		}
		int pos = 0;
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					int bitpos = bitsPerByte * (pos % partsPerByte);
					voxels[x, y, z] = (byte)((uint)(data[pos / partsPerByte] >> bitpos) & 3u);
					pos++;
				}
			}
		}
		return voxels;
	}

	protected void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos)
	{
		byte[] data;
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(ms);
			binaryWriter.Write(voxelPos.X);
			binaryWriter.Write(voxelPos.Y);
			binaryWriter.Write(voxelPos.Z);
			data = ms.ToArray();
		}
		((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, 1002, data);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			int recipeid = SerializerUtil.Deserialize<int>(data);
			SmithingRecipe recipe = Api.GetSmithingRecipes().FirstOrDefault((SmithingRecipe r) => r.RecipeId == recipeid);
			if (recipe == null)
			{
				Api.World.Logger.Error("Client tried to selected smithing recipe with id {0}, but no such recipe exists!");
				ditchWorkItemStack(player);
				return;
			}
			List<SmithingRecipe> list = (WorkItemStack?.Collectible as ItemWorkItem)?.GetMatchingRecipes(workItemStack);
			if (list == null || list.FirstOrDefault((SmithingRecipe r) => r.RecipeId == recipeid) == null)
			{
				Api.World.Logger.Error("Client tried to selected smithing recipe with id {0}, but it is not a valid one for the given work item stack!", recipe.RecipeId);
				ditchWorkItemStack(player);
				return;
			}
			SelectedRecipeId = recipe.RecipeId;
			MarkDirty();
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		switch (packetid)
		{
		case 1003:
			ditchWorkItemStack(player);
			break;
		case 1002:
		{
			Vec3i voxelPos;
			using (MemoryStream ms = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(ms);
				voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
			}
			OnUseOver(player, voxelPos, new BlockSelection
			{
				Position = Pos
			});
			break;
		}
		}
	}

	internal void OpenDialog(ItemStack ingredient)
	{
		List<SmithingRecipe> recipes = (ingredient.Collectible as IAnvilWorkable).GetMatchingRecipes(ingredient);
		List<ItemStack> stacks = recipes.Select((SmithingRecipe r) => r.Output.ResolvedItemstack).ToList();
		_ = (IClientWorldAccessor)Api.World;
		ICoreClientAPI capi = Api as ICoreClientAPI;
		dlg?.Dispose();
		dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select smithing recipe"), stacks.ToArray(), delegate(int selectedIndex)
		{
			SelectedRecipeId = recipes[selectedIndex].RecipeId;
			capi.Network.SendBlockEntityPacket(Pos, 1001, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
		}, delegate
		{
			capi.Network.SendBlockEntityPacket(Pos, 1003);
		}, Pos, Api as ICoreClientAPI);
		dlg.TryOpen();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		dsc.AppendLine(Lang.Get("Tier {0} anvil", OwnMetalTier));
		if (workItemStack != null && SelectedRecipe != null)
		{
			float temperature = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
			dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe.Output?.ResolvedItemstack?.GetName()));
			if (temperature < 25f)
			{
				dsc.AppendLine(Lang.Get("Temperature: Cold"));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
			}
			if (!CanWorkCurrent)
			{
				dsc.AppendLine(Lang.Get("Too cold to work"));
			}
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack itemStack = workItemStack;
		if (itemStack != null && !itemStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			workItemStack = null;
		}
		workItemStack?.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(workItemStack), oldBlockIdMapping, oldItemIdMapping, resolveImports);
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		if (workItemStack != null)
		{
			if (workItemStack.Class == EnumItemClass.Item)
			{
				itemIdMapping[workItemStack.Id] = workItemStack.Item.Code;
			}
			else
			{
				blockIdMapping[workItemStack.Id] = workItemStack.Block.Code;
			}
			workItemStack.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(workItemStack), blockIdMapping, itemIdMapping);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		workitemRenderer?.Dispose();
		dlg?.TryClose();
		dlg?.Dispose();
		if (Api is ICoreClientAPI capi)
		{
			capi.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f));
		return true;
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public void CoolNow(float amountRel)
	{
		if (workItemStack != null)
		{
			float temp = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
			if (temp > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.25, null, randomizePitch: false, 16f);
			}
			workItemStack.Collectible.SetTemperature(Api.World, workItemStack, Math.Max(20f, temp - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
	}
}
