namespace Vintagestory.API.Client;

internal class ElementEmptyBounds : ElementBounds
{
	public override double relX => 0.0;

	public override double relY => 0.0;

	public override double absX => 0.0;

	public override double absY => 0.0;

	public override double renderX => 0.0;

	public override double renderY => 0.0;

	public override double drawX => 0.0;

	public override double drawY => 0.0;

	public override double OuterWidth => 1.0;

	public override double OuterHeight => 1.0;

	public override int OuterWidthInt => 1;

	public override int OuterHeightInt => 1;

	public override double InnerHeight => 1.0;

	public override double InnerWidth => 1.0;

	public ElementEmptyBounds()
	{
		base.BothSizing = ElementSizing.FitToChildren;
	}

	public override void CalcWorldBounds()
	{
	}
}
