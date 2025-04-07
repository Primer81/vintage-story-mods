#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

//
// Summary:
//     A service, which provides access to translated strings, based on key/value pairs
//     read from JSON files.
public class TranslationService : ITranslationService
{
    internal Dictionary<string, string> entryCache = new Dictionary<string, string>();

    private Dictionary<string, KeyValuePair<Regex, string>> regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();

    private Dictionary<string, string> wildcardCache = new Dictionary<string, string>();

    private HashSet<string> notFound = new HashSet<string>();

    private IAssetManager assetManager;

    private readonly ILogger logger;

    internal bool loaded;

    private string preLoadAssetsPath;

    public EnumLinebreakBehavior LineBreakBehavior { get; set; }

    //
    // Summary:
    //     Gets the language code that this translation service caters for.
    //
    // Value:
    //     A string, that contains the language code that this translation service caters
    //     for.
    public string LanguageCode { get; }

    //
    // Summary:
    //     Initialises a new instance of the Vintagestory.API.Config.TranslationService
    //     class.
    //
    // Parameters:
    //   languageCode:
    //     The language code that this translation service caters for.
    //
    //   logger:
    //     The Vintagestory.API.Common.ILogger instance used within the sided API.
    //
    //   assetManager:
    //     The Vintagestory.API.Common.IAssetManager instance used within the sided API.
    //
    //
    //   lbBehavior:
    public TranslationService(string languageCode, ILogger logger, IAssetManager assetManager = null, EnumLinebreakBehavior lbBehavior = EnumLinebreakBehavior.AfterWord)
    {
        LanguageCode = languageCode;
        this.logger = logger;
        this.assetManager = assetManager;
        LineBreakBehavior = lbBehavior;
    }

    //
    // Summary:
    //     Loads translation key/value pairs from all relevant JSON files within the Asset
    //     Manager.
    public void Load(bool lazyLoad = false)
    {
        preLoadAssetsPath = null;
        if (lazyLoad)
        {
            return;
        }

        loaded = true;
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        Dictionary<string, KeyValuePair<Regex, string>> dictionary2 = new Dictionary<string, KeyValuePair<Regex, string>>();
        Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
        foreach (IAsset item in assetManager.Origins.SelectMany((IAssetOrigin p) => from a in p.GetAssets(AssetCategory.lang)
                                                                                    where a.Name.Equals(LanguageCode + ".json")
                                                                                    select a))
        {
            try
            {
                string value = item.ToText();
                LoadEntries(dictionary, dictionary2, dictionary3, JsonConvert.DeserializeObject<Dictionary<string, string>>(value), item.Location.Domain);
            }
            catch (Exception e)
            {
                logger.Error("Failed to load language file: " + item.Name);
                logger.Error(e);
            }
        }

        entryCache = dictionary;
        regexCache = dictionary2;
        wildcardCache = dictionary3;
    }

    //
    // Summary:
    //     Loads only the vanilla JSON files, without dealing with mods, or resource-packs.
    //
    //
    // Parameters:
    //   assetsPath:
    //     The root assets path to load the vanilla files from.
    //
    //   lazyLoad:
    public void PreLoad(string assetsPath, bool lazyLoad = false)
    {
        preLoadAssetsPath = assetsPath;
        if (lazyLoad)
        {
            return;
        }

        loaded = true;
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        Dictionary<string, KeyValuePair<Regex, string>> dictionary2 = new Dictionary<string, KeyValuePair<Regex, string>>();
        Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
        foreach (FileInfo item in new DirectoryInfo(Path.Combine(assetsPath, "game", "lang")).EnumerateFiles(LanguageCode + ".json", SearchOption.AllDirectories))
        {
            try
            {
                string value = File.ReadAllText(item.FullName);
                LoadEntries(dictionary, dictionary2, dictionary3, JsonConvert.DeserializeObject<Dictionary<string, string>>(value));
            }
            catch (Exception e)
            {
                logger.Error("Failed to load language file: " + item.Name);
                logger.Error(e);
            }
        }

        entryCache = dictionary;
        regexCache = dictionary2;
        wildcardCache = dictionary3;
    }

    protected void EnsureLoaded()
    {
        if (!loaded)
        {
            if (preLoadAssetsPath != null)
            {
                PreLoad(preLoadAssetsPath);
            }
            else
            {
                Load();
            }
        }
    }

    //
    // Summary:
    //     Sets the loaded flag to false, so that the next lookup causes it to reload all
    //     translation entries
    public void Invalidate()
    {
        loaded = false;
    }

    protected string Format(string value, params object[] args)
    {
        if (value.ContainsFast("{p"))
        {
            return PluralFormat(value, args);
        }

        return TryFormat(value, args);
    }

