using System;
using System.Collections.Generic;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementContainer : GuiElement
{
	/// <summary>
	/// The cells in the list.  See IGuiElementCell for how it's supposed to function.
	/// </summary>
	public List<GuiElement> Elements = new List<GuiElement>();

	/// <summary>
	/// the space between the cells.  Default: 10
	/// </summary>
	public int unscaledCellSpacing = 10;

	/// <summary>
	/// The padding on the vertical axis of the cell.  Default: 2
	/// </summary>
	public int UnscaledCellVerPadding = 4;

	/// <summary>
	/// The padding on the horizontal axis of the cell.  Default: 7
	/// </summary>
	public int UnscaledCellHorPadding = 7;

	private LoadedTexture listTexture;

	private ElementBounds insideBounds;

	private int childFocusIndex;

	/// <summary>
	/// Creates a new list in the current GUI.
	/// </summary>
	/// <param name="capi">The Client API.</param>
	/// <param name="bounds">The bounds of the list.</param>
	public GuiElementContainer(ICoreClientAPI capi, ElementBounds bounds)
		: base(capi, bounds)
	{
		listTexture = new LoadedTexture(capi);
		bounds.IsDrawingSurface = true;
	}

	public override void BeforeCalcBounds()
	{
		base.BeforeCalcBounds();
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		CalcTotalHeight();
	}

	internal void ReloadCells()
	{
		CalcTotalHeight();
		ComposeList();
	}

	/// <summary>
	/// Calculates the total height for the list.
	/// </summary>
	public void CalcTotalHeight()
	{
		double height = 0.0;
		foreach (GuiElement cell in Elements)
		{
			cell.BeforeCalcBounds();
			height = Math.Max(height, cell.Bounds.fixedY + cell.Bounds.fixedHeight);
		}
		Bounds.fixedHeight = height + (double)unscaledCellSpacing;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		Bounds.CalcWorldBounds();
		ComposeList();
	}

	private void ComposeList()
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context ctx = genContext(surface);
		CalcTotalHeight();
		Bounds.CalcWorldBounds();
		foreach (GuiElement element in Elements)
		{
			element.ComposeElements(ctx, surface);
		}
		generateTexture(surface, ref listTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	public void Clear()
	{
		Elements.Clear();
		Bounds.ChildBounds.Clear();
	}

	/// <summary>
	/// Adds a cell to the list.
	/// </summary>
	/// <param name="elem">The cell to add.</param>
	/// <param name="afterPosition">The position of the cell to add after.  (Default: -1)</param>
	public void Add(GuiElement elem, int afterPosition = -1)
	{
		if (afterPosition == -1)
		{
			Elements.Add(elem);
		}
		else
		{
			Elements.Insert(afterPosition, elem);
		}
		elem.InsideClipBounds = InsideClipBounds;
		Bounds.WithChild(elem.Bounds);
	}

	/// <summary>
	/// Removes a cell at a specified position.
	/// </summary>
	/// <param name="position">The position of the cell to remove.</param>
	public void RemoveCell(int position)
	{
		Elements.RemoveAt(position);
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		int nowFocusIndex = -1;
		int i = 0;
		foreach (GuiElement element in Elements)
		{
			element.OnMouseUp(api, args);
			if (args.Handled)
			{
				nowFocusIndex = i;
				break;
			}
			i++;
		}
		if (childFocusIndex >= 0 && childFocusIndex < Elements.Count && nowFocusIndex != childFocusIndex)
		{
			Elements[childFocusIndex].OnFocusLost();
		}
		if (nowFocusIndex >= 0)
		{
			Elements[nowFocusIndex].OnFocusGained();
		}
		childFocusIndex = nowFocusIndex;
		if (!args.Handled)
		{
			base.OnMouseUp(api, args);
		}
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		int nowFocusIndex = -1;
		int i = 0;
		foreach (GuiElement element in Elements)
		{
			element.OnMouseDown(api, args);
			if (args.Handled)
			{
				nowFocusIndex = i;
				break;
			}
			i++;
		}
		if (childFocusIndex >= 0 && nowFocusIndex != childFocusIndex)
		{
			Elements[childFocusIndex].OnFocusLost();
		}
		if (nowFocusIndex >= 0)
		{
			Elements[nowFocusIndex].OnFocusGained();
		}
		childFocusIndex = nowFocusIndex;
		if (!args.Handled)
		{
			base.OnMouseDown(api, args);
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		foreach (GuiElement element in Elements)
		{
			element.OnMouseMove(api, args);
		}
		if (!args.Handled)
		{
			base.OnMouseMove(api, args);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		base.OnKeyDown(api, args);
		if (childFocusIndex >= 0)
		{
			Elements[childFocusIndex].OnKeyDown(api, args);
		}
	}

	public override void OnKeyPress(ICoreClientAPI api, KeyEvent args)
	{
		base.OnKeyPress(api, args);
		if (childFocusIndex >= 0)
		{
			Elements[childFocusIndex].OnKeyPress(api, args);
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (!Bounds.ParentBounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			return;
		}
		int dx = api.Input.MouseX - (int)Bounds.absX;
		int dy = api.Input.MouseY - (int)Bounds.absY;
		foreach (GuiElement element in Elements)
		{
			element.Bounds.PositionInside(dx, dy);
			element.OnMouseWheel(api, args);
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(listTexture.TextureId, Bounds);
		foreach (GuiElement element in Elements)
		{
			element.RenderInteractiveElements(deltaTime);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		listTexture.Dispose();
		foreach (GuiElement element in Elements)
		{
			element.Dispose();
		}
	}
}
