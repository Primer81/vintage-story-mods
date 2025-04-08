using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public abstract class RegistryObjectType
{
	internal volatile int parseStarted;

	[DocumentAsJson]
	public bool Enabled = true;

	public JObject jsonObject;

	[DocumentAsJson]
	public AssetLocation Code;

	[DocumentAsJson]
	public RegistryObjectVariantGroup[] VariantGroups;

	public OrderedDictionary<string, string> Variant = new OrderedDictionary<string, string>();

	[JsonProperty]
	public WorldInteraction[] Interactions;

	[JsonProperty]
	public AssetLocation[] SkipVariants;

	[JsonProperty]
	public AssetLocation[] AllowedVariants;

	public HashSet<AssetLocation> AllowedVariantsQuickLookup = new HashSet<AssetLocation>();

	[JsonProperty]
	public string Class;

	public bool WildCardMatch(AssetLocation[] wildcards)
	{
		foreach (AssetLocation wildcard in wildcards)
		{
			if (WildCardMatch(wildcard))
			{
				return true;
			}
		}
		return false;
	}

	public bool WildCardMatch(AssetLocation wildCard)
	{
		if (wildCard == Code)
		{
			return true;
		}
		if (Code == null || wildCard.Domain != Code.Domain)
		{
			return false;
		}
		string pattern = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
		return Regex.IsMatch(Code.Path, "^" + pattern + "$", RegexOptions.IgnoreCase);
	}

	public static bool WildCardMatches(string blockCode, List<string> wildCards, out string matchingWildcard)
	{
		foreach (string wildcard in wildCards)
		{
			if (WildcardUtil.Match(wildcard, blockCode))
			{
				matchingWildcard = wildcard;
				return true;
			}
		}
		matchingWildcard = null;
		return false;
	}

	public static bool WildCardMatch(AssetLocation wildCard, AssetLocation blockCode)
	{
		if (wildCard == blockCode)
		{
			return true;
		}
		string pattern = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
		return Regex.IsMatch(blockCode.Path, "^" + pattern + "$", RegexOptions.IgnoreCase);
	}

	public static bool WildCardMatches(AssetLocation blockCode, List<AssetLocation> wildCards, out AssetLocation matchingWildcard)
	{
		foreach (AssetLocation wildcard in wildCards)
		{
			if (WildCardMatch(wildcard, blockCode))
			{
				matchingWildcard = wildcard;
				return true;
			}
		}
		matchingWildcard = null;
		return false;
	}

	internal virtual void CreateBasetype(ICoreAPI api, string filepathForLogging, string entryDomain, JObject entityTypeObject)
	{
		loadInherits(api, ref entityTypeObject, entryDomain, filepathForLogging);
		AssetLocation location;
		try
		{
			location = entityTypeObject.GetValue("code", StringComparison.InvariantCultureIgnoreCase).ToObject<AssetLocation>(entryDomain);
		}
		catch (Exception e)
		{
			throw new Exception("Asset has no valid code property. Will ignore. Exception thrown:-", e);
		}
		Code = location;
		if (entityTypeObject.TryGetValue("variantgroups", StringComparison.InvariantCultureIgnoreCase, out JToken property))
		{
			VariantGroups = property.ToObject<RegistryObjectVariantGroup[]>();
			entityTypeObject.Remove(property.Path);
		}
		if (entityTypeObject.TryGetValue("skipVariants", StringComparison.InvariantCultureIgnoreCase, out property))
		{
			SkipVariants = property.ToObject<AssetLocation[]>(entryDomain);
			entityTypeObject.Remove(property.Path);
		}
		if (entityTypeObject.TryGetValue("allowedVariants", StringComparison.InvariantCultureIgnoreCase, out property))
		{
			AllowedVariants = property.ToObject<AssetLocation[]>(entryDomain);
			entityTypeObject.Remove(property.Path);
		}
		if (entityTypeObject.TryGetValue("enabled", StringComparison.InvariantCultureIgnoreCase, out property))
		{
			Enabled = property.ToObject<bool>();
			entityTypeObject.Remove(property.Path);
		}
		else
		{
			Enabled = true;
		}
		jsonObject = entityTypeObject;
	}

	private void loadInherits(ICoreAPI api, ref JObject entityTypeObject, string entryDomain, string parentFileNameForLogging)
	{
		if (!entityTypeObject.TryGetValue("inheritFrom", StringComparison.InvariantCultureIgnoreCase, out JToken iftok))
		{
			return;
		}
		AssetLocation inheritFrom = iftok.ToObject<AssetLocation>(entryDomain).WithPathAppendixOnce(".json");
		IAsset asset = api.Assets.TryGet(inheritFrom);
		if (asset != null)
		{
			try
			{
				JObject inheritedObj = JObject.Parse(asset.ToText());
				loadInherits(api, ref inheritedObj, entryDomain, inheritFrom.ToShortString());
				inheritedObj.Merge(entityTypeObject, new JsonMergeSettings
				{
					MergeArrayHandling = MergeArrayHandling.Replace,
					PropertyNameComparison = StringComparison.InvariantCultureIgnoreCase
				});
				entityTypeObject = inheritedObj;
				entityTypeObject.Remove("inheritFrom");
				return;
			}
			catch (Exception e)
			{
				api.Logger.Error(Lang.Get("File {0} wants to inherit from {1}, but this is not valid json. Exception: {2}.", parentFileNameForLogging, inheritFrom, e));
				return;
			}
		}
		api.Logger.Error(Lang.Get("File {0} wants to inherit from {1}, but this file does not exist. Will ignore.", parentFileNameForLogging, inheritFrom));
	}

	internal virtual RegistryObjectType CreateAndPopulate(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant)
	{
		return this;
	}

	protected T CreateResolvedType<T>(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant) where T : RegistryObjectType, new()
	{
		T resolvedType = new T
		{
			Code = Code,
			VariantGroups = VariantGroups,
			Enabled = Enabled,
			jsonObject = jobject,
			Variant = variant
		};
		try
		{
			solveByType(jobject, fullcode.Path, variant);
		}
		catch (Exception e2)
		{
			api.Server.Logger.Error("Exception thrown while trying to resolve *byType properties of type {0}, variant {1}. Will ignore most of the attributes. Exception thrown:", Code, fullcode);
			api.Server.Logger.Error(e2);
		}
		try
		{
			JsonUtil.PopulateObject(resolvedType, jobject, deserializer);
		}
		catch (Exception e)
		{
			api.Server.Logger.Error("Exception thrown while trying to parse json data of the type with code {0}, variant {1}. Will ignore most of the attributes. Exception:", Code, fullcode);
			api.Server.Logger.Error(e);
		}
		resolvedType.Code = fullcode;
		resolvedType.jsonObject = null;
		return resolvedType;
	}

	protected static void solveByType(JToken json, string codePath, OrderedDictionary<string, string> searchReplace)
	{
		if (json is JObject jsonObj)
		{
			List<string> propertiesToRemove = null;
			Dictionary<string, JToken> propertiesToAdd = null;
			foreach (KeyValuePair<string, JToken> entry in jsonObj)
			{
				if (!entry.Key.EndsWith("byType", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string trueKey = entry.Key.Substring(0, entry.Key.Length - "byType".Length);
				foreach (KeyValuePair<string, JToken> byTypeProperty in entry.Value as JObject)
				{
					if (WildcardUtil.Match(byTypeProperty.Key, codePath))
					{
						JToken typedToken = byTypeProperty.Value;
						if (propertiesToAdd == null)
						{
							propertiesToAdd = new Dictionary<string, JToken>();
						}
						propertiesToAdd.Add(trueKey, typedToken);
						break;
					}
				}
				if (propertiesToRemove == null)
				{
					propertiesToRemove = new List<string>();
				}
				propertiesToRemove.Add(entry.Key);
			}
			if (propertiesToRemove != null)
			{
				foreach (string property2 in propertiesToRemove)
				{
					jsonObj.Remove(property2);
				}
				if (propertiesToAdd != null)
				{
					foreach (KeyValuePair<string, JToken> property in propertiesToAdd)
					{
						if (jsonObj[property.Key] is JObject jobject)
						{
							jobject.Merge(property.Value);
						}
						else
						{
							jsonObj[property.Key] = property.Value;
						}
					}
				}
			}
			{
				foreach (KeyValuePair<string, JToken> item in jsonObj)
				{
					solveByType(item.Value, codePath, searchReplace);
				}
				return;
			}
		}
		if (json.Type == JTokenType.String)
		{
			string value = (string)(json as JValue).Value;
			if (value.Contains("{"))
			{
				(json as JValue).Value = RegistryObject.FillPlaceHolder(value, searchReplace);
			}
		}
		else
		{
			if (!(json is JArray jarray))
			{
				return;
			}
			foreach (JToken item2 in jarray)
			{
				solveByType(item2, codePath, searchReplace);
			}
		}
	}
}
