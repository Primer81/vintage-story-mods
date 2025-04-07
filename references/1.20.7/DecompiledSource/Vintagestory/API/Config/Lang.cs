#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

//
// Summary:
//     Utility class for enabling i18n. Loads language entries from assets/[locale].json
//
//
// Remarks:
//     Kept legacy code structure and arguments for backwards compatibility.
public static class Lang
{
    public static Dictionary<string, ITranslationService> AvailableLanguages { get; } = new Dictionary<string, ITranslationService>();


    //
    // Summary:
    //     Gets the language code that this currently used to translate values.
    //
    // Value:
    //     A string, that contains he language code that this currently used to translate
    //     values.
    public static string CurrentLocale { get; private set; }

    public static string DefaultLocale { get; set; } = "en";


    //
    // Summary:
    //     Loads all translations
    //
    // Parameters:
    //   logger:
    //     The Vintagestory.API.Common.ILogger instance used within the sided API.
    //
    //   assetManager:
    //     The Vintagestory.API.Common.IAssetManager instance used within the sided API.
    //
    //
    //   language:
    //     The desired language
    public static void Load(ILogger logger, IAssetManager assetManager, string language = "en")
    {
        CurrentLocale = language;
        JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
        foreach (JsonObject jsonObject in array)
        {
            string text = jsonObject["code"].AsString();
            EnumLinebreakBehavior lbBehavior = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
            LoadLanguage(logger, assetManager, text, text != CurrentLocale, lbBehavior);
        }

        if (!AvailableLanguages.ContainsKey(language))
        {
            logger.Error("Language '{0}' not found. Will default to english.", language);
            CurrentLocale = "en";
        }
    }

    //
    // Summary:
    //     Changes the current language for the game.
    //
    // Parameters:
    //   languageCode:
    //     The language code to set as the language for the game.
    public static void ChangeLanguage(string languageCode)
    {
        CurrentLocale = languageCode;
    }

    //
    // Summary:
    //     Loads translation key/value pairs from all relevant JSON files within the Asset
    //     Manager.
    //
    // Parameters:
    //   logger:
    //     The Vintagestory.API.Common.ILogger instance used within the sided API.
    //
    //   assetManager:
    //     The Vintagestory.API.Common.IAssetManager instance used within the sided API.
    //
    //
    //   languageCode:
    //     The language code to use as the default language.
    //
    //   lazyLoad:
    //
    //   lbBehavior:
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

    //
    // Summary:
    //     Loads only the vanilla JSON files, without dealing with mods, or resource-packs.
    //
    //
    // Parameters:
    //   logger:
    //     The Vintagestory.API.Common.ILogger instance used within the sided API.
    //
    //   assetsPath:
    //     The root assets path to load the vanilla files from.
    //
    //   defaultLanguage:
    //     The language code to use as the default language.
    public static void PreLoad(ILogger logger, string assetsPath, string defaultLanguage = "en")
    {
        CurrentLocale = defaultLanguage;
        JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
        bool flag = false;
        JsonObject[] array2 = array;
        foreach (JsonObject jsonObject in array2)
        {
            string text = jsonObject["code"].AsString();
            EnumLinebreakBehavior lbBehavior = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
            TranslationService translationService = new TranslationService(text, logger, null, lbBehavior);
            bool lazyLoad = text != defaultLanguage;
            translationService.PreLoad(assetsPath, lazyLoad);
            AvailableLanguages[text] = translationService;
            if (text == defaultLanguage)
            {
                AvailableLanguages[text].PreLoad(assetsPath);
                flag = true;
            }
        }

        if (defaultLanguage != "en" && !flag)
        {
            logger.Error("Language '{0}' not found. Will default to english.", defaultLanguage);
            AvailableLanguages["en"].PreLoad(assetsPath);
            CurrentLocale = "en";
        }
    }

    //
    // Summary:
    //     Gets a translation entry for given key, if any matching wildcarded keys are found
    //     within the cache.
    //
    // Parameters:
    //   key:
    //     The key.
    //
    //   args:
    //     The arguments to interpolate into the resulting string.
    //
    // Returns:
    //     Returns the key as a default value, if no results are found; otherwise returns
    //     the pre-formatted, translated value.
    public static string GetIfExists(string key, params object[] args)
    {
        if (!HasTranslation(key))
        {
            return AvailableLanguages[DefaultLocale].GetIfExists(key, args);
        }

        return AvailableLanguages[CurrentLocale].GetIfExists(key, args);
    }

    //
    // Summary:
    //     Gets a translation entry for given key using given locale
    //
    // Parameters:
    //   langcode:
    //
    //   key:
    //
    //   args:
    public static string GetL(string langcode, string key, params object[] args)
    {
        if (!AvailableLanguages.TryGetValue(langcode, out var value) || !value.HasTranslation(key, findWildcarded: false))
        {
            return AvailableLanguages[DefaultLocale].Get(key, args);
        }

        return value.Get(key, args);
    }

