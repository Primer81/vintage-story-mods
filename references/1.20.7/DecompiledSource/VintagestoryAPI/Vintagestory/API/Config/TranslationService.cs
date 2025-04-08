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

/// <summary>
/// A service, which provides access to translated strings, based on key/value pairs read from JSON files.
/// </summary>
/// <seealso cref="T:Vintagestory.API.Config.ITranslationService" />
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

	/// <summary>
	/// Gets the language code that this translation service caters for.
	/// </summary>
	/// <value>A string, that contains the language code that this translation service caters for.</value>
	public string LanguageCode { get; }

	/// <summary>
	/// Initialises a new instance of the <see cref="T:Vintagestory.API.Config.TranslationService" /> class.
	/// </summary>
	/// <param name="languageCode">The language code that this translation service caters for.</param>
	/// <param name="logger">The <see cref="T:Vintagestory.API.Common.ILogger" /> instance used within the sided API.</param>
	/// <param name="assetManager">The <see cref="T:Vintagestory.API.Common.IAssetManager" /> instance used within the sided API.</param>
	/// <param name="lbBehavior"></param>
	public TranslationService(string languageCode, ILogger logger, IAssetManager assetManager = null, EnumLinebreakBehavior lbBehavior = EnumLinebreakBehavior.AfterWord)
	{
		LanguageCode = languageCode;
		this.logger = logger;
		this.assetManager = assetManager;
		LineBreakBehavior = lbBehavior;
	}

	/// <summary>
	/// Loads translation key/value pairs from all relevant JSON files within the Asset Manager.
	/// </summary>
	public void Load(bool lazyLoad = false)
	{
		preLoadAssetsPath = null;
		if (lazyLoad)
		{
			return;
		}
		loaded = true;
		Dictionary<string, string> entryCache = new Dictionary<string, string>();
		Dictionary<string, KeyValuePair<Regex, string>> regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();
		Dictionary<string, string> wildcardCache = new Dictionary<string, string>();
		foreach (IAsset asset in assetManager.Origins.SelectMany((IAssetOrigin p) => from a in p.GetAssets(AssetCategory.lang)
			where a.Name.Equals(LanguageCode + ".json")
			select a))
		{
			try
			{
				string json = asset.ToText();
				LoadEntries(entryCache, regexCache, wildcardCache, JsonConvert.DeserializeObject<Dictionary<string, string>>(json), asset.Location.Domain);
			}
			catch (Exception ex)
			{
				logger.Error("Failed to load language file: " + asset.Name);
				logger.Error(ex);
			}
		}
		this.entryCache = entryCache;
		this.regexCache = regexCache;
		this.wildcardCache = wildcardCache;
	}

	/// <summary>
	/// Loads only the vanilla JSON files, without dealing with mods, or resource-packs.
	/// </summary>
	/// <param name="assetsPath">The root assets path to load the vanilla files from.</param>
	/// <param name="lazyLoad"></param>
	public void PreLoad(string assetsPath, bool lazyLoad = false)
	{
		preLoadAssetsPath = assetsPath;
		if (lazyLoad)
		{
			return;
		}
		loaded = true;
		Dictionary<string, string> entryCache = new Dictionary<string, string>();
		Dictionary<string, KeyValuePair<Regex, string>> regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();
		Dictionary<string, string> wildcardCache = new Dictionary<string, string>();
		foreach (FileInfo file in new DirectoryInfo(Path.Combine(assetsPath, "game", "lang")).EnumerateFiles(LanguageCode + ".json", SearchOption.AllDirectories))
		{
			try
			{
				string json = File.ReadAllText(file.FullName);
				LoadEntries(entryCache, regexCache, wildcardCache, JsonConvert.DeserializeObject<Dictionary<string, string>>(json));
			}
			catch (Exception ex)
			{
				logger.Error("Failed to load language file: " + file.Name);
				logger.Error(ex);
			}
		}
		this.entryCache = entryCache;
		this.regexCache = regexCache;
		this.wildcardCache = wildcardCache;
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

	/// <summary>
	/// Sets the loaded flag to false, so that the next lookup causes it to reload all translation entries
	/// </summary>
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
		catch (Exception ex)
		{
			logger.Error(ex);
			result = value;
			if (logger != null)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Translation string format exception thrown for: \"");
				foreach (char c in value)
				{
					sb.Append(c);
					if (c == '{' || c == '}')
					{
						sb.Append(c);
					}
				}
				sb.Append("\"\n   Args were: ");
				for (int i = 0; i < args.Length; i++)
				{
					if (i > 0)
					{
						sb.Append(", ");
					}
					sb.Append(args[i].ToString());
				}
				try
				{
					logger.Warning(sb.ToString());
				}
				catch (Exception e)
				{
					logger.Error("Exception thrown when trying to print exception message for an incorrect translation entry. Exception: ");
					logger.Error(e);
				}
			}
		}
		return result;
	}

	private string PluralFormat(string value, object[] args)
	{
		int start = value.IndexOfOrdinal("{p");
		if (value.Length < start + 5)
		{
			return TryFormat(value, args);
		}
		int pluralOffset = start + 4;
		int end = value.IndexOf('}', pluralOffset);
		char c = value[start + 2];
		if (c < '0' || c > '9')
		{
			return TryFormat(value, args);
		}
		if (end < 0)
		{
			return TryFormat(value, args);
		}
		int argNum = c - 48;
		if ((c = value[start + 3]) != ':')
		{
			if (value[start + 4] != ':' || c < '0' || c > '9')
			{
				return TryFormat(value, args);
			}
			argNum = argNum * 10 + c - 48;
			pluralOffset++;
		}
		if (argNum >= args.Length)
		{
			throw new IndexOutOfRangeException("Index out of range: Plural format {p#:...} referenced an argument " + argNum + " but only " + args.Length + " arguments were available in the code");
		}
		float N = 0f;
		try
		{
			N = float.Parse(args[argNum].ToString());
		}
		catch (Exception)
		{
		}
		string before = value.Substring(0, start);
		string plural = value.Substring(pluralOffset, end - pluralOffset);
		string after = value.Substring(end + 1);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TryFormat(before, args));
		stringBuilder.Append(BuildPluralFormat(plural, N));
		stringBuilder.Append(Format(after, args));
		return stringBuilder.ToString();
	}

	internal static string BuildPluralFormat(string input, float n)
	{
		string[] plurals = input.Split('|');
		int round = 3;
		if (plurals.Length >= 2)
		{
			if (!TryGuessRounding(plurals[1], out round))
			{
				TryGuessRounding(plurals[^1], out round);
			}
			if (round < 0 || round > 15)
			{
				round = 3;
			}
		}
		int index = (int)Math.Ceiling(Math.Round(n, round));
		if (index < 0 || index >= plurals.Length)
		{
			index = plurals.Length - 1;
		}
		if (plurals.Length >= 2)
		{
			if (n == 1f)
			{
				index = 1;
			}
			else if (n < 1f)
			{
				index = 0;
			}
		}
		return WithNumberFormatting(plurals[index], n);
	}

	private static bool TryGuessRounding(string entry, out int round)
	{
		string partB;
		string numberFormatting = GetNumberFormattingFrom(entry, out partB);
		if (numberFormatting.IndexOf('.') > 0)
		{
			round = numberFormatting.Length - numberFormatting.IndexOf('.') - 1;
			return true;
		}
		if (numberFormatting.Length > 0)
		{
			round = 0;
			return true;
		}
		round = 3;
		return false;
	}

	internal static string GetNumberFormattingFrom(string rawResult, out string partB)
	{
		int i = GetStartIndexOfNumberFormat(rawResult);
		if (i >= 0)
		{
			int j = i;
			while (++j < rawResult.Length)
			{
				char c = rawResult[j];
				if (c != '#' && c != '.' && c != '0' && c != ',')
				{
					break;
				}
			}
			partB = rawResult.Substring(j);
			return rawResult.Substring(i, j - i);
		}
		partB = rawResult;
		return "";
	}

	private static int GetStartIndexOfNumberFormat(string rawResult)
	{
		int indexHash = rawResult.IndexOf('#');
		int indexZero = rawResult.IndexOf('0');
		int i = -1;
		if (indexHash >= 0 && indexZero >= 0)
		{
			i = Math.Min(indexHash, indexZero);
		}
		else if (indexHash >= 0)
		{
			i = indexHash;
		}
		else if (indexZero >= 0)
		{
			i = indexZero;
		}
		return i;
	}

	internal static string WithNumberFormatting(string rawResult, float n)
	{
		int i = GetStartIndexOfNumberFormat(rawResult);
		if (i < 0)
		{
			return rawResult;
		}
		string partA = rawResult.Substring(0, i);
		string partB;
		string numberFormatting = GetNumberFormattingFrom(rawResult, out partB);
		string number;
		try
		{
			number = ((numberFormatting.Length != 1 || n != 0f) ? n.ToString(numberFormatting, GlobalConstants.DefaultCultureInfo) : "0");
		}
		catch (Exception)
		{
			number = n.ToString(GlobalConstants.DefaultCultureInfo);
		}
		return partA + number + partB;
	}

	/// <summary>
	/// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>
	///     Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated
	///     value.
	/// </returns>
	public string GetIfExists(string key, params object[] args)
	{
		EnsureLoaded();
		if (!entryCache.TryGetValue(KeyWithDomain(key), out var value))
		{
			return null;
		}
		return Format(value, args);
	}

	/// <summary>
	/// Gets a translation for a given key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>
	///     Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated
	///     value.
	/// </returns>
	public string Get(string key, params object[] args)
	{
		return Format(GetUnformatted(key), args);
	}

	/// <summary>
	/// Retrieves a list of all translation entries within the cache.
	/// </summary>
	/// <returns>A dictionary of localisation entries.</returns>
	public IDictionary<string, string> GetAllEntries()
	{
		EnsureLoaded();
		return entryCache;
	}

	/// <summary>
	/// Gets the raw, unformatted translated value for the key provided.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>
	///     Returns the key as a default value, if no results are found; otherwise returns the unformatted, translated
	///     value.
	/// </returns>
	public string GetUnformatted(string key)
	{
		EnsureLoaded();
		if (!entryCache.TryGetValue(KeyWithDomain(key), out var value))
		{
			return key;
		}
		return value;
	}

	/// <summary>
	/// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>
	/// Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated
	/// value.
	/// </returns>
	public string GetMatching(string key, params object[] args)
	{
		EnsureLoaded();
		string value = GetMatchingIfExists(KeyWithDomain(key), args);
		if (!string.IsNullOrEmpty(value))
		{
			return value;
		}
		return Format(key, args);
	}

	/// <summary>
	/// Determines whether the specified key has a translation.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="findWildcarded">if set to <c>true</c>, the scan will include any wildcarded values.</param>
	/// <returns><c>true</c> if the specified key has a translation; otherwise, <c>false</c>.</returns>
	public bool HasTranslation(string key, bool findWildcarded = true)
	{
		return HasTranslation(key, findWildcarded, logErrors: true);
	}

	/// <summary>
	/// Determines whether the specified key has a translation.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="findWildcarded">if set to <c>true</c>, the scan will include any wildcarded values.</param>
	/// <param name="logErrors">if set to <c>true</c>, will add "Lang key not found" logging</param>
	/// <returns><c>true</c> if the specified key has a translation; otherwise, <c>false</c>.</returns>
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
			bool result = wildcardCache.Any((KeyValuePair<string, string> pair) => key.StartsWithFast(pair.Key));
			if (!result)
			{
				result = regexCache.Values.Any((KeyValuePair<Regex, string> pair) => pair.Key.IsMatch(validKey));
			}
			if (!result && logErrors && !key.Contains("desc-") && notFound.Add(key))
			{
				logger.VerboseDebug("Lang key not found: " + key.Replace("{", "{{").Replace("}", "}}"));
			}
			return result;
		}
		return false;
	}

	/// <summary>
	/// Specifies an asset manager to use, when the service has been lazy-loaded.
	/// </summary>
	/// <param name="assetManager">The <see cref="T:Vintagestory.API.Common.IAssetManager" /> instance used within the sided API.</param>
	public void UseAssetManager(IAssetManager assetManager)
	{
		this.assetManager = assetManager;
	}

	/// <summary>
	/// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="args">The arguments to interpolate into the resulting string.</param>
	/// <returns>
	/// Returns <c>null</c> as a default value, if no results are found; otherwise returns the pre-formatted, translated value.
	/// </returns>
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
		string key = KeyWithDomain(entry.Key, domain);
		switch (key.CountChars('*'))
		{
		case 0:
			entryCache[key] = entry.Value;
			return;
		case 1:
			if (key.EndsWith('*'))
			{
				wildcardCache[key.TrimEnd('*')] = entry.Value;
				return;
			}
			break;
		}
		Regex regex = new Regex("^" + key.Replace("*", "(.*)") + "$", RegexOptions.Compiled);
		regexCache[key] = new KeyValuePair<Regex, string>(regex, entry.Value);
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
