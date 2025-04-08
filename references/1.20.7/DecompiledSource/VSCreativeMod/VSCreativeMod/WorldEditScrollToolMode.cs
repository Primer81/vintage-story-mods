using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.ServerMods.WorldEdit;

namespace VSCreativeMod;

public class WorldEditScrollToolMode : GuiDialog
{
	private readonly WorldEditClientHandler _worldEditClientHandler;

	private List<SkillItem> _multilineItems;

	public override string ToggleKeyCombinationCode { get; }

	public WorldEditScrollToolMode(ICoreClientAPI capi, WorldEditClientHandler worldEditClientHandler)
		: base(capi)
	{
		_worldEditClientHandler = worldEditClientHandler;
	}

	public override void OnGuiOpened()
	{
		ComposeDialog();
	}

	private void ComposeDialog()
	{
		ClearComposers();
		double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = 2.0 * size;
		int rows = 2;
		_multilineItems = _worldEditClientHandler.ownWorkspace.ToolInstance.GetAvailableModes(capi);
		foreach (SkillItem val in _multilineItems)
		{
			innerWidth = Math.Max(innerWidth, CairoFont.WhiteSmallishText().GetTextExtents(val.Name).Width / (double)RuntimeEnv.GUIScale + 1.0);
		}
		string title = "WorldEdit Scroll tool mode";
		innerWidth = Math.Max(innerWidth, CairoFont.WhiteSmallishText().GetTextExtents(title).Width / (double)RuntimeEnv.GUIScale + 1.0);
		ElementBounds skillGridBounds = ElementBounds.Fixed(0.0, 30.0, innerWidth, (double)rows * size);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, (double)rows * (size + 2.0) + 5.0, innerWidth, 25.0);
		ElementBounds textBounds2 = ElementBounds.Fixed(0.0, 0.0, innerWidth, 25.0);
		base.SingleComposer = capi.Gui.CreateCompo("toolmodeselect", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0), withTitleBar: false).BeginChildElements()
			.AddStaticText(title, CairoFont.WhiteSmallishText(), textBounds2);
		base.SingleComposer.AddSkillItemGrid(_multilineItems, _multilineItems.Count, 1, delegate(int num)
		{
			OnSlotClick(num);
		}, skillGridBounds, "skillitemgrid-1");
		base.SingleComposer.GetSkillItemGrid("skillitemgrid-1").OnSlotOver = OnSlotOver;
		base.SingleComposer.AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "name").EndChildElements().Compose();
	}

	private void OnSlotOver(int num)
	{
		if (num < _multilineItems.Count)
		{
			base.SingleComposer.GetDynamicText("name").SetNewText(_multilineItems[num].Name);
		}
	}

	private void OnSlotClick(int num)
	{
		Enum.TryParse<EnumWeToolMode>(_worldEditClientHandler.ownWorkspace.ToolInstance.GetAvailableModes(capi)[num].Name, out var mode);
		_worldEditClientHandler.ownWorkspace.ToolInstance.ScrollMode = mode;
		if (mode == EnumWeToolMode.MoveFar || mode == EnumWeToolMode.MoveNear)
		{
			capi.SendChatMessage("/we normalize quiet");
		}
		TryClose();
	}
}
