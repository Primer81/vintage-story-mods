using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Vintagestory.API.Config;

namespace Vintagestory.API.Util;

public static class StringUtil
{
	public unsafe static int GetNonRandomizedHashCode(this string str)
	{
		fixed (char* ptr2 = str)
		{
			uint hash1 = 352654597u;
			uint hash2 = hash1;
			uint* ptr = (uint*)ptr2;
			int length = str.Length;
			while (length > 2)
			{
				length -= 4;
				hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ *ptr;
				hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
				ptr += 2;
			}
			if (length > 0)
			{
				hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ *ptr;
			}
			return (int)(hash1 + hash2 * 1566083941);
		}
	}

	/// <summary>
	/// IMPORTANT!   This method should be used for every IndexOf operation in our code (except possibly in localised output to the user). This is important in order to avoid any
	/// culture-specific different results even when indexing GLSL shader code or other code strings, etc., or other strings in English, when the current culture is a different language
	/// (Known issue in the Thai language which has no spaces and treats punctuation marks as invisible, see https://github.com/dotnet/runtime/issues/59120)
	/// <br />See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static int IndexOfOrdinal(this string a, string b)
	{
		return a.IndexOf(b, StringComparison.Ordinal);
	}

	/// <summary>
	/// IMPORTANT!   This method should be used for every StartsWith operation in our code (except possibly in localised output to the user). This is important in order to avoid any
	/// culture-specific different results even when examining strings in English, when the user machine's current culture is a different language
	/// (Known issue in the Thai language which has no spaces and treats punctuation marks as invisible, see https://github.com/dotnet/runtime/issues/59120)
	/// <br />See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool StartsWithOrdinal(this string a, string b)
	{
		return a.StartsWith(b, StringComparison.Ordinal);
	}

	/// <summary>
	/// IMPORTANT!   This method should be used for every EndsWith operation in our code (except possibly in localised output to the user). This is important in order to avoid any
	/// culture-specific different results even when examining strings in English, when the user machine's current culture is a different language
	/// (Known issue in the Thai language which has no spaces and treats punctuation marks as invisible, see https://github.com/dotnet/runtime/issues/59120)
	/// <br />See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool EndsWithOrdinal(this string a, string b)
	{
		return a.EndsWith(b, StringComparison.Ordinal);
	}

	/// <summary>
	/// This should be used for every string comparison when ordering strings (except possibly in localised output to the user) in order to avoid any
	/// culture specific string comparison issues in certain languages (worst in the Thai language which has no spaces and treats punctuation marks as invisible)
	/// <br />See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static int CompareOrdinal(this string a, string b)
	{
		return string.CompareOrdinal(a, b);
	}

	/// <summary>
	/// Convert the first character to an uppercase one
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static string UcFirst(this string text)
	{
		return text.Substring(0, 1).ToUpperInvariant() + text.Substring(1);
	}

	public static bool ToBool(this string text, bool defaultValue = false)
	{
		switch (text?.ToLowerInvariant())
		{
		case "true":
		case "yes":
		case "1":
			return true;
		case "false":
		case "no":
		case "0":
			return false;
		default:
			return defaultValue;
		}
	}

	public static string RemoveFileEnding(this string text)
	{
		return text.Substring(0, text.IndexOf('.'));
	}

	public static int ToInt(this string text, int defaultValue = 0)
	{
		if (!int.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static long ToLong(this string text, long defaultValue = 0L)
	{
		if (!long.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static float ToFloat(this string text, float defaultValue = 0f)
	{
		if (!float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static double ToDouble(this string text, double defaultValue = 0.0)
	{
		if (!double.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static double? ToDoubleOrNull(this string text, double? defaultValue = 0.0)
	{
		if (!double.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static float? ToFloatOrNull(this string text, float? defaultValue = 0f)
	{
		if (!float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static int CountChars(this string text, char c)
	{
		int cnt = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == c)
			{
				cnt++;
			}
		}
		return cnt;
	}

	public static bool ContainsFast(this string value, string reference)
	{
		if (reference.Length > value.Length)
		{
			return false;
		}
		int j = 0;
		for (int i = 0; i < value.Length; i++)
		{
			j = ((value[i] == reference[j]) ? (j + 1) : 0);
			if (j >= reference.Length)
			{
				return true;
			}
		}
		return false;
	}

	public static bool ContainsFast(this string value, char reference)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] == reference)
			{
				return true;
			}
		}
		return false;
	}

	public static bool StartsWithFast(this string value, string reference)
	{
		if (reference.Length > value.Length)
		{
			return false;
		}
		for (int i = reference.Length - 1; i >= 0; i--)
		{
			if (value[i] != reference[i])
			{
				return false;
			}
		}
		return true;
	}

	public static bool StartsWithFast(this string value, string reference, int offset)
	{
		if (reference.Length + offset > value.Length)
		{
			return false;
		}
		for (int i = reference.Length + offset - 1; i >= offset; i--)
		{
			if (value[i] != reference[i - offset])
			{
				return false;
			}
		}
		return true;
	}

	public static bool EqualsFast(this string value, string reference)
	{
		if (reference.Length != value.Length)
		{
			return false;
		}
		for (int i = reference.Length - 1; i >= 0; i--)
		{
			if (value[i] != reference[i])
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// A fast case-insensitive string comparison for "ordinal" culture i.e. plain ASCII comparison used for internal strings such as asset paths 
	/// </summary>
	/// <param name="value"></param>
	/// <param name="reference"></param>
	/// <returns></returns>
	public static bool EqualsFastIgnoreCase(this string value, string reference)
	{
		if (reference.Length != value.Length)
		{
			return false;
		}
		for (int i = reference.Length - 1; i >= 0; i--)
		{
			char a;
			char b;
			if ((a = value[i]) != (b = reference[i]) && ((a & 0xFFDF) != (b & 0xFFDF) || (a & 0xFFDF) < 65 || (a & 0xFFDF) > 90))
			{
				return false;
			}
		}
		return true;
	}

	public static bool FastStartsWith(string value, string reference, int len)
	{
		if (len > reference.Length)
		{
			throw new ArgumentException("reference must be longer than len");
		}
		if (len > value.Length)
		{
			return false;
		}
		for (int i = 0; i < len; i++)
		{
			if (value[i] != reference[i])
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Removes diacritics and replaces quotation marks, guillemets and brackets with a blank space. Used to create a search friendly term
	/// </summary>
	/// <param name="stIn"></param>
	/// <returns></returns>
	public static string ToSearchFriendly(this string stIn)
	{
		string stFormD = stIn.Normalize(NormalizationForm.FormD);
		StringBuilder sb = new StringBuilder();
		foreach (char chr in stFormD)
		{
			if (chr == '«' || chr == '»' || chr == '"' || chr == '(' || chr == ')')
			{
				sb.Append(' ');
			}
			else if (CharUnicodeInfo.GetUnicodeCategory(chr) != UnicodeCategory.NonSpacingMark)
			{
				sb.Append(chr);
			}
		}
		return sb.ToString().Normalize(NormalizationForm.FormC);
	}
}
