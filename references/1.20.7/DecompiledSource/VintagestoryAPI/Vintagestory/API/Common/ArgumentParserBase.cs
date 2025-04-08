using System;
using System.Collections.Generic;
using System.Text;

namespace Vintagestory.API.Common;

public abstract class ArgumentParserBase : ICommandArgumentParser
{
	protected string lastErrorMessage;

	protected bool isMandatoryArg;

	protected int argCount = 1;

	protected string argName;

	public string LastErrorMessage => lastErrorMessage;

	public string ArgumentName => argName;

	public bool IsMandatoryArg => isMandatoryArg;

	public bool IsMissing { get; set; }

	public int ArgCount => argCount;

	protected ArgumentParserBase(string argName, bool isMandatoryArg)
	{
		this.argName = argName;
		this.isMandatoryArg = isMandatoryArg;
	}

	public virtual string[] GetValidRange(CmdArgs args)
	{
		return null;
	}

	public abstract object GetValue();

	public abstract void SetValue(object data);

	public virtual string GetSyntax()
	{
		if (!isMandatoryArg)
		{
			return "<i>[" + argName + "]</i>";
		}
		return "<i>&lt;" + argName + "&gt;</i>";
	}

	public virtual string GetSyntaxUnformatted()
	{
		if (!isMandatoryArg)
		{
			return "[" + argName + "]";
		}
		return "&lt;" + argName + "&gt;";
	}

	public virtual string GetSyntaxExplanation(string indent)
	{
		return null;
	}

	public virtual string GetLastError()
	{
		return lastErrorMessage;
	}

	public abstract EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null);

	protected Dictionary<string, string> parseSubArgs(string strargs)
	{
		Dictionary<string, string> subargs = new Dictionary<string, string>();
		if (strargs.Length == 0)
		{
			return subargs;
		}
		StringBuilder argssb = new StringBuilder();
		bool inside = false;
		foreach (char a in strargs)
		{
			switch (a)
			{
			case '[':
				inside = true;
				continue;
			default:
				if (inside)
				{
					argssb.Append(a);
				}
				continue;
			case ']':
				break;
			}
			break;
		}
		string[] array = argssb.ToString().Split(',');
		foreach (string arg in array)
		{
			if (arg.Length != 0)
			{
				string[] keyval = arg.Split('=');
				if (keyval.Length >= 2)
				{
					subargs[keyval[0].ToLowerInvariant().Trim()] = keyval[1].Trim();
				}
			}
		}
		return subargs;
	}

	public virtual void PreProcess(TextCommandCallingArgs args)
	{
		IsMissing = args.RawArgs.Length == 0;
	}
}
