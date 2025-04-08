using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public class ClassRegistryAPI : IClassRegistryAPI
{
	private IWorldAccessor world;

	internal ClassRegistry registry;

	public Dictionary<string, Type> BlockClassToTypeMapping => registry.BlockClassToTypeMapping;

	public Dictionary<string, Type> ItemClassToTypeMapping => registry.ItemClassToTypeMapping;

	public ClassRegistryAPI(IWorldAccessor world, ClassRegistry registry)
	{
		this.world = world;
		this.registry = registry;
	}

	public Block CreateBlock(string blockclass)
	{
		return registry.CreateBlock(blockclass);
	}

	public BlockBehavior CreateBlockBehavior(Block block, string blockclass)
	{
		return registry.CreateBlockBehavior(block, blockclass);
	}

	public Type GetBlockBehaviorClass(string blockclass)
	{
		Type type = null;
		registry.blockbehaviorToTypeMapping.TryGetValue(blockclass, out type);
		return type;
	}

	public Item CreateItem(string itemclass)
	{
		return registry.CreateItem(itemclass);
	}

	public IAttribute CreateItemstackAttribute(ItemStack itemstack = null)
	{
		return new ItemstackAttribute(itemstack);
	}

	public IAttribute CreateStringAttribute(string value = null)
	{
		return new StringAttribute(value);
	}

	public ITreeAttribute CreateTreeAttribute()
	{
		return new TreeAttribute();
	}

	public JsonTreeAttribute CreateJsonTreeAttributeFromDict(Dictionary<string, JsonTreeAttribute> attributes)
	{
		JsonTreeAttribute tree = new JsonTreeAttribute();
		if (attributes == null)
		{
			return tree;
		}
		tree.type = EnumAttributeType.Tree;
		foreach (KeyValuePair<string, JsonTreeAttribute> val in attributes)
		{
			tree.elems[val.Key] = val.Value.Clone();
		}
		return tree;
	}

	public Entity CreateEntity(string entityClass)
	{
		return registry.CreateEntity(entityClass);
	}

	public Entity CreateEntity(EntityProperties entityType)
	{
		Entity entity = registry.CreateEntity(entityType.Class);
		entity.Code = entityType.Code;
		return entity;
	}

	public BlockEntity CreateBlockEntity(string blockEntityClass)
	{
		return registry.CreateBlockEntity(blockEntityClass);
	}

	public Type GetBlockEntity(string blockEntityClass)
	{
		Type type = null;
		registry.blockEntityClassnameToTypeMapping.TryGetValue(blockEntityClass, out type);
		return type;
	}

	public EntityBehavior CreateEntityBehavior(Entity forEntity, string entityBehaviorName)
	{
		return registry.CreateEntityBehavior(forEntity, entityBehaviorName);
	}

	public string GetBlockEntityClass(Type type)
	{
		string classsName = null;
		registry.blockEntityTypeToClassnameMapping.TryGetValue(type, out classsName);
		return classsName;
	}

	public CropBehavior CreateCropBehavior(Block forBlock, string cropBehaviorName)
	{
		return registry.createCropBehavior(forBlock, cropBehaviorName);
	}

	public IInventoryNetworkUtil CreateInvNetworkUtil(InventoryBase inv, ICoreAPI api)
	{
		return new InventoryNetworkUtil(inv, api);
	}

	public IMountableSeat GetMountable(TreeAttribute tree)
	{
		return registry.GetMountable(world, tree);
	}

	public Type GetBlockClass(string blockclass)
	{
		return registry.GetBlockClass(blockclass);
	}

	public Type GetItemClass(string itemClass)
	{
		return registry.GetItemClass(itemClass);
	}

	public string GetBlockBehaviorClassName(Type blockBehaviorType)
	{
		return registry.GetBlockBehaviorClassName(blockBehaviorType);
	}

	public string GetEntityClassName(Type entityType)
	{
		return registry.GetEntityClassName(entityType);
	}

	public Type GetBlockEntityBehaviorClass(string name)
	{
		registry.blockentitybehaviorToTypeMapping.TryGetValue(name, out var t);
		return t;
	}

	public BlockEntityBehavior CreateBlockEntityBehavior(BlockEntity blockEntity, string name)
	{
		return registry.CreateBlockEntityBehavior(blockEntity, name);
	}

	public Type GetEntityBehaviorClass(string entityBehaviorName)
	{
		return registry.GetEntityBehaviorClass(entityBehaviorName);
	}

	public CollectibleBehavior CreateCollectibleBehavior(CollectibleObject forCollectible, string code)
	{
		return registry.CreateCollectibleBehavior(forCollectible, code);
	}

	public Type GetCollectibleBehaviorClass(string code)
	{
		return registry.GetCollectibleBehaviorClass(code);
	}

	public string GetCollectibleBehaviorClassName(Type type)
	{
		return registry.GetCollectibleBehaviorClassName(type);
	}

	public void RegisterParticlePropertyProvider(string className, Type ParticleProvider)
	{
		registry.RegisterParticlePropertyProvider(className, ParticleProvider);
	}

	public IParticlePropertiesProvider CreateParticlePropertyProvider(Type entityType)
	{
		return registry.CreateParticlePropertyProvider(entityType);
	}

	public IParticlePropertiesProvider CreateParticlePropertyProvider(string className)
	{
		return registry.CreateParticlePropertyProvider(className);
	}
}