    public static string GetMatchingL(string langcode, string key, params object[] args)
    {
        if (!AvailableLanguages.TryGetValue(langcode, out var value) || !value.HasTranslation(key))
        {
            return AvailableLanguages[DefaultLocale].GetMatching(key, args);
        }

        return value.GetMatching(key, args);
    }

    //
    // Summary:
    //     Gets a translation entry for given key using the current locale
    //
    // Parameters:
    //   key:
    //     The key.
    //
    //   args:
    //     The arguments to interpolate into the resulting string.
    //
    // Returns:
    //     Returns the key as a default value, if no results are found; otherwise returns
    //     the pre-formatted, translated value.
    public static string Get(string key, params object[] args)
    {
        if (!HasTranslation(key, findWildcarded: false))
        {
            return AvailableLanguages[DefaultLocale].Get(key, args);
        }

        return AvailableLanguages[CurrentLocale].Get(key, args);
    }

    //
    // Summary:
    //     Gets a translation entry for given key using the current locale. Also tries the
    //     fallback key - useful if we change a key but not all languages updated yet
    //
    // Parameters:
    //   key:
    //
    //   fallbackKey:
    //
    //   args:
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

    //
    // Summary:
    //     Gets the raw, unformatted translated value for the key provided.
    //
    // Parameters:
    //   key:
    //     The key.
    //
    // Returns:
    //     Returns the key as a default value, if no results are found; otherwise returns
    //     the unformatted, translated value.
    public static string GetUnformatted(string key)
    {
        if (!HasTranslation(key))
        {
            return AvailableLanguages[DefaultLocale].GetUnformatted(key);
        }

        return AvailableLanguages[CurrentLocale].GetUnformatted(key);
    }

    //
    // Summary:
    //     Gets a translation for a given key, if any matching wildcarded keys are found
    //     within the cache.
    //
    // Parameters:
    //   key:
    //     The key.
    //
    //   args:
    //     The arguments to interpolate into the resulting string.
    //
    // Returns:
    //     Returns the key as a default value, if no results are found; otherwise returns
    //     the pre-formatted, translated value.
    public static string GetMatching(string key, params object[] args)
    {
        if (!HasTranslation(key))
        {
            return AvailableLanguages[DefaultLocale].GetMatching(key, args);
        }

        return AvailableLanguages[CurrentLocale].GetMatching(key, args);
    }

    //
    // Summary:
    //     Gets a translation entry for given key, if any matching wildcarded keys are found
    //     within the cache.
    //
    // Parameters:
    //   key:
    //     The key.
    //
    //   args:
    //     The arguments to interpolate into the resulting string.
    //
    // Returns:
    //     Returns null as a default value, if no results are found; otherwise returns the
    //     pre-formatted, translated value.
    public static string GetMatchingIfExists(string key, params object[] args)
    {
        if (!HasTranslation(key, findWildcarded: true, logErrors: false))
        {
            return AvailableLanguages[DefaultLocale].GetMatchingIfExists(key, args);
        }

        return AvailableLanguages[CurrentLocale].GetMatchingIfExists(key, args);
    }

    //
    // Summary:
    //     Retrieves a list of all translation entries within the cache.
    //
    // Returns:
    //     A dictionary of localisation entries.
    public static IDictionary<string, string> GetAllEntries()
    {
        Dictionary<string, string> source = AvailableLanguages[DefaultLocale].GetAllEntries().ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
        Dictionary<string, string> currentEntries = AvailableLanguages[CurrentLocale].GetAllEntries().ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
        foreach (KeyValuePair<string, string> item in source.Where((KeyValuePair<string, string> entry) => !currentEntries.ContainsKey(entry.Key)))
        {
            currentEntries.Add(item.Key, item.Value);
        }

        return currentEntries;
    }

    //
    // Summary:
    //     Determines whether the specified key has a translation.
    //
    // Parameters:
    //   key:
    //     The key.
    //
    //   findWildcarded:
    //     if set to true, the scan will include any wildcarded values.
    //
    //   logErrors:
    //
    // Returns:
    //     true if the specified key has a translation; otherwise, false.
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
        string text = "";
        string[] array = code.Path.Split('-');
        for (int i = 0; i < array.Length; i++)
        {
            if (!(array[i] == "north") && !(array[i] == "east") && !(array[i] == "west") && !(array[i] == "south") && !(array[i] == "up") && !(array[i] == "down"))
            {
                if (i > 0)
                {
                    text += " ";
                }

                if (i > 0 && i == array.Length - 1)
                {
                    text += "(";
                }

                text = ((i != 0) ? (text + array[i]) : (text + array[i].First().ToString().ToUpper() + array[i].Substring(1)));
                if (i > 0 && i == array.Length - 1)
                {
                    text += ")";
                }
            }
        }

        return text;
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
#if false // Decompilation log
'181' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
