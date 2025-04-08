namespace Vintagestory.API.Common;

/// <summary>
/// For performance, don't build and store new concatenated strings for every block variant, item and entity, when these will only be used (if ever) for error logging
/// </summary>
public struct SourceStringComponents
{
	private readonly string message;

	private readonly string domain;

	private readonly string path;

	private readonly int alternate;

	private readonly object[] formattedArguments;

	/// <summary>
	/// Store references to the source strings, to be able to build a logging string later if necessary
	/// </summary>
	public SourceStringComponents(string message, string sourceDomain, string sourcePath, int sourceAlt)
	{
		formattedArguments = null;
		this.message = message;
		domain = sourceDomain;
		path = sourcePath;
		alternate = sourceAlt;
	}

	public SourceStringComponents(string message, AssetLocation source, int sourceAlt = -1)
	{
		formattedArguments = null;
		this.message = message;
		domain = source.Domain;
		path = source.Path;
		alternate = -1;
	}

	public SourceStringComponents(string formattedString, params object[] arguments)
	{
		domain = null;
		path = null;
		alternate = 0;
		message = formattedString;
		formattedArguments = arguments;
	}

	public override string ToString()
	{
		if (formattedArguments != null)
		{
			return string.Format(message, formattedArguments);
		}
		if (alternate >= 0)
		{
			return message + domain + ":" + path + " alternate:" + alternate;
		}
		return message + domain + ":" + path;
	}
}
