using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityTextInput : GuiDialogTextInput
{
	private BlockPos blockEntityPos;

	public GuiDialogBlockEntityTextInput(string DialogTitle, BlockPos blockEntityPos, string text, ICoreClientAPI capi, TextAreaConfig signConfig)
		: base(DialogTitle, text, capi, signConfig)
	{
		this.blockEntityPos = blockEntityPos;
	}

	public override void OnSave(string text)
	{
		byte[] data = SerializerUtil.Serialize(new EditSignPacket
		{
			Text = text,
			FontSize = FontSize
		});
		capi.Network.SendBlockEntityPacket(blockEntityPos, 1002, data);
	}
}
