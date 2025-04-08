using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityRecipeSelector : GuiDialogGeneric
{
	private BlockPos blockEntityPos;

	private int prevSlotOver = -1;

	private List<SkillItem> skillItems;

	private bool didSelect;

	private Action<int> onSelectedRecipe;

	private Action onCancelSelect;

	private readonly double floatyDialogPosition = 0.5;

	private readonly double floatyDialogAlign = 0.75;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogBlockEntityRecipeSelector(string DialogTitle, ItemStack[] recipeOutputs, Action<int> onSelectedRecipe, Action onCancelSelect, BlockPos blockEntityPos, ICoreClientAPI capi)
		: base(DialogTitle, capi)
	{
		this.blockEntityPos = blockEntityPos;
		this.onSelectedRecipe = onSelectedRecipe;
		this.onCancelSelect = onCancelSelect;
		skillItems = new List<SkillItem>();
		double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		foreach (ItemStack stack in recipeOutputs)
		{
			ItemSlot dummySlot = new DummySlot(stack);
			string key = GetCraftDescKey(stack);
			string desc = Lang.GetMatching(key);
			if (desc == key)
			{
				desc = "";
			}
			skillItems.Add(new SkillItem
			{
				Code = stack.Collectible.Code.Clone(),
				Name = stack.GetName(),
				Description = desc,
				RenderHandler = delegate(AssetLocation code, float dt, double posX, double posY)
				{
					double num = GuiElement.scaled(size - 5.0);
					capi.Render.RenderItemstackToGui(dummySlot, posX + num / 2.0, posY + num / 2.0, 100.0, (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize), -1);
				}
			});
		}
		SetupDialog();
	}

	public string GetCraftDescKey(ItemStack stack)
	{
		string type = stack.Class.Name();
		return stack.Collectible.Code?.Domain + ":" + type + "craftdesc-" + stack.Collectible.Code?.Path;
	}

	private void SetupDialog()
	{
		int num = Math.Max(1, skillItems.Count);
		int cols = Math.Min(num, 7);
		int rows = (int)Math.Ceiling((float)num / (float)cols);
		double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = Math.Max(300.0, (double)cols * size);
		ElementBounds skillGridBounds = ElementBounds.Fixed(0.0, 30.0, innerWidth, (double)rows * size);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, (double)rows * size + 50.0, innerWidth, 33.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		base.SingleComposer = capi.Gui.CreateCompo("toolmodeselect" + blockEntityPos, ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Select Recipe"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddSkillItemGrid(skillItems, cols, rows, OnSlotClick, skillGridBounds, "skillitemgrid")
			.AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "name")
			.AddDynamicText("", CairoFont.WhiteDetailText(), textBounds.BelowCopy(0.0, 10.0), "desc")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = OnSlotOver;
	}

	private void OnSlotOver(int num)
	{
		if (num < skillItems.Count && num != prevSlotOver)
		{
			prevSlotOver = num;
			base.SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name);
			base.SingleComposer.GetDynamicText("desc").SetNewText(skillItems[num].Description);
		}
	}

	private void OnSlotClick(int num)
	{
		onSelectedRecipe(num);
		didSelect = true;
		TryClose();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		if (!didSelect)
		{
			onCancelSelect();
		}
	}

	public override bool TryClose()
	{
		return base.TryClose();
	}

	public override bool TryOpen()
	{
		return base.TryOpen();
	}

	private void SendInvPacket(object packet)
	{
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			Vec3d pos = MatrixToolsd.Project(new Vec3d((double)blockEntityPos.X + 0.5, (double)blockEntityPos.Y + floatyDialogPosition, (double)blockEntityPos.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (pos.Z < 0.0)
			{
				return;
			}
			base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
			base.SingleComposer.Bounds.fixedOffsetX = 0.0;
			base.SingleComposer.Bounds.fixedOffsetY = 0.0;
			base.SingleComposer.Bounds.absFixedX = pos.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
			base.SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - pos.Y - base.SingleComposer.Bounds.OuterHeight * floatyDialogAlign;
			base.SingleComposer.Bounds.absMarginX = 0.0;
			base.SingleComposer.Bounds.absMarginY = 0.0;
		}
		base.OnRenderGUI(deltaTime);
	}
}
