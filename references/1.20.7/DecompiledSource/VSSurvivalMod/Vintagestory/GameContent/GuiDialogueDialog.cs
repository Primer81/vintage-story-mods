using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogueDialog : GuiDialog
{
	protected GuiDialog chatDialog;

	private ElementBounds clipBounds;

	private GuiElementRichtext textElem;

	private EntityAgent npcEntity;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogueDialog(ICoreClientAPI capi, EntityAgent npcEntity)
		: base(capi)
	{
		this.npcEntity = npcEntity;
	}

	public void InitAndOpen()
	{
		Compose();
		TryOpen();
	}

	public void ClearDialogue()
	{
		RichTextComponentBase[] components = textElem.Components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].Dispose();
		}
		GuiElementRichtext guiElementRichtext = textElem;
		components = new RichTextComponent[0];
		guiElementRichtext.SetNewText(components);
	}

	public void EmitDialogue(RichTextComponentBase[] cmps)
	{
		RichTextComponentBase[] components = textElem.Components;
		for (int i = 0; i < components.Length; i++)
		{
			if (components[i] is LinkTextComponent linkComp)
			{
				linkComp.Clickable = false;
			}
		}
		textElem.AppendText(cmps);
		updateScrollbarBounds();
	}

	public override void OnKeyDown(KeyEvent args)
	{
		if (args.KeyCode >= 110 && args.KeyCode < 118)
		{
			int index = args.KeyCode - 110;
			int i = 0;
			RichTextComponentBase[] components = textElem.Components;
			for (int j = 0; j < components.Length; j++)
			{
				if (components[j] is LinkTextComponent { Clickable: not false } linkComp)
				{
					if (i == index)
					{
						linkComp.Trigger();
						args.Handled = true;
						break;
					}
					i++;
				}
			}
		}
		base.OnKeyDown(args);
	}

	public void Compose()
	{
		ClearComposers();
		CairoFont.WhiteMediumText().WithFont(GuiStyle.DecorativeFontName).WithColor(GuiStyle.DiscoveryTextColor)
			.WithStroke(GuiStyle.DialogBorderColor, 2.0)
			.WithOrientation(EnumTextOrientation.Center);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		int w = 600;
		int h = 470;
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 30.0, w, h);
		clipBounds = textBounds.ForkBoundingParent();
		ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(3.0 + textBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		string traderName = npcEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
		string dlgTitle = Lang.Get("tradingwindow-" + npcEntity.Code.Path, traderName);
		base.SingleComposer = capi.Gui.CreateCompo("dialogue-" + npcEntity.EntityId, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(dlgTitle, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.BeginClip(clipBounds)
			.AddInset(insetBounds, 3)
			.AddRichtext("", CairoFont.WhiteSmallText(), textBounds.WithFixedPadding(5.0).WithFixedSize(w - 10, h - 10), "dialogueText")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
			.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, leftButton.FixedUnder(clipBounds, 20.0))
			.Compose();
		textElem = base.SingleComposer.GetRichtext("dialogueText");
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private bool OnByeClicked()
	{
		TryClose();
		return true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
	}

	private void updateScrollbarBounds()
	{
		if (textElem != null)
		{
			GuiElementScrollbar scrollbar = base.SingleComposer.GetScrollbar("scrollbar");
			scrollbar.Bounds.CalcWorldBounds();
			scrollbar.SetHeights((float)clipBounds.fixedHeight, (float)textElem.Bounds.fixedHeight);
			scrollbar.ScrollToBottom();
		}
	}

	private void OnNewScrollbarValue(float value)
	{
		textElem.Bounds.fixedY = 0f - value;
		textElem.Bounds.CalcWorldBounds();
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		EntityPos playerPos = capi.World.Player.Entity.Pos;
		if (IsOpened() && playerPos.SquareDistanceTo(npcEntity.Pos) > 25f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closedlg");
		}
	}
}