    private string TryFormat(string value, params object[] args)
    {
        string result;
        try
        {
            result = string.Format(value, args);
        }
        catch (Exception e)
        {
            logger.Error(e);
            result = value;
            if (logger != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Translation string format exception thrown for: \"");
                foreach (char c in value)
                {
                    stringBuilder.Append(c);
                    if (c == '{' || c == '}')
                    {
                        stringBuilder.Append(c);
                    }
                }

                stringBuilder.Append("\"\n   Args were: ");
                for (int j = 0; j < args.Length; j++)
                {
                    if (j > 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(args[j].ToString());
                }

                try
                {
                    logger.Warning(stringBuilder.ToString());
                }
                catch (Exception e2)
                {
                    logger.Error("Exception thrown when trying to print exception message for an incorrect translation entry. Exception: ");
                    logger.Error(e2);
                }
            }
        }

        return result;
    }

    private string PluralFormat(string value, object[] args)
    {
        int num = value.IndexOfOrdinal("{p");
        if (value.Length < num + 5)
        {
            return TryFormat(value, args);
        }

        int num2 = num + 4;
        int num3 = value.IndexOf('}', num2);
        char c = value[num + 2];
        if (c < '0' || c > '9')
        {
            return TryFormat(value, args);
        }

        if (num3 < 0)
        {
            return TryFormat(value, args);
        }

        int num4 = c - 48;
        if ((c = value[num + 3]) != ':')
        {
            if (value[num + 4] != ':' || c < '0' || c > '9')
            {
                return TryFormat(value, args);
            }

            num4 = num4 * 10 + c - 48;
            num2++;
        }

        if (num4 >= args.Length)
        {
            throw new IndexOutOfRangeException("Index out of range: Plural format {p#:...} referenced an argument " + num4 + " but only " + args.Length + " arguments were available in the code");
        }

        float n = 0f;
        try
        {
            n = float.Parse(args[num4].ToString());
        }
        catch (Exception)
        {
        }

        string value2 = value.Substring(0, num);
        string input = value.Substring(num2, num3 - num2);
        string value3 = value.Substring(num3 + 1);
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(TryFormat(value2, args));
        stringBuilder.Append(BuildPluralFormat(input, n));
        stringBuilder.Append(Format(value3, args));
        return stringBuilder.ToString();
    }

    internal static string BuildPluralFormat(string input, float n)
    {
        string[] array = input.Split('|');
        int round = 3;
        if (array.Length >= 2)
        {
            if (!TryGuessRounding(array[1], out round))
            {
                TryGuessRounding(array[^1], out round);
            }

            if (round < 0 || round > 15)
            {
                round = 3;
            }
        }

        int num = (int)Math.Ceiling(Math.Round(n, round));
        if (num < 0 || num >= array.Length)
        {
            num = array.Length - 1;
        }

        if (array.Length >= 2)
        {
            if (n == 1f)
            {
                num = 1;
            }
            else if (n < 1f)
            {
                num = 0;
            }
        }

        return WithNumberFormatting(array[num], n);
    }

    private static bool TryGuessRounding(string entry, out int round)
    {
        string partB;
        string numberFormattingFrom = GetNumberFormattingFrom(entry, out partB);
        if (numberFormattingFrom.IndexOf('.') > 0)
        {
            round = numberFormattingFrom.Length - numberFormattingFrom.IndexOf('.') - 1;
            return true;
        }

        if (numberFormattingFrom.Length > 0)
        {
            round = 0;
            return true;
        }

        round = 3;
        return false;
    }

    internal static string GetNumberFormattingFrom(string rawResult, out string partB)
    {
        int startIndexOfNumberFormat = GetStartIndexOfNumberFormat(rawResult);
        if (startIndexOfNumberFormat >= 0)
        {
            int num = startIndexOfNumberFormat;
            while (++num < rawResult.Length)
            {
                char c = rawResult[num];
                if (c != '#' && c != '.' && c != '0' && c != ',')
                {
                    break;
                }
            }

            partB = rawResult.Substring(num);
            return rawResult.Substring(startIndexOfNumberFormat, num - startIndexOfNumberFormat);
        }

        partB = rawResult;
        return "";
    }

    private static int GetStartIndexOfNumberFormat(string rawResult)
    {
        int num = rawResult.IndexOf('#');
        int num2 = rawResult.IndexOf('0');
        int result = -1;
        if (num >= 0 && num2 >= 0)
        {
            result = Math.Min(num, num2);
        }
        else if (num >= 0)
        {
            result = num;
        }
        else if (num2 >= 0)
        {
            result = num2;
        }

        return result;
    }

