using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// Draws an itemstack 
/// </summary>
public class ItemstackComponentBase : RichTextComponentBase
{
	private static int tooltipOffsetX = 10;

	private static int tooltipOffsetY = 40;

	protected ItemSlot renderedTooltipSlot;

	protected GuiElementItemstackInfo stackInfo;

	protected ICoreClientAPI capi;

	private bool bottomOverlap;

	private bool rightOverlap;

	private bool recalcAlignmentOffset;

	protected ElementBounds stackInfoBounds;

	protected ElementBounds parentBounds;

	public double offY;

	public double offX;

	protected DummyInventory dummyInv;

	private long lastHoverslotInfoTextUpdateTotalMs;

	public ItemstackComponentBase(ICoreClientAPI capi)
		: base(capi)
	{
		this.capi = capi;
		dummyInv = new DummyInventory(capi);
		dummyInv.OnAcquireTransitionSpeed += (EnumTransitionType transType, ItemStack stack, float mul) => 0f;
		renderedTooltipSlot = new DummySlot(null, dummyInv);
		stackInfoBounds = ElementBounds.FixedSize(EnumDialogArea.None, GuiElementItemstackInfo.BoxWidth, 0.0).WithFixedPadding(6f + 4f * RuntimeEnv.GUIScale).WithFixedPosition(12f + 8f / RuntimeEnv.GUIScale, 28f + 12f / RuntimeEnv.GUIScale);
		parentBounds = ElementBounds.Fixed(0.0, 0.0, 1.0, 1.0);
		parentBounds.WithParent(ElementBounds.Empty);
		stackInfoBounds.WithParent(parentBounds);
		stackInfo = new GuiElementItemstackInfo(capi, stackInfoBounds, OnRequireInfoText);
		stackInfo.SetSourceSlot(renderedTooltipSlot);
		stackInfo.ComposeElements(null, null);
		stackInfo.RecompCheckIgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes;
	}

	protected virtual string OnRequireInfoText(ItemSlot slot)
	{
		return slot.GetStackDescription(capi.World, capi.Settings.Bool["extendedDebugInfo"]);
	}

	public void RenderItemstackTooltip(ItemSlot slot, double renderX, double renderY, float dt)
	{
		parentBounds.fixedX = renderX / (double)RuntimeEnv.GUIScale;
		parentBounds.fixedY = renderY / (double)RuntimeEnv.GUIScale;
		parentBounds.CalcWorldBounds();
		renderedTooltipSlot.Itemstack = slot.Itemstack;
		renderedTooltipSlot.BackgroundIcon = slot.BackgroundIcon;
		if (capi.ElapsedMilliseconds - lastHoverslotInfoTextUpdateTotalMs > 1000)
		{
			stackInfo.SetSourceSlot(null);
		}
		stackInfo.SetSourceSlot(renderedTooltipSlot);
		lastHoverslotInfoTextUpdateTotalMs = capi.ElapsedMilliseconds;
		bool newRightOverlap = (double)capi.Input.MouseX + stackInfoBounds.OuterWidth > (double)(capi.Render.FrameWidth - 5);
		bool newBottomOverlap = (double)capi.Input.MouseY + stackInfoBounds.OuterHeight > (double)(capi.Render.FrameHeight - 5);
		if (recalcAlignmentOffset || bottomOverlap != newBottomOverlap || newRightOverlap != rightOverlap)
		{
			stackInfoBounds.WithFixedAlignmentOffset(newRightOverlap ? ((0.0 - stackInfoBounds.OuterWidth) / (double)RuntimeEnv.GUIScale - (double)tooltipOffsetX) : 0.0, newBottomOverlap ? ((0.0 - stackInfoBounds.OuterHeight) / (double)RuntimeEnv.GUIScale - (double)tooltipOffsetY) : 0.0);
			stackInfoBounds.CalcWorldBounds();
			stackInfoBounds.fixedOffsetY += Math.Max(0.0, 0.0 - stackInfoBounds.renderY);
			stackInfoBounds.CalcWorldBounds();
			bottomOverlap = newBottomOverlap;
			rightOverlap = newRightOverlap;
			recalcAlignmentOffset = false;
		}
		if (capi.Render.ScissorStack.Count > 0)
		{
			capi.Render.GlScissorFlag(enable: false);
			stackInfo.RenderInteractiveElements(dt);
			capi.Render.GlScissorFlag(enable: true);
		}
		else
		{
			stackInfo.RenderInteractiveElements(dt);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		stackInfo.Dispose();
	}
}
