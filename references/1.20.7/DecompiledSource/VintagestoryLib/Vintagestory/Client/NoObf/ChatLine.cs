using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ChatLine
{
	public string Text;

	public long PostedMs;

	public int TextColor;

	public int BackgroundColor;

	private ChatLine()
	{
	}

	public ChatLine Create(string text, EnumChatType chatType, long msEllapsed)
	{
		return new ChatLine
		{
			Text = text,
			PostedMs = msEllapsed,
			TextColor = TextColorFromChatSource(chatType),
			BackgroundColor = BackColorFromChatSource(chatType)
		};
	}

	private int BackColorFromChatSource(EnumChatType chatType)
	{
		if (chatType == EnumChatType.Notification)
		{
			return -1;
		}
		return 0;
	}

	private int TextColorFromChatSource(EnumChatType chatType)
	{
		return chatType switch
		{
			EnumChatType.CommandError => ColorUtil.ToRgba(255, 255, 192, 192), 
			EnumChatType.CommandSuccess => ColorUtil.ToRgba(255, 192, 255, 192), 
			EnumChatType.OwnMessage => ColorUtil.ToRgba(255, 192, 192, 192), 
			EnumChatType.Notification => ColorUtil.BlackArgb, 
			_ => -1, 
		};
	}
}
