using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class TextAreaConfig
{
	public int MaxWidth = 160;

	public int MaxHeight = 165;

	public float FontSize = 20f;

	public bool BoldFont;

	public EnumVerticalAlign VerticalAlign;

	public string FontName = GuiStyle.StandardFontName;

	public float textVoxelOffsetX;

	public float textVoxelOffsetY;

	public float textVoxelOffsetZ;

	public float textVoxelWidth = 14f;

	public float textVoxelHeight = 6.5f;

	public bool WithScrollbar;

	public TextAreaConfig CopyWithFontSize(float fontSize)
	{
		return new TextAreaConfig
		{
			MaxWidth = MaxWidth,
			MaxHeight = MaxHeight,
			FontSize = fontSize,
			BoldFont = BoldFont,
			FontName = FontName,
			textVoxelWidth = textVoxelWidth,
			textVoxelHeight = textVoxelHeight,
			textVoxelOffsetX = textVoxelOffsetX,
			textVoxelOffsetY = textVoxelOffsetY,
			textVoxelOffsetZ = textVoxelOffsetZ,
			WithScrollbar = WithScrollbar,
			VerticalAlign = VerticalAlign
		};
	}
}
