namespace Vintagestory.API.Common;

public class TextCommandResult
{
	public string ErrorCode;

	/// <summary>
	/// Will be displayed with a Lang.Get()
	/// </summary>
	public string StatusMessage;

	public EnumCommandStatus Status;

	public object Data;

	public object[] MessageParams;

	public static TextCommandResult Deferred => new TextCommandResult
	{
		Status = EnumCommandStatus.Deferred
	};

	public static OnCommandDelegate DeferredHandler => (TextCommandCallingArgs args) => Deferred;

	public static TextCommandResult Success(string message = "", object data = null)
	{
		return new TextCommandResult
		{
			Status = EnumCommandStatus.Success,
			Data = data,
			StatusMessage = message
		};
	}

	public static TextCommandResult Error(string message, string errorCode = "")
	{
		return new TextCommandResult
		{
			Status = EnumCommandStatus.Error,
			StatusMessage = message,
			ErrorCode = errorCode
		};
	}
}
