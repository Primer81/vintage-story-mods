using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public class ClassRegistry
{
	public Dictionary<string, GetMountableDelegate> mountableEntries = new Dictionary<string, GetMountableDelegate>();

	public Dictionary<string, Type> inventoryClassToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> RecipeRegistryToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<Type, string> TypeToRecipeRegistryMapping = new Dictionary<Type, string>();

	public Dictionary<string, Type> BlockClassToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> blockbehaviorToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> blockentitybehaviorToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> collectibleBehaviorToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> cropbehaviorToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> ItemClassToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<string, Type> entityClassNameToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<Type, string> entityTypeToClassNameMapping = new Dictionary<Type, string>();

	public Dictionary<string, Type> EntityRendererClassNameToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<Type, string> EntityRendererTypeToClassNameMapping = new Dictionary<Type, string>();

	public Dictionary<string, Type> entityBehaviorClassNameToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<Type, string> entityBehaviorTypeToClassNameMapping = new Dictionary<Type, string>();

	public Dictionary<string, Type> blockEntityClassnameToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<Type, string> blockEntityTypeToClassnameMapping = new Dictionary<Type, string>();

	public static Dictionary<string, string> legacyBlockEntityClassNames = new Dictionary<string, string>
	{
		{ "Chest", "GenericContainer" },
		{ "Basket", "GenericContainer" },
		{ "Axle", "Generic" },
		{ "AngledGears", "Generic" },
		{ "WindmillRotor", "Generic" },
		{ "ClutterBookshelf", "Generic" },
		{ "Clutter", "Generic" },
		{ "EchoChamber", "Resonator" },
		{ "BECommand", "GuiConfigurableCommands" },
		{ "BEConditional", "Conditional" },
		{ "BETicker", "Ticker" }
	};

	public Dictionary<string, Type> ParticleProviderClassnameToTypeMapping = new Dictionary<string, Type>();

	public Dictionary<Type, string> ParticleProviderTypeToClassnameMapping = new Dictionary<Type, string>();

	public ClassRegistry()
	{
		RegisterDefaultInventories();
		RegisterDefaultParticlePropertyProviders();
		RegisterItemClass("Item", typeof(Item));
		RegisterBlockClass("Block", typeof(Block));
		RegisterEntityType("EntityItem", typeof(EntityItem));
		RegisterEntityType("EntityChunky", typeof(EntityChunky));
		RegisterEntityType("EntityPlayer", typeof(EntityPlayer));
		RegisterEntityType("EntityHumanoid", typeof(EntityHumanoid));
		RegisterEntityType("EntityAgent", typeof(EntityAgent));
		RegisterentityBehavior("passivephysics", typeof(EntityBehaviorPassivePhysics));
	}

	public void RegisterMountable(string className, GetMountableDelegate mountableInstancer)
	{
		mountableEntries[className] = mountableInstancer;
	}

	public IMountableSeat GetMountable(IWorldAccessor world, TreeAttribute tree)
	{
		string className = tree.GetString("className");
		if (mountableEntries.TryGetValue(className, out var dele))
		{
			return dele(world, tree);
		}
		return null;
	}

	public void RegisterInventoryClass(string inventoryClass, Type inventory)
	{
		inventoryClassToTypeMapping[inventoryClass] = inventory;
	}

	public InventoryBase CreateInventory(string inventoryClass, string inventoryId, ICoreAPI api)
	{
		if (!inventoryClassToTypeMapping.TryGetValue(inventoryClass, out var inventoryType))
		{
			throw new Exception("Don't know how to instantiate inventory of class '" + inventoryClass + "' did you forget to register a mapping?");
		}
		try
		{
			return (InventoryBase)Activator.CreateInstance(inventoryType, inventoryId, api);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error while instantiating inventory of class '{inventoryClass}' and id '{inventoryId}':\n {exception}", exception);
		}
	}

	private void RegisterDefaultInventories()
	{
		RegisterInventoryClass("creative", typeof(InventoryPlayerCreative));
		RegisterInventoryClass("backpack", typeof(InventoryPlayerBackPacks));
		RegisterInventoryClass("ground", typeof(InventoryPlayerGround));
		RegisterInventoryClass("hotbar", typeof(InventoryPlayerHotbar));
		RegisterInventoryClass("mouse", typeof(InventoryPlayerMouseCursor));
		RegisterInventoryClass("craftinggrid", typeof(InventoryCraftingGrid));
		RegisterInventoryClass("character", typeof(InventoryCharacter));
	}

	public void RegisterRecipeRegistry(string recipeRegistryCode, Type recipeRegistry)
	{
		RecipeRegistryToTypeMapping[recipeRegistryCode] = recipeRegistry;
		TypeToRecipeRegistryMapping[recipeRegistry] = recipeRegistryCode;
	}

	public void RegisterRecipeRegistry<T>(string recipeRegistryCode)
	{
		RecipeRegistryToTypeMapping[recipeRegistryCode] = typeof(T);
	}

	public T CreateRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
	{
		if (!RecipeRegistryToTypeMapping.TryGetValue(recipeRegistryCode, out var recipeType))
		{
			throw new Exception("Don't know how to instantiate recipe registry of class '" + recipeRegistryCode + "' did you forget to register a mapping?");
		}
		try
		{
			return (T)Activator.CreateInstance(recipeType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating recipe registry of class '{typeof(T)}' and code '{recipeRegistryCode}':\n{exception}", exception);
		}
	}

	public string GetRecipeRegistryCode<T>() where T : RecipeRegistryBase
	{
		return TypeToRecipeRegistryMapping[typeof(T)];
	}

	public void RegisterBlockClass(string blockClass, Type block)
	{
		BlockClassToTypeMapping[blockClass] = block;
	}

	public Block CreateBlock(string blockClass)
	{
		if (!BlockClassToTypeMapping.TryGetValue(blockClass, out var blockType))
		{
			throw new Exception("Don't know how to instantiate block of class '" + blockClass + "' did you forget to register a mapping?");
		}
		try
		{
			return (Block)Activator.CreateInstance(blockType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating block class '{blockClass}':\n{exception}", exception);
		}
	}

	public Type GetBlockClass(string blockClass)
	{
		Type val = null;
		BlockClassToTypeMapping.TryGetValue(blockClass, out val);
		return val;
	}

	public void RegisterBlockBehaviorClass(string code, Type block)
	{
		blockbehaviorToTypeMapping[code] = block;
	}

	public string GetBlockBehaviorClassName(Type blockBehaviorType)
	{
		return blockbehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == blockBehaviorType).Key;
	}

	public BlockBehavior CreateBlockBehavior(Block block, string blockClass)
	{
		if (!blockbehaviorToTypeMapping.TryGetValue(blockClass, out var behaviorType))
		{
			throw new Exception("Don't know how to instantiate block behavior of class '" + blockClass + "' did you forget to register a mapping?");
		}
		try
		{
			return (BlockBehavior)Activator.CreateInstance(behaviorType, block);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating block behavior '{blockClass}' for block '{block.Code}':\n{exception}", exception);
		}
	}

	public void RegisterBlockEntityBehaviorClass(string blockClass, Type blockentity)
	{
		blockentitybehaviorToTypeMapping[blockClass] = blockentity;
	}

	public string GetBlockEntityBehaviorClassName(Type blockBehaviorType)
	{
		return blockentitybehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == blockBehaviorType).Key;
	}

	public BlockEntityBehavior CreateBlockEntityBehavior(BlockEntity blockentity, string blockEntityClass)
	{
		if (!blockentitybehaviorToTypeMapping.TryGetValue(blockEntityClass, out var beType))
		{
			throw new Exception("Don't know how to instantiate block entity behavior of class '" + blockEntityClass + "' did you forget to register a mapping?");
		}
		try
		{
			return (BlockEntityBehavior)Activator.CreateInstance(beType, blockentity);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating block entity behavior '{blockEntityClass}' for block '{blockentity.Block.Code}':\n{exception}", exception);
		}
	}

	public void RegisterCollectibleBehaviorClass(string code, Type block)
	{
		collectibleBehaviorToTypeMapping[code] = block;
	}

	public string GetCollectibleBehaviorClassName(Type blockBehaviorType)
	{
		return collectibleBehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == blockBehaviorType).Key;
	}

	public CollectibleBehavior CreateCollectibleBehavior(CollectibleObject collectible, string code)
	{
		if (!collectibleBehaviorToTypeMapping.TryGetValue(code, out var behaviorType))
		{
			throw new Exception("Don't know how to instantiate collectible behavior of class '" + code + "' did you forget to register a mapping?");
		}
		try
		{
			return (CollectibleBehavior)Activator.CreateInstance(behaviorType, collectible);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating collectible behavior '{code}' for '{collectible.Code}':\n{exception}", exception);
		}
	}

	public Type GetCollectibleBehaviorClass(string code)
	{
		collectibleBehaviorToTypeMapping.TryGetValue(code, out var type);
		return type;
	}

	public void RegisterCropBehavior(string cropBehaviorClass, Type block)
	{
		cropbehaviorToTypeMapping[cropBehaviorClass] = block;
	}

	public string GetCropBehaviorClassName(Type cropBehaviorType)
	{
		return cropbehaviorToTypeMapping.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == cropBehaviorType).Key;
	}

	public CropBehavior createCropBehavior(Block block, string cropBehaviorClass)
	{
		if (!cropbehaviorToTypeMapping.TryGetValue(cropBehaviorClass, out var behaviorType))
		{
			throw new Exception("Don't know how to instantiate crop behavior of class '" + cropBehaviorClass + "' did you forget to register a mapping?");
		}
		try
		{
			return (CropBehavior)Activator.CreateInstance(behaviorType, block);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating crop behavior '{cropBehaviorClass}' for '{block.Code}':\n{exception}", exception);
		}
	}

	public void RegisterItemClass(string itemClass, Type item)
	{
		ItemClassToTypeMapping[itemClass] = item;
	}

	public Item CreateItem(string itemClass)
	{
		if (!ItemClassToTypeMapping.TryGetValue(itemClass, out var itemType))
		{
			throw new Exception("Don't know how to instantiate item of class '" + itemClass + "' did you forget to register a mapping?");
		}
		try
		{
			return (Item)Activator.CreateInstance(itemType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating item '{itemClass}':\n{exception}", exception);
		}
	}

	public Type GetItemClass(string itemClass)
	{
		Type val = null;
		ItemClassToTypeMapping.TryGetValue(itemClass, out val);
		return val;
	}

	public void RegisterEntityType(string className, Type entity)
	{
		entityClassNameToTypeMapping[className] = entity;
		entityTypeToClassNameMapping[entity] = className;
	}

	public string GetEntityClassName(Type entityType)
	{
		if (!entityTypeToClassNameMapping.ContainsKey(entityType))
		{
			throw new Exception($"I don't have a mapping for entity type '{entityType}' did you forget to register a mapping?");
		}
		return entityTypeToClassNameMapping[entityType];
	}

	public Entity CreateEntity(Type entityType)
	{
		if (!entityClassNameToTypeMapping.ContainsValue(entityType))
		{
			throw new Exception($"Don't know how to instantiate entity of type '{entityType}' did you forget to register a mapping?");
		}
		try
		{
			return (Entity)Activator.CreateInstance(entityType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating entity '{entityType}':\n{exception}", exception);
		}
	}

	public Entity CreateEntity(string className)
	{
		if (className == "player")
		{
			className = "EntityPlayer";
		}
		if (className == "item")
		{
			className = "EntityItem";
		}
		if (className == "playerbot")
		{
			className = "EntityNpc";
		}
		if (className == "humanoid")
		{
			className = "EntityHumanoid";
		}
		if (className == "living")
		{
			className = "EntityAgent";
		}
		if (className == "blockfalling")
		{
			className = "EntityBlockFalling";
		}
		if (className == "projectile")
		{
			className = "EntityProjectile";
		}
		if (!entityClassNameToTypeMapping.TryGetValue(className, out var entityType))
		{
			throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
		}
		try
		{
			return (Entity)Activator.CreateInstance(entityType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating entity '{entityType}':\n{exception}", exception);
		}
	}

	public void RegisterEntityRendererType(string className, Type EntityRenderer)
	{
		if (EntityRendererClassNameToTypeMapping.TryGetValue(className, out var oldType))
		{
			EntityRendererTypeToClassNameMapping.Remove(oldType);
		}
		EntityRendererClassNameToTypeMapping[className] = EntityRenderer;
		EntityRendererTypeToClassNameMapping[EntityRenderer] = className;
	}

	public string GetEntityRendererClassName(Type EntityRendererType)
	{
		if (!EntityRendererTypeToClassNameMapping.ContainsKey(EntityRendererType))
		{
			throw new Exception("I don't have a mapping for EntityRenderer type " + EntityRendererType?.ToString() + " did you forget to register a mapping?");
		}
		return EntityRendererTypeToClassNameMapping[EntityRendererType];
	}

	public EntityRenderer CreateEntityRenderer(Type EntityRendererType)
	{
		if (!EntityRendererClassNameToTypeMapping.ContainsValue(EntityRendererType))
		{
			throw new Exception("Don't know how to instantiate EntityRenderer of type " + EntityRendererType?.ToString() + " did you forget to register a mapping?");
		}
		return (EntityRenderer)Activator.CreateInstance(EntityRendererType);
	}

	public EntityRenderer CreateEntityRenderer(string className, params object[] args)
	{
		if (!EntityRendererClassNameToTypeMapping.TryGetValue(className, out var rendererType))
		{
			throw new Exception("Don't know how to instantiate EntityRenderer of type " + className + " did you forget to register a mapping?");
		}
		return (EntityRenderer)Activator.CreateInstance(rendererType, args);
	}

	public Type GetEntityBehaviorClass(string entityBehaviorName)
	{
		entityBehaviorClassNameToTypeMapping.TryGetValue(entityBehaviorName, out var type);
		return type;
	}

	public void RegisterentityBehavior(string className, Type entityBehavior)
	{
		if (!entityBehaviorTypeToClassNameMapping.ContainsKey(entityBehavior))
		{
			entityBehaviorClassNameToTypeMapping.Add(className, entityBehavior);
			entityBehaviorTypeToClassNameMapping.Add(entityBehavior, className);
		}
	}

	public string GetEntityBehaviorClassName(Type entityBehaviorType)
	{
		if (!entityBehaviorTypeToClassNameMapping.ContainsKey(entityBehaviorType))
		{
			throw new Exception($"I don't have a mapping for EntityBehavior type '{entityBehaviorType}' did you forget to register a mapping?");
		}
		return entityBehaviorTypeToClassNameMapping[entityBehaviorType];
	}

	public EntityBehavior CreateEntityBehavior(Entity forEntity, Type entityBehaviorType)
	{
		if (!entityBehaviorClassNameToTypeMapping.ContainsValue(entityBehaviorType))
		{
			throw new Exception($"Don't know how to instantiate entityBehavior of type '{entityBehaviorType}' did you forget to register a mapping?");
		}
		try
		{
			return (EntityBehavior)Activator.CreateInstance(entityBehaviorType, forEntity);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error while instantiating entity behavior '{entityBehaviorType}' for '{forEntity.Code}':\n{exception}", exception);
		}
	}

	public EntityBehavior CreateEntityBehavior(Entity forEntity, string className)
	{
		if (!entityBehaviorClassNameToTypeMapping.TryGetValue(className, out var behaviorType))
		{
			throw new Exception("Don't know how to instantiate entityBehavior of type '" + className + "' did you forget to register a mapping?");
		}
		try
		{
			return (EntityBehavior)Activator.CreateInstance(behaviorType, forEntity);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error while instantiating entity behavior '{className}' for entity '{forEntity.Code}':\n{exception}", exception);
		}
	}

	public void RegisterBlockEntityType(string className, Type blockentity)
	{
		if (legacyBlockEntityClassNames.ContainsKey(className))
		{
			throw new ArgumentException("Classname '" + className + "' is a reserved name for backwards compatibility reasons. Please use another term.");
		}
		blockEntityClassnameToTypeMapping[className] = blockentity;
		blockEntityTypeToClassnameMapping[blockentity] = className;
	}

	public BlockEntity CreateBlockEntity(Type entityType)
	{
		if (!blockEntityClassnameToTypeMapping.ContainsValue(entityType))
		{
			throw new Exception($"Don't know how to instantiate entity of type '{entityType}' did you forget to register a mapping?");
		}
		try
		{
			return (BlockEntity)Activator.CreateInstance(entityType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating block entity '{entityType}':\n{exception}", exception);
		}
	}

	public BlockEntity CreateBlockEntity(string className)
	{
		if (legacyBlockEntityClassNames.ContainsKey(className))
		{
			className = legacyBlockEntityClassNames[className];
		}
		if (!blockEntityClassnameToTypeMapping.TryGetValue(className, out var beType))
		{
			throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
		}
		try
		{
			return (BlockEntity)Activator.CreateInstance(beType);
		}
		catch (Exception exception)
		{
			throw new Exception($"Error on instantiating block entity '{className}':\n{exception}", exception);
		}
	}

	public Type GetBlockEntityType(string className)
	{
		if (legacyBlockEntityClassNames.ContainsKey(className))
		{
			className = legacyBlockEntityClassNames[className];
		}
		if (!blockEntityClassnameToTypeMapping.TryGetValue(className, out var beType))
		{
			throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
		}
		return beType;
	}

	public void RegisterParticlePropertyProvider(string className, Type ParticleProvider)
	{
		if (!ParticleProviderTypeToClassnameMapping.ContainsKey(ParticleProvider))
		{
			ParticleProviderClassnameToTypeMapping.Add(className, ParticleProvider);
			ParticleProviderTypeToClassnameMapping.Add(ParticleProvider, className);
		}
	}

	public IParticlePropertiesProvider CreateParticlePropertyProvider(Type entityType)
	{
		if (!ParticleProviderClassnameToTypeMapping.ContainsValue(entityType))
		{
			throw new Exception($"Don't know how to instantiate entity of type '{entityType}' did you forget to register a mapping?");
		}
		return (IParticlePropertiesProvider)Activator.CreateInstance(entityType);
	}

	public IParticlePropertiesProvider CreateParticlePropertyProvider(string className)
	{
		if (!ParticleProviderClassnameToTypeMapping.TryGetValue(className, out var providerType))
		{
			throw new Exception("Don't know how to instantiate entity of type '" + className + "' did you forget to register a mapping?");
		}
		return (IParticlePropertiesProvider)Activator.CreateInstance(providerType);
	}

	private void RegisterDefaultParticlePropertyProviders()
	{
		RegisterParticlePropertyProvider("simple", typeof(SimpleParticleProperties));
		RegisterParticlePropertyProvider("advanced", typeof(AdvancedParticleProperties));
		RegisterParticlePropertyProvider("bubbles", typeof(AirBubbleParticles));
		RegisterParticlePropertyProvider("watersplash", typeof(WaterSplashParticles));
		RegisterParticlePropertyProvider("explosion", typeof(ExplosionSmokeParticles));
		RegisterParticlePropertyProvider("stackcubes", typeof(StackCubeParticles));
		RegisterParticlePropertyProvider("block", typeof(BlockCubeParticles));
		RegisterParticlePropertyProvider("entity", typeof(EntityCubeParticles));
		RegisterParticlePropertyProvider("blockbreaking", typeof(BlockBrokenParticleProps));
	}
}
