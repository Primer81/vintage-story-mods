using System;
using System.Collections.Generic;
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

public class BlockEntityPitKiln : BlockEntityGroundStorage, IHeatSource
{
	protected ILoadedSound ambientSound;

	protected BuildStage[] buildStages;

	protected Shape shape;

	protected MeshData mesh;

	protected string[] selectiveElements;

	protected Dictionary<string, string> textureCodeReplace = new Dictionary<string, string>();

	protected int currentBuildStage;

	public bool Lit;

	public double BurningUntilTotalHours;

	public float BurnTimeHours = 20f;

	private bool nowTesselatingKiln;

	private ITexPositionSource blockTexPos;

	public bool IsComplete => currentBuildStage >= buildStages.Length;

	public BuildStage NextBuildStage => buildStages[currentBuildStage];

	protected override int invSlotCount => 10;

	public override TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (nowTesselatingKiln)
			{
				if (textureCodeReplace.TryGetValue(textureCode, out var replaceCode))
				{
					textureCode = replaceCode;
				}
				return blockTexPos[textureCode];
			}
			return base[textureCode];
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		BEBehaviorBurning behavior = GetBehavior<BEBehaviorBurning>();
		if (Lit)
		{
			BurningUntilTotalHours = Math.Min(api.World.Calendar.TotalHours + (double)BurnTimeHours, BurningUntilTotalHours);
		}
		behavior.OnFireTick = delegate
		{
			if (api.World.Calendar.TotalHours >= BurningUntilTotalHours && IsAreaLoaded())
			{
				OnFired();
			}
		};
		behavior.OnFireDeath = KillFire;
		behavior.ShouldBurn = () => Lit;
		behavior.OnCanBurn = delegate(BlockPos pos)
		{
			if (pos == Pos && !Lit && IsComplete)
			{
				return true;
			}
			Block block = Api.World.BlockAccessor.GetBlock(pos);
			Block block2 = Api.World.BlockAccessor.GetBlock(Pos.UpCopy());
			return block?.CombustibleProps != null && block.CombustibleProps.BurnDuration > 0f && (!IsAreaLoaded() || block2.Replaceable >= 6000);
		};
		base.Initialize(api);
		DetermineBuildStages();
		behavior.FuelPos = Pos.Copy();
		behavior.FirePos = Pos.UpCopy();
	}

	public bool IsAreaLoaded()
	{
		if (Api == null || Api.Side == EnumAppSide.Client)
		{
			return true;
		}
		ICoreServerAPI sapi = Api as ICoreServerAPI;
		int sizeX = sapi.WorldManager.MapSizeX / 32;
		int sizeY = sapi.WorldManager.MapSizeY / 32;
		int sizeZ = sapi.WorldManager.MapSizeZ / 32;
		int num = GameMath.Clamp((Pos.X - 1) / 32, 0, sizeX - 1);
		int maxcx = GameMath.Clamp((Pos.X + 1) / 32, 0, sizeX - 1);
		int mincy = GameMath.Clamp((Pos.Y - 1) / 32, 0, sizeY - 1);
		int maxcy = GameMath.Clamp((Pos.Y + 1) / 32, 0, sizeY - 1);
		int mincz = GameMath.Clamp((Pos.Z - 1) / 32, 0, sizeZ - 1);
		int maxcz = GameMath.Clamp((Pos.Z + 1) / 32, 0, sizeZ - 1);
		for (int cx = num; cx <= maxcx; cx++)
		{
			for (int cy = mincy; cy <= maxcy; cy++)
			{
				for (int cz = mincz; cz <= maxcz; cz++)
				{
					if (sapi.WorldManager.GetChunk(cx, cy, cz) == null)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public override bool OnPlayerInteractStart(IPlayer player, BlockSelection bs)
	{
		ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (hotbarSlot.Empty)
		{
			return false;
		}
		if (currentBuildStage < buildStages.Length)
		{
			BuildStageMaterial[] mats = buildStages[currentBuildStage].Materials;
			for (int i = 0; i < mats.Length; i++)
			{
				ItemStack stack = mats[i].ItemStack;
				if (!stack.Equals(Api.World, hotbarSlot.Itemstack, GlobalConstants.IgnoredStackAttributes) || stack.StackSize > hotbarSlot.StackSize || !isSameMatAsPreviouslyAdded(stack))
				{
					continue;
				}
				int toMove = stack.StackSize;
				for (int j = 4; j < invSlotCount; j++)
				{
					if (toMove <= 0)
					{
						break;
					}
					toMove -= hotbarSlot.TryPutInto(Api.World, inventory[j], toMove);
				}
				hotbarSlot.MarkDirty();
				currentBuildStage++;
				mesh = null;
				MarkDirty(redrawOnClient: true);
				updateSelectiveElements();
				(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				JsonObject attributes = stack.Collectible.Attributes;
				if (attributes != null && attributes["placeSound"].Exists)
				{
					AssetLocation sound = AssetLocation.Create(stack.Collectible.Attributes["placeSound"].AsString(), stack.Collectible.Code.Domain);
					if (sound != null)
					{
						Api.World.PlaySoundAt(sound.WithPathPrefixOnce("sounds/"), Pos, -0.4, player, randomizePitch: true, 12f);
					}
				}
			}
		}
		DetermineStorageProperties(null);
		return true;
	}

	protected bool isSameMatAsPreviouslyAdded(ItemStack newStack)
	{
		BuildStage bstage = buildStages[currentBuildStage];
		for (int i = 0; i < inventory.Count; i++)
		{
			ItemSlot slot = inventory[i];
			if (!slot.Empty && bstage.Materials.FirstOrDefault((BuildStageMaterial bsm) => bsm.ItemStack.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes)) != null && !newStack.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes))
			{
				return false;
			}
		}
		return true;
	}

	public override void DetermineStorageProperties(ItemSlot sourceSlot)
	{
		base.DetermineStorageProperties(sourceSlot);
		if (buildStages != null)
		{
			colBoxes[0].X1 = 0f;
			colBoxes[0].X2 = 1f;
			colBoxes[0].Z1 = 0f;
			colBoxes[0].Z2 = 1f;
			colBoxes[0].Y2 = Math.Max(colBoxes[0].Y2, buildStages[Math.Min(buildStages.Length - 1, currentBuildStage)].MinHitboxY2 / 16f);
			selBoxes[0] = colBoxes[0];
		}
	}

	public new float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return Lit ? 10 : 0;
	}

	public void OnFired()
	{
		if (IsValidPitKiln())
		{
			for (int i = 0; i < 4; i++)
			{
				ItemSlot slot = inventory[i];
				if (!slot.Empty)
				{
					ItemStack rawStack = slot.Itemstack;
					ItemStack firedStack = rawStack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
					if (firedStack != null)
					{
						slot.Itemstack = firedStack.Clone();
						slot.Itemstack.StackSize = rawStack.StackSize / rawStack.Collectible.CombustibleProps.SmeltedRatio;
					}
				}
			}
			MarkDirty(redrawOnClient: true);
		}
		KillFire(consumefuel: true);
	}

	protected bool IsValidPitKiln()
	{
		IWorldAccessor world = Api.World;
		BlockFacing[] array = BlockFacing.HORIZONTALS.Append(BlockFacing.DOWN);
		foreach (BlockFacing face in array)
		{
			BlockPos npos = Pos.AddCopy(face);
			Block block = world.BlockAccessor.GetBlock(npos);
			if (!block.CanAttachBlockAt(world.BlockAccessor, base.Block, npos, face.Opposite))
			{
				return false;
			}
			if (block.CombustibleProps != null)
			{
				return false;
			}
		}
		if (world.BlockAccessor.GetBlock(Pos.UpCopy()).Replaceable < 6000)
		{
			return false;
		}
		return true;
	}

	public void OnCreated(IPlayer byPlayer)
	{
		base.StorageProps = null;
		mesh = null;
		DetermineBuildStages();
		DetermineStorageProperties(null);
		ItemStack itemStack2 = (inventory[4].Itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(buildStages[0].Materials[0].ItemStack.StackSize));
		ItemStack stack = itemStack2;
		currentBuildStage++;
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes["placeSound"].Exists)
		{
			AssetLocation sound = AssetLocation.Create(stack.Collectible.Attributes["placeSound"].AsString(), stack.Collectible.Code.Domain);
			if (sound != null)
			{
				Api.World.PlaySoundAt(sound.WithPathPrefixOnce("sounds/"), Pos, -0.4, byPlayer, randomizePitch: true, 12f);
			}
		}
		byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		updateSelectiveElements();
	}

	public void DetermineBuildStages()
	{
		BlockPitkiln blockpk = base.Block as BlockPitkiln;
		bool found = false;
		foreach (KeyValuePair<string, BuildStage[]> val in blockpk.BuildStagesByBlock)
		{
			if (!inventory[0].Empty && WildcardUtil.Match(new AssetLocation(val.Key), inventory[0].Itemstack.Collectible.Code))
			{
				buildStages = val.Value;
				shape = blockpk.ShapesByBlock[val.Key];
				found = true;
				break;
			}
		}
		if (!found && blockpk.BuildStagesByBlock.TryGetValue("*", out buildStages))
		{
			shape = blockpk.ShapesByBlock["*"];
		}
		updateSelectiveElements();
	}

	private void updateSelectiveElements()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			textureCodeReplace.Clear();
			Dictionary<string, string> matCodeToEleCode = new Dictionary<string, string>();
			for (int j = 0; j < currentBuildStage; j++)
			{
				BuildStage bStage = buildStages[j];
				if (!matCodeToEleCode.ContainsKey(bStage.MatCode))
				{
					BuildStageMaterial bsm = currentlyUsedMaterialOfStage(bStage);
					matCodeToEleCode[bStage.MatCode] = bsm?.EleCode;
					if (bsm.TextureCodeReplace != null)
					{
						textureCodeReplace[bsm.TextureCodeReplace.From] = bsm.TextureCodeReplace.To;
					}
				}
			}
			selectiveElements = new string[currentBuildStage];
			for (int i = 0; i < currentBuildStage; i++)
			{
				string eleName = buildStages[i].ElementName;
				if (matCodeToEleCode.TryGetValue(buildStages[i].MatCode, out var replace))
				{
					eleName = eleName.Replace("{eleCode}", replace);
				}
				selectiveElements[i] = eleName;
			}
		}
		else
		{
			for (int k = 0; k < currentBuildStage; k++)
			{
				BuildStage bStage2 = buildStages[k];
				BuildStageMaterial bsm2 = currentlyUsedMaterialOfStage(bStage2);
				if (bsm2 != null && bsm2.BurnTimeHours.HasValue)
				{
					BurnTimeHours = bsm2.BurnTimeHours.Value;
				}
			}
		}
		colBoxes[0].X1 = 0f;
		colBoxes[0].X2 = 1f;
		colBoxes[0].Z1 = 0f;
		colBoxes[0].Z2 = 1f;
		colBoxes[0].Y2 = Math.Max(colBoxes[0].Y2, buildStages[Math.Min(buildStages.Length - 1, currentBuildStage)].MinHitboxY2 / 16f);
		selBoxes[0] = colBoxes[0];
	}

	private BuildStageMaterial currentlyUsedMaterialOfStage(BuildStage buildStage)
	{
		BuildStageMaterial[] bsms = buildStage.Materials;
		foreach (BuildStageMaterial bsm in bsms)
		{
			foreach (ItemSlot slot in inventory)
			{
				if (!slot.Empty && slot.Itemstack.Equals(Api.World, bsm.ItemStack, GlobalConstants.IgnoredStackAttributes))
				{
					return bsm;
				}
			}
		}
		return null;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		BurningUntilTotalHours = tree.GetDouble("burnUntil");
		int prevStage = currentBuildStage;
		bool prevLit = Lit;
		currentBuildStage = tree.GetInt("currentBuildStage");
		Lit = tree.GetBool("lit");
		if (Api != null)
		{
			DetermineBuildStages();
			if (Api.Side == EnumAppSide.Client)
			{
				if (prevStage != currentBuildStage)
				{
					mesh = null;
				}
				if (!prevLit && Lit)
				{
					TryIgnite(null);
				}
				if (prevLit && !Lit)
				{
					GetBehavior<BEBehaviorBurning>().KillFire(consumeFuel: false);
				}
			}
		}
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("currentBuildStage", currentBuildStage);
		tree.SetBool("lit", Lit);
		tree.SetDouble("burnUntil", BurningUntilTotalHours);
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		DetermineBuildStages();
		if (mesh == null)
		{
			nowTesselatingKiln = true;
			blockTexPos = tesselator.GetTextureSource(base.Block);
			tesselator.TesselateShape("pitkiln", shape, out mesh, this, null, 0, 0, 0, null, selectiveElements);
			nowTesselatingKiln = false;
			mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 1.005f, 1.005f, 1.005f);
			mesh.Translate(0f, (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 10) / 500f, 0f);
		}
		meshdata.AddMeshData(mesh);
		base.OnTesselation(meshdata, tesselator);
		return true;
	}

	public new bool CanIgnite()
	{
		if (IsComplete && IsValidPitKiln())
		{
			return !GetBehavior<BEBehaviorBurning>().IsBurning;
		}
		return false;
	}

	public void TryIgnite(IPlayer byPlayer)
	{
		BurningUntilTotalHours = Api.World.Calendar.TotalHours + (double)BurnTimeHours;
		BEBehaviorBurning behavior = GetBehavior<BEBehaviorBurning>();
		Lit = true;
		behavior.OnFirePlaced(Pos.UpCopy(), Pos.Copy(), byPlayer?.PlayerUID);
		Api.World.BlockAccessor.ExchangeBlock(base.Block.Id, Pos);
		MarkDirty(redrawOnClient: true);
	}

	public override string GetBlockName()
	{
		return Lang.Get("Pit kiln");
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (!inventory.Empty)
		{
			string[] contentSummary = getContentSummary();
			foreach (string line in contentSummary)
			{
				dsc.AppendLine(line);
			}
			if (Lit)
			{
				dsc.AppendLine(Lang.Get("Lit"));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Unlit"));
			}
		}
	}

	public override string[] getContentSummary()
	{
		OrderedDictionary<string, int> dict = new OrderedDictionary<string, int>();
		for (int i = 0; i < 4; i++)
		{
			ItemSlot slot = inventory[i];
			if (!slot.Empty)
			{
				string stackName = slot.Itemstack.GetName();
				if (!dict.TryGetValue(stackName, out var cnt))
				{
					cnt = 0;
				}
				dict[stackName] = cnt + slot.StackSize;
			}
		}
		return dict.Select((KeyValuePair<string, int> elem) => Lang.Get("{0}x {1}", elem.Value, elem.Key)).ToArray();
	}

	public void KillFire(bool consumefuel)
	{
		if (!consumefuel)
		{
			Lit = false;
			Api.World.BlockAccessor.RemoveBlockLight((base.Block as BlockPitkiln).litKilnLightHsv, Pos);
			MarkDirty(redrawOnClient: true);
		}
		else if (Api.Side != EnumAppSide.Client)
		{
			Block blockgs = Api.World.GetBlock(new AssetLocation("groundstorage"));
			Api.World.BlockAccessor.SetBlock(blockgs.Id, Pos);
			Api.World.BlockAccessor.RemoveBlockLight((base.Block as BlockPitkiln).litKilnLightHsv, Pos);
			BlockEntityGroundStorage begs = Api.World.BlockAccessor.GetBlockEntity(Pos) as BlockEntityGroundStorage;
			GroundStorageProperties storeprops = (inventory.FirstNonEmptySlot?.Itemstack)?.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
			begs.ForceStorageProps(storeprops ?? base.StorageProps);
			for (int i = 0; i < 4; i++)
			{
				begs.Inventory[i] = inventory[i];
			}
			MarkDirty(redrawOnClient: true);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
	}
}
