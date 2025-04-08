using System;

namespace Vintagestory.API.Common;

public interface ICommandArgumentParser
{
	/// <summary>
	/// Return -1 to ignore arg count checking
	/// </summary>
	int ArgCount { get; }

	string LastErrorMessage { get; }

	string ArgumentName { get; }

	bool IsMandatoryArg { get; }

	bool IsMissing { get; set; }

	void PreProcess(TextCommandCallingArgs args);

	/// <summary>
	/// Parse the args.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="onReady">Only needs to be called when returning Deferred as parseresult</param>
	/// <returns></returns>
	EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null);

	string[] GetValidRange(CmdArgs args);

	object GetValue();

	string GetSyntax();

	string GetSyntaxExplanation(string indent);

	/// <summary>
	/// Used by the async system
	/// </summary>
	/// <param name="data"></param>
	void SetValue(object data);
}
