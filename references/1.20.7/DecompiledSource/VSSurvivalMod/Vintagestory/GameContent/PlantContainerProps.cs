using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class PlantContainerProps
{
	public CompositeShape Shape;

	public Dictionary<string, CompositeTexture> Textures;

	public ModelTransform Transform;

	public bool RandomRotate = true;
}
