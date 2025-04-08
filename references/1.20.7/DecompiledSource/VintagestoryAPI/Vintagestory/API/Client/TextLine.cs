namespace Vintagestory.API.Client;

public class TextLine
{
	/// <summary>
	/// The text of the text line.
	/// </summary>
	public string Text;

	/// <summary>
	/// The bounds of the line of text.
	/// </summary>
	public LineRectangled Bounds;

	public double LeftSpace;

	public double RightSpace;

	public double NextOffsetX;
}
