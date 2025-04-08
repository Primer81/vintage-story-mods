using System.Collections.Generic;

namespace Vintagestory.API.Server;

public interface IGroupManager
{
	Dictionary<int, PlayerGroup> PlayerGroupsById { get; }

	PlayerGroup GetPlayerGroupByName(string name);

	void AddPlayerGroup(PlayerGroup group);

	void RemovePlayerGroup(PlayerGroup group);
}
