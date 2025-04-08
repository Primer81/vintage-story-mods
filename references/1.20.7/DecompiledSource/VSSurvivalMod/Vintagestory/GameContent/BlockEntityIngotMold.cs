using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityIngotMold : BlockEntity, ILiquidMetalSink, ITemperatureSensitive, ITexPositionSource, IRotatable
{
	protected long lastPouringMarkdirtyMs;

	protected IngotMoldRenderer ingotRenderer;

	public MeshData MoldMesh;

	public ItemStack ContentsLeft;

	public ItemStack ContentsRight;

	public int FillLevelLeft;

	public int FillLevelRight;

	public int QuantityMolds = 1;

	public bool IsRightSideSelected;

	public bool ShatteredLeft;

	public bool ShatteredRight;

	public int RequiredUnits = 100;

	private ICoreClientAPI capi;

	public float MeshAngle;

	private ITexPositionSource tmpTextureSource;

	private AssetLocation metalTexLoc;

	private MeshData shatteredMeshLeft;

	private MeshData shatteredMeshRight;

	public static Vec3f left = new Vec3f(-0.25f, 0f, 0f);

	public static Vec3f right = new Vec3f(0.1875f, 0f, 0f);

	public float TemperatureLeft => ContentsLeft?.Collectible.GetTemperature(Api.World, ContentsLeft) ?? 0f;

	public float TemperatureRight => ContentsRight?.Collectible.GetTemperature(Api.World, ContentsRight) ?? 0f;

	public bool IsHardenedLeft => TemperatureLeft < 0.3f * ContentsLeft?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsLeft));

	public bool IsHardenedRight => TemperatureRight < 0.3f * ContentsRight?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsRight));

	public bool IsLiquidLeft => TemperatureLeft > 0.8f * ContentsLeft?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsLeft));

	public bool IsLiquidRight => TemperatureRight > 0.8f * ContentsRight?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(ContentsRight));

	public bool IsFullLeft => FillLevelLeft >= RequiredUnits;

	public bool IsFullRight => FillLevelRight >= RequiredUnits;

	public bool IsHot
	{
		get
		{
			if (!(TemperatureLeft >= 200f))
			{
				return TemperatureRight >= 200f;
			}
			return true;
		}
	}

	public bool CanReceiveAny
	{
		get
		{
			if (base.Block.Code.Path.Contains("burned"))
			{
				return !BothShattered;
			}
			return false;
		}
	}

	private bool BothShattered
	{
		get
		{
			if (ShatteredLeft)
			{
				return ShatteredRight;
			}
			return false;
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "metal")
			{
				return capi.BlockTextureAtlas[metalTexLoc];
			}
			return tmpTextureSource[textureCode];
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (ContentsLeft != null)
		{
			ContentsLeft.ResolveBlockOrItem(api.World);
		}
		if (ContentsRight != null)
		{
			ContentsRight.ResolveBlockOrItem(api.World);
		}
		capi = api as ICoreClientAPI;
		if (capi != null && !BothShattered)
		{
			capi.Event.RegisterRenderer(ingotRenderer = new IngotMoldRenderer(this, capi), EnumRenderStage.Opaque, "ingotmold");
			UpdateIngotRenderer();
			if (MoldMesh == null)
			{
				GenMeshes();
			}
		}
		if (!BothShattered)
		{
			RegisterGameTickListener(OnGameTick, 50);
		}
	}

	private void OnGameTick(float dt)
	{
		if (ingotRenderer != null)
		{
			ingotRenderer.QuantityMolds = QuantityMolds;
			ingotRenderer.LevelLeft = ((!ShatteredLeft) ? FillLevelLeft : 0);
			ingotRenderer.LevelRight = ((!ShatteredRight) ? FillLevelRight : 0);
		}
		if (ContentsLeft != null && ingotRenderer != null)
		{
			ingotRenderer.stack = ContentsLeft;
			ingotRenderer.TemperatureLeft = Math.Min(1300f, ContentsLeft.Collectible.GetTemperature(Api.World, ContentsLeft));
		}
		if (ContentsRight != null && ingotRenderer != null)
		{
			ingotRenderer.stack = ContentsRight;
			ingotRenderer.TemperatureRight = Math.Min(1300f, ContentsRight.Collectible.GetTemperature(Api.World, ContentsRight));
		}
	}

	public bool CanReceive(ItemStack metal)
	{
		if (ContentsLeft != null && ContentsRight != null && (!ContentsLeft.Collectible.Equals(ContentsLeft, metal, GlobalConstants.IgnoredStackAttributes) || FillLevelLeft >= RequiredUnits))
		{
			if (ContentsRight.Collectible.Equals(ContentsRight, metal, GlobalConstants.IgnoredStackAttributes))
			{
				return FillLevelRight < RequiredUnits;
			}
			return false;
		}
		return true;
	}

	public void BeginFill(Vec3d hitPosition)
	{
		SetSelectedSide(hitPosition);
	}

	private void SetSelectedSide(Vec3d hitPosition)
	{
		switch (BlockFacing.HorizontalFromAngle(MeshAngle).Index)
		{
		case 0:
			IsRightSideSelected = hitPosition.Z < 0.5;
			break;
		case 1:
			IsRightSideSelected = hitPosition.X >= 0.5;
			break;
		case 2:
			IsRightSideSelected = hitPosition.Z >= 0.5;
			break;
		case 3:
			IsRightSideSelected = hitPosition.X < 0.5;
			break;
		}
	}

	public bool OnPlayerInteract(IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
	{
		if (BothShattered)
		{
			return false;
		}
		bool moldInHands = HasMoldInHands(byPlayer);
		bool sneaking = byPlayer.Entity.Controls.ShiftKey;
		if (!sneaking)
		{
			if (byPlayer.Entity.Controls.HandUse != 0)
			{
				return false;
			}
			bool handled = TryTakeIngot(byPlayer, hitPosition);
			if (!handled)
			{
				handled = TryTakeMold(byPlayer, hitPosition);
			}
			return handled;
		}
		if (sneaking && moldInHands)
		{
			return TryPutMold(byPlayer);
		}
		return false;
	}

	public ItemStack GetStateAwareContentsLeft()
	{
		if (ContentsLeft != null && FillLevelLeft >= RequiredUnits)
		{
			if (ShatteredLeft)
			{
				return GetShatteredStack(ContentsLeft, FillLevelLeft);
			}
			if (TemperatureLeft < 300f)
			{
				ItemStack itemStack = ContentsLeft.Clone();
				ITreeAttribute obj = itemStack.Attributes["temperature"] as ITreeAttribute;
				if (obj != null)
				{
					obj.RemoveAttribute("cooldownSpeed");
					return itemStack;
				}
				return itemStack;
			}
		}
		return null;
	}

	public ItemStack GetStateAwareContentsRight()
	{
		if (ContentsRight != null && FillLevelRight >= RequiredUnits)
		{
			if (ShatteredRight)
			{
				return GetShatteredStack(ContentsRight, FillLevelRight);
			}
			if (TemperatureRight < 300f)
			{
				ItemStack itemStack = ContentsRight.Clone();
				ITreeAttribute obj = itemStack.Attributes["temperature"] as ITreeAttribute;
				if (obj != null)
				{
					obj.RemoveAttribute("cooldownSpeed");
					return itemStack;
				}
				return itemStack;
			}
		}
		return null;
	}

	protected ItemStack GetShatteredStack(ItemStack contents, int fillLevel)
	{
		JsonItemStack shatteredStack = contents.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
		if (shatteredStack != null)
		{
			shatteredStack.Resolve(Api.World, "shatteredStack for" + contents.Collectible.Code);
			if (shatteredStack.ResolvedItemstack != null)
			{
				ItemStack resolvedItemstack = shatteredStack.ResolvedItemstack;
				resolvedItemstack.StackSize = (int)((double)((float)fillLevel / 5f) * (0.699999988079071 + Api.World.Rand.NextDouble() * 0.10000000149011612));
				return resolvedItemstack;
			}
		}
		return null;
	}

	protected bool TryTakeIngot(IPlayer byPlayer, Vec3d hitPosition)
	{
		if (Api is ICoreServerAPI)
		{
			MarkDirty();
		}
		ItemStack leftStack = ((!IsHardenedLeft) ? null : GetStateAwareContentsLeft());
		SetSelectedSide(hitPosition);
		if (leftStack != null && (!IsRightSideSelected || QuantityMolds == 1) && !ShatteredLeft)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos, -0.5, byPlayer, randomizePitch: false);
			if (Api is ICoreServerAPI)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(leftStack))
				{
					Api.World.SpawnItemEntity(leftStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Ingot mold at {2}.", byPlayer.PlayerName, leftStack.Collectible.Code, Pos);
				ContentsLeft = null;
				FillLevelLeft = 0;
			}
			return true;
		}
		ItemStack rightStack = ((!IsHardenedRight) ? null : GetStateAwareContentsRight());
		if (rightStack != null && IsRightSideSelected && !ShatteredRight)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos, -0.5, byPlayer, randomizePitch: false);
			if (Api is ICoreServerAPI)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(rightStack))
				{
					Api.World.SpawnItemEntity(rightStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Ingot mold at {2}.", byPlayer.PlayerName, rightStack.Collectible.Code, Pos);
				ContentsRight = null;
				FillLevelRight = 0;
			}
			return true;
		}
		return false;
	}

	protected bool TryTakeMold(IPlayer byPlayer, Vec3d hitPosition)
	{
		ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (activeStack != null)
		{
			CollectibleObject collectible = activeStack.Collectible;
			if (!(collectible is BlockToolMold) && !(collectible is BlockIngotMold))
			{
				return false;
			}
		}
		if (FillLevelLeft != 0 || FillLevelRight != 0)
		{
			return false;
		}
		ItemStack itemStack = new ItemStack(base.Block);
		if (FillLevelLeft == 0 && !ShatteredLeft)
		{
			QuantityMolds--;
			if (ingotRenderer != null)
			{
				ingotRenderer.QuantityMolds = QuantityMolds;
			}
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Ingot mold at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
			if (QuantityMolds == 0)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
			}
			else
			{
				MarkDirty(redrawOnClient: true);
			}
			if (base.Block.Sounds?.Place != null)
			{
				Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.5, byPlayer, randomizePitch: false);
			}
			return true;
		}
		if (FillLevelRight == 0 && !ShatteredRight)
		{
			QuantityMolds--;
			if (ingotRenderer != null)
			{
				ingotRenderer.QuantityMolds = QuantityMolds;
			}
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Ingot mold at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
			if (QuantityMolds == 0)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
			}
			else
			{
				MarkDirty(redrawOnClient: true);
			}
			if (base.Block.Sounds?.Place != null)
			{
				Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.5, byPlayer, randomizePitch: false);
			}
			return true;
		}
		return false;
	}

	protected bool TryPutMold(IPlayer byPlayer)
	{
		if (QuantityMolds >= 2)
		{
			return false;
		}
		QuantityMolds++;
		if (ingotRenderer != null)
		{
			ingotRenderer.QuantityMolds = QuantityMolds;
		}
		if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize--;
			if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize == 0)
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;
			}
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		}
		if (base.Block.Sounds?.Place != null)
		{
			Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.5, byPlayer, randomizePitch: false);
		}
		MarkDirty(redrawOnClient: true);
		return true;
	}

	protected bool HasMoldInHands(IPlayer byPlayer)
	{
		if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack != null)
		{
			return byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible == base.Block;
		}
		return false;
	}

	public void UpdateIngotRenderer()
	{
		if (ingotRenderer == null)
		{
			return;
		}
		if (BothShattered)
		{
			capi.Event.UnregisterRenderer(ingotRenderer, EnumRenderStage.Opaque);
			return;
		}
		ingotRenderer.QuantityMolds = QuantityMolds;
		ingotRenderer.LevelLeft = ((!ShatteredLeft) ? FillLevelLeft : 0);
		ingotRenderer.LevelRight = ((!ShatteredRight) ? FillLevelRight : 0);
		if (ContentsLeft?.Collectible != null)
		{
			ingotRenderer.TextureNameLeft = new AssetLocation("block/metal/ingot/" + ContentsLeft.Collectible.LastCodePart() + ".png");
		}
		else
		{
			ingotRenderer.TextureNameLeft = null;
		}
		if (ContentsRight?.Collectible != null)
		{
			ingotRenderer.TextureNameRight = new AssetLocation("block/metal/ingot/" + ContentsRight.Collectible.LastCodePart() + ".png");
		}
		else
		{
			ingotRenderer.TextureNameRight = null;
		}
	}

	public void ReceiveLiquidMetal(ItemStack metal, ref int amount, float temperature)
	{
		if (lastPouringMarkdirtyMs + 500 < Api.World.ElapsedMilliseconds)
		{
			MarkDirty(redrawOnClient: true);
			lastPouringMarkdirtyMs = Api.World.ElapsedMilliseconds + 500;
		}
		if ((QuantityMolds == 1 || !IsRightSideSelected) && FillLevelLeft < RequiredUnits && (ContentsLeft == null || metal.Collectible.Equals(ContentsLeft, metal, GlobalConstants.IgnoredStackAttributes)))
		{
			if (ContentsLeft == null)
			{
				ContentsLeft = metal.Clone();
				ContentsLeft.ResolveBlockOrItem(Api.World);
				ContentsLeft.Collectible.SetTemperature(Api.World, ContentsLeft, temperature, delayCooldown: false);
				ContentsLeft.StackSize = 1;
				(ContentsLeft.Attributes["temperature"] as ITreeAttribute)?.SetFloat("cooldownSpeed", 300f);
			}
			else
			{
				ContentsLeft.Collectible.SetTemperature(Api.World, ContentsLeft, temperature, delayCooldown: false);
			}
			int amountToFill = Math.Min(amount, RequiredUnits - FillLevelLeft);
			FillLevelLeft += amountToFill;
			amount -= amountToFill;
			UpdateIngotRenderer();
		}
		else if (IsRightSideSelected && QuantityMolds > 1 && FillLevelRight < RequiredUnits && (ContentsRight == null || metal.Collectible.Equals(ContentsRight, metal, GlobalConstants.IgnoredStackAttributes)))
		{
			if (ContentsRight == null)
			{
				ContentsRight = metal.Clone();
				ContentsRight.ResolveBlockOrItem(Api.World);
				ContentsRight.Collectible.SetTemperature(Api.World, ContentsRight, temperature, delayCooldown: false);
				ContentsRight.StackSize = 1;
				(ContentsRight.Attributes["temperature"] as ITreeAttribute)?.SetFloat("cooldownSpeed", 300f);
			}
			else
			{
				ContentsRight.Collectible.SetTemperature(Api.World, ContentsRight, temperature, delayCooldown: false);
			}
			int amountToFill2 = Math.Min(amount, RequiredUnits - FillLevelRight);
			FillLevelRight += amountToFill2;
			amount -= amountToFill2;
			UpdateIngotRenderer();
		}
	}

	public void OnPourOver()
	{
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (ingotRenderer != null)
		{
			ingotRenderer.Dispose();
			ingotRenderer = null;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		switch (QuantityMolds)
		{
		case 0:
			return true;
		case 1:
		{
			if (ShatteredLeft)
			{
				EnsureShatteredMeshesLoaded();
			}
			float[] leftTfMat = Mat4f.Create();
			Mat4f.Translate(leftTfMat, leftTfMat, 0.5f, 0f, 0.5f);
			Mat4f.RotateY(leftTfMat, leftTfMat, MeshAngle);
			Mat4f.Translate(leftTfMat, leftTfMat, -0.5f, -0f, -0.5f);
			mesher.AddMeshData(ShatteredLeft ? shatteredMeshLeft : MoldMesh, leftTfMat);
			break;
		}
		case 2:
		{
			if (ShatteredLeft || ShatteredRight)
			{
				EnsureShatteredMeshesLoaded();
			}
			Matrixf matrixfl = new Matrixf().Identity();
			matrixfl.Translate(0.5f, 0f, 0.5f).RotateY(MeshAngle).Translate(-0.5f, -0f, -0.5f)
				.Translate(left);
			Matrixf matrixfr = new Matrixf().Identity();
			matrixfr.Translate(0.5f, 0f, 0.5f).RotateY(MeshAngle).Translate(-0.5f, -0f, -0.5f)
				.Translate(right);
			mesher.AddMeshData(ShatteredLeft ? shatteredMeshLeft : MoldMesh, matrixfl.Values);
			mesher.AddMeshData(ShatteredRight ? shatteredMeshRight : MoldMesh, matrixfr.Values);
			break;
		}
		}
		return true;
	}

	private void EnsureShatteredMeshesLoaded()
	{
		if (ShatteredLeft && shatteredMeshLeft == null)
		{
			metalTexLoc = ((ContentsLeft == null) ? new AssetLocation("block/transparent") : new AssetLocation("block/metal/ingot/" + ContentsLeft.Collectible.LastCodePart()));
			capi.Tesselator.TesselateShape("shatteredmold", getShatteredShape(), out shatteredMeshLeft, this, null, 0, 0, 0);
		}
		if (ShatteredRight && shatteredMeshRight == null)
		{
			metalTexLoc = ((ContentsRight == null) ? new AssetLocation("block/transparent") : new AssetLocation("block/metal/ingot/" + ContentsRight.Collectible.LastCodePart()));
			capi.Tesselator.TesselateShape("shatteredmold", getShatteredShape(), out shatteredMeshRight, this, null, 0, 0, 0);
		}
	}

	private Shape getShatteredShape()
	{
		tmpTextureSource = capi.Tesselator.GetTextureSource(base.Block);
		CompositeShape cshape = base.Block.Attributes["shatteredShape"].AsObject<CompositeShape>();
		cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		return Shape.TryGet(Api, cshape.Base);
	}

	private void GenMeshes()
	{
		MoldMesh = ObjectCacheUtil.GetOrCreate(Api, "ingotmold", delegate
		{
			ITexPositionSource textureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(base.Block);
			ITesselatorAPI tesselator = ((ICoreClientAPI)Api).Tesselator;
			Shape shapeBase = Shape.TryGet(Api, "shapes/block/clay/mold/ingot.json");
			tesselator.TesselateShape("ingotmold", shapeBase, out var modeldata, textureSource, null, 0, 0, 0);
			return modeldata;
		});
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		ContentsLeft = tree.GetItemstack("contentsLeft");
		FillLevelLeft = tree.GetInt("fillLevelLeft");
		if (Api?.World != null && ContentsLeft != null)
		{
			ContentsLeft.ResolveBlockOrItem(Api.World);
		}
		ContentsRight = tree.GetItemstack("contentsRight");
		FillLevelRight = tree.GetInt("fillLevelRight");
		if (Api?.World != null && ContentsRight != null)
		{
			ContentsRight.ResolveBlockOrItem(Api.World);
		}
		QuantityMolds = tree.GetInt("quantityMolds");
		ShatteredLeft = tree.GetBool("shatteredLeft");
		ShatteredRight = tree.GetBool("shatteredRight");
		MeshAngle = tree.GetFloat("meshAngle");
		UpdateIngotRenderer();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetItemstack("contentsLeft", ContentsLeft);
		tree.SetInt("fillLevelLeft", FillLevelLeft);
		tree.SetItemstack("contentsRight", ContentsRight);
		tree.SetInt("fillLevelRight", FillLevelRight);
		tree.SetInt("quantityMolds", QuantityMolds);
		tree.SetBool("shatteredLeft", ShatteredLeft);
		tree.SetBool("shatteredRight", ShatteredRight);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		string contents = "";
		if (BothShattered)
		{
			dsc.AppendLine(Lang.Get("Has shattered."));
			return;
		}
		if (ContentsLeft != null)
		{
			if (ShatteredLeft)
			{
				dsc.AppendLine(Lang.Get("Has shattered."));
			}
			else
			{
				string mat2 = ContentsLeft.Collectible?.Variant["metal"];
				string contentsLocalized2 = ((mat2 == null) ? ContentsLeft.GetName() : Lang.Get("material-" + mat2));
				string state2 = (IsLiquidLeft ? Lang.Get("liquid") : (IsHardenedLeft ? Lang.Get("hardened") : Lang.Get("soft")));
				string temp2 = ((TemperatureLeft < 21f) ? Lang.Get("Cold") : Lang.Get("{0}°C", (int)TemperatureLeft));
				contents = Lang.Get("{0} units of {1} {2} ({3})", FillLevelLeft, state2, contentsLocalized2, temp2) + "\n";
			}
		}
		if (ContentsRight != null)
		{
			if (ShatteredRight)
			{
				dsc.AppendLine(Lang.Get("Has shattered."));
			}
			else
			{
				string mat = ContentsRight.Collectible?.Variant["metal"];
				string contentsLocalized = ((mat == null) ? ContentsRight.GetName() : Lang.Get("material-" + mat));
				string state = (IsLiquidRight ? Lang.Get("liquid") : (IsHardenedRight ? Lang.Get("hardened") : Lang.Get("soft")));
				string temp = ((TemperatureRight < 21f) ? Lang.Get("Cold") : Lang.Get("{0}°C", (int)TemperatureRight));
				contents = contents + Lang.Get("{0} units of {1} {2} ({3})", FillLevelRight, state, contentsLocalized, temp) + "\n";
			}
		}
		dsc.AppendLine((contents.Length == 0) ? Lang.Get("Empty") : contents);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ingotRenderer?.Dispose();
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		ContentsLeft?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(ContentsLeft), blockIdMapping, itemIdMapping);
		ContentsRight?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(ContentsRight), blockIdMapping, itemIdMapping);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack contentsLeft = ContentsLeft;
		if (contentsLeft != null && !contentsLeft.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			ContentsLeft = null;
		}
		ItemStack contentsRight = ContentsRight;
		if (contentsRight != null && !contentsRight.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			ContentsRight = null;
		}
	}

	public void CoolNow(float amountRel)
	{
		float leftbreakchance = Math.Max(0f, amountRel - 0.6f) * Math.Max(TemperatureLeft - 250f, 0f) / 5000f;
		float rightbreakchance = Math.Max(0f, amountRel - 0.6f) * Math.Max(TemperatureRight - 250f, 0f) / 5000f;
		if (Api.World.Rand.NextDouble() < (double)leftbreakchance)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
			ShatteredLeft = true;
			ContentsLeft.Collectible.SetTemperature(Api.World, ContentsLeft, 20f, delayCooldown: false);
			base.Block.SpawnBlockBrokenParticles(Pos);
			base.Block.SpawnBlockBrokenParticles(Pos);
			MarkDirty(redrawOnClient: true);
		}
		else if (ContentsLeft != null)
		{
			float temp2 = TemperatureLeft;
			if (temp2 > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.4, null, randomizePitch: false, 16f);
			}
			ContentsLeft.Collectible.SetTemperature(Api.World, ContentsLeft, Math.Max(20f, temp2 - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
		if (Api.World.Rand.NextDouble() < (double)rightbreakchance)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
			ShatteredRight = true;
			ContentsRight.Collectible.SetTemperature(Api.World, ContentsRight, 20f, delayCooldown: false);
			base.Block.SpawnBlockBrokenParticles(Pos);
			base.Block.SpawnBlockBrokenParticles(Pos);
			MarkDirty(redrawOnClient: true);
		}
		else if (ContentsRight != null)
		{
			float temp = TemperatureRight;
			if (temp > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
			}
			ContentsRight.Collectible.SetTemperature(Api.World, ContentsRight, Math.Max(20f, temp - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
