using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ILiquidMetalSink
{
	bool CanReceiveAny { get; }

	bool CanReceive(ItemStack key);

	void BeginFill(Vec3d hitPosition);

	void ReceiveLiquidMetal(ItemStack key, ref int transferedAmount, float temp);

	void OnPourOver();
}
