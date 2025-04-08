using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

/// <summary>
/// Utility class for enabling i18n. Loads language entries from assets/[locale].json
/// </summary>
/// <remarks>
/// Kept legacy code structure and arguments for backwards compatibility.
/// </remarks>
public static class Lang
{
	public static Dictionary<string, ITranslationService> AvailableLanguages { get; } = new Dictionary<string, ITranslationService>();


	/// <summary>
	/// Gets the language code that this currently used to translate values.
	/// </summary>
	/// <value>A string, that contains he language code that this currently used to translate values.</value>
	public static string CurrentLocale { get; private set; }

	public static string DefaultLocale { get; set; } = "en";


	/// <summary>
	/// Loads all translations
	/// </summary>
	/// <param name="logger">The <see cref="T:Vintagestory.API.Common.ILogger" /> instance used within the sided API.</param>
	/// <param name="assetManager">The <see cref="T:Vintagestory.API.Common.IAssetManager" /> instance used within the sided API.</param>
	/// <param name="language">The desired language</param>
	public static void Load(ILogger logger, IAssetManager assetManager, string language = "en")
	{
		CurrentLocale = language;
		JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
		foreach (JsonObject jsonObject in array)
		{
			string languageCode = jsonObject["code"].AsString();
			EnumLinebreakBehavior lbBehavior = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
			LoadLanguage(logger, assetManager, languageCode, languageCode != CurrentLocale, lbBehavior);
		}
		if (!AvailableLanguages.ContainsKey(language))
		{
			logger.Error("Language '{0}' not found. Will default to english.", language);
			CurrentLocale = "en";
		}
	}

	/// <summary>
	/// Changes the current language for the game.
	/// </summary>
	/// <param name="languageCode">The language code to set as the language for the game.</param>
	public static void ChangeLanguage(string languageCode)
	{
		CurrentLocale = languageCode;
	}

	/// <summary>
	/// Loads translation key/value pairs from all relevant JSON files within the Asset Manager.
	/// </summary>
	/// <param name="logger">The <see cref="T:Vintagestory.API.Common.ILogger" /> instance used within the sided API.</param>
	/// <param name="assetManager">The <see cref="T:Vintagestory.API.Common.IAssetManager" /> instance used within the sided API.</param>
	/// <param name="languageCode">The language code to use as the default language.</param>
	/// <param name="lazyLoad"></param>
	/// <param name="lbBehavior"></param>
	public static void LoadLanguage(ILogger logger, IAssetManager assetManager, string languageCode = "en", bool lazyLoad = false, EnumLinebreakBehavior lbBehavior = EnumLinebreakBehavior.AfterWord)
	{
		if (AvailableLanguages.ContainsKey(languageCode))
		{
			AvailableLanguages[languageCode].UseAssetManager(assetManager);
			AvailableLanguages[languageCode].Load(lazyLoad);
		}
		else
		{
			TranslationService translationService = new TranslationService(languageCode, logger, assetManager, lbBehavior);
			translationService.Load(lazyLoad);
			AvailableLanguages.Add(languageCode, translationService);
		}
	}

	/// <summary>
	/// Loads only the vanilla JSON files, without dealing with mods, or resource-packs.
	/// </summary>
	/// <param name="logger">The <see cref="T:Vintagestory.API.Common.ILogger" /> instance used within the sided API.</param>
	/// <param name="assetsPath">The root assets path to load the vanilla files from.</param>
	/// <param name="defaultLanguage">The language code to use as the default language.</param>
	public static void PreLoad(ILogger logger, string assetsPath, string defaultLanguage = "en")
	{
		CurrentLocale = defaultLanguage;
		JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
		bool found = false;
		JsonObject[] array2 = array;
		foreach (JsonObject jsonObject in array2)
		{
			string languageCode = jsonObject["code"].AsString();
			EnumLinebreakBehavior lbBehavior = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
			TranslationService translationService = new TranslationService(languageCode, logger, null, lbBehavior);
			bool lazyLoad = languageCode != defaultLanguage;
			translationService.PreLoad(assetsPath, lazyLoad);
			AvailableLanguages[languageCode] = translationService;
			if (languageCode == defaultLanguage)
			{
				AvailableLanguages[languageCode].PreLoad(assetsPath);
				found = true;
			}
		}
		if (defaultLanguage != "en" && !found)
		{
			logger.Error("Language '{0}' not found. Will default to english.", defaultLanguage);
			AvailableLanguages["en"].PreLoad(assetsPath);
			CurrentLocale = "en";
		}
	}

	/// <summary>
	/// Gets a translation entry for given key, if any matching wildcarded keys are found within the cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated value.</returns>
	public static string GetIfExists(string key, params object[] args)
	{
		if (!HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetIfExists(key, args);
		}
		return AvailableLanguages[CurrentLocale].GetIfExists(key, args);
	}

	/// <summary>
	/// Gets a translation entry for given key using given locale
	/// </summary>
	/// <param name="langcode"></param>
	/// <param name="key"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public static string GetL(string langcode, string key, params object[] args)
	{
		if (!AvailableLanguages.TryGetValue(langcode, out var language) || !language.HasTranslation(key, findWildcarded: false))
		{
			return AvailableLanguages[DefaultLocale].Get(key, args);
		}
		return language.Get(key, args);
	}

