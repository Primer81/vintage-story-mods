using Vintagestory.API.Client;

namespace Vintagestory.Client;

internal class ElementWindowBounds : ElementBounds
{
	private int width;

	private int height;

	public override double relX => 0.0;

	public override double relY => 0.0;

	public override double absX => 0.0;

	public override double absY => 0.0;

	public override double renderX => 0.0;

	public override double renderY => 0.0;

	public override double drawX => 0.0;

	public override double drawY => 0.0;

	public override double OuterWidth => width;

	public override double OuterHeight => height;

	public override double InnerWidth => width;

	public override double InnerHeight => height;

	public override int OuterWidthInt => width;

	public override int OuterHeightInt => height;

	public override bool RequiresRecalculation
	{
		get
		{
			if (width == ScreenManager.Platform.WindowSize.Width)
			{
				return height != ScreenManager.Platform.WindowSize.Height;
			}
			return true;
		}
	}

	public ElementWindowBounds()
	{
		IsWindowBounds = true;
		width = ScreenManager.Platform.WindowSize.Width;
		height = ScreenManager.Platform.WindowSize.Height;
		Initialized = true;
	}

	public override void CalcWorldBounds()
	{
		IsWindowBounds = true;
		width = ScreenManager.Platform.WindowSize.Width;
		height = ScreenManager.Platform.WindowSize.Height;
	}
}