    internal static string WithNumberFormatting(string rawResult, float n)
    {
        int startIndexOfNumberFormat = GetStartIndexOfNumberFormat(rawResult);
        if (startIndexOfNumberFormat < 0)
        {
            return rawResult;
        }

        string text = rawResult.Substring(0, startIndexOfNumberFormat);
        string partB;
        string numberFormattingFrom = GetNumberFormattingFrom(rawResult, out partB);
        string text2;
        try
        {
            text2 = ((numberFormattingFrom.Length != 1 || n != 0f) ? n.ToString(numberFormattingFrom, GlobalConstants.DefaultCultureInfo) : "0");
        }
        catch (Exception)
        {
            text2 = n.ToString(GlobalConstants.DefaultCultureInfo);
        }

        return text + text2 + partB;
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
    public string GetIfExists(string key, params object[] args)
    {
        EnsureLoaded();
        if (!entryCache.TryGetValue(KeyWithDomain(key), out var value))
        {
            return null;
        }

        return Format(value, args);
    }

    //
    // Summary:
    //     Gets a translation for a given key.
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
    public string Get(string key, params object[] args)
    {
        return Format(GetUnformatted(key), args);
    }

    //
    // Summary:
    //     Retrieves a list of all translation entries within the cache.
    //
    // Returns:
    //     A dictionary of localisation entries.
    public IDictionary<string, string> GetAllEntries()
    {
        EnsureLoaded();
        return entryCache;
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
    public string GetUnformatted(string key)
    {
        EnsureLoaded();
        if (!entryCache.TryGetValue(KeyWithDomain(key), out var value))
        {
            return key;
        }

        return value;
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
    public string GetMatching(string key, params object[] args)
    {
        EnsureLoaded();
        string matchingIfExists = GetMatchingIfExists(KeyWithDomain(key), args);
        if (!string.IsNullOrEmpty(matchingIfExists))
        {
            return matchingIfExists;
        }

        return Format(key, args);
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
    // Returns:
    //     true if the specified key has a translation; otherwise, false.
    public bool HasTranslation(string key, bool findWildcarded = true)
    {
        return HasTranslation(key, findWildcarded, logErrors: true);
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
    //     if set to true, will add "Lang key not found" logging
    //
    // Returns:
    //     true if the specified key has a translation; otherwise, false.
    public bool HasTranslation(string key, bool findWildcarded, bool logErrors)
    {
        EnsureLoaded();
        string validKey = KeyWithDomain(key);
        if (entryCache.ContainsKey(validKey))
        {
            return true;
        }

        if (findWildcarded)
        {
            if (!key.Contains(":"))
            {
                key = "game:" + key;
            }

            bool flag = wildcardCache.Any((KeyValuePair<string, string> pair) => key.StartsWithFast(pair.Key));
            if (!flag)
            {
                flag = regexCache.Values.Any((KeyValuePair<Regex, string> pair) => pair.Key.IsMatch(validKey));
            }

            if (!flag && logErrors && !key.Contains("desc-") && notFound.Add(key))
            {
                logger.VerboseDebug("Lang key not found: " + key.Replace("{", "{{").Replace("}", "}}"));
            }

            return flag;
        }

        return false;
    }

    //
    // Summary:
    //     Specifies an asset manager to use, when the service has been lazy-loaded.
    //
    // Parameters:
    //   assetManager:
    //     The Vintagestory.API.Common.IAssetManager instance used within the sided API.
    public void UseAssetManager(IAssetManager assetManager)
    {
        this.assetManager = assetManager;
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
    //     Returns null as a default value, if no results are found; otherwise returns the
    //     pre-formatted, translated value.
    public string GetMatchingIfExists(string key, params object[] args)
    {
        EnsureLoaded();
        string validKey = KeyWithDomain(key);
        if (entryCache.TryGetValue(validKey, out var value))
        {
            return Format(value, args);
        }

        using (IEnumerator<KeyValuePair<string, string>> enumerator = wildcardCache.Where((KeyValuePair<string, string> pair) => validKey.StartsWithFast(pair.Key)).GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                return Format(enumerator.Current.Value, args);
            }
        }

        return (from pair in regexCache.Values
                where pair.Key.IsMatch(validKey)
                select Format(pair.Value, args)).FirstOrDefault();
    }

    private void LoadEntries(Dictionary<string, string> entryCache, Dictionary<string, KeyValuePair<Regex, string>> regexCache, Dictionary<string, string> wildcardCache, Dictionary<string, string> entries, string domain = "game")
    {
        foreach (KeyValuePair<string, string> entry in entries)
        {
            LoadEntry(entryCache, regexCache, wildcardCache, entry, domain);
        }
    }

    private void LoadEntry(Dictionary<string, string> entryCache, Dictionary<string, KeyValuePair<Regex, string>> regexCache, Dictionary<string, string> wildcardCache, KeyValuePair<string, string> entry, string domain = "game")
    {
        string text = KeyWithDomain(entry.Key, domain);
        switch (text.CountChars('*'))
        {
            case 0:
                entryCache[text] = entry.Value;
                return;
            case 1:
                if (text.EndsWith('*'))
                {
                    wildcardCache[text.TrimEnd('*')] = entry.Value;
                    return;
                }

                break;
        }

        Regex key = new Regex("^" + text.Replace("*", "(.*)") + "$", RegexOptions.Compiled);
        regexCache[text] = new KeyValuePair<Regex, string>(key, entry.Value);
    }

    private static string KeyWithDomain(string key, string domain = "game")
    {
        if (key.Contains(':'))
        {
            return key;
        }

        return new StringBuilder(domain).Append(':').Append(key).ToString();
    }

    public void InitialiseSearch()
    {
        regexCache.Values.Any((KeyValuePair<Regex, string> pair) => pair.Key.IsMatch("nonsense_value_and_fairly_longgg"));
    }
}
#if false // Decompilation log
'182' items in cache
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
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
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
