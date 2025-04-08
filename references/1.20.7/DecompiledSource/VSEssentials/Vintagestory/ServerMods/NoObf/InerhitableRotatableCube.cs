using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

public class InerhitableRotatableCube
{
	public float? X1;

	public float? Y1;

	public float? Z1;

	public float? X2;

	public float? Y2;

	public float? Z2;

	public float RotateX;

	public float RotateY;

	public float RotateZ;

	public Cuboidf InheritedCopy(Cuboidf parent)
	{
		return new Cuboidf((!X1.HasValue) ? parent.X1 : X1.Value, (!Y1.HasValue) ? parent.Y1 : Y1.Value, (!Z1.HasValue) ? parent.Z1 : Z1.Value, (!X2.HasValue) ? parent.X2 : X2.Value, (!Y2.HasValue) ? parent.Y2 : Y2.Value, (!Z2.HasValue) ? parent.Z2 : Z2.Value).RotatedCopy(RotateX, RotateY, RotateZ, new Vec3d(0.5, 0.5, 0.5));
	}
}
