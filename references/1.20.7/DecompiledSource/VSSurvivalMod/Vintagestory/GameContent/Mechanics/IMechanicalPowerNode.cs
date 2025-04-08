using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public interface IMechanicalPowerNode
{
	float GearedRatio { get; set; }

	float GetTorque(long tick, float speed, out float resistance);

	void LeaveNetwork();

	BlockPos GetPosition();
}
