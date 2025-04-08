using System;
using System.IO;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogSignPost : GuiDialogGeneric
{
	private BlockPos blockEntityPos;

	public Action<string[]> OnTextChanged;

	public Action OnCloseCancel;

	private bool didSave;

	private CairoFont signPostFont;

	private bool ignorechange;

	public GuiDialogSignPost(string DialogTitle, BlockPos blockEntityPos, string[] textByCardinalDirection, ICoreClientAPI capi, CairoFont signPostFont)
		: base(DialogTitle, capi)
	{
		this.signPostFont = signPostFont;
		this.blockEntityPos = blockEntityPos;
		ElementBounds line = ElementBounds.Fixed(0.0, 0.0, 150.0, 20.0);
		ElementBounds input = ElementBounds.Fixed(0.0, 15.0, 150.0, 25.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(60.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
		float inputLineY = 27f;
		float textLineY = 32f;
		float width = 250f;
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText(Lang.Get("North"), CairoFont.WhiteDetailText(), line = line.BelowCopy().WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy().WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text0")
			.AddStaticText(Lang.Get("Northeast"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text1")
			.AddStaticText(Lang.Get("East"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text2")
			.AddStaticText(Lang.Get("Southeast"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text3")
			.AddStaticText(Lang.Get("South"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text4")
			.AddStaticText(Lang.Get("Southwest"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text5")
			.AddStaticText(Lang.Get("West"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text6")
			.AddStaticText(Lang.Get("Northwest"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, textLineY).WithFixedWidth(width))
			.AddTextInput(input = input.BelowCopy(0.0, inputLineY).WithFixedWidth(width), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text7")
			.AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, input = input.BelowCopy(0.0, 20.0).WithFixedSize(100.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(10.0, 2.0))
			.AddSmallButton(Lang.Get("Save"), OnButtonSave, input = input.FlatCopy().WithFixedSize(100.0, 20.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
		for (int i = 0; i < 8; i++)
		{
			base.SingleComposer.GetTextInput("text" + i).SetValue(textByCardinalDirection[i]);
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
	}

	private void OnTextChangedDlg(string text)
	{
		if (ignorechange)
		{
			return;
		}
		ignorechange = true;
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context ctx = new Context(surface);
		signPostFont.SetupContext(ctx);
		string[] textByCardinal = new string[8];
		for (int i = 0; i < 8; i++)
		{
			GuiElementTextInput texinput = base.SingleComposer.GetTextInput("text" + i);
			textByCardinal[i] = texinput.GetText();
			if (textByCardinal[i] == null)
			{
				textByCardinal[i] = "";
			}
			int j = 0;
			while (ctx.TextExtents(textByCardinal[i]).Width > 185.0 && j++ < 100)
			{
				textByCardinal[i] = textByCardinal[i].Substring(0, textByCardinal[i].Length - 1);
			}
			texinput.SetValue(textByCardinal[i]);
		}
		OnTextChanged?.Invoke(textByCardinal);
		ignorechange = false;
		surface.Dispose();
		ctx.Dispose();
	}

	private void OnTitleBarClose()
	{
		OnButtonCancel();
	}

	private bool OnButtonSave()
	{
		string[] textByCardinal = new string[8];
		for (int j = 0; j < 8; j++)
		{
			GuiElementTextInput texinput = base.SingleComposer.GetTextInput("text" + j);
			textByCardinal[j] = texinput.GetText();
		}
		byte[] data;
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter writer = new BinaryWriter(ms);
			for (int i = 0; i < 8; i++)
			{
				writer.Write(textByCardinal[i]);
			}
			data = ms.ToArray();
		}
		capi.Network.SendBlockEntityPacket(blockEntityPos, 1002, data);
		didSave = true;
		TryClose();
		return true;
	}

	private bool OnButtonCancel()
	{
		TryClose();
		return true;
	}

	public override void OnGuiClosed()
	{
		if (!didSave)
		{
			OnCloseCancel?.Invoke();
		}
		base.OnGuiClosed();
	}
}
