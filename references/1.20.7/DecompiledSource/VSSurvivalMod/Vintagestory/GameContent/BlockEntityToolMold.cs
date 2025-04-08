using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityToolMold : BlockEntity, ILiquidMetalSink, ITemperatureSensitive, ITexPositionSource
{
	protected ToolMoldRenderer renderer;

	protected Cuboidf[] fillQuadsByLevel;

	protected int requiredUnits = 100;

	protected float fillHeight = 1f;

	public ItemStack MetalContent;

	public int FillLevel;

	public bool FillSide;

	public bool Shattered;

	private ICoreClientAPI capi;

	private ITexPositionSource tmpTextureSource;

	private AssetLocation metalTexLoc;

	private MeshData shatteredMesh;

	public float Temperature => MetalContent?.Collectible.GetTemperature(Api.World, MetalContent) ?? 0f;

	public bool IsHardened => Temperature < 0.3f * MetalContent?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(MetalContent));

	public float ShatterChance
	{
		get
		{
			if (MetalContent != null)
			{
				return GameMath.Clamp((Temperature - 0.3f * MetalContent.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(MetalContent))) / 1000f, 0f, 1f);
			}
			return 0f;
		}
	}

	public bool IsLiquid => Temperature > 0.8f * MetalContent?.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(MetalContent));

	public bool IsFull => FillLevel >= requiredUnits;

	public bool CanReceiveAny
	{
		get
		{
			if (!Shattered)
			{
				return base.Block.Code.Path.Contains("burned");
			}
			return false;
		}
	}

	public bool IsHot => Temperature >= 200f;

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
		if (MetalContent != null)
		{
			MetalContent.ResolveBlockOrItem(Api.World);
		}
		if (base.Block != null && !(base.Block.Code == null) && base.Block.Attributes != null)
		{
			fillHeight = base.Block.Attributes["fillHeight"].AsFloat(1f);
			requiredUnits = base.Block.Attributes["requiredUnits"].AsInt(100);
			if (base.Block.Attributes["fillQuadsByLevel"].Exists)
			{
				fillQuadsByLevel = base.Block.Attributes["fillQuadsByLevel"].AsObject<Cuboidf[]>();
			}
			if (fillQuadsByLevel == null)
			{
				fillQuadsByLevel = new Cuboidf[1]
				{
					new Cuboidf(2f, 0f, 2f, 14f, 0f, 14f)
				};
			}
			capi = api as ICoreClientAPI;
			if (capi != null && !Shattered)
			{
				capi.Event.RegisterRenderer(renderer = new ToolMoldRenderer(Pos, capi, fillQuadsByLevel), EnumRenderStage.Opaque, "toolmoldrenderer");
				UpdateRenderer();
			}
			if (!Shattered)
			{
				RegisterGameTickListener(OnGameTick, 50);
			}
		}
	}

	private void OnGameTick(float dt)
	{
		if (renderer != null)
		{
			renderer.Level = (float)FillLevel * fillHeight / (float)requiredUnits;
		}
		if (MetalContent != null && renderer != null)
		{
			renderer.stack = MetalContent;
			renderer.Temperature = Math.Min(1300f, MetalContent.Collectible.GetTemperature(Api.World, MetalContent));
		}
	}

	public bool CanReceive(ItemStack metal)
	{
		if ((MetalContent == null || (MetalContent.Collectible.Equals(MetalContent, metal, GlobalConstants.IgnoredStackAttributes) && FillLevel < requiredUnits)) && GetMoldedStacks(metal) != null && GetMoldedStacks(metal).Length != 0)
		{
			return !Shattered;
		}
		return false;
	}

	public void BeginFill(Vec3d hitPosition)
	{
		FillSide = hitPosition.X >= 0.5;
	}

	public bool OnPlayerInteract(IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
	{
		if (Shattered)
		{
			return false;
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			if (byPlayer.Entity.Controls.HandUse != 0)
			{
				return false;
			}
			bool handled = TryTakeContents(byPlayer);
			if (!handled && FillLevel == 0)
			{
				ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
				if (activeStack != null)
				{
					CollectibleObject collectible = activeStack.Collectible;
					if (!(collectible is BlockToolMold) && !(collectible is BlockIngotMold))
					{
						return handled;
					}
				}
				ItemStack itemStack = new ItemStack(Api.World.BlockAccessor.GetBlock(base.Block.CodeWithVariant("side", "north")));
				if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
				{
					Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Tool mold at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
				Api.World.BlockAccessor.SetBlock(0, Pos);
				if (base.Block.Sounds?.Place != null)
				{
					Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.5, byPlayer, randomizePitch: false);
				}
				handled = true;
			}
			return handled;
		}
		return false;
	}

	protected virtual bool TryTakeContents(IPlayer byPlayer)
	{
		if (Shattered)
		{
			return false;
		}
		if (Api is ICoreServerAPI)
		{
			MarkDirty();
		}
		if (MetalContent != null && FillLevel >= requiredUnits && IsHardened)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos, -0.5, byPlayer, randomizePitch: false);
			if (Api is ICoreServerAPI)
			{
				ItemStack[] outstacks = GetStateAwareMoldedStacks();
				if (outstacks != null)
				{
					ItemStack[] array = outstacks;
					foreach (ItemStack outstack in array)
					{
						int quantity = outstack.StackSize;
						if (!byPlayer.InventoryManager.TryGiveItemstack(outstack))
						{
							Api.World.SpawnItemEntity(outstack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
						}
						Api.World.Logger.Audit("{0} Took {1}x{2} from Tool mold at {3}.", byPlayer.PlayerName, quantity, outstack.Collectible.Code, Pos);
					}
				}
				MetalContent = null;
				FillLevel = 0;
			}
			UpdateRenderer();
			return true;
		}
		return false;
	}

	public void UpdateRenderer()
	{
		if (renderer == null)
		{
			return;
		}
		if (Shattered && renderer != null)
		{
			(Api as ICoreClientAPI).Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
			renderer = null;
			return;
		}
		renderer.Level = (float)FillLevel * fillHeight / (float)requiredUnits;
		if (MetalContent?.Collectible != null)
		{
			renderer.TextureName = new AssetLocation("block/metal/ingot/" + MetalContent.Collectible.LastCodePart() + ".png");
		}
		else
		{
			renderer.TextureName = null;
		}
	}

	public void ReceiveLiquidMetal(ItemStack metal, ref int amount, float temperature)
	{
		if (FillLevel < requiredUnits && (MetalContent == null || metal.Collectible.Equals(MetalContent, metal, GlobalConstants.IgnoredStackAttributes)))
		{
			if (MetalContent == null)
			{
				MetalContent = metal.Clone();
				MetalContent.ResolveBlockOrItem(Api.World);
				MetalContent.Collectible.SetTemperature(Api.World, MetalContent, temperature, delayCooldown: false);
				MetalContent.StackSize = 1;
				(MetalContent.Attributes["temperature"] as ITreeAttribute)?.SetFloat("cooldownSpeed", 300f);
			}
			else
			{
				MetalContent.Collectible.SetTemperature(Api.World, MetalContent, temperature, delayCooldown: false);
			}
			int amountToFill = Math.Min(amount, requiredUnits - FillLevel);
			FillLevel += amountToFill;
			amount -= amountToFill;
			UpdateRenderer();
		}
	}

	public void OnPourOver()
	{
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
		renderer = null;
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
		renderer = null;
	}

	public ItemStack[] GetStateAwareMoldedStacks()
	{
		if (FillLevel < requiredUnits)
		{
			return null;
		}
		if (MetalContent?.Collectible == null)
		{
			return null;
		}
		if (Shattered)
		{
			JsonItemStack shatteredStack = MetalContent.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
			if (shatteredStack != null)
			{
				shatteredStack.Resolve(Api.World, "shatteredStack for" + MetalContent.Collectible.Code);
				if (shatteredStack.ResolvedItemstack != null)
				{
					ItemStack[] obj = new ItemStack[1] { shatteredStack.ResolvedItemstack };
					obj[0].StackSize = (int)((double)((float)FillLevel / 5f) * (0.699999988079071 + Api.World.Rand.NextDouble() * 0.10000000149011612));
					return obj;
				}
			}
		}
		return GetMoldedStacks(MetalContent);
	}

	public ItemStack[] GetMoldedStacks(ItemStack fromMetal)
	{
		try
		{
			if (base.Block.Attributes["drop"].Exists)
			{
				JsonItemStack jstack = base.Block.Attributes["drop"].AsObject<JsonItemStack>(null, base.Block.Code.Domain);
				if (jstack == null)
				{
					return null;
				}
				ItemStack stack = stackFromCode(jstack, fromMetal);
				if (stack == null)
				{
					return new ItemStack[0];
				}
				if (MetalContent != null)
				{
					stack.Collectible.SetTemperature(Api.World, stack, MetalContent.Collectible.GetTemperature(Api.World, MetalContent));
				}
				return new ItemStack[1] { stack };
			}
			JsonItemStack[] array = base.Block.Attributes["drops"].AsObject<JsonItemStack[]>(null, base.Block.Code.Domain);
			List<ItemStack> stacks = new List<ItemStack>();
			JsonItemStack[] array2 = array;
			foreach (JsonItemStack jstack2 in array2)
			{
				ItemStack stack2 = stackFromCode(jstack2, fromMetal);
				if (MetalContent != null)
				{
					stack2.Collectible.SetTemperature(Api.World, stack2, MetalContent.Collectible.GetTemperature(Api.World, MetalContent));
				}
				if (stack2 != null)
				{
					stacks.Add(stack2);
				}
			}
			return stacks.ToArray();
		}
		catch (JsonReaderException)
		{
			Api.World.Logger.Error("Failed getting molded stacks from tool mold of block {0}, probably unable to parse drop or drops attribute", base.Block.Code);
			throw;
		}
	}

	public ItemStack stackFromCode(JsonItemStack jstack, ItemStack fromMetal)
	{
		string metaltype = fromMetal.Collectible.LastCodePart();
		string tooltype = base.Block.LastCodePart();
		jstack.Code.Path = jstack.Code.Path.Replace("{tooltype}", tooltype).Replace("{metal}", metaltype);
		jstack.Resolve(Api.World, "tool mold drop for " + base.Block.Code);
		return jstack.ResolvedItemstack;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
	{
		base.FromTreeAttributes(tree, worldForResolve);
		MetalContent = tree.GetItemstack("contents");
		FillLevel = tree.GetInt("fillLevel");
		Shattered = tree.GetBool("shattered");
		if (Api?.World != null && MetalContent != null)
		{
			MetalContent.ResolveBlockOrItem(Api.World);
		}
		UpdateRenderer();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetItemstack("contents", MetalContent);
		tree.SetInt("fillLevel", FillLevel);
		tree.SetBool("shattered", Shattered);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (Shattered)
		{
			dsc.AppendLine(Lang.Get("Has shattered."));
			return;
		}
		string contents;
		if (MetalContent != null)
		{
			string state = (IsLiquid ? Lang.Get("liquid") : (IsHardened ? Lang.Get("hardened") : Lang.Get("soft")));
			string temp = ((Temperature < 21f) ? Lang.Get("Cold") : Lang.Get("{0}°C", (int)Temperature));
			string matkey = "material-" + MetalContent.Collectible.Variant["metal"];
			string mat = (Lang.HasTranslation(matkey) ? Lang.Get(matkey) : Lang.Get(MetalContent.GetName()));
			contents = Lang.Get("{0}/{4} units of {1} {2} ({3})", FillLevel, state, mat, temp, requiredUnits) + "\n";
		}
		else
		{
			contents = Lang.Get("0/{0} units of metal", requiredUnits) + "\n";
		}
		dsc.AppendLine((contents.Length == 0) ? Lang.Get("Empty") : contents);
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		MetalContent?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(MetalContent), blockIdMapping, itemIdMapping);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack metalContent = MetalContent;
		if (metalContent != null)
		{
			metalContent.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve);
			if (0 == 0)
			{
				goto IL_0020;
			}
		}
		MetalContent = null;
		goto IL_0020;
		IL_0020:
		ITreeAttribute obj = MetalContent?.Attributes["temperature"] as ITreeAttribute;
		if (obj != null && obj.HasAttribute("temperatureLastUpdate"))
		{
			((ITreeAttribute)MetalContent.Attributes["temperature"]).SetDouble("temperatureLastUpdate", worldForResolve.Calendar.TotalHours);
		}
	}

	public void CoolNow(float amountRel)
	{
		float breakchance = Math.Max(0f, amountRel - 0.6f) * Math.Max(Temperature - 250f, 0f) / 5000f;
		if (Api.World.Rand.NextDouble() < (double)breakchance)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
			base.Block.SpawnBlockBrokenParticles(Pos);
			base.Block.SpawnBlockBrokenParticles(Pos);
			MetalContent.Collectible.SetTemperature(Api.World, MetalContent, 20f, delayCooldown: false);
			Shattered = true;
			MarkDirty(redrawOnClient: true);
		}
		else if (MetalContent != null)
		{
			float temp = Temperature;
			if (temp > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
			}
			MetalContent.Collectible.SetTemperature(Api.World, MetalContent, Math.Max(20f, temp - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (Shattered)
		{
			if (shatteredMesh == null)
			{
				metalTexLoc = ((MetalContent == null) ? new AssetLocation("block/transparent") : new AssetLocation("block/metal/ingot/" + MetalContent.Collectible.LastCodePart()));
				tmpTextureSource = capi.Tesselator.GetTextureSource(base.Block);
				ITesselatorAPI tesselator = capi.Tesselator;
				CompositeShape cshape = base.Block.Attributes["shatteredShape"].AsObject<CompositeShape>();
				cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
				Shape shape = Shape.TryGet(Api, cshape.Base);
				tesselator.TesselateShape("shatteredmold", shape, out shatteredMesh, this, null, 0, 0, 0);
			}
			mesher.AddMeshData(shatteredMesh);
			return true;
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
