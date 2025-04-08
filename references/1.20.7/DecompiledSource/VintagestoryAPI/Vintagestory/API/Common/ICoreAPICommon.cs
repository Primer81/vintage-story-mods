using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

/// <summary>
/// API Methods common to the server and client
/// </summary>
public interface ICoreAPICommon
{
	/// <summary>
	/// Can be used to store non-persistent, game wide data. E.g. used for firewood piles to pregenerate all meshes only once during startup
	/// </summary>
	Dictionary<string, object> ObjectCache { get; }

	/// <summary>
	/// Returns the root path of the games data folder
	/// </summary>
	/// <value></value>
	string DataBasePath { get; }

	/// <summary>
	/// Registers a recipe registry
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="recipeRegistryCode"></param>
	/// <returns></returns>
	T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase;

	/// <summary>
	/// Registers a new color map. Typically used to color in-game blocks with a texture - i.e. climate and seasonal coloring
	/// </summary>
	void RegisterColorMap(ColorMap map);

	/// <summary>
	/// Registers a non-block entity. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className"></param>
	/// <param name="entity"></param>
	void RegisterEntity(string className, Type entity);

	/// <summary>
	/// Registers a non-block entity behavior. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className"></param>
	/// <param name="entityBehavior"></param>
	void RegisterEntityBehaviorClass(string className, Type entityBehavior);

	/// <summary>
	/// Register a new Blockclass. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className">Class to register</param>
	/// <param name="blockType">Name of the class</param>
	void RegisterBlockClass(string className, Type blockType);

	/// <summary>
	/// Register a new crop behavior
	/// </summary>
	/// <param name="className"></param>
	/// <param name="type"></param>
	void RegisterCropBehavior(string className, Type type);

	/// <summary>
	/// Register a new BlockEntity Class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className"></param>
	/// <param name="blockentityType"></param>
	void RegisterBlockEntityClass(string className, Type blockentityType);

	/// <summary>
	/// Register a new Item Class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className"></param>
	/// <param name="itemType"></param>
	void RegisterItemClass(string className, Type itemType);

	/// <summary>
	/// Registers a new collectible behavior class, which can be used for items or for blocks. 
	/// </summary>
	/// <param name="className"></param>
	/// <param name="blockBehaviorType"></param>
	void RegisterCollectibleBehaviorClass(string className, Type blockBehaviorType);

	/// <summary>
	/// Register a new block behavior class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className"></param>
	/// <param name="blockBehaviorType"></param>
	void RegisterBlockBehaviorClass(string className, Type blockBehaviorType);

	/// <summary>
	/// Register a new block entity behavior class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
	/// </summary>
	/// <param name="className"></param>
	/// <param name="blockEntityBehaviorType"></param>
	void RegisterBlockEntityBehaviorClass(string className, Type blockEntityBehaviorType);

	/// <summary>
	/// Register a new block behavior class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
	/// Make your your delegate also set tree.SetString("className", "[your className]");
	/// </summary>
	/// <param name="className"></param>
	/// <param name="mountableInstancer"></param>
	void RegisterMountable(string className, GetMountableDelegate mountableInstancer);

	/// <summary>
	/// Returns the path to given foldername inside the games data folder. Ensures that the folder exists
	/// </summary>
	/// <param name="foldername"></param>
	/// <returns></returns>
	string GetOrCreateDataPath(string foldername);

	/// <summary>
	/// Stores a config file in a prepared modconfig folder. You only need to provide a unique filename.
	/// For T just make a class with all fields public - this is your configuration class. Be sure to set useful default values for your settings
	/// Be aware that these configs are not synchronized between client and server. Each side will store their own copy of the configuration. If these configs affect gameplay, you will likely need to synchronize it via network packet
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="jsonSerializeableData"></param>
	/// <param name="filename"></param>
	void StoreModConfig<T>(T jsonSerializeableData, string filename);

	/// <summary>
	/// Stores a config file in a prepared modconfig folder. You only need to provide a unique filename.
	/// Be aware that these configs are not synchronized between client and server. Each side will store their own copy of the configuration. If these configs affect gameplay, you will likely need to synchronize it to client it via network packet
	/// </summary>
	/// <param name="jobj"></param>
	/// <param name="filename"></param>
	void StoreModConfig(JsonObject jobj, string filename);

	/// <summary>
	/// Loads a config file from a prepared modconfig folder, you only need to provide a unique filename. Recommendation: Surround this call with a try/catch in case the user made a typo while changing the configuration
	/// Be aware that these configs are not synchronized between client and server. Each side will store their own copy of the configuration. If these configs affect gameplay, you will likely need to synchronize it to client via network packet
	/// Returns null if the file does not exist
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="filename"></param>
	/// <returns></returns>
	T LoadModConfig<T>(string filename);

	/// <summary>
	/// Loads a config file from a prepared modconfig folder, you only need to provide a unique filename. Recommendation: Surround this call with a try/catch in case the user made a typo while changing the configuration
	/// Be aware that these configs are not synchronized between client and server. Each side will store their own copy of the configuration. If these configs affect gameplay, you will likely need to synchronize it to client via network packet
	/// Returns null if the file does not exist
	/// </summary>
	/// <param name="filename"></param>
	/// <returns></returns>
	JsonObject LoadModConfig(string filename);
}
