using System;

namespace Vintagestory.API.Client;

public interface IGuiElementCell : IDisposable
{
	ElementBounds InsideClipBounds { get; set; }

	/// <summary>
	/// The bounds of the cell.
	/// </summary>
	ElementBounds Bounds { get; }

	string MouseOverCursor { get; }

	/// <summary>
	/// The event fired when the cell is rendered.
	/// </summary>
	/// <param name="api">The Client API</param>
	/// <param name="deltaTime">The change in time.</param>
	void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime);

	/// <summary>
	/// Called when the cell is modified and needs to be updated.
	/// </summary>
	void UpdateCellHeight();

	void OnMouseUpOnElement(MouseEvent args, int elementIndex);

	void OnMouseDownOnElement(MouseEvent args, int elementIndex);

	void OnMouseMoveOnElement(MouseEvent args, int elementIndex);
}
