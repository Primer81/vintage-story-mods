using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

internal class GuiDialogMacroEditor : GuiDialog
{
	private List<SkillItem> skillItems;

	private int rows = 2;

	private int cols = 8;

	private int selectedIndex = -1;

	private IMacroBase currentMacro;

	private SkillItem currentSkillitem;

	private HotkeyCapturer hotkeyCapturer = new HotkeyCapturer();

	internal IMacroBase SelectedMacro
	{
		get
		{
			(capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(selectedIndex, out var macro);
			return macro;
		}
	}

	public override string ToggleKeyCombinationCode => "macroeditor";

	public override bool PrefersUngrabbedMouse => true;

	public GuiDialogMacroEditor(ICoreClientAPI capi)
		: base(capi)
	{
		skillItems = new List<SkillItem>();
		ComposeDialog();
	}

	private void LoadSkillList()
	{
		skillItems.Clear();
		for (int i = 0; i < cols * rows; i++)
		{
			(capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(i, out var j);
			SkillItem sk;
			if (j == null)
			{
				sk = new SkillItem();
			}
			else
			{
				if (j.iconTexture == null)
				{
					(j as Macro).GenTexture(capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
				}
				sk = new SkillItem
				{
					Code = new AssetLocation(j.Code),
					Name = j.Name,
					Hotkey = j.KeyCombination,
					Texture = j.iconTexture
				};
			}
			skillItems.Add(sk);
		}
	}

	private void ComposeDialog()
	{
		LoadSkillList();
		selectedIndex = 0;
		currentSkillitem = skillItems[0];
		int spacing = 5;
		double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = (double)cols * size;
		ElementBounds macroBounds = ElementBounds.Fixed(0.0, 30.0, innerWidth, (double)rows * size);
		ElementBounds macroInsetBounds = macroBounds.ForkBoundingParent(3.0, 6.0, 3.0, 3.0);
		double halfWidth = innerWidth / 2.0 - 5.0;
		ElementBounds nameBounds = ElementBounds.FixedSize(halfWidth, 30.0).FixedUnder(macroInsetBounds, spacing + 10);
		ElementBounds hotkeyBounds = ElementBounds.Fixed(innerWidth / 2.0 + 8.0, 0.0, halfWidth, 30.0).FixedUnder(macroInsetBounds, spacing + 10);
		ElementBounds nameInputBounds = ElementBounds.FixedSize(halfWidth, 30.0).FixedUnder(nameBounds, spacing - 10);
		ElementBounds hotkeyInputBounds = ElementBounds.Fixed(innerWidth / 2.0 + 8.0, 0.0, halfWidth, 30.0).FixedUnder(hotkeyBounds, spacing - 10);
		ElementBounds commmandsBounds = ElementBounds.FixedSize(300.0, 30.0).FixedUnder(nameInputBounds, spacing + 10);
		ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0, 100.0);
		ElementBounds clippingBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0 - 1.0, 99.0).FixedUnder(commmandsBounds, spacing - 10);
		ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(clippingBounds.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
		ElementBounds clearMacroBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, 6 + 2 * spacing).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds saveMacroBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, 6 + 2 * spacing).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("texteditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Macro Editor"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddInset(macroInsetBounds, 3)
			.BeginChildElements()
			.AddSkillItemGrid(skillItems, cols, rows, OnSlotClick, macroBounds, "skillitemgrid")
			.EndChildElements()
			.AddStaticText(Lang.Get("macroname"), CairoFont.WhiteSmallText(), nameBounds)
			.AddStaticText(Lang.Get("macrohotkey"), CairoFont.WhiteSmallText(), hotkeyBounds)
			.AddTextInput(nameInputBounds, OnMacroNameChanged, CairoFont.TextInput(), "macroname")
			.AddInset(hotkeyInputBounds, 2, 0.7f)
			.AddDynamicText("", CairoFont.TextInput(), hotkeyInputBounds.FlatCopy().WithFixedPadding(3.0, 3.0).WithFixedOffset(3.0, 3.0), "hotkey")
			.AddStaticText(Lang.Get("macrocommands"), CairoFont.WhiteSmallText(), commmandsBounds)
			.BeginClip(clippingBounds)
			.AddTextArea(textAreaBounds, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
			.AddSmallButton(Lang.Get("Delete"), OnClearMacro, clearMacroBounds)
			.AddSmallButton(Lang.Get("Save"), OnSaveMacro, saveMacroBounds)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)textAreaBounds.fixedHeight - 1f, (float)textAreaBounds.fixedHeight);
		base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = 0;
		OnSlotClick(0);
		base.SingleComposer.UnfocusOwnElements();
	}

