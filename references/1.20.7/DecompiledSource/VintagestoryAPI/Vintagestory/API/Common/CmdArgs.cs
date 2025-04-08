using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

/// <summary>
/// The arguments from a client or sever command
/// </summary>
public class CmdArgs
{
	private List<string> args = new List<string>();

	/// <summary>
	/// Returns the n-th arugment
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public string this[int index]
	{
		get
		{
			return args[index];
		}
		set
		{
			args[index] = value;
		}
	}

	/// <summary>
	/// Amount of arguments passed
	/// </summary>
	public int Length => args.Count;

	/// <summary>
	/// Creates a new instance of the CmdArgs util with no arguments
	/// </summary>
	public CmdArgs()
	{
	}

	/// <summary>
	/// Creates a new instance of the CmdArgs util
	/// </summary>
	/// <param name="joinedargs"></param>
	public CmdArgs(string joinedargs)
	{
		string trimmed = Regex.Replace(joinedargs.Trim(), "\\s+", " ");
		Push(trimmed);
	}

	public void Push(string joinedargs)
	{
		string[] args = new string[0];
		if (joinedargs.Length > 0)
		{
			args = joinedargs.Split(' ');
		}
		this.args.AddRange(args);
	}

	/// <summary>
	/// Creates a new instance of the CmdArgs util
	/// </summary>
	/// <param name="args"></param>
	public CmdArgs(string[] args)
	{
		this.args = new List<string>(args);
	}

	/// <summary>
	/// Returns all remaining arguments as single merged string, concatenated with spaces
	/// </summary>
	/// <returns></returns>
	public string PopAll()
	{
		string result = string.Join(" ", args.ToArray(), 0, args.Count);
		args.Clear();
		return result;
	}

	/// <summary>
	/// Returns the first char of the first argument
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public char? PeekChar(char? defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		if (args[0].Length == 0)
		{
			return '\0';
		}
		return args[0][0];
	}

	/// <summary>
	/// Remove the first character from the first argument and returns it
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public char? PopChar(char? defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		string text = args[0];
		args[0] = args[0].Substring(1);
		return text[0];
	}

	/// <summary>
	/// Removes the first argument and returns it, scans until it encounters a white space
	/// </summary>
	public string PopWord(string defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		string result = args[0];
		args.RemoveAt(0);
		return result;
	}

	/// <summary>
	/// Removes the first argument and returns it
	/// </summary>
	public string PeekWord(string defaultValue = null)
	{
		if (args.Count == 0)
		{
			return defaultValue;
		}
		return args[0];
	}

	public string PopUntil(char endChar)
	{
		StringBuilder sb = new StringBuilder();
		string all = PopAll();
		for (int i = 0; i < all.Length && all[i] != endChar; i++)
		{
			sb.Append(all[i]);
		}
		Push(all.Substring(sb.Length).TrimStart());
		return sb.ToString();
	}

