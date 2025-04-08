using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class Macro : MacroBase
{
	public override void GenTexture(ICoreClientAPI capi, int size)
	{
		int seed = string.Join("", base.Commands).GetHashCode();
		int addLines = GameMath.Clamp(base.Commands.Length / 2, 0, 5);
		base.iconTexture = capi.Gui.Icons.GenTexture(48, 48, delegate(Context ctx, ImageSurface surface)
		{
			capi.Gui.Icons.DrawRandomSymbol(ctx, 0.0, 0.0, 48.0, GuiStyle.MacroIconColor, 2.0, seed, addLines);
		});
	}
}
