using System;
using System.Collections.Generic;
using System.Text;

namespace ProperVersion;

/// <summary>
///   Implementation of Semantic Verisoning standard, version 2.0.0.
///   See https://semver.org/ for specifications.
/// </summary>
public class SemVer : IComparable<SemVer>, IEquatable<SemVer>
{
	private static readonly string[] PART_LOOKUP = new string[5] { "MAJOR", "MINOR", "PATCH", "PRE_RELEASE", "BUILD_METADATA" };

	public int Major { get; }

	public int Minor { get; }

	public int Patch { get; }

	public string[] PreReleaseIdentifiers { get; }

	public string[] BuildMetadataIdentifiers { get; }

	public string PreRelease => string.Join(".", PreReleaseIdentifiers);

	public string BuildMetadata => string.Join(".", BuildMetadataIdentifiers);

	public SemVer(int major, int minor, int patch)
		: this(major, minor, patch, new string[0], null, new string[0], null)
	{
	}

	public SemVer(int major, int minor, int patch, string preRelease = "", string buildMetadata = "")
		: this(major, minor, patch, SplitIdentifiers(preRelease), "preRelease", SplitIdentifiers(buildMetadata), "buildMetadata")
	{
	}

	public SemVer(int major, int minor, int patch, string[] preReleaseIdentifiers = null, string[] buildMetadataIdentifiers = null)
		: this(major, minor, patch, preReleaseIdentifiers, "preReleaseIdentifiers", buildMetadataIdentifiers, "buildMetadataIdentifiers")
	{
	}

	private SemVer(int major, int minor, int patch, string[] preReleaseIdentifiers, string preReleaseParamName, string[] buildMetadataIdentifiers, string buildMetadataParamName)
	{
		if (major < 0)
		{
			throw new ArgumentOutOfRangeException("major", major, "Major value must be 0 or positive");
		}
		if (minor < 0)
		{
			throw new ArgumentOutOfRangeException("minor", minor, "Minor value must be 0 or positive");
		}
		if (patch < 0)
		{
			throw new ArgumentOutOfRangeException("patch", patch, "Patch value must be 0 or positive");
		}
		if (preReleaseIdentifiers == null)
		{
			preReleaseIdentifiers = new string[0];
		}
		for (int j = 0; j < preReleaseIdentifiers.Length; j++)
		{
			string ident2 = preReleaseIdentifiers[j];
			if (ident2 == null)
			{
				throw new ArgumentException($"{preReleaseParamName} contains null element at index {j}", preReleaseParamName);
			}
			if (ident2.Length == 0)
			{
				throw new ArgumentException($"{preReleaseParamName} contains empty identifier at index {j}", preReleaseParamName);
			}
			if (!IsValidIdentifier(ident2))
			{
				throw new ArgumentException($"{preReleaseParamName} contains invalid identifier ('{ident2}') at index {j}", preReleaseParamName);
			}
			if (IsNumericIdent(ident2) && ident2[0] == '0' && ident2.Length > 1)
			{
				throw new ArgumentException($"{preReleaseParamName} contains numeric identifier with leading zero(es) at index {j}", preReleaseParamName);
			}
		}
		if (buildMetadataIdentifiers == null)
		{
			buildMetadataIdentifiers = new string[0];
		}
		for (int i = 0; i < buildMetadataIdentifiers.Length; i++)
		{
			string ident = buildMetadataIdentifiers[i];
			if (ident == null)
			{
				throw new ArgumentException($"{buildMetadataParamName} contains null element at index {i}", buildMetadataParamName);
			}
			if (ident.Length == 0)
			{
				throw new ArgumentException($"{buildMetadataParamName} contains empty identifier at index {i}", buildMetadataParamName);
			}
			if (!IsValidIdentifier(ident))
			{
				throw new ArgumentException($"{buildMetadataParamName} contains invalid identifier ('{ident}') at index {i}", buildMetadataParamName);
			}
		}
		Major = major;
		Minor = minor;
		Patch = patch;
		PreReleaseIdentifiers = preReleaseIdentifiers;
		BuildMetadataIdentifiers = buildMetadataIdentifiers;
	}

	/// <summary>
	///   Converts the specified string representation of a
	///   semantic version to its <see cref="T:ProperVersion.SemVer" /> equivalent.
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException"> Thrown if the specified string is null. </exception>
	/// <exception cref="T:System.FormatException"> Thrown if the specified string doesn't contain a proper properly formatted semantic version. </exception>
	public static SemVer Parse(string s)
	{
		TryParse(s, out var result, throwException: true);
		return result;
	}