	public string PopCodeBlock(char blockOpenChar, char blockCloseChar, out string parseErrorMsg)
	{
		parseErrorMsg = null;
		string all = PopAll();
		StringBuilder sb = new StringBuilder();
		int depth = 0;
		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == blockOpenChar)
			{
				depth++;
			}
			if (depth == 0)
			{
				ReadOnlySpan<char> readOnlySpan = "First character is not ";
				char reference = blockOpenChar;
				parseErrorMsg = Lang.Get(string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference), ". Please consume all input until the block open char"));
				return null;
			}
			if (depth > 0)
			{
				sb.Append(all[i]);
			}
			if (all[i] == blockCloseChar)
			{
				depth--;
				if (depth <= 0)
				{
					break;
				}
			}
		}
		if (depth > 0)
		{
			ReadOnlySpan<char> readOnlySpan2 = "Incomplete block. At least one ";
			char reference = blockCloseChar;
			parseErrorMsg = Lang.Get(string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference), " is missing"));
			return null;
		}
		Push(all.Substring(sb.Length).TrimStart());
		return sb.ToString();
	}

	/// <summary>
	/// Adds an arg to the beginning
	/// </summary>
	/// <param name="arg"></param>
	/// <returns></returns>
	public void PushSingle(string arg)
	{
		args.Insert(0, arg);
	}

	/// <summary>
	/// Adds an arg to the end
	/// </summary>
	/// <param name="arg"></param>
	public void AppendSingle(string arg)
	{
		args.Add(arg);
	}

	/// <summary>
	/// Tries to retrieve arg at given index as enum value or default if not enough arguments or not part of the enum
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public T PopEnum<T>(T defaultValue = default(T))
	{
		string arg = PopWord();
		if (arg == null)
		{
			return defaultValue;
		}
		if (int.TryParse(arg, out var val) && Enum.IsDefined(typeof(T), val))
		{
			return (T)Enum.ToObject(typeof(T), val);
		}
		return default(T);
	}

	/// <summary>
	/// Tries to retrieve arg at given index as int, or null if not enough arguments or not an integer
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public int? PopInt(int? defaultValue = null)
	{
		string arg = PopWord();
		if (arg == null)
		{
			return defaultValue;
		}
		if (TryParseIntFancy(arg, out var val))
		{
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Tries to parse a string to an integer, returns 0 if it fails
	/// <br />Enhancements over system int.TryParse():  (1) the thousands separator (comma) is ignored  (2) hex numbers are possible with 0x prefix  (3) uint values over int.MaxValue are possible (may convert to a negative int, but that can be converted back to the original uint), more generally any valid long value is permitted
	/// <br />All three enhancements are useful if we ever need, in a command, to set - for example - a color value (32 bits) from hex or unsigned decimal
	/// </summary>
	/// <param name="arg"></param>
	/// <param name="val"></param>
	/// <returns></returns>
	private bool TryParseIntFancy(string arg, out int val)
	{
		arg = arg.Replace(",", "");
		if (arg.StartsWith("0x") && int.TryParse(arg.Substring(2), NumberStyles.HexNumber, GlobalConstants.DefaultCultureInfo, out val))
		{
			return true;
		}
		if (long.TryParse(arg, out var longval))
		{
			val = (int)longval;
			return true;
		}
		val = 0;
		return false;
	}

	/// <summary>
	/// Tries to retrieve arg at given index as long, or null if not enough arguments or not a long
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public long? PopLong(long? defaultValue = null)
	{
		string arg = PopWord();
		if (arg == null)
		{
			return defaultValue;
		}
		if (long.TryParse(arg, out var val))
		{
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Tries to retrieve arg at given index as boolean, or null if not enough arguments or not an integer
	/// <br />'true', 'yes' and '1' will all be interpreted as true.  Parameter trueAlias (with default value 'on') allows one additional word to be used to signify true.  Anything else will return false.
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <param name="trueAlias"></param>
	/// <returns></returns>
	public bool? PopBool(bool? defaultValue = null, string trueAlias = "on")
	{
		string arg = PopWord()?.ToLowerInvariant();
		if (arg == null)
		{
			return defaultValue;
		}
		int value;
		switch (arg)
		{
		default:
			value = ((arg == trueAlias) ? 1 : 0);
			break;
		case "1":
		case "yes":
		case "true":
		case "on":
			value = 1;
			break;
		}
		return (byte)value != 0;
	}

	/// <summary>
	/// Tries to retrieve arg at given index as int, or null if not enough arguments or not an integer
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public double? PopDouble(double? defaultValue = null)
	{
		string arg = PopWord();
		if (arg == null)
		{
			return defaultValue;
		}
		return arg.ToDoubleOrNull(defaultValue);
	}

	/// <summary>
	/// Tries to retrieve arg at given index as float, or null if not enough arguments or not a float
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public float? PopFloat(float? defaultValue = null)
	{
		string arg = PopWord();
		if (arg == null)
		{
			return defaultValue;
		}
		return arg.ToFloatOrNull(defaultValue);
	}

	/// <summary>
	/// Tries to retrieve 3 int coordinates from the next 3 arguments
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public Vec3i PopVec3i(Vec3i defaultValue = null)
	{
		int? x = PopInt(defaultValue?.X);
		int? y = PopInt(defaultValue?.Y);
		int? z = PopInt(defaultValue?.Z);
		if (!x.HasValue || !y.HasValue || !z.HasValue)
		{
			return defaultValue;
		}
		return new Vec3i(x.Value, y.Value, z.Value);
	}

	public Vec3d PopVec3d(Vec3d defaultValue = null)
	{
		double? x = PopDouble(defaultValue?.X);
		double? y = PopDouble(defaultValue?.Y);
		double? z = PopDouble(defaultValue?.Z);
		if (!x.HasValue || !y.HasValue || !z.HasValue)
		{
			return defaultValue;
		}
		return new Vec3d(x.Value, y.Value, z.Value);
	}

	/// <summary>
	/// Retrieves a player position with following syntax:
	/// [coord] [coord] [coord]
	/// whereas 
	/// [coord] may be ~[decimal]  or =[decimal] or [decimal]
	/// ~ denotes a position relative to the player
	/// = denotes an absolute position
	/// no prefix denots a position relative to the map middle
	/// </summary>
	/// <param name="playerPos"></param>
	/// <param name="mapMiddle"></param>
	/// <returns></returns>
	public Vec3d PopFlexiblePos(Vec3d playerPos, Vec3d mapMiddle)
	{
		if (args.Count < 3)
		{
			return null;
		}
		Vec3d outPos = new Vec3d();
		for (int i = 0; i < 3; i++)
		{
			switch (PeekChar().Value)
			{
			case '~':
			{
				PopChar();
				double? val;
				if (PeekChar() != '\0')
				{
					val = PopDouble();
					if (!val.HasValue)
					{
						return null;
					}
				}
				else
				{
					val = 0.0;
					PopWord();
				}
				outPos[i] = val.Value + playerPos[i];
				break;
			}
			case '=':
			{
				PopChar();
				double? val = PopDouble();
				if (!val.HasValue)
				{
					return null;
				}
				outPos[i] = val.Value;
				break;
			}
			default:
			{
				double? val = PopDouble();
				if (!val.HasValue)
				{
					return null;
				}
				outPos[i] = val.Value + mapMiddle[i];
				break;
			}
			}
		}
		return outPos;
	}

	/// <summary>
	/// Retrieves a player position with following syntax:
	/// [coord] [coord] [coord]
	/// whereas 
	/// [coord] may be ~[decimal]  or =[decimal] or [decimal]
	/// ~ denotes a position relative to the player
	/// = denotes an absolute position
	/// no prefix denots a position relative to the map middle
	/// </summary>
	/// <param name="playerPos"></param>
	/// <param name="mapMiddle"></param>
	/// <returns></returns>
	public Vec2i PopFlexiblePos2D(Vec3d playerPos, Vec3d mapMiddle)
	{
		if (args.Count < 2)
		{
			return null;
		}
		Vec2i outPos = new Vec2i();
		for (int i = 0; i < 2; i++)
		{
			double? val;
			switch (PeekChar().Value)
			{
			case '~':
				PopChar();
				if (PeekChar() != '\0')
				{
					val = PopDouble();
					if (!val.HasValue)
					{
						return null;
					}
				}
				else
				{
					val = 0.0;
					PopWord();
				}
				outPos[i] = (int)(val.Value + playerPos[i * 2]);
				continue;
			case '=':
				PopChar();
				val = PopDouble();
				if (!val.HasValue)
				{
					return null;
				}
				outPos[i] = (int)val.Value;
				continue;
			case '+':
				PopChar();
				break;
			}
			val = PopDouble();
			if (!val.HasValue)
			{
				return null;
			}
			outPos[i] = (int)(val.Value + mapMiddle[i * 2]);
		}
		return outPos;
	}

	public CmdArgs Clone()
	{
		return new CmdArgs(args.ToArray());
	}
}
