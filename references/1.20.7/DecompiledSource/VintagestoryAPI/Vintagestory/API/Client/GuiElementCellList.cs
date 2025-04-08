using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementCellList<T> : GuiElement
{
	/// <summary>
	/// The cells in the list.  See IGuiElementCell for how it's supposed to function.
	/// </summary>
	public List<IGuiElementCell> elementCells = new List<IGuiElementCell>();

	private List<IGuiElementCell> visibleCells = new List<IGuiElementCell>();

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

	private Func<IGuiElementCell, bool> cellFilter;

	private OnRequireCell<T> cellcreator;

	private bool didInitialize;

	private IEnumerable<T> cellsTmp;

	public override ElementBounds InsideClipBounds
	{
		get
		{
			return base.InsideClipBounds;
		}
		set
		{
			base.InsideClipBounds = value;
			foreach (IGuiElementCell elementCell in elementCells)
			{
				elementCell.InsideClipBounds = InsideClipBounds;
			}
		}
	}

	/// <summary>
	/// Creates a new list in the current GUI.
	/// </summary>
	/// <param name="capi">The Client API.</param>
	/// <param name="bounds">The bounds of the list.</param>
	/// <param name="cellCreator">The event fired when a cell is requested by the gui</param>
	/// <param name="cells">The array of cells initialized with the list.</param>
	public GuiElementCellList(ICoreClientAPI capi, ElementBounds bounds, OnRequireCell<T> cellCreator, IEnumerable<T> cells = null)
		: base(capi, bounds)
	{
		cellcreator = cellCreator;
		cellsTmp = cells;
		Bounds.IsDrawingSurface = true;
	}

	private void Initialize()
	{
		if (cellsTmp != null)
		{
			foreach (T cell in cellsTmp)
			{
				AddCell(cell);
			}
			visibleCells.Clear();
			visibleCells.AddRange(elementCells);
		}
		CalcTotalHeight();
		didInitialize = true;
	}

	public void ReloadCells(IEnumerable<T> cells)
	{
		foreach (IGuiElementCell elementCell in elementCells)
		{
			elementCell?.Dispose();
		}
		elementCells.Clear();
		foreach (T cell in cells)
		{
			AddCell(cell);
		}
		visibleCells.Clear();
		visibleCells.AddRange(elementCells);
		CalcTotalHeight();
	}

	public override void BeforeCalcBounds()
	{
		if (!didInitialize)
		{
			Initialize();
		}
		else
		{
			CalcTotalHeight();
		}
	}

	/// <summary>
	/// Calculates the total height for the list.
	/// </summary>
	public void CalcTotalHeight()
	{
		Bounds.CalcWorldBounds();
		double height = 0.0;
		double unscaledHeight = 0.0;
		foreach (IGuiElementCell cell in visibleCells)
		{
			cell.UpdateCellHeight();
			cell.Bounds.WithFixedPosition(0.0, unscaledHeight);
			cell.Bounds.CalcWorldBounds();
			height += cell.Bounds.fixedHeight + (double)unscaledCellSpacing + (double)(2 * UnscaledCellVerPadding);
			unscaledHeight += cell.Bounds.OuterHeight / (double)RuntimeEnv.GUIScale + (double)unscaledCellSpacing;
		}
		Bounds.fixedHeight = height + (double)unscaledCellSpacing;
		Bounds.CalcWorldBounds();
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	internal void FilterCells(Func<IGuiElementCell, bool> onFilter)
	{
		cellFilter = onFilter;
		visibleCells.Clear();
		foreach (IGuiElementCell elem in elementCells)
		{
			if (cellFilter(elem))
			{
				visibleCells.Add(elem);
			}
		}
		CalcTotalHeight();
	}

	/// <summary>
	/// Adds a cell to the list.
	/// </summary>
	/// <param name="cell">The cell to add.</param>
	/// <param name="afterPosition">The position of the cell to add after.  (Default: -1)</param>
	protected void AddCell(T cell, int afterPosition = -1)
	{
		ElementBounds cellBounds = new ElementBounds
		{
			fixedPaddingX = UnscaledCellHorPadding,
			fixedPaddingY = UnscaledCellVerPadding,
			fixedWidth = Bounds.fixedWidth - 2.0 * Bounds.fixedPaddingX - (double)(2 * UnscaledCellHorPadding),
			fixedHeight = 0.0,
			BothSizing = ElementSizing.Fixed
		}.WithParent(Bounds);
		IGuiElementCell cellElem = cellcreator(cell, cellBounds);
		cellElem.InsideClipBounds = InsideClipBounds;
		if (afterPosition == -1)
		{
			elementCells.Add(cellElem);
		}
		else
		{
			elementCells.Insert(afterPosition, cellElem);
		}
	}

	/// <summary>
	/// Removes a cell at a specified position.
	/// </summary>
	/// <param name="position">The position of the cell to remove.</param>
	protected void RemoveCell(int position)
	{
		elementCells.RemoveAt(position);
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mousex = api.Input.MouseX;
		int mousey = api.Input.MouseY;
		foreach (IGuiElementCell element in visibleCells)
		{
			if (element.Bounds.PositionInside(mousex, mousey) != null)
			{
				element.OnMouseUpOnElement(args, elementCells.IndexOf(element));
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mousex = api.Input.MouseX;
		int mousey = api.Input.MouseY;
		foreach (IGuiElementCell element in visibleCells)
		{
			if (element.Bounds.PositionInside(mousex, mousey) != null)
			{
				element.OnMouseDownOnElement(args, elementCells.IndexOf(element));
			}
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mousex = api.Input.MouseX;
		int mousey = api.Input.MouseY;
		foreach (IGuiElementCell element in visibleCells)
		{
			if (element.Bounds.PositionInside(mousex, mousey) != null)
			{
				element.OnMouseMoveOnElement(args, elementCells.IndexOf(element));
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		MouseOverCursor = null;
		foreach (IGuiElementCell element in visibleCells)
		{
			if (element.Bounds.PartiallyInside(Bounds.ParentBounds))
			{
				element.OnRenderInteractiveElements(api, deltaTime);
				if (element.MouseOverCursor != null)
				{
					MouseOverCursor = element.MouseOverCursor;
				}
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		foreach (IGuiElementCell elementCell in elementCells)
		{
			elementCell.Dispose();
		}
	}
}
