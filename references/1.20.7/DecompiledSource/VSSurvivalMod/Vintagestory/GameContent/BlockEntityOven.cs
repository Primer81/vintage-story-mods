using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityOven : BlockEntityDisplay, IHeatSource
{
	public static int BakingStageThreshold = 100;

	public const int maxBakingTemperatureAccepted = 260;

	private bool burning;

	private bool clientSidePrevBurning;

	public float prevOvenTemperature = 20f;

	public float ovenTemperature = 20f;

	private float fuelBurnTime;

	private readonly OvenItemData[] bakingData;

	private ItemStack lastRemoved;

	private int rotationDeg;

	private Random prng;

	private int syncCount;

	private ILoadedSound ambientSound;

	internal InventoryOven ovenInv;

	public virtual float maxTemperature => 300f;

	public virtual int bakeableCapacity => 4;

	public virtual int fuelitemCapacity => 6;

	private EnumOvenContentMode OvenContentMode
	{
		get
		{
			ItemSlot slot = ovenInv.FirstNonEmptySlot;
			if (slot == null)
			{
				return EnumOvenContentMode.Firewood;
			}
			BakingProperties bakingProps = BakingProperties.ReadFrom(slot.Itemstack);
			if (bakingProps == null)
			{
				return EnumOvenContentMode.Firewood;
			}
			if (!bakingProps.LargeItem)
			{
				return EnumOvenContentMode.Quadrants;
			}
			return EnumOvenContentMode.SingleCenter;
		}
	}

	public override InventoryBase Inventory => ovenInv;

	public override string InventoryClassName => "oven";

	public ItemSlot FuelSlot => ovenInv[0];

	public bool HasFuel
	{
		get
		{
			ItemStack itemstack = FuelSlot.Itemstack;
			if (itemstack == null)
			{
				return false;
			}
			return (itemstack.Collectible?.Attributes?.IsTrue("isClayOvenFuel")).GetValueOrDefault();
		}
	}

	public bool IsBurning => burning;

	public bool HasBakeables
	{
		get
		{
			for (int i = 0; i < bakeableCapacity; i++)
			{
				if (!ovenInv[i].Empty && (i != 0 || !HasFuel))
				{
					return true;
				}
			}
			return false;
		}
	}

	public override int DisplayedItems
	{
		get
		{
			if (OvenContentMode == EnumOvenContentMode.Quadrants)
			{
				return 4;
			}
			return 1;
		}
	}

	public BlockEntityOven()
	{
		bakingData = new OvenItemData[bakeableCapacity];
		for (int i = 0; i < bakeableCapacity; i++)
		{
			bakingData[i] = new OvenItemData();
		}
		ovenInv = new InventoryOven("oven-0", bakeableCapacity);
	}

	public override void Initialize(ICoreAPI api)
	{
		capi = api as ICoreClientAPI;
		base.Initialize(api);
		ovenInv.LateInitialize(InventoryClassName + "-" + Pos, api);
		RegisterGameTickListener(OnBurnTick, 100);
		prng = new Random(Pos.GetHashCode());
		SetRotation();
	}

	private void SetRotation()
	{
		switch (base.Block.Variant["side"])
		{
		case "south":
			rotationDeg = 270;
			break;
		case "west":
			rotationDeg = 180;
			break;
		case "east":
			rotationDeg = 0;
			break;
		default:
			rotationDeg = 90;
			break;
		}
	}

	public virtual bool OnInteract(IPlayer byPlayer, BlockSelection bs)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (slot.Empty)
		{
			if (TryTake(byPlayer))
			{
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				return true;
			}
			return false;
		}
		CollectibleObject colObj = slot.Itemstack.Collectible;
		JsonObject attributes = colObj.Attributes;
		if (attributes != null && attributes.IsTrue("isClayOvenFuel"))
		{
			if (TryAddFuel(slot))
			{
				AssetLocation sound2 = slot.Itemstack?.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((sound2 != null) ? sound2 : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return true;
			}
			return false;
		}
		if (colObj.Attributes?["bakingProperties"] == null)
		{
			CombustibleProperties combustibleProps = colObj.CombustibleProps;
			if (combustibleProps == null || combustibleProps.SmeltingType != EnumSmeltType.Bake || colObj.CombustibleProps.MeltingPoint >= 260)
			{
				if (TryTake(byPlayer))
				{
					byPlayer.InventoryManager.BroadcastHotbarSlot();
					return true;
				}
				return false;
			}
		}
		if (slot.Itemstack.Equals(Api.World, lastRemoved, GlobalConstants.IgnoredStackAttributes) && !ovenInv[0].Empty)
		{
			if (TryTake(byPlayer))
			{
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				return true;
			}
		}
		else
		{
			AssetLocation stackName = slot.Itemstack?.Collectible.Code;
			if (TryPut(slot))
			{
				AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/buildhigh"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				byPlayer.InventoryManager.BroadcastHotbarSlot();
				Api.World.Logger.Audit("{0} Put 1x{1} into Clay oven at {2}.", byPlayer.PlayerName, stackName, Pos);
				return true;
			}
			if (slot.Itemstack.Block?.GetBehavior<BlockBehaviorCanIgnite>() == null)
			{
				ICoreClientAPI capi = Api as ICoreClientAPI;
				if (capi != null && (slot.Empty || !slot.Itemstack.Attributes.GetBool("bakeable", defaultValue: true)))
				{
					capi.TriggerIngameError(this, "notbakeable", Lang.Get("This item is not bakeable."));
				}
				else if (capi != null && !slot.Empty)
				{
					capi.TriggerIngameError(this, "notbakeable", burning ? Lang.Get("Wait until the fire is out") : Lang.Get("Oven is full"));
				}
				return true;
			}
		}
		return false;
	}

	protected virtual bool TryAddFuel(ItemSlot slot)
	{
		if (IsBurning || HasBakeables)
		{
			return false;
		}
		if (FuelSlot.Empty || FuelSlot.Itemstack.StackSize < fuelitemCapacity)
		{
			int num = slot.TryPutInto(Api.World, FuelSlot);
			if (num > 0)
			{
				updateMesh(0);
				MarkDirty(redrawOnClient: true);
				lastRemoved = null;
			}
			return num > 0;
		}
		return false;
	}

	protected virtual bool TryPut(ItemSlot slot)
	{
		if (IsBurning || HasFuel)
		{
			return false;
		}
		BakingProperties bakingProps = BakingProperties.ReadFrom(slot.Itemstack);
		if (bakingProps == null)
		{
			return false;
		}
		if (!slot.Itemstack.Attributes.GetBool("bakeable", defaultValue: true))
		{
			return false;
		}
		if (bakingProps.LargeItem && !ovenInv.Empty)
		{
			return false;
		}
		for (int index = 0; index < bakeableCapacity; index++)
		{
			if (ovenInv[index].Empty)
			{
				int num = slot.TryPutInto(Api.World, ovenInv[index]);
				if (num > 0)
				{
					bakingData[index] = new OvenItemData(ovenInv[index].Itemstack);
					updateMesh(index);
					MarkDirty(redrawOnClient: true);
					lastRemoved = null;
				}
				return num > 0;
			}
			if (index == 0)
			{
				BakingProperties props = BakingProperties.ReadFrom(ovenInv[0].Itemstack);
				if (props != null && props.LargeItem)
				{
					return false;
				}
			}
		}
		return false;
	}

	protected virtual bool TryTake(IPlayer byPlayer)
	{
		if (IsBurning)
		{
			return false;
		}
		for (int index = bakeableCapacity; index >= 0; index--)
		{
			if (!ovenInv[index].Empty)
			{
				ItemStack stack = ovenInv[index].TakeOut(1);
				lastRemoved = stack?.Clone();
				if (byPlayer.InventoryManager.TryGiveItemstack(stack))
				{
					AssetLocation sound = stack.Block?.Sounds?.Place;
					Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/throw"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				}
				if (stack.StackSize > 0)
				{
					Api.World.SpawnItemEntity(stack, Pos);
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Clay oven at {2}.", byPlayer.PlayerName, stack.Collectible.Code, Pos);
				bakingData[index].CurHeightMul = 1f;
				updateMesh(index);
				MarkDirty(redrawOnClient: true);
				return true;
			}
		}
		return false;
	}

	public virtual ItemStack[] CanAdd(ItemStack[] itemstacks)
	{
		if (IsBurning)
		{
			return null;
		}
		if (!FuelSlot.Empty)
		{
			return null;
		}
		if (ovenTemperature <= (float)(EnvironmentTemperature() + 25))
		{
			return null;
		}
		for (int i = 0; i < bakeableCapacity; i++)
		{
			if (ovenInv[i].Empty)
			{
				return itemstacks;
			}
		}
		return null;
	}

	public virtual ItemStack[] CanAddAsFuel(ItemStack[] itemstacks)
	{
		if (IsBurning)
		{
			return null;
		}
		for (int i = 0; i < bakeableCapacity; i++)
		{
			if (!ovenInv[i].Empty)
			{
				return null;
			}
		}
		if (FuelSlot.StackSize >= fuelitemCapacity)
		{
			return null;
		}
		return itemstacks;
	}

	public bool TryIgnite()
	{
		if (!CanIgnite())
		{
			return false;
		}
		burning = true;
		fuelBurnTime = 45 + FuelSlot.StackSize * 5;
		MarkDirty();
		ambientSound?.Start();
		return true;
	}

	public bool CanIgnite()
	{
		if (!FuelSlot.Empty)
		{
			return !burning;
		}
		return false;
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return Math.Max((ovenTemperature - 20f) / (maxTemperature - 20f) * 8f, 0f);
	}

	protected virtual void OnBurnTick(float dt)
	{
		dt *= 1.25f;
		if (Api is ICoreClientAPI)
		{
			return;
		}
		if (fuelBurnTime > 0f)
		{
			fuelBurnTime -= dt;
			if (fuelBurnTime <= 0f)
			{
				fuelBurnTime = 0f;
				burning = false;
				CombustibleProperties props = FuelSlot.Itemstack?.Collectible.CombustibleProps;
				if (props?.SmeltedStack == null)
				{
					FuelSlot.Itemstack = null;
					for (int i = 0; i < bakeableCapacity; i++)
					{
						bakingData[i].CurHeightMul = 1f;
					}
				}
				else
				{
					int count = FuelSlot.StackSize;
					FuelSlot.Itemstack = props.SmeltedStack.ResolvedItemstack.Clone();
					FuelSlot.Itemstack.StackSize = count * props.SmeltedRatio;
				}
				MarkDirty(redrawOnClient: true);
			}
		}
		if (IsBurning)
		{
			ovenTemperature = ChangeTemperature(ovenTemperature, maxTemperature, dt * (float)FuelSlot.StackSize / (float)fuelitemCapacity);
		}
		else
		{
			int environmentTemperature = EnvironmentTemperature();
			if (ovenTemperature > (float)environmentTemperature)
			{
				HeatInput(dt);
				ovenTemperature = ChangeTemperature(ovenTemperature, environmentTemperature, dt / 24f);
			}
		}
		if (++syncCount % 5 == 0 && (IsBurning || prevOvenTemperature != ovenTemperature || !Inventory[0].Empty || !Inventory[1].Empty || !Inventory[2].Empty || !Inventory[3].Empty))
		{
			MarkDirty();
			prevOvenTemperature = ovenTemperature;
		}
	}

	protected virtual void HeatInput(float dt)
	{
		for (int slotIndex = 0; slotIndex < bakeableCapacity; slotIndex++)
		{
			ItemStack stack = ovenInv[slotIndex].Itemstack;
			if (stack != null && HeatStack(stack, dt, slotIndex) >= 100f)
			{
				IncrementallyBake(dt * 1.2f, slotIndex);
			}
		}
	}

	protected virtual float HeatStack(ItemStack stack, float dt, int i)
	{
		float oldTemp = bakingData[i].temp;
		float nowTemp = oldTemp;
		if (oldTemp < ovenTemperature)
		{
			float f = (1f + GameMath.Clamp((ovenTemperature - oldTemp) / 28f, 0f, 1.6f)) * dt;
			nowTemp = ChangeTemperature(oldTemp, ovenTemperature, f);
			int maxTemp = Math.Max(stack.Collectible.CombustibleProps?.MaxTemperature ?? 0, stack.ItemAttributes?["maxTemperature"].AsInt() ?? 0);
			if (maxTemp > 0)
			{
				nowTemp = Math.Min(maxTemp, nowTemp);
			}
		}
		else if (oldTemp > ovenTemperature)
		{
			float f2 = (1f + GameMath.Clamp((oldTemp - ovenTemperature) / 28f, 0f, 1.6f)) * dt;
			nowTemp = ChangeTemperature(oldTemp, ovenTemperature, f2);
		}
		if (oldTemp != nowTemp)
		{
			bakingData[i].temp = nowTemp;
		}
		return nowTemp;
	}

	protected virtual void IncrementallyBake(float dt, int slotIndex)
	{
		ItemSlot slot = Inventory[slotIndex];
		OvenItemData bakeData = bakingData[slotIndex];
		float targetTemp = bakeData.BrowningPoint;
		if (targetTemp == 0f)
		{
			targetTemp = 160f;
		}
		float num = bakeData.temp / targetTemp;
		float timeFactor = bakeData.TimeToBake;
		if (timeFactor == 0f)
		{
			timeFactor = 1f;
		}
		float delta = (float)GameMath.Clamp((int)num, 1, 30) * dt / timeFactor;
		float currentLevel = bakeData.BakedLevel;
		if (bakeData.temp > targetTemp)
		{
			currentLevel = (bakeData.BakedLevel += delta);
		}
		BakingProperties bakeProps = BakingProperties.ReadFrom(slot.Itemstack);
		float levelFrom = bakeProps?.LevelFrom ?? 0f;
		float levelTo = bakeProps?.LevelTo ?? 1f;
		float startHeightMul = bakeProps?.StartScaleY ?? 1f;
		float endHeightMul = bakeProps?.EndScaleY ?? 1f;
		float progress = GameMath.Clamp((currentLevel - levelFrom) / (levelTo - levelFrom), 0f, 1f);
		float nowHeightMulStaged = (float)(int)(GameMath.Mix(startHeightMul, endHeightMul, progress) * (float)BakingStageThreshold) / (float)BakingStageThreshold;
		bool reDraw = nowHeightMulStaged != bakeData.CurHeightMul;
		bakeData.CurHeightMul = nowHeightMulStaged;
		if (currentLevel > levelTo)
		{
			float nowTemp = bakeData.temp;
			string resultCode = bakeProps?.ResultCode;
			if (resultCode != null)
			{
				ItemStack resultStack = null;
				if (slot.Itemstack.Class == EnumItemClass.Block)
				{
					Block block = Api.World.GetBlock(new AssetLocation(resultCode));
					if (block != null)
					{
						resultStack = new ItemStack(block);
					}
				}
				else
				{
					Item item = Api.World.GetItem(new AssetLocation(resultCode));
					if (item != null)
					{
						resultStack = new ItemStack(item);
					}
				}
				if (resultStack != null)
				{
					if (ovenInv[slotIndex].Itemstack.Collectible is IBakeableCallback collObjCb)
					{
						collObjCb.OnBaked(ovenInv[slotIndex].Itemstack, resultStack);
					}
					ovenInv[slotIndex].Itemstack = resultStack;
					bakingData[slotIndex] = new OvenItemData(resultStack);
					bakingData[slotIndex].temp = nowTemp;
					reDraw = true;
				}
			}
			else
			{
				ItemSlot result = new DummySlot(null);
				if (slot.Itemstack.Collectible.CanSmelt(Api.World, ovenInv, slot.Itemstack, null))
				{
					slot.Itemstack.Collectible.DoSmelt(Api.World, ovenInv, ovenInv[slotIndex], result);
					if (!result.Empty)
					{
						ovenInv[slotIndex].Itemstack = result.Itemstack;
						bakingData[slotIndex] = new OvenItemData(result.Itemstack);
						bakingData[slotIndex].temp = nowTemp;
						reDraw = true;
					}
				}
			}
		}
		if (reDraw)
		{
			updateMesh(slotIndex);
			MarkDirty(redrawOnClient: true);
		}
	}

	protected virtual int EnvironmentTemperature()
	{
		return (int)Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
	}

	public virtual float ChangeTemperature(float fromTemp, float toTemp, float dt)
	{
		float diff = Math.Abs(fromTemp - toTemp);
		diff *= GameMath.Sqrt(diff);
		dt += dt * (diff / 480f);
		if (diff < dt)
		{
			return toTemp;
		}
		if (fromTemp > toTemp)
		{
			dt = (0f - dt) / 2f;
		}
		if (Math.Abs(fromTemp - toTemp) < 1f)
		{
			return toTemp;
		}
		return fromTemp + dt;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		ovenInv.FromTreeAttributes(tree);
		burning = tree.GetInt("burn") > 0;
		rotationDeg = tree.GetInt("rota");
		ovenTemperature = tree.GetFloat("temp");
		fuelBurnTime = tree.GetFloat("tfuel");
		for (int i = 0; i < bakeableCapacity; i++)
		{
			bakingData[i] = OvenItemData.ReadFromTree(tree, i);
		}
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			updateMeshes();
			if (clientSidePrevBurning != IsBurning)
			{
				ToggleAmbientSounds(IsBurning);
				clientSidePrevBurning = IsBurning;
				MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ovenInv.ToTreeAttributes(tree);
		tree.SetInt("burn", burning ? 1 : 0);
		tree.SetInt("rota", rotationDeg);
		tree.SetFloat("temp", ovenTemperature);
		tree.SetFloat("tfuel", fuelBurnTime);
		for (int i = 0; i < bakeableCapacity; i++)
		{
			bakingData[i].WriteToTree(tree, i);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (ovenTemperature <= 25f)
		{
			sb.AppendLine(Lang.Get("Temperature: {0}", Lang.Get("Cold")));
			if (!IsBurning)
			{
				sb.AppendLine(Lang.Get("clayoven-preheat-warning"));
			}
		}
		else
		{
			sb.AppendLine(Lang.Get("Temperature: {0}°C", (int)ovenTemperature));
			if (ovenTemperature < 100f && !IsBurning)
			{
				sb.AppendLine(Lang.Get("Reheat to continue baking"));
			}
		}
		sb.AppendLine();
		for (int index = 0; index < bakeableCapacity; index++)
		{
			if (!ovenInv[index].Empty)
			{
				ItemStack stack = ovenInv[index].Itemstack;
				sb.Append(stack.GetName());
				sb.AppendLine(" (" + Lang.Get("{0}°C", (int)bakingData[index].temp) + ")");
			}
		}
	}

	public virtual void ToggleAmbientSounds(bool on)
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return;
		}
		if (on)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/environment/fireplace.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.1f, 0.5f),
					DisposeOnFinish = false,
					Volume = 0.66f
				});
				ambientSound.Start();
			}
		}
		else
		{
			ambientSound.Stop();
			ambientSound.Dispose();
			ambientSound = null;
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] tfMatrices = new float[DisplayedItems][];
		Vec3f[] offs = new Vec3f[DisplayedItems];
		switch (OvenContentMode)
		{
		case EnumOvenContentMode.Firewood:
			offs[0] = new Vec3f();
			break;
		case EnumOvenContentMode.Quadrants:
			offs[0] = new Vec3f(-0.125f, 0.0625f, -5f / 32f);
			offs[1] = new Vec3f(-0.125f, 0.0625f, 5f / 32f);
			offs[2] = new Vec3f(0.1875f, 0.0625f, -5f / 32f);
			offs[3] = new Vec3f(0.1875f, 0.0625f, 5f / 32f);
			break;
		case EnumOvenContentMode.SingleCenter:
			offs[0] = new Vec3f(0f, 0.0625f, 0f);
			break;
		}
		for (int i = 0; i < tfMatrices.Length; i++)
		{
			Vec3f off = offs[i];
			float scaleY = ((OvenContentMode == EnumOvenContentMode.Firewood) ? 0.9f : bakingData[i].CurHeightMul);
			tfMatrices[i] = new Matrixf().Translate(off.X, off.Y, off.Z).Translate(0.5f, 0f, 0.5f).RotateYDeg(rotationDeg + ((OvenContentMode == EnumOvenContentMode.Firewood) ? 270 : 0))
				.Scale(0.9f, scaleY, 0.9f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	protected override string getMeshCacheKey(ItemStack stack)
	{
		string scaleY = "";
		for (int i = 0; i < bakingData.Length; i++)
		{
			if (Inventory[i].Itemstack == stack)
			{
				scaleY = "-" + bakingData[i].CurHeightMul;
				break;
			}
		}
		return ((OvenContentMode == EnumOvenContentMode.Firewood) ? (stack.StackSize + "x") : "") + base.getMeshCacheKey(stack) + scaleY;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		tfMatrices = genTransformationMatrices();
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	protected override MeshData getOrCreateMesh(ItemStack stack, int index)
	{
		if (OvenContentMode == EnumOvenContentMode.Firewood)
		{
			MeshData mesh = getMesh(stack);
			if (mesh != null)
			{
				return mesh;
			}
			AssetLocation loc = AssetLocation.Create(base.Block.Attributes["ovenFuelShape"].AsString(), base.Block.Code.Domain).WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			nowTesselatingShape = Shape.TryGet(capi, loc);
			nowTesselatingObj = stack.Collectible;
			if (nowTesselatingShape == null)
			{
				capi.Logger.Error(string.Concat("Stacking model shape for collectible ", stack.Collectible.Code, " not found. Block will be invisible!"));
				return null;
			}
			capi.Tesselator.TesselateShape("ovenFuelShape", nowTesselatingShape, out mesh, this, null, 0, 0, 0, stack.StackSize);
			string key = getMeshCacheKey(stack);
			base.MeshCache[key] = mesh;
			return mesh;
		}
		return base.getOrCreateMesh(stack, index);
	}

	public virtual void RenderParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking, AdvancedParticleProperties[] particles)
	{
		if (fuelBurnTime < 3f)
		{
			return;
		}
		int logsCount = FuelSlot.StackSize;
		bool fireFull = logsCount > 3;
		double[] x = new double[4];
		float[] z = new float[4];
		for (int i = 0; i < particles.Length; i++)
		{
			if ((i >= 12 && (float)prng.Next(0, 90) > fuelBurnTime) || (i >= 8 && i < 12 && (float)prng.Next(0, 12) > fuelBurnTime) || (i >= 4 && i < 4 && prng.Next(0, 6) == 0))
			{
				continue;
			}
			if (i >= 4 && logsCount < 3)
			{
				bool rotated = rotationDeg >= 180;
				if ((!rotated && z[i % 2] > (float)logsCount * 0.2f + 0.14f) || (rotated && z[i % 2] < (float)(3 - logsCount) * 0.2f + 0.14f))
				{
					continue;
				}
			}
			AdvancedParticleProperties bps = particles[i];
			bps.WindAffectednesAtPos = 0f;
			bps.basePos.X = pos.X;
			bps.basePos.Y = (float)pos.Y + (fireFull ? (3f / 32f) : (1f / 32f));
			bps.basePos.Z = pos.Z;
			if (i >= 4)
			{
				bool rotate = rotationDeg % 180 > 0;
				if (fireFull)
				{
					rotate = !rotate;
				}
				bps.basePos.Z += (rotate ? x[i % 2] : ((double)z[i % 2]));
				bps.basePos.X += (rotate ? ((double)z[i % 2]) : x[i % 2]);
				bps.basePos.Y += (float)(fireFull ? 4 : 3) / 32f;
				switch (rotationDeg)
				{
				case 0:
					bps.basePos.X -= (fireFull ? 0.08f : 0.12f);
					break;
				case 180:
					bps.basePos.X += (fireFull ? 0.08f : 0.12f);
					break;
				case 90:
					bps.basePos.Z += (fireFull ? 0.08f : 0.12f);
					break;
				default:
					bps.basePos.Z -= (fireFull ? 0.08f : 0.12f);
					break;
				}
			}
			else
			{
				x[i] = prng.NextDouble() * 0.4000000059604645 + 0.33000001311302185;
				z[i] = 0.26f + (float)prng.Next(0, 3) * 0.2f + (float)prng.NextDouble() * 0.08f;
			}
			manager.Spawn(bps);
		}
	}
}
