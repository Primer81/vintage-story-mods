namespace Vintagestory.API.Common;

/// <summary>
/// Materials of which a block may be made of.
/// Currently only used for mining speed for tools and blast resistance.
/// </summary>
[DocumentAsJson]
public enum EnumBlockMaterial
{
	Air,
	Soil,
	Gravel,
	Sand,
	Wood,
	Leaves,
	Stone,
	Ore,
	Liquid,
	Snow,
	Ice,
	Metal,
	Mantle,
	Plant,
	Glass,
	Ceramic,
	Cloth,
	Lava,
	Brick,
	Fire,
	Meta,
	Other
}
