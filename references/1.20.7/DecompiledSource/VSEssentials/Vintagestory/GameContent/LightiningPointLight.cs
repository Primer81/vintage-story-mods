using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class LightiningPointLight : IPointLight
{
	public Vec3f Color { get; set; }

	public Vec3d Pos { get; set; }

	public LightiningPointLight(Vec3f color, Vec3d pos)
	{
		Color = color;
		Pos = pos;
	}
}
