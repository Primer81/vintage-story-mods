using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class CropPropConfig
{
	public bool RandomizeRotations = true;

	public float MonthStart;

	public float MonthEnd;

	public int Stages;

	public CompositeShape Shape;

	public Dictionary<string, CompositeTexture> Textures;

	public int BakedAlternatesLength = -1;
}
