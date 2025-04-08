using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class LiquidTopOpenContainerProps
{
	public float CapacityLitres = 10f;

	public float TransferSizeLitres = 0.01f;

	public AssetLocation EmptyShapeLoc;

	public AssetLocation OpaqueContentShapeLoc;

	public AssetLocation LiquidContentShapeLoc;

	public float LiquidMaxYTranslate;
}
