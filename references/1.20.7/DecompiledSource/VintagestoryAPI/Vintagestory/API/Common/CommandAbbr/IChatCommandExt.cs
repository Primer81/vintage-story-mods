namespace Vintagestory.API.Common.CommandAbbr;

/// <summary>
/// ChatCommand Abbreviations
/// </summary>
public static class IChatCommandExt
{
	/// <summary>
	/// Alias of WithDescription()
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="description"></param>
	/// <returns></returns>
	public static IChatCommand WithDesc(this IChatCommand cmd, string description)
	{
		return cmd.WithDescription(description);
	}

	/// <summary>
	/// Alias for BeginSubCommand
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	public static IChatCommand BeginSub(this IChatCommand cmd, string name)
	{
		return cmd.BeginSubCommand(name);
	}

	/// <summary>
	/// Alias for BeginSubCommands
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	public static IChatCommand BeginSubs(this IChatCommand cmd, params string[] name)
	{
		return cmd.BeginSubCommands(name);
	}

	/// <summary>
	/// Alias for EndSubCommand
	/// </summary>
	/// <param name="cmd"></param>
	/// <returns></returns>
	public static IChatCommand EndSub(this IChatCommand cmd)
	{
		return cmd.EndSubCommand();
	}
}