	public static string GetMatchingL(string langcode, string key, params object[] args)
	{
		if (!AvailableLanguages.TryGetValue(langcode, out var language) || !language.HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetMatching(key, args);
		}
		return language.GetMatching(key, args);
	}

	/// <summary>
	/// Gets a translation entry for given key using the current locale
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated value.</returns>
	public static string Get(string key, params object[] args)
	{
		if (!HasTranslation(key, findWildcarded: false))
		{
			return AvailableLanguages[DefaultLocale].Get(key, args);
		}
		return AvailableLanguages[CurrentLocale].Get(key, args);
	}

	/// <summary>
	/// Gets a translation entry for given key using the current locale. Also tries the fallback key - useful if we change a key but not all languages updated yet
	/// </summary>
	/// <param name="key"></param>
	/// <param name="fallbackKey"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public static string GetWithFallback(string key, string fallbackKey, params object[] args)
	{
		if (!HasTranslation(key, findWildcarded: false))
		{
			if (!HasTranslation(fallbackKey, findWildcarded: false))
			{
				return AvailableLanguages[DefaultLocale].Get(key, args);
			}
			return AvailableLanguages[CurrentLocale].Get(fallbackKey, args);
		}
		return AvailableLanguages[CurrentLocale].Get(key, args);
	}

	/// <summary>
	/// Gets the raw, unformatted translated value for the key provided.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>Returns the key as a default value, if no results are found; otherwise returns the unformatted, translated value.</returns>
	public static string GetUnformatted(string key)
	{
		if (!HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetUnformatted(key);
		}
		return AvailableLanguages[CurrentLocale].GetUnformatted(key);
	}

	/// <summary>
	/// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated value.</returns>
	public static string GetMatching(string key, params object[] args)
	{
		if (!HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetMatching(key, args);
		}
		return AvailableLanguages[CurrentLocale].GetMatching(key, args);
	}

	/// <summary>
	/// Gets a translation entry for given key, if any matching wildcarded keys are found within the cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>Returns <c>null</c> as a default value, if no results are found; otherwise returns the pre-formatted, translated value.</returns>
	public static string GetMatchingIfExists(string key, params object[] args)
	{
		if (!HasTranslation(key, findWildcarded: true, logErrors: false))
		{
			return AvailableLanguages[DefaultLocale].GetMatchingIfExists(key, args);
		}
		return AvailableLanguages[CurrentLocale].GetMatchingIfExists(key, args);
	}

	/// <summary>
	/// Retrieves a list of all translation entries within the cache.
	/// </summary>
	/// <returns>A dictionary of localisation entries.</returns>
	public static IDictionary<string, string> GetAllEntries()
	{
		Dictionary<string, string> source = AvailableLanguages[DefaultLocale].GetAllEntries().ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
		Dictionary<string, string> currentEntries = AvailableLanguages[CurrentLocale].GetAllEntries().ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
		foreach (KeyValuePair<string, string> entry2 in source.Where((KeyValuePair<string, string> entry) => !currentEntries.ContainsKey(entry.Key)))
		{
			currentEntries.Add(entry2.Key, entry2.Value);
		}
		return currentEntries;
	}

	/// <summary>
	/// Determines whether the specified key has a translation.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="findWildcarded">if set to <c>true</c>, the scan will include any wildcarded values.</param>
	/// <param name="logErrors"></param>
	/// <returns><c>true</c> if the specified key has a translation; otherwise, <c>false</c>.</returns>
	public static bool HasTranslation(string key, bool findWildcarded = true, bool logErrors = true)
	{
		return AvailableLanguages[CurrentLocale].HasTranslation(key, findWildcarded, logErrors);
	}

	public static void InitialiseSearch()
	{
		AvailableLanguages[CurrentLocale].InitialiseSearch();
	}

	public static string GetNamePlaceHolder(AssetLocation code)
	{
		string hint = "";
		string[] parts = code.Path.Split('-');
		for (int i = 0; i < parts.Length; i++)
		{
			if (!(parts[i] == "north") && !(parts[i] == "east") && !(parts[i] == "west") && !(parts[i] == "south") && !(parts[i] == "up") && !(parts[i] == "down"))
			{
				if (i > 0)
				{
					hint += " ";
				}
				if (i > 0 && i == parts.Length - 1)
				{
					hint += "(";
				}
				hint = ((i != 0) ? (hint + parts[i]) : (hint + parts[i].First().ToString().ToUpper() + parts[i].Substring(1)));
				if (i > 0 && i == parts.Length - 1)
				{
					hint += ")";
				}
			}
		}
		return hint;
	}

	public static bool UsesNonLatinCharacters(string lang)
	{
		if (!(lang == "ar") && !lang.StartsWithOrdinal("zh-"))
		{
			switch (lang)
			{
			default:
				return lang == "ru";
			case "ja":
			case "ko":
			case "th":
			case "uk":
				break;
			}
		}
		return true;
	}
}
