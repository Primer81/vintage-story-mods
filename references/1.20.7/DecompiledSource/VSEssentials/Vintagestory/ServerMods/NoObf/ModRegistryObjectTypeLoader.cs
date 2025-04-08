using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

public class ModRegistryObjectTypeLoader : ModSystem
{
	public Dictionary<AssetLocation, StandardWorldProperty> worldProperties;

	public Dictionary<AssetLocation, VariantEntry[]> worldPropertiesVariants;

	private Dictionary<AssetLocation, RegistryObjectType> blockTypes;

	private Dictionary<AssetLocation, RegistryObjectType> itemTypes;

	private Dictionary<AssetLocation, RegistryObjectType> entityTypes;

	private List<RegistryObjectType>[] itemVariants;

	private List<RegistryObjectType>[] blockVariants;

	private List<RegistryObjectType>[] entityVariants;

	private ICoreServerAPI api;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.2;
	}

	public override void AssetsLoaded(ICoreAPI coreApi)
	{
		if (!(coreApi is ICoreServerAPI api))
		{
			return;
		}
		this.api = api;
		api.Logger.VerboseDebug("Starting to gather blocktypes, itemtypes and entities");
		LoadWorldProperties();
		int maxThreads = (api.Server.IsDedicated ? 3 : 8);
		int threads = GameMath.Clamp(Environment.ProcessorCount / 2 - 2, 1, maxThreads);
		itemTypes = new Dictionary<AssetLocation, RegistryObjectType>();
		blockTypes = new Dictionary<AssetLocation, RegistryObjectType>();
		entityTypes = new Dictionary<AssetLocation, RegistryObjectType>();
		foreach (KeyValuePair<AssetLocation, JObject> entry3 in api.Assets.GetMany<JObject>(api.Server.Logger, "itemtypes/"))
		{
			if (entry3.Key.Path.EndsWithOrdinal(".json"))
			{
				try
				{
					ItemType et3 = new ItemType();
					et3.CreateBasetype(api, entry3.Key.Path, entry3.Key.Domain, entry3.Value);
					itemTypes.Add(entry3.Key, et3);
				}
				catch (Exception e3)
				{
					api.World.Logger.Error("Item type {0} could not be loaded. Will ignore. Exception thrown:", entry3.Key);
					api.World.Logger.Error(e3);
				}
			}
		}
		itemVariants = new List<RegistryObjectType>[itemTypes.Count];
		api.Logger.VerboseDebug("Starting parsing ItemTypes in " + threads + " threads");
		PrepareForLoading(threads);
		foreach (KeyValuePair<AssetLocation, JObject> entry2 in api.Assets.GetMany<JObject>(api.Server.Logger, "entities/"))
		{
			if (entry2.Key.Path.EndsWithOrdinal(".json"))
			{
				try
				{
					EntityType et2 = new EntityType();
					et2.CreateBasetype(api, entry2.Key.Path, entry2.Key.Domain, entry2.Value);
					entityTypes.Add(entry2.Key, et2);
				}
				catch (Exception e2)
				{
					api.World.Logger.Error("Entity type {0} could not be loaded. Will ignore. Exception thrown:", entry2.Key);
					api.World.Logger.Error(e2);
				}
			}
		}
		entityVariants = new List<RegistryObjectType>[entityTypes.Count];
		foreach (KeyValuePair<AssetLocation, JObject> entry in api.Assets.GetMany<JObject>(api.Server.Logger, "blocktypes/"))
		{
			if (entry.Key.Path.EndsWithOrdinal(".json"))
			{
				try
				{
					BlockType et = new BlockType();
					et.CreateBasetype(api, entry.Key.Path, entry.Key.Domain, entry.Value);
					blockTypes.Add(entry.Key, et);
				}
				catch (Exception e)
				{
					api.World.Logger.Error("Block type {0} could not be loaded. Will ignore. Exception thrown:", entry.Key);
					api.World.Logger.Error(e);
				}
			}
		}
		blockVariants = new List<RegistryObjectType>[blockTypes.Count];
		TyronThreadPool.QueueTask(GatherAllTypes_Async, "gatheralltypes");
		api.Logger.StoryEvent(Lang.Get("It remembers..."));
		api.Logger.VerboseDebug("Gathered all types, starting to load items");
		LoadItems(itemVariants);
		api.Logger.VerboseDebug("Parsed and loaded items");
		api.Logger.StoryEvent(Lang.Get("...all that came before"));
		LoadBlocks(blockVariants);
		api.Logger.VerboseDebug("Parsed and loaded blocks");
		LoadEntities(entityVariants);
		api.Logger.VerboseDebug("Parsed and loaded entities");
		api.Server.LogNotification("BlockLoader: Entities, Blocks and Items loaded");
		FreeRam();
		api.TriggerOnAssetsFirstLoaded();
	}

	private void LoadWorldProperties()
	{
		worldProperties = new Dictionary<AssetLocation, StandardWorldProperty>();
		foreach (KeyValuePair<AssetLocation, StandardWorldProperty> entry in api.Assets.GetMany<StandardWorldProperty>(api.Server.Logger, "worldproperties/"))
		{
			AssetLocation loc = entry.Key.Clone();
			loc.Path = loc.Path.Replace("worldproperties/", "");
			loc.RemoveEnding();
			entry.Value.Code.Domain = entry.Key.Domain;
			worldProperties.Add(loc, entry.Value);
		}
		worldPropertiesVariants = new Dictionary<AssetLocation, VariantEntry[]>();
		foreach (KeyValuePair<AssetLocation, StandardWorldProperty> val in worldProperties)
		{
			if (val.Value == null)
			{
				continue;
			}
			WorldPropertyVariant[] variants = val.Value.Variants;
			if (variants == null)
			{
				continue;
			}
			if (val.Value.Code == null)
			{
				api.Server.LogError("Error in worldproperties {0}, code is null, so I won't load it", val.Key);
				continue;
			}
			worldPropertiesVariants[val.Value.Code] = new VariantEntry[variants.Length];
			for (int i = 0; i < variants.Length; i++)
			{
				if (variants[i].Code == null)
				{
					api.Server.LogError("Error in worldproperties {0}, variant {1}, code is null, so I won't load it", val.Key, i);
					worldPropertiesVariants[val.Value.Code] = worldPropertiesVariants[val.Value.Code].RemoveEntry(i);
				}
				else
				{
					worldPropertiesVariants[val.Value.Code][i] = new VariantEntry
					{
						Code = variants[i].Code.Path
					};
				}
			}
		}
	}

	private void LoadEntities(List<RegistryObjectType>[] variantLists)
	{
		LoadFromVariants(variantLists, "entitie", delegate(List<RegistryObjectType> variants)
		{
			foreach (EntityType entityType in variants)
			{
				api.RegisterEntityClass(entityType.Class, entityType.CreateProperties());
			}
		});
	}

	private void LoadItems(List<RegistryObjectType>[] variantLists)
	{
		LoadFromVariants(variantLists, "item", delegate(List<RegistryObjectType> variants)
		{
			foreach (ItemType variant in variants)
			{
				Item item = variant.CreateItem(api);
				try
				{
					api.RegisterItem(item);
				}
				catch (Exception e)
				{
					api.Server.Logger.Error("Failed registering item {0}:", item.Code);
					api.Server.Logger.Error(e);
				}
			}
		});
	}

	private void LoadBlocks(List<RegistryObjectType>[] variantLists)
	{
		LoadFromVariants(variantLists, "block", delegate(List<RegistryObjectType> variants)
		{
			foreach (BlockType variant in variants)
			{
				Block block = variant.CreateBlock(api);
				try
				{
					api.RegisterBlock(block);
				}
				catch (Exception e)
				{
					api.Server.Logger.Error("Failed registering block {0}", block.Code);
					api.Server.Logger.Error(e);
				}
			}
		});
	}

	private void PrepareForLoading(int threadsCount)
	{
		for (int i = 0; i < threadsCount; i++)
		{
			TyronThreadPool.QueueTask(GatherAllTypes_Async, "gatheralltypes" + i);
		}
	}

	private void GatherAllTypes_Async()
	{
		GatherTypes_Async(itemVariants, itemTypes);
		int timeOut = 1000;
		bool logged = false;
		while (blockVariants == null)
		{
			if (--timeOut == 0)
			{
				return;
			}
			if (!logged)
			{
				api.Logger.VerboseDebug("Waiting for entityTypes to be gathered");
				logged = true;
			}
			Thread.Sleep(10);
		}
		if (logged)
		{
			api.Logger.VerboseDebug("EntityTypes now all gathered");
		}
		GatherTypes_Async(blockVariants, blockTypes);
		timeOut = 1000;
		logged = false;
		while (entityVariants == null)
		{
			if (--timeOut == 0)
			{
				return;
			}
			if (!logged)
			{
				api.Logger.VerboseDebug("Waiting for blockTypes to be gathered");
				logged = true;
			}
			Thread.Sleep(10);
		}
		if (logged)
		{
			api.Logger.VerboseDebug("BlockTypes now all gathered");
		}
		GatherTypes_Async(entityVariants, entityTypes);
	}

	private void GatherTypes_Async(List<RegistryObjectType>[] resolvedTypeLists, Dictionary<AssetLocation, RegistryObjectType> baseTypes)
	{
		int i = 0;
		foreach (RegistryObjectType val in baseTypes.Values)
		{
			if (AsyncHelper.CanProceedOnThisThread(ref val.parseStarted))
			{
				List<RegistryObjectType> resolvedTypes = new List<RegistryObjectType>();
				try
				{
					if (val.Enabled)
					{
						GatherVariantsAndPopulate(val, resolvedTypes);
					}
				}
				finally
				{
					resolvedTypeLists[i] = resolvedTypes;
				}
			}
			i++;
		}
	}

	private void GatherVariantsAndPopulate(RegistryObjectType baseType, List<RegistryObjectType> typesResolved)
	{
		List<ResolvedVariant> variants = null;
		if (baseType.VariantGroups != null && baseType.VariantGroups.Length != 0)
		{
			try
			{
				variants = GatherVariants(baseType.Code, baseType.VariantGroups, baseType.Code, baseType.AllowedVariants, baseType.SkipVariants);
			}
			catch (Exception e)
			{
				api.Server.Logger.Error("Exception thrown while trying to gather all variants of the block/item/entity type with code {0}. May lead to the whole type being ignored. Exception:", baseType.Code);
				api.Server.Logger.Error(e);
				return;
			}
		}
		JsonSerializer deserializer = JsonUtil.CreateSerializerForDomain(baseType.Code.Domain);
		if (variants == null || variants.Count == 0)
		{
			RegistryObjectType resolvedType = baseType.CreateAndPopulate(api, baseType.Code.Clone(), baseType.jsonObject, deserializer, new OrderedDictionary<string, string>());
			typesResolved.Add(resolvedType);
		}
		else
		{
			int count = 1;
			foreach (ResolvedVariant variant in variants)
			{
				JObject jobject = ((count++ == variants.Count) ? baseType.jsonObject : (baseType.jsonObject.DeepClone() as JObject));
				RegistryObjectType resolvedType2 = baseType.CreateAndPopulate(api, variant.Code, jobject, deserializer, variant.CodeParts);
				typesResolved.Add(resolvedType2);
			}
		}
		baseType.jsonObject = null;
	}

	private void LoadFromVariants(List<RegistryObjectType>[] variantLists, string typeForLog, Action<List<RegistryObjectType>> register)
	{
		int count = 0;
		for (int i = 0; i < variantLists.Length; i++)
		{
			List<RegistryObjectType> variants;
			for (variants = variantLists[i]; variants == null; variants = variantLists[i])
			{
				Thread.Sleep(10);
			}
			count += variants.Count;
			register(variants);
		}
		api.Server.LogNotification("Loaded " + count + " unique " + typeForLog + "s");
	}

	public StandardWorldProperty GetWorldPropertyByCode(AssetLocation code)
	{
		StandardWorldProperty property = null;
		worldProperties.TryGetValue(code, out property);
		return property;
	}

	private List<ResolvedVariant> GatherVariants(AssetLocation baseCode, RegistryObjectVariantGroup[] variantgroups, AssetLocation location, AssetLocation[] allowedVariants, AssetLocation[] skipVariants)
	{
		List<ResolvedVariant> variantsFinal = new List<ResolvedVariant>();
		OrderedDictionary<string, VariantEntry[]> variantsMul = new OrderedDictionary<string, VariantEntry[]>();
		for (int j = 0; j < variantgroups.Length; j++)
		{
			if (variantgroups[j].LoadFromProperties != null)
			{
				CollectFromWorldProperties(variantgroups[j], variantgroups, variantsMul, variantsFinal, location);
			}
			if (variantgroups[j].LoadFromPropertiesCombine != null)
			{
				CollectFromWorldPropertiesCombine(variantgroups[j].LoadFromPropertiesCombine, variantgroups[j], variantgroups, variantsMul, variantsFinal, location);
			}
			if (variantgroups[j].States != null)
			{
				CollectFromStateList(variantgroups[j], variantgroups, variantsMul, variantsFinal, location);
			}
		}
		VariantEntry[,] variants = MultiplyProperties(variantsMul.Values.ToArray());
		for (int i = 0; i < variants.GetLength(0); i++)
		{
			ResolvedVariant resolved = new ResolvedVariant();
			for (int k = 0; k < variants.GetLength(1); k++)
			{
				VariantEntry variant = variants[i, k];
				if (variant.Codes != null)
				{
					for (int l = 0; l < variant.Codes.Count; l++)
					{
						resolved.CodeParts.Add(variant.Types[l], variant.Codes[l]);
					}
				}
				else
				{
					resolved.CodeParts.Add(variantsMul.GetKeyAtIndex(k), variant.Code);
				}
			}
			variantsFinal.Add(resolved);
		}
		foreach (ResolvedVariant item in variantsFinal)
		{
			item.ResolveCode(baseCode);
		}
		if (skipVariants != null)
		{
			List<ResolvedVariant> filteredVariants2 = new List<ResolvedVariant>();
			HashSet<AssetLocation> skipVariantsHash = new HashSet<AssetLocation>();
			List<AssetLocation> skipVariantsWildCards = new List<AssetLocation>();
			AssetLocation[] array = skipVariants;
			foreach (AssetLocation val2 in array)
			{
				if (val2.IsWildCard)
				{
					skipVariantsWildCards.Add(val2);
				}
				else
				{
					skipVariantsHash.Add(val2);
				}
			}
			foreach (ResolvedVariant var in variantsFinal)
			{
				if (!skipVariantsHash.Contains(var.Code) && !(skipVariantsWildCards.FirstOrDefault((AssetLocation v) => WildcardUtil.Match(v, var.Code)) != null))
				{
					filteredVariants2.Add(var);
				}
			}
			variantsFinal = filteredVariants2;
		}
		if (allowedVariants != null)
		{
			List<ResolvedVariant> filteredVariants = new List<ResolvedVariant>();
			HashSet<AssetLocation> allowVariantsHash = new HashSet<AssetLocation>();
			List<AssetLocation> allowVariantsWildCards = new List<AssetLocation>();
			AssetLocation[] array = allowedVariants;
			foreach (AssetLocation val in array)
			{
				if (val.IsWildCard)
				{
					allowVariantsWildCards.Add(val);
				}
				else
				{
					allowVariantsHash.Add(val);
				}
			}
			foreach (ResolvedVariant var2 in variantsFinal)
			{
				if (allowVariantsHash.Contains(var2.Code) || allowVariantsWildCards.FirstOrDefault((AssetLocation v) => WildcardUtil.Match(v, var2.Code)) != null)
				{
					filteredVariants.Add(var2);
				}
			}
			variantsFinal = filteredVariants;
		}
		return variantsFinal;
	}

	private void CollectFromStateList(RegistryObjectVariantGroup variantGroup, RegistryObjectVariantGroup[] variantgroups, OrderedDictionary<string, VariantEntry[]> variantsMul, List<ResolvedVariant> blockvariantsFinal, AssetLocation filename)
	{
		if (variantGroup.Code == null)
		{
			api.Server.LogError("Error in itemtype {0}, a variantgroup using a state list must have a code. Ignoring.", filename);
			return;
		}
		string[] states = variantGroup.States;
		string type = variantGroup.Code;
		if (variantGroup.Combine == EnumCombination.Add)
		{
			for (int l = 0; l < states.Length; l++)
			{
				ResolvedVariant resolved = new ResolvedVariant();
				resolved.CodeParts.Add(type, states[l]);
				blockvariantsFinal.Add(resolved);
			}
		}
		if (variantGroup.Combine != EnumCombination.Multiply)
		{
			return;
		}
		List<VariantEntry> stateList = new List<VariantEntry>();
		for (int k = 0; k < states.Length; k++)
		{
			stateList.Add(new VariantEntry
			{
				Code = states[k]
			});
		}
		foreach (RegistryObjectVariantGroup cvg in variantgroups)
		{
			if (cvg.Combine != EnumCombination.SelectiveMultiply || !(cvg.OnVariant == variantGroup.Code))
			{
				continue;
			}
			for (int m = 0; m < stateList.Count; m++)
			{
				VariantEntry old = stateList[m];
				if (!(cvg.Code != old.Code))
				{
					stateList.RemoveAt(m);
					for (int j = 0; j < cvg.States.Length; j++)
					{
						List<string> codes = old.Codes ?? new List<string> { old.Code };
						List<string> types = old.Types ?? new List<string> { variantGroup.Code };
						string state = cvg.States[j];
						codes.Add(state);
						types.Add(cvg.Code);
						stateList.Insert(m, new VariantEntry
						{
							Code = ((state.Length == 0) ? old.Code : (old.Code + "-" + state)),
							Codes = codes,
							Types = types
						});
					}
				}
			}
		}
		if (variantsMul.ContainsKey(type))
		{
			stateList.AddRange(variantsMul[type]);
			variantsMul[type] = stateList.ToArray();
		}
		else
		{
			variantsMul.Add(type, stateList.ToArray());
		}
	}

	private void CollectFromWorldProperties(RegistryObjectVariantGroup variantGroup, RegistryObjectVariantGroup[] variantgroups, OrderedDictionary<string, VariantEntry[]> blockvariantsMul, List<ResolvedVariant> blockvariantsFinal, AssetLocation location)
	{
		CollectFromWorldPropertiesCombine(new AssetLocation[1] { variantGroup.LoadFromProperties }, variantGroup, variantgroups, blockvariantsMul, blockvariantsFinal, location);
	}

	private void CollectFromWorldPropertiesCombine(AssetLocation[] propList, RegistryObjectVariantGroup variantGroup, RegistryObjectVariantGroup[] variantgroups, OrderedDictionary<string, VariantEntry[]> blockvariantsMul, List<ResolvedVariant> blockvariantsFinal, AssetLocation location)
	{
		if (propList.Length > 1 && variantGroup.Code == null)
		{
			api.Server.LogError("Error in item or block {0}, defined a variantgroup with loadFromPropertiesCombine (first element: {1}), but did not explicitly declare a code for this variant group, hence I do not know which code to use. Ignoring.", location, propList[0]);
			return;
		}
		foreach (AssetLocation val in propList)
		{
			StandardWorldProperty property = GetWorldPropertyByCode(val);
			if (property == null)
			{
				api.Server.LogError("Error in item or block {0}, worldproperty {1} does not exist (or is empty). Ignoring.", location, variantGroup.LoadFromProperties);
				break;
			}
			string typename = ((variantGroup.Code == null) ? property.Code.Path : variantGroup.Code);
			if (variantGroup.Combine == EnumCombination.Add)
			{
				WorldPropertyVariant[] variants2 = property.Variants;
				foreach (WorldPropertyVariant variant in variants2)
				{
					ResolvedVariant resolved = new ResolvedVariant();
					resolved.CodeParts.Add(typename, variant.Code.Path);
					blockvariantsFinal.Add(resolved);
				}
			}
			if (variantGroup.Combine == EnumCombination.Multiply)
			{
				if (blockvariantsMul.TryGetValue(typename, out var variants))
				{
					blockvariantsMul[typename] = variants.Append(worldPropertiesVariants[property.Code]);
				}
				else
				{
					blockvariantsMul.Add(typename, worldPropertiesVariants[property.Code]);
				}
			}
		}
	}

	private VariantEntry[,] MultiplyProperties(VariantEntry[][] variants)
	{
		int resultingQuantiy = 1;
		for (int j = 0; j < variants.Length; j++)
		{
			resultingQuantiy *= variants[j].Length;
		}
		VariantEntry[,] multipliedProperties = new VariantEntry[resultingQuantiy, variants.Length];
		for (int i = 0; i < resultingQuantiy; i++)
		{
			int index = i;
			for (int k = 0; k < variants.Length; k++)
			{
				int variantLength = variants[k].Length;
				VariantEntry variant = variants[k][index % variantLength];
				multipliedProperties[i, k] = new VariantEntry
				{
					Code = variant.Code,
					Codes = variant.Codes,
					Types = variant.Types
				};
				index /= variantLength;
			}
		}
		return multipliedProperties;
	}

	private void FreeRam()
	{
		blockTypes = null;
		blockVariants = null;
		itemTypes = null;
		itemVariants = null;
		entityTypes = null;
		entityVariants = null;
		worldProperties = null;
		worldPropertiesVariants = null;
	}
}
