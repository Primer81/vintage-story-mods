using System;
using System.Collections.Generic;

namespace Vintagestory.API.Server;

public interface IPlayerDataManager
{
	/// <summary>
	/// Returns a copy of the player data dictionary loaded by the server. Thats the contents of Playerdata/playerdata.json
	/// </summary>
	Dictionary<string, IServerPlayerData> PlayerDataByUid { get; }

	/// <summary>
	/// Retrieve a players offline, world-agnostic data by player uid
	/// </summary>
	/// <param name="playerUid"></param>
	/// <returns></returns>
	IServerPlayerData GetPlayerDataByUid(string playerUid);

	/// <summary>
	/// Retrieve a players offline, world-agnostic data by his last known name
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	IServerPlayerData GetPlayerDataByLastKnownName(string name);

	/// <summary>
	/// Resolves a player name to a player uid, independent on whether this player is online, offline or never even joined the server. This is done by contacting the auth server, so please use this method sparingly.
	/// </summary>
	/// <param name="playername"></param>
	/// <param name="onPlayerReceived"></param>
	void ResolvePlayerName(string playername, Action<EnumServerResponse, string> onPlayerReceived);

	/// <summary>
	/// Resolves a player uid to a player name, independent on whether this player is online, offline or never even joined the server. This is done by contacting the auth server, so please use this method sparingly.
	/// </summary>
	/// <param name="playeruid"></param>
	/// <param name="onPlayerReceived"></param>
	void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> onPlayerReceived);
}
