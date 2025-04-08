using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

internal class BlockCandle : BlockBunchOCandles
{
	public BlockCandle()
	{
		candleWickPositions = new Vec3f[1]
		{
			new Vec3f(7.8f, 4f, 7.8f)
		};
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		QuantityCandles = 1;
	}
}
