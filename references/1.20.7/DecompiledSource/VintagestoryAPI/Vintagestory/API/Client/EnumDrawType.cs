namespace Vintagestory.API.Client;

/// <summary>
/// Draw types for blocks.
/// </summary>
[DocumentAsJson]
public enum EnumDrawType
{
	BlockLayer_1 = 1,
	BlockLayer_2,
	BlockLayer_3,
	BlockLayer_4,
	BlockLayer_5,
	BlockLayer_6,
	BlockLayer_7,
	/// <summary>
	/// You will most likely use JSON for all assets with custom shapes.
	/// </summary>
	JSON,
	Empty,
	Cube,
	Cross,
	Transparent,
	Liquid,
	TopSoil,
	CrossAndSnowlayer,
	JSONAndWater,
	JSONAndSnowLayer,
	CrossAndSnowlayer_2,
	CrossAndSnowlayer_3,
	CrossAndSnowlayer_4,
	SurfaceLayer
}
