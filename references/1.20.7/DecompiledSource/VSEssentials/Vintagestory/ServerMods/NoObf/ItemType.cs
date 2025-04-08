using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class ItemType : CollectibleType
{
	public ItemType()
	{
		Class = "Item";
		GuiTransform = ModelTransform.ItemDefaultGui();
		FpHandTransform = ModelTransform.ItemDefaultFp();
		TpHandTransform = ModelTransform.ItemDefaultTp();
		TpOffHandTransform = null;
		GroundTransform = ModelTransform.ItemDefaultGround();
	}

	internal override RegistryObjectType CreateAndPopulate(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant)
	{
		ItemType resolvedType = CreateResolvedType<ItemType>(api, fullcode, jobject, deserializer, variant);
		if (resolvedType.Shape != null && !resolvedType.Shape.VoxelizeTexture && jobject["guiTransform"]?["rotate"] == null)
		{
			GuiTransform = ModelTransform.ItemDefaultGui();
			GuiTransform.Rotate = true;
		}
		return resolvedType;
	}

	public void InitItem(IClassRegistryAPI instancer, ILogger logger, Item item, OrderedDictionary<string, string> searchReplace)
	{
		item.CreativeInventoryTabs = BlockType.GetCreativeTabs(item.Code, CreativeInventory, searchReplace);
		CollectibleBehaviorType[] behaviorTypes = Behaviors;
		if (behaviorTypes == null)
		{
			return;
		}
		List<CollectibleBehavior> collbehaviors = new List<CollectibleBehavior>();
		foreach (CollectibleBehaviorType behaviorType in behaviorTypes)
		{
			if (instancer.GetCollectibleBehaviorClass(behaviorType.name) != null)
			{
				CollectibleBehavior behavior = instancer.CreateCollectibleBehavior(item, behaviorType.name);
				if (behaviorType.properties == null)
				{
					behaviorType.properties = new JsonObject(new JObject());
				}
				try
				{
					behavior.Initialize(behaviorType.properties);
				}
				catch (Exception e)
				{
					logger.Error("Failed calling Initialize() on collectible behavior {0} for item {1}, using properties {2}. Will continue anyway.", behaviorType.name, item.Code, behaviorType.properties.ToString());
					logger.Error(e);
				}
				collbehaviors.Add(behavior);
			}
			else
			{
				logger.Warning(Lang.Get("Collectible behavior {0} for item {1} not found", behaviorType.name, item.Code));
			}
		}
		item.CollectibleBehaviors = collbehaviors.ToArray();
	}

	public Item CreateItem(ICoreServerAPI api)
	{
		Item item;
		if (api.ClassRegistry.GetItemClass(Class) == null)
		{
			api.Server.Logger.Error("Item with code {0} has defined an item class {1}, but no such class registered. Will ignore.", Code, Class);
			item = new Item();
		}
		else
		{
			item = api.ClassRegistry.CreateItem(Class);
		}
		item.Code = Code;
		item.VariantStrict = Variant;
		item.Variant = new RelaxedReadOnlyDictionary<string, string>(Variant);
		item.Class = Class;
		item.Textures = Textures;
		item.MaterialDensity = MaterialDensity;
		item.GuiTransform = GuiTransform;
		item.FpHandTransform = FpHandTransform;
		item.TpHandTransform = TpHandTransform;
		item.TpOffHandTransform = TpOffHandTransform;
		item.GroundTransform = GroundTransform;
		item.LightHsv = LightHsv;
		item.DamagedBy = (EnumItemDamageSource[])DamagedBy?.Clone();
		item.MaxStackSize = MaxStackSize;
		if (Attributes != null)
		{
			item.Attributes = Attributes;
		}
		item.CombustibleProps = CombustibleProps;
		item.NutritionProps = NutritionProps;
		item.TransitionableProps = TransitionableProps;
		item.GrindingProps = GrindingProps;
		item.CrushingProps = CrushingProps;
		item.Shape = Shape;
		item.Tool = Tool;
		item.AttackPower = AttackPower;
		item.LiquidSelectable = LiquidSelectable;
		item.ToolTier = ToolTier;
		item.HeldSounds = HeldSounds?.Clone();
		item.Durability = Durability;
		item.Dimensions = Size ?? CollectibleObject.DefaultSize;
		item.MiningSpeed = MiningSpeed;
		item.AttackRange = AttackRange;
		item.StorageFlags = (EnumItemStorageFlags)StorageFlags;
		item.RenderAlphaTest = RenderAlphaTest;
		item.HeldTpHitAnimation = HeldTpHitAnimation;
		item.HeldRightTpIdleAnimation = HeldRightTpIdleAnimation;
		item.HeldLeftTpIdleAnimation = HeldLeftTpIdleAnimation;
		item.HeldLeftReadyAnimation = HeldLeftReadyAnimation;
		item.HeldRightReadyAnimation = HeldRightReadyAnimation;
		item.HeldTpUseAnimation = HeldTpUseAnimation;
		item.CreativeInventoryStacks = ((CreativeInventoryStacks == null) ? null : ((CreativeTabAndStackList[])CreativeInventoryStacks.Clone()));
		item.MatterState = MatterState;
		item.ParticleProperties = ParticleProperties;
		InitItem(api.ClassRegistry, api.World.Logger, item, Variant);
		return item;
	}
}
