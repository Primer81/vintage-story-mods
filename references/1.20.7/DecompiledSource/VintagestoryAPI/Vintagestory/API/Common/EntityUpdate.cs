using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public class EntityUpdate
{
	/// <summary>
	/// If set this entity was spawned or Moved (position needs to be set too)
	/// </summary>
	public long EntityId = -1L;

	/// <summary>
	/// If set this entity needs to be spawned
	/// </summary>
	public EntityProperties EntityProperties;

	/// <summary>
	/// If set the entity was moved
	/// </summary>
	public EntityPos OldPosition;

	public EntityPos NewPosition;
}