	/// <summary>
	///   Tries to convert the specified string representation of a
	///   semantic version to its <see cref="T:ProperVersion.SemVer" /> equivalent,
	///   returning true if successful.
	/// </summary>
	/// <param name="s"></param>
	/// <param name="result">
	///   When this method returns, contains a valid, non-null SemVer,
	///   If the conversion failed, this is set to the parser's best guess.
	/// </param>
	/// <exception cref="T:System.ArgumentNullException"> Thrown if the specified string is null. </exception>
	public static bool TryParse(string s, out SemVer result)
	{
		string error;
		return TryParse(s, out result, out error);
	}

	/// <summary>
	///   Tries to convert the specified string representation of a
	///   semantic version to its <see cref="T:ProperVersion.SemVer" /> equivalent,
	///   returning true if successful.
	/// </summary>
	/// <param name="s"></param>
	/// <param name="result">
	///   When this method returns, contains a valid, non-null SemVer,
	///   If the conversion failed, this is set to the method's best guess.
	/// </param>
	/// <param name="error">
	///   When this method returns, contains the first error describing
	///   why the conversion failed, or null if it succeeded.
	/// </param>
	/// <exception cref="T:System.ArgumentNullException"> Thrown if the specified string is null. </exception>
	public static bool TryParse(string s, out SemVer result, out string error)
	{
		error = TryParse(s, out result, throwException: false);
		return error == null;
	}