	private void OnTextAreaCursorMoved(double posX, double posY)
	{
		double lineHeight = base.SingleComposer.GetTextArea("commands").Font.GetFontExtents().Height;
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY);
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY + lineHeight + 5.0);
	}

	private void OnCommandCodeChanged(string newCode)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
	}

	private void OnMacroNameChanged(string newname)
	{
	}

	private void OnSlotClick(int index)
	{
		GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
		base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = index;
		selectedIndex = index;
		currentSkillitem = skillItems[index];
		if ((capi.World as ClientMain).macroManager.MacrosByIndex.ContainsKey(index))
		{
			currentMacro = SelectedMacro;
		}
		else
		{
			currentMacro = new Macro();
			currentSkillitem = new SkillItem();
		}
		textInput.SetValue(currentSkillitem.Name);
		textArea.LoadValue(textArea.Lineize(string.Join("\r\n", currentMacro.Commands)));
		if (currentSkillitem.Hotkey != null)
		{
			hotkeyText.SetNewText(currentSkillitem.Hotkey?.ToString() ?? "");
		}
		else
		{
			hotkeyText.SetNewText("");
		}
		base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ComposeDialog();
	}

	private bool OnClearMacro()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		(capi.World as ClientMain).macroManager.DeleteMacro(selectedIndex);
		GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
		textInput.SetValue("");
		textArea.SetValue("");
		dynamicText.SetNewText("");
		currentMacro = new Macro();
		currentSkillitem = new SkillItem();
		LoadSkillList();
		return true;
	}

	private bool OnSaveMacro()
	{
		if (selectedIndex < 0 || currentMacro == null)
		{
			return true;
		}
		GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		currentMacro.Name = textInput.GetText();
		if (currentMacro.Name.Length == 0)
		{
			currentMacro.Name = "Macro " + (selectedIndex + 1);
			textInput.SetValue(currentMacro.Name);
		}
		currentMacro.Commands = textArea.GetLines().ToArray();
		for (int i = 0; i < currentMacro.Commands.Length; i++)
		{
			currentMacro.Commands[i] = currentMacro.Commands[i].TrimEnd('\n', '\r');
		}
		currentMacro.Index = selectedIndex;
		currentMacro.Code = Regex.Replace(currentMacro.Name.Replace(" ", "_"), "[^a-z0-9_-]+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		currentMacro.GenTexture(capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
		MacroManager mm = (capi.World as ClientMain).macroManager;
		base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.DialogDefaultTextColor);
		if (mm.MacrosByIndex.Values.FirstOrDefault((IMacroBase m) => m.Code == currentMacro.Code && m.Index != selectedIndex) != null)
		{
			capi.TriggerIngameError(this, "duplicatemacro", Lang.Get("A macro of this name exists already, please choose another name"));
			base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.ErrorTextColor);
			return false;
		}
		mm.SetMacro(selectedIndex, currentMacro);
		LoadSkillList();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		textArea.Bounds.fixedY = 1f - value;
		textArea.Bounds.CalcWorldBounds();
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		if (selectedIndex >= 0)
		{
			GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
			hotkeyText.Font.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
			hotkeyText.RecomposeText();
			if (hotkeyText.Bounds.PointInside(args.X, args.Y))
			{
				hotkeyText.SetNewText("?");
				hotkeyCapturer.BeginCapture();
			}
			else
			{
				CancelCapture();
			}
		}
	}

	public override void OnKeyUp(KeyEvent args)
	{
		if (!hotkeyCapturer.OnKeyUp(args, delegate
		{
			if (currentMacro != null)
			{
				currentMacro.KeyCombination = hotkeyCapturer.CapturedKeyComb;
				GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
				if (ScreenManager.hotkeyManager.IsHotKeyRegistered(currentMacro.KeyCombination))
				{
					dynamicText.Font.Color = GuiStyle.ErrorTextColor;
				}
				else
				{
					dynamicText.Font.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
				}
				dynamicText.SetNewText(hotkeyCapturer.CapturedKeyComb.ToString(), autoHeight: false, forceRedraw: true);
			}
		}))
		{
			base.OnKeyUp(args);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		if (hotkeyCapturer.OnKeyDown(args))
		{
			if (hotkeyCapturer.IsCapturing())
			{
				base.SingleComposer.GetDynamicText("hotkey").SetNewText(hotkeyCapturer.CapturingKeyComb.ToString());
			}
			else
			{
				CancelCapture();
			}
		}
		else
		{
			base.OnKeyDown(args);
		}
	}

	private void CancelCapture()
	{
		GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
		if (SelectedMacro?.KeyCombination != null)
		{
			hotkeyText.SetNewText(SelectedMacro.KeyCombination.ToString());
		}
		hotkeyCapturer.EndCapture();
	}

	public override bool CaptureAllInputs()
	{
		return hotkeyCapturer.IsCapturing();
	}

	public override void Dispose()
	{
		base.Dispose();
		if (skillItems == null)
		{
			return;
		}
		foreach (SkillItem skillItem in skillItems)
		{
			skillItem?.Dispose();
		}
	}
}
