using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

/// <summary>
/// Common API Components that are available on the server and the client. Cast to ICoreServerAPI or ICoreClientAPI to access side specific features.
/// </summary>
public interface ICoreAPI : ICoreAPICommon
{
	/// <summary>
	/// The local Logger instance.
	/// </summary>
	ILogger Logger { get; }

	/// <summary>
	/// The command line arguments that were used to start the client or server application
	/// </summary>
	string[] CmdlArguments { get; }

	IChatCommandApi ChatCommands { get; }

	/// <summary>
	/// Returns if you are currently on server or on client
	/// </summary>
	EnumAppSide Side { get; }

	/// <summary>
	/// Api component to register/trigger events
	/// </summary>
	IEventAPI Event { get; }

	/// <summary>
	/// Second API Component for access/modify everything game world related
	/// </summary>
	IWorldAccessor World { get; }

	/// <summary>
	/// API Compoment for creating instances of certain classes, such as Itemstacks
	/// </summary>
	IClassRegistryAPI ClassRegistry { get; }

	/// <summary>
	/// API for sending/receiving network packets
	/// </summary>
	INetworkAPI Network { get; }

	/// <summary>
	/// API Component for loading and reloading one or multiple assets at once from the assets folder
	/// </summary>
	IAssetManager Assets { get; }

	/// <summary>
	/// API Component for checking for and interacting with other mods and mod systems
	/// </summary>
	IModLoader ModLoader { get; }

	/// <summary>
	/// Registers a new entity config for given entity class
	/// </summary>
	/// <param name="entityClassName"></param>
	/// <param name="config"></param>
	void RegisterEntityClass(string entityClassName, EntityProperties config);
}