	private static string TryParse(string s, out SemVer result, bool throwException)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		StringBuilder sb = new StringBuilder();
		string error = null;
		int mode = 0;
		int index = 0;
		char? chr = null;
		int[] versions = new int[3];
		List<string>[] data = new List<string>[2]
		{
			new List<string>(),
			new List<string>()
		};
		for (; index <= s.Length; index++)
		{
			chr = ((index < s.Length) ? new char?(s[index]) : null);
			if (mode <= 2)
			{
				if (chr >= '0' && chr <= '9')
				{
					sb.Append(chr);
					if (sb.Length == 2 && sb[0] == '0')
					{
						Error("{0} version contains leading zero");
					}
					continue;
				}
				if (sb.Length == 0)
				{
					Error("Expected {0} version number, found {1}");
				}
				else
				{
					versions[mode] = int.Parse(sb.ToString());
				}
				sb.Clear();
				if (chr == '.')
				{
					if (mode == 2)
					{
						Error("Expected PRE_RELEASE or BUILD_METADATA, found {1}");
					}
					mode++;
					continue;
				}
				if (mode != 2)
				{
					Error("Expected {0} version, found {1}", nextMode: true);
				}
				if (chr == '-')
				{
					mode = 3;
				}
				else if (chr == '+')
				{
					mode = 4;
				}
				else if (chr.HasValue)
				{
					Error("Expected PRE_RELEASE or BUILD_METADATA, found {1}");
					mode = 3;
					index--;
				}
				continue;
			}
			if (chr.HasValue && IsValidIdentifierChar(chr.Value))
			{
				sb.Append(chr);
				continue;
			}
			if (chr != '.' && chr.HasValue && (chr != '+' || mode != 3))
			{
				Error("Unexpected character {1} in {0} identifier");
				continue;
			}
			if (sb.Length == 0)
			{
				Error("Expected {0} identifier, found {1}");
			}
			else
			{
				string ident = sb.ToString();
				if (mode == 3 && IsNumericIdent(ident) && ident[0] == '0' && ident.Length > 1)
				{
					Error("{0} numeric identifier contains leading zero");
					ident = ident.TrimStart('0');
				}
				data[mode - 3].Add(ident);
			}
			sb.Clear();
			if (chr == '+' && mode == 3)
			{
				mode++;
			}
		}
		result = new SemVer(versions[0], versions[1], versions[2], data[0].ToArray(), data[1].ToArray());
		return error;
		void Error(string message, bool nextMode = false)
		{
			if (error == null)
			{
				error = $"Failed parsing version string '{s}' at index {index}: " + string.Format(message, PART_LOOKUP[nextMode ? (mode + 1) : mode], chr.HasValue ? $"'{chr}'" : "end of string");
				if (throwException)
				{
					throw new FormatException(error);
				}
			}
		}
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder().Append(Major).Append('.').Append(Minor)
			.Append('.')
			.Append(Patch);
		if (PreReleaseIdentifiers.Length != 0)
		{
			sb.Append('-').Append(PreRelease);
		}
		if (BuildMetadataIdentifiers.Length != 0)
		{
			sb.Append('+').Append(BuildMetadata);
		}
		return sb.ToString();
	}

	public static bool operator ==(SemVer left, SemVer right)
	{
		if ((object)left != right)
		{
			return left?.Equals(right) ?? false;
		}
		return true;
	}

	public static bool operator !=(SemVer left, SemVer right)
	{
		return !(left == right);
	}

	public static bool operator >(SemVer left, SemVer right)
	{
		if (left != null && right != null)
		{
			return Compare(left, right) > 0;
		}
		return false;
	}

	public static bool operator <(SemVer left, SemVer right)
	{
		if (left != null && right != null)
		{
			return Compare(left, right) < 0;
		}
		return false;
	}

	public static bool operator >=(SemVer left, SemVer right)
	{
		if (left != null && right != null)
		{
			return Compare(left, right) >= 0;
		}
		return false;
	}

	public static bool operator <=(SemVer left, SemVer right)
	{
		if (left != null && right != null)
		{
			return Compare(left, right) <= 0;
		}
		return false;
	}

	public int CompareTo(SemVer other)
	{
		return Compare(this, other);
	}

	public static int Compare(SemVer left, SemVer right)
	{
		if ((object)left == right)
		{
			return 0;
		}
		if ((object)left == null)
		{
			return -1;
		}
		if ((object)right == null)
		{
			return 1;
		}
		int majorDiff = left.Major.CompareTo(right.Major);
		if (majorDiff != 0)
		{
			return majorDiff;
		}
		int minorDiff = left.Minor.CompareTo(right.Minor);
		if (minorDiff != 0)
		{
			return minorDiff;
		}
		int patchDiff = left.Patch.CompareTo(right.Patch);
		if (patchDiff != 0)
		{
			return patchDiff;
		}
		bool leftHasPreRelease = left.PreReleaseIdentifiers.Length != 0;
		bool rightHasPreRelease = right.PreReleaseIdentifiers.Length != 0;
		if (leftHasPreRelease != rightHasPreRelease)
		{
			if (!leftHasPreRelease)
			{
				return 1;
			}
			return -1;
		}
		int minCount = Math.Min(left.PreReleaseIdentifiers.Length, right.PreReleaseIdentifiers.Length);
		for (int i = 0; i < minCount; i++)
		{
			string leftIndent = left.PreReleaseIdentifiers[i];
			string rightIndent = right.PreReleaseIdentifiers[i];
			bool leftIdentIsNumeric = IsNumericIdent(leftIndent);
			bool rightIdentIsNumeric = IsNumericIdent(rightIndent);
			if (leftIdentIsNumeric != rightIdentIsNumeric)
			{
				if (!leftIdentIsNumeric)
				{
					return 1;
				}
				return -1;
			}
			int identDiff = (leftIdentIsNumeric ? int.Parse(leftIndent).CompareTo(int.Parse(rightIndent)) : string.CompareOrdinal(leftIndent, rightIndent));
			if (identDiff != 0)
			{
				return identDiff;
			}
		}
		return left.PreReleaseIdentifiers.Length - right.PreReleaseIdentifiers.Length;
	}

	public bool Equals(SemVer other)
	{
		if (other != null && Major == other.Major && Minor == other.Minor && Patch == other.Patch && PreRelease == other.PreRelease)
		{
			return BuildMetadata == other.BuildMetadata;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as SemVer);
	}

	public override int GetHashCode()
	{
		return Major ^ (Minor << 8) ^ (Patch << 16) ^ PreRelease.GetHashCode() ^ (BuildMetadata.GetHashCode() << 6);
	}

	/// <summary>
	///   Returns whether the specified string contains only valid
	///   identifier characters. That is, only alphanumeric characters
	///   and hyphens, [0-9A-Za-z-]. Does not check for empty identifiers.
	/// </summary>
	private static bool IsValidIdentifier(string ident)
	{
		for (int i = 0; i < ident.Length; i++)
		{
			if (!IsValidIdentifierChar(ident[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsValidIdentifierChar(char chr)
	{
		if ((chr < '0' || chr > '9') && (chr < 'A' || chr > 'Z') && (chr < 'a' || chr > 'z'))
		{
			return chr == '-';
		}
		return true;
	}

	/// <summary>
	///   Returns whether the specified string is a
	///   numeric identifier (only contains digits).
	/// </summary>
	private static bool IsNumericIdent(string ident)
	{
		for (int i = 0; i < ident.Length; i++)
		{
			if (ident[i] < '0' || ident[i] > '9')
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	///   Splits a string into dot-separated identifiers.
	///   Both null and empty strings return an empty array.
	/// </summary>
	private static string[] SplitIdentifiers(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return new string[0];
		}
		return str.Split('.');
	}
}
