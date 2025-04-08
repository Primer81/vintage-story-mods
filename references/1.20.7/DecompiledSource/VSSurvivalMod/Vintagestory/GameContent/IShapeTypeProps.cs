using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IShapeTypeProps
{
	string TextureFlipCode { get; }

	string TextureFlipGroupCode { get; }

	Dictionary<string, CompositeTexture> Textures { get; }

	byte[] LightHsv { get; }

	string HashKey { get; }

	bool RandomizeYSize { get; }

	string Code { get; }

	Vec3f Rotation { get; }

	Cuboidf[] ColSelBoxes { get; set; }

	Cuboidf[] SelBoxes { get; set; }

	ModelTransform GuiTransform { get; set; }

	ModelTransform FpTtransform { get; set; }

	ModelTransform TpTransform { get; set; }

	ModelTransform GroundTransform { get; set; }

	string RotInterval { get; }

	string FirstTexture { get; set; }

	TextureAtlasPosition TexPos { get; set; }

	Dictionary<long, Cuboidf[]> ColSelBoxesByHashkey { get; }

	Dictionary<long, Cuboidf[]> SelBoxesByHashkey { get; set; }

	AssetLocation ShapePath { get; }

	Shape ShapeResolved { get; set; }

	BlockDropItemStack[] Drops { get; set; }

	string HeldIdleAnim { get; set; }

	string HeldReadyAnim { get; set; }

	int Reparability { get; set; }

	bool CanAttachBlockAt(Vec3f blockRot, BlockFacing blockFace, Cuboidi attachmentArea = null);
}
