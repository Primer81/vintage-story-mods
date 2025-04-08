using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFirepit : BlockEntityOpenableContainer, IHeatSource, IFirePit, ITemperatureSensitive
{
	internal InventorySmelting inventory;

	public float prevFurnaceTemperature = 20f;

	public float furnaceTemperature = 20f;

	public int maxTemperature;

	public float inputStackCookingTime;

	public float fuelBurnTime;

	public float maxFuelBurnTime;

	public float smokeLevel;

	public bool canIgniteFuel;

	public float cachedFuel;

	public double extinguishedTotalHours;

	private GuiDialogBlockEntityFirepit clientDialog;

	private bool clientSidePrevBurning;

	private FirepitContentsRenderer renderer;

	private bool shouldRedraw;

	public float emptyFirepitBurnTimeMulBonus = 4f;

	public bool IsHot => IsBurning;

	public virtual bool BurnsAllFuell => true;

	public virtual float HeatModifier => 1f;

	public virtual float BurnDurationModifier => 1f;

	public override string InventoryClassName => "stove";

	public virtual string DialogTitle => Lang.Get("Firepit");

	public override InventoryBase Inventory => inventory;

	public bool IsSmoldering => canIgniteFuel;

	public bool IsBurning => fuelBurnTime > 0f;

	public float InputStackTemp
	{
		get
		{
			return GetTemp(inputStack);
		}
		set
		{
			SetTemp(inputStack, value);
		}
	}

	public float OutputStackTemp
	{
		get
		{
			return GetTemp(outputStack);
		}
		set
		{
			SetTemp(outputStack, value);
		}
	}

	public ItemSlot fuelSlot => inventory[0];

	public ItemSlot inputSlot => inventory[1];

	public ItemSlot outputSlot => inventory[2];

	public ItemSlot[] otherCookingSlots => inventory.CookingSlots;

	public ItemStack fuelStack
	{
		get
		{
			return inventory[0].Itemstack;
		}
		set
		{
			inventory[0].Itemstack = value;
			inventory[0].MarkDirty();
		}
	}

	public ItemStack inputStack
	{
		get
		{
			return inventory[1].Itemstack;
		}
		set
		{
			inventory[1].Itemstack = value;
			inventory[1].MarkDirty();
		}
	}

	public ItemStack outputStack
	{
		get
		{
			return inventory[2].Itemstack;
		}
		set
		{
			inventory[2].Itemstack = value;
			inventory[2].MarkDirty();
		}
	}

	public CombustibleProperties fuelCombustibleOpts => getCombustibleOpts(0);

	public EnumFirepitModel CurrentModel { get; private set; }

	public virtual int enviromentTemperature()
	{
		return 20;
	}

	public virtual float maxCookingTime()
	{
		if (inputSlot.Itemstack != null)
		{
			return inputSlot.Itemstack.Collectible.GetMeltingDuration(Api.World, inventory, inputSlot);
		}
		return 30f;
	}

	public BlockEntityFirepit()
	{
		inventory = new InventorySmelting(null, null);
		inventory.SlotModified += OnSlotModifid;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.pos = Pos;
		inventory.LateInitialize("smelting-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
		RegisterGameTickListener(OnBurnTick, 100);
		RegisterGameTickListener(On500msTick, 500);
		if (api is ICoreClientAPI)
		{
			renderer = new FirepitContentsRenderer(api as ICoreClientAPI, Pos);
			(api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "firepit");
			UpdateRenderer();
		}
	}

	private void OnSlotModifid(int slotid)
	{
		base.Block = Api.World.BlockAccessor.GetBlock(Pos);
		UpdateRenderer();
		MarkDirty(Api.Side == EnumAppSide.Server);
		shouldRedraw = true;
		if (Api is ICoreClientAPI && clientDialog != null)
		{
			SetDialogValues(clientDialog.Attributes);
		}
		Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
	}

	private void On500msTick(float dt)
	{
		if (Api is ICoreServerAPI && (IsBurning || prevFurnaceTemperature != furnaceTemperature))
		{
			MarkDirty();
		}
		prevFurnaceTemperature = furnaceTemperature;
	}

	private void OnBurnTick(float dt)
	{
		if (base.Block.Code.Path.Contains("construct"))
		{
			return;
		}
		if (Api is ICoreClientAPI)
		{
			renderer?.contentStackRenderer?.OnUpdate(InputStackTemp);
			return;
		}
		if (fuelBurnTime > 0f)
		{
			bool lowFuelConsumption = Math.Abs(furnaceTemperature - (float)maxTemperature) < 50f && inputSlot.Empty;
			fuelBurnTime -= dt / (lowFuelConsumption ? emptyFirepitBurnTimeMulBonus : 1f);
			if (fuelBurnTime <= 0f)
			{
				fuelBurnTime = 0f;
				maxFuelBurnTime = 0f;
				if (!canSmelt())
				{
					setBlockState("extinct");
					extinguishedTotalHours = Api.World.Calendar.TotalHours;
				}
			}
		}
		if (!IsBurning && base.Block.Variant["burnstate"] == "extinct" && Api.World.Calendar.TotalHours - extinguishedTotalHours > 2.0)
		{
			canIgniteFuel = false;
			setBlockState("cold");
		}
		if (IsBurning)
		{
			furnaceTemperature = changeTemperature(furnaceTemperature, maxTemperature, dt);
		}
		if (canHeatInput())
		{
			heatInput(dt);
		}
		else
		{
			inputStackCookingTime = 0f;
		}
		if (canHeatOutput())
		{
			heatOutput(dt);
		}
		if (canSmeltInput() && inputStackCookingTime > maxCookingTime())
		{
			smeltItems();
		}
		if (!IsBurning && canIgniteFuel && canSmelt())
		{
			igniteFuel();
		}
		if (!IsBurning)
		{
			furnaceTemperature = changeTemperature(furnaceTemperature, enviromentTemperature(), dt);
		}
	}

	public EnumIgniteState GetIgnitableState(float secondsIgniting)
	{
		if (fuelSlot.Empty)
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (IsBurning)
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 3f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public float changeTemperature(float fromTemp, float toTemp, float dt)
	{
		float diff = Math.Abs(fromTemp - toTemp);
		dt += dt * (diff / 28f);
		if (diff < dt)
		{
			return toTemp;
		}
		if (fromTemp > toTemp)
		{
			dt = 0f - dt;
		}
		if (Math.Abs(fromTemp - toTemp) < 1f)
		{
			return toTemp;
		}
		return fromTemp + dt;
	}

	private bool canSmelt()
	{
		CombustibleProperties fuelCopts = fuelCombustibleOpts;
		if (fuelCopts == null)
		{
			return false;
		}
		bool smeltableInput = canHeatInput();
		if (BurnsAllFuell || smeltableInput)
		{
			return (float)fuelCopts.BurnTemperature * HeatModifier > 0f;
		}
		return false;
	}

	public void heatInput(float dt)
	{
		float oldTemp = InputStackTemp;
		float nowTemp = oldTemp;
		float meltingPoint = inputSlot.Itemstack.Collectible.GetMeltingPoint(Api.World, inventory, inputSlot);
		if (oldTemp < furnaceTemperature)
		{
			float f = (1f + GameMath.Clamp((furnaceTemperature - oldTemp) / 30f, 0f, 1.6f)) * dt;
			if (nowTemp >= meltingPoint)
			{
				f /= 11f;
			}
			float newTemp = changeTemperature(oldTemp, furnaceTemperature, f);
			int maxTemp = Math.Max((inputStack.Collectible.CombustibleProps != null) ? inputStack.Collectible.CombustibleProps.MaxTemperature : 0, (inputStack.ItemAttributes?["maxTemperature"] != null) ? inputStack.ItemAttributes["maxTemperature"].AsInt() : 0);
			if (maxTemp > 0)
			{
				newTemp = Math.Min(maxTemp, newTemp);
			}
			if (oldTemp != newTemp)
			{
				InputStackTemp = newTemp;
				nowTemp = newTemp;
			}
		}
		if (nowTemp >= meltingPoint)
		{
			float diff = nowTemp / meltingPoint;
			inputStackCookingTime += (float)GameMath.Clamp((int)diff, 1, 30) * dt;
		}
		else if (inputStackCookingTime > 0f)
		{
			inputStackCookingTime -= 1f;
		}
	}

	public void heatOutput(float dt)
	{
		float oldTemp = OutputStackTemp;
		if (oldTemp < furnaceTemperature)
		{
			float newTemp = changeTemperature(oldTemp, furnaceTemperature, 2f * dt);
			int maxTemp = Math.Max((outputStack.Collectible.CombustibleProps != null) ? outputStack.Collectible.CombustibleProps.MaxTemperature : 0, (outputStack.ItemAttributes?["maxTemperature"] != null) ? outputStack.ItemAttributes["maxTemperature"].AsInt() : 0);
			if (maxTemp > 0)
			{
				newTemp = Math.Min(maxTemp, newTemp);
			}
			if (oldTemp != newTemp)
			{
				OutputStackTemp = newTemp;
			}
		}
	}

	public void CoolNow(float amountRel)
	{
		Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
		fuelBurnTime -= amountRel / 10f;
		if (Api.World.Rand.NextDouble() < (double)(amountRel / 5f) || fuelBurnTime <= 0f)
		{
			setBlockState("cold");
			extinguishedTotalHours = -99.0;
			canIgniteFuel = false;
			fuelBurnTime = 0f;
			maxFuelBurnTime = 0f;
		}
		MarkDirty(redrawOnClient: true);
	}

	private float GetTemp(ItemStack stack)
	{
		if (stack == null)
		{
			return enviromentTemperature();
		}
		if (inventory.CookingSlots.Length != 0)
		{
			bool haveStack = false;
			float lowestTemp = 0f;
			for (int i = 0; i < inventory.CookingSlots.Length; i++)
			{
				ItemStack cookingStack = inventory.CookingSlots[i].Itemstack;
				if (cookingStack != null)
				{
					float stackTemp = cookingStack.Collectible.GetTemperature(Api.World, cookingStack);
					lowestTemp = (haveStack ? Math.Min(lowestTemp, stackTemp) : stackTemp);
					haveStack = true;
				}
			}
			return lowestTemp;
		}
		return stack.Collectible.GetTemperature(Api.World, stack);
	}

	private void SetTemp(ItemStack stack, float value)
	{
		if (stack == null)
		{
			return;
		}
		if (inventory.CookingSlots.Length != 0)
		{
			for (int i = 0; i < inventory.CookingSlots.Length; i++)
			{
				inventory.CookingSlots[i].Itemstack?.Collectible.SetTemperature(Api.World, inventory.CookingSlots[i].Itemstack, value);
			}
		}
		else
		{
			stack.Collectible.SetTemperature(Api.World, stack, value);
		}
	}

	public void igniteFuel()
	{
		igniteWithFuel(fuelStack);
		fuelStack.StackSize--;
		if (fuelStack.StackSize <= 0)
		{
			fuelStack = null;
		}
	}

	public void igniteWithFuel(IItemStack stack)
	{
		CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;
		maxFuelBurnTime = (fuelBurnTime = fuelCopts.BurnDuration * BurnDurationModifier);
		maxTemperature = (int)((float)fuelCopts.BurnTemperature * HeatModifier);
		smokeLevel = fuelCopts.SmokeLevel;
		setBlockState("lit");
		MarkDirty(redrawOnClient: true);
	}

	public void setBlockState(string state)
	{
		AssetLocation loc = base.Block.CodeWithVariant("burnstate", state);
		Block block = Api.World.GetBlock(loc);
		if (block != null)
		{
			Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
			base.Block = block;
		}
	}

	public bool canHeatInput()
	{
		if (!canSmeltInput())
		{
			if (inputStack?.ItemAttributes?["allowHeating"] != null)
			{
				return inputStack.ItemAttributes["allowHeating"].AsBool();
			}
			return false;
		}
		return true;
	}

	public bool canHeatOutput()
	{
		if (outputStack?.ItemAttributes?["allowHeating"] != null)
		{
			return outputStack.ItemAttributes["allowHeating"].AsBool();
		}
		return false;
	}

	public bool canSmeltInput()
	{
		if (inputStack == null)
		{
			return false;
		}
		if (inputStack.Collectible.OnSmeltAttempt(inventory))
		{
			MarkDirty(redrawOnClient: true);
		}
		if (inputStack.Collectible.CanSmelt(Api.World, inventory, inputSlot.Itemstack, outputSlot.Itemstack))
		{
			if (inputStack.Collectible.CombustibleProps != null)
			{
				return !inputStack.Collectible.CombustibleProps.RequiresContainer;
			}
			return true;
		}
		return false;
	}

	public void smeltItems()
	{
		inputStack.Collectible.DoSmelt(Api.World, inventory, inputSlot, outputSlot);
		InputStackTemp = enviromentTemperature();
		inputStackCookingTime = 0f;
		MarkDirty(redrawOnClient: true);
		inputSlot.MarkDirty();
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			toggleInventoryDialogClient(byPlayer, delegate
			{
				SyncedTreeAttribute syncedTreeAttribute = new SyncedTreeAttribute();
				SetDialogValues(syncedTreeAttribute);
				clientDialog = new GuiDialogBlockEntityFirepit(DialogTitle, Inventory, Pos, syncedTreeAttribute, Api as ICoreClientAPI);
				return clientDialog;
			});
		}
		return true;
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(player, packetid, data);
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			(Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
			invDialog?.TryClose();
			invDialog?.Dispose();
			invDialog = null;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		if (Api != null)
		{
			Inventory.AfterBlocksLoaded(Api.World);
		}
		furnaceTemperature = tree.GetFloat("furnaceTemperature");
		maxTemperature = tree.GetInt("maxTemperature");
		inputStackCookingTime = tree.GetFloat("oreCookingTime");
		fuelBurnTime = tree.GetFloat("fuelBurnTime");
		maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
		extinguishedTotalHours = tree.GetDouble("extinguishedTotalHours");
		canIgniteFuel = tree.GetBool("canIgniteFuel", defaultValue: true);
		cachedFuel = tree.GetFloat("cachedFuel");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			UpdateRenderer();
			if (clientDialog != null)
			{
				SetDialogValues(clientDialog.Attributes);
			}
		}
		ICoreAPI api2 = Api;
		if (api2 != null && api2.Side == EnumAppSide.Client && (clientSidePrevBurning != IsBurning || shouldRedraw))
		{
			GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(IsBurning);
			clientSidePrevBurning = IsBurning;
			MarkDirty(redrawOnClient: true);
			shouldRedraw = false;
		}
	}

	private void UpdateRenderer()
	{
		if (renderer == null)
		{
			return;
		}
		ItemStack contentStack = ((inputStack == null) ? outputStack : inputStack);
		if (renderer.ContentStack != null && renderer.contentStackRenderer != null && contentStack?.Collectible is IInFirepitRendererSupplier && renderer.ContentStack.Equals(Api.World, contentStack, GlobalConstants.IgnoredStackAttributes))
		{
			return;
		}
		renderer.contentStackRenderer?.Dispose();
		renderer.contentStackRenderer = null;
		if (contentStack?.Collectible is IInFirepitRendererSupplier)
		{
			IInFirepitRenderer childrenderer = (contentStack?.Collectible as IInFirepitRendererSupplier).GetRendererWhenInFirepit(contentStack, this, contentStack == outputStack);
			if (childrenderer != null)
			{
				renderer.SetChildRenderer(contentStack, childrenderer);
				return;
			}
		}
		InFirePitProps props = GetRenderProps(contentStack);
		if (contentStack?.Collectible != null && !(contentStack?.Collectible is IInFirepitMeshSupplier) && props != null)
		{
			renderer.SetContents(contentStack, props.Transform);
		}
		else
		{
			renderer.SetContents(null, null);
		}
	}

	private void SetDialogValues(ITreeAttribute dialogTree)
	{
		dialogTree.SetFloat("furnaceTemperature", furnaceTemperature);
		dialogTree.SetInt("maxTemperature", maxTemperature);
		dialogTree.SetFloat("oreCookingTime", inputStackCookingTime);
		dialogTree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
		dialogTree.SetFloat("fuelBurnTime", fuelBurnTime);
		if (inputSlot.Itemstack != null)
		{
			float meltingDuration = inputSlot.Itemstack.Collectible.GetMeltingDuration(Api.World, inventory, inputSlot);
			dialogTree.SetFloat("oreTemperature", InputStackTemp);
			dialogTree.SetFloat("maxOreCookingTime", meltingDuration);
		}
		else
		{
			dialogTree.RemoveAttribute("oreTemperature");
		}
		dialogTree.SetString("outputText", inventory.GetOutputText());
		dialogTree.SetInt("haveCookingContainer", inventory.HaveCookingContainer ? 1 : 0);
		dialogTree.SetInt("quantityCookingSlots", inventory.CookingSlots.Length);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ITreeAttribute invtree = new TreeAttribute();
		Inventory.ToTreeAttributes(invtree);
		tree["inventory"] = invtree;
		tree.SetFloat("furnaceTemperature", furnaceTemperature);
		tree.SetInt("maxTemperature", maxTemperature);
		tree.SetFloat("oreCookingTime", inputStackCookingTime);
		tree.SetFloat("fuelBurnTime", fuelBurnTime);
		tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
		tree.SetDouble("extinguishedTotalHours", extinguishedTotalHours);
		tree.SetBool("canIgniteFuel", canIgniteFuel);
		tree.SetFloat("cachedFuel", cachedFuel);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
		renderer = null;
		if (clientDialog != null)
		{
			clientDialog.TryClose();
			clientDialog?.Dispose();
			clientDialog = null;
		}
	}

	public CombustibleProperties getCombustibleOpts(int slotid)
	{
		ItemSlot slot = inventory[slotid];
		if (slot.Itemstack == null)
		{
			return null;
		}
		return slot.Itemstack.Collectible.CombustibleProps;
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (ItemSlot slot2 in Inventory)
		{
			if (slot2.Itemstack != null)
			{
				if (slot2.Itemstack.Class == EnumItemClass.Item)
				{
					itemIdMapping[slot2.Itemstack.Item.Id] = slot2.Itemstack.Item.Code;
				}
				else
				{
					blockIdMapping[slot2.Itemstack.Block.BlockId] = slot2.Itemstack.Block.Code;
				}
				slot2.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot2, blockIdMapping, itemIdMapping);
			}
		}
		ItemSlot[] cookingSlots = inventory.CookingSlots;
		foreach (ItemSlot slot in cookingSlots)
		{
			if (slot.Itemstack != null)
			{
				if (slot.Itemstack.Class == EnumItemClass.Item)
				{
					itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
				}
				else
				{
					blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
				}
				slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
			}
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (base.Block == null || base.Block.Code.Path.Contains("construct"))
		{
			return false;
		}
		ItemStack contentStack = ((inputStack == null) ? outputStack : inputStack);
		MeshData contentmesh = getContentMesh(contentStack, tesselator);
		if (contentmesh != null)
		{
			mesher.AddMeshData(contentmesh);
		}
		string burnState = base.Block.Variant["burnstate"];
		string contentState = CurrentModel.ToString().ToLowerInvariant();
		if (burnState == "cold" && fuelSlot.Empty)
		{
			burnState = "extinct";
		}
		if (burnState == null)
		{
			return true;
		}
		mesher.AddMeshData(getOrCreateMesh(burnState, contentState));
		return true;
	}

	private MeshData getContentMesh(ItemStack contentStack, ITesselatorAPI tesselator)
	{
		CurrentModel = EnumFirepitModel.Normal;
		if (contentStack == null)
		{
			return null;
		}
		if (contentStack.Collectible is IInFirepitMeshSupplier)
		{
			EnumFirepitModel model2 = EnumFirepitModel.Normal;
			MeshData mesh = (contentStack.Collectible as IInFirepitMeshSupplier).GetMeshWhenInFirepit(contentStack, Api.World, Pos, ref model2);
			CurrentModel = model2;
			if (mesh != null)
			{
				return mesh;
			}
		}
		if (contentStack.Collectible is IInFirepitRendererSupplier)
		{
			EnumFirepitModel model = (contentStack.Collectible as IInFirepitRendererSupplier).GetDesiredFirepitModel(contentStack, this, contentStack == outputStack);
			CurrentModel = model;
			return null;
		}
		InFirePitProps renderProps = GetRenderProps(contentStack);
		if (renderProps != null)
		{
			CurrentModel = renderProps.UseFirepitModel;
			if (contentStack.Class != EnumItemClass.Item)
			{
				tesselator.TesselateBlock(contentStack.Block, out var ingredientMesh);
				ingredientMesh.ModelTransform(renderProps.Transform);
				if (!IsBurning && renderProps.UseFirepitModel != EnumFirepitModel.Spit)
				{
					ingredientMesh.Translate(0f, -0.0625f, 0f);
				}
				return ingredientMesh;
			}
			return null;
		}
		if (renderer.RequireSpit)
		{
			CurrentModel = EnumFirepitModel.Spit;
		}
		return null;
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
	}

	private InFirePitProps GetRenderProps(ItemStack contentStack)
	{
		if (contentStack != null && (contentStack.ItemAttributes?.KeyExists("inFirePitProps")).GetValueOrDefault())
		{
			InFirePitProps inFirePitProps = contentStack.ItemAttributes["inFirePitProps"].AsObject<InFirePitProps>();
			inFirePitProps.Transform.EnsureDefaultValues();
			return inFirePitProps;
		}
		return null;
	}

	public MeshData getOrCreateMesh(string burnstate, string contentstate)
	{
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, "firepit-meshes", () => new Dictionary<string, MeshData>());
		string key = burnstate + "-" + contentstate;
		if (!orCreate.TryGetValue(key, out var meshdata))
		{
			Block block = Api.World.BlockAccessor.GetBlock(Pos);
			if (block.BlockId == 0)
			{
				return null;
			}
			_ = new MeshData[17];
			((ICoreClientAPI)Api).Tesselator.TesselateShape(block, Shape.TryGet(Api, "shapes/block/wood/firepit/" + key + ".json"), out meshdata);
		}
		return meshdata;
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		if (!IsBurning)
		{
			if (!IsSmoldering)
			{
				return 0f;
			}
			return 0.25f;
		}
		return 10f;
	}
}
