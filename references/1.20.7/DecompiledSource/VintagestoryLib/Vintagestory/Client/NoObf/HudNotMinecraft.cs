using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class HudNotMinecraft : HudElement
{
	private LoadedTexture crossTexture;

	private Block grassBlock;

	private ItemSlot dummySlot;

	private ElementBounds crossBounds;

	public override bool Focusable => false;

	public HudNotMinecraft(ICoreClientAPI capi)
		: base(capi)
	{
		capi.ChatCommands.Create("notminecraft").WithDescription("No, this is not Minecraft").HandleWith(OnNotMinecraft);
	}

	private TextCommandResult OnNotMinecraft(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (IsOpened())
		{
			TryClose();
		}
		else
		{
			TryOpen();
		}
		return TextCommandResult.Success();
	}

	public override void OnGuiOpened()
	{
		grassBlock = capi.World.GetBlock(new AssetLocation("soil-medium-normal"));
		dummySlot = new DummySlot(new ItemStack(grassBlock));
		string text = "No, this is not Minecraft";
		double textWidth = CairoFont.WhiteSmallishText().GetTextExtents(text).Width / (double)RuntimeEnv.GUIScale;
		ElementBounds.Fixed(EnumDialogArea.RightBottom, 0.0, 0.0, 45.0, 20.0);
		ElementBounds textBounds = ElementBounds.Fixed(47.0, 13.0, 330.0, 40.0);
		ElementBounds hudbounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, textWidth + 60.0, 50.0).WithFixedAlignmentOffset(10.0, 10.0);
		crossBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 35.0, 35.0).WithFixedPadding(17.0, 17.0);
		crossBounds.WithParent(hudbounds);
		crossBounds.CalcWorldBounds();
		base.SingleComposer = capi.Gui.CreateCompo("notminecraftdialog", hudbounds).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false).BeginChildElements()
			.AddStaticText(text, CairoFont.WhiteSmallishText(), textBounds)
			.EndChildElements()
			.Compose();
		TryOpen();
		ClientMain obj = capi.World as ClientMain;
		obj.LastReceivedMilliseconds = obj.ElapsedMilliseconds;
		crossTexture = capi.Gui.Icons.GenTexture((int)crossBounds.InnerWidth, (int)crossBounds.InnerHeight, delegate(Context ctx, ImageSurface surface)
		{
			ctx.SetSourceRGBA(0.8, 0.0, 0.0, 1.0);
			capi.Gui.Icons.DrawCross(ctx, 0.0, 0.0, 4.0, crossBounds.InnerWidth - GuiElement.scaled(5.0));
		});
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
		if (IsOpened())
		{
			int size = (int)GuiElement.scaled(22.0);
			capi.Render.RenderItemstackToGui(dummySlot, crossBounds.drawX + crossBounds.InnerWidth / 2.0, crossBounds.drawY + crossBounds.InnerHeight / 2.0, 50.0, size, -1);
			capi.Render.Render2DLoadedTexture(crossTexture, (int)crossBounds.drawX, (int)crossBounds.drawY, 200f);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
	}
}
