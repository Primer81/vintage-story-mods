using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

/// <summary>
/// This method needs to find your mountable based on the tree attribute data, which you can write to in IMountable.MountableToTreeAttributes()
/// For example if its an entity, you will want to store the entity id, then this method can simply contain <code>return world.GetEntityById(tree.GetLong("entityId"));</code>
/// </summary>
/// <param name="world"></param>
/// <param name="tree"></param>
/// <returns></returns>
public delegate IMountableSeat GetMountableDelegate(IWorldAccessor world, TreeAttribute tree);
