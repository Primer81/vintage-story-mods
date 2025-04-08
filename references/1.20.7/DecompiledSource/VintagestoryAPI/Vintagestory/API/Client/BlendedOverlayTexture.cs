using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Defines a texture to be overlayed on another texture.
/// </summary>
[DocumentAsJson]
public class BlendedOverlayTexture
{
	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The path to the texture to use as an overlay.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation Base;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Normal</jsondefault>-->
	/// The type of blend for each pixel.
	/// </summary>
	[DocumentAsJson]
	public EnumColorBlendMode BlendMode;

	public BlendedOverlayTexture Clone()
	{
		return new BlendedOverlayTexture
		{
			Base = Base.Clone(),
			BlendMode = BlendMode
		};
	}

	public override string ToString()
	{
		return Base.ToString() + "-b" + BlendMode;
	}

	public void ToString(StringBuilder sb)
	{
		sb.Append(Base.ToString());
		sb.Append("-b");
		sb.Append(BlendMode);
	}
}
