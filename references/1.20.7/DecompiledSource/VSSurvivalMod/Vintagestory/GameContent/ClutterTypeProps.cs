using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ClutterTypeProps : IShapeTypeProps
{
	public ModelTransformNoDefaults GuiTf { get; set; }

	public ModelTransformNoDefaults FpTf { get; set; }

	public ModelTransformNoDefaults TpTf { get; set; }

	public ModelTransformNoDefaults GroundTf { get; set; }

	public string Code { get; set; }

	public Vec3f Rotation { get; set; } = new Vec3f();


	public Cuboidf[] ColSelBoxes { get; set; }

	public Cuboidf[] SelBoxes { get; set; }

	public ModelTransform GuiTransform { get; set; }

	public ModelTransform FpTtransform { get; set; }

	public ModelTransform TpTransform { get; set; }

	public ModelTransform GroundTransform { get; set; }

	public string RotInterval { get; set; } = "22.5deg";


	public string FirstTexture { get; set; }

	public TextureAtlasPosition TexPos { get; set; }

	public Dictionary<long, Cuboidf[]> ColSelBoxesByHashkey { get; set; } = new Dictionary<long, Cuboidf[]>();


	public Dictionary<long, Cuboidf[]> SelBoxesByHashkey { get; set; }

	public AssetLocation ShapePath { get; set; }

	public Shape ShapeResolved { get; set; }

	public string HashKey => Code;

	public bool RandomizeYSize { get; set; } = true;


	[Obsolete("Use RandomizeYSize instead")]
	public bool Randomize
	{
		get
		{
			return RandomizeYSize;
		}
		set
		{
			RandomizeYSize = value;
		}
	}

	public bool Climbable { get; set; }

	public byte[] LightHsv { get; set; }

	public Dictionary<string, CompositeTexture> Textures { get; set; }

	public string TextureFlipCode { get; set; }

	public string TextureFlipGroupCode { get; set; }

	public Dictionary<string, bool> SideAttachable { get; set; }

	public BlockDropItemStack[] Drops { get; set; }

	public int Reparability { get; set; }

	public string HeldReadyAnim { get; set; }

	public string HeldIdleAnim { get; set; }

	public bool CanAttachBlockAt(Vec3f blockRot, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (SideAttachable != null)
		{
			SideAttachable.TryGetValue(blockFace.Code, out var val);
			return val;
		}
		return false;
	}

	public ClutterTypeProps Clone()
	{
		return new ClutterTypeProps
		{
			GuiTf = GuiTransform?.Clone(),
			FpTf = FpTtransform?.Clone(),
			TpTf = TpTransform?.Clone(),
			GroundTf = GroundTransform?.Clone(),
			Code = Code,
			Rotation = new Vec3f
			{
				X = Rotation.X,
				Y = Rotation.Y,
				Z = Rotation.Z
			},
			ColSelBoxes = ColSelBoxes?.Select((Cuboidf box) => box?.Clone()).ToArray(),
			GuiTransform = GuiTransform?.Clone(),
			FpTtransform = FpTtransform?.Clone(),
			TpTransform = TpTransform?.Clone(),
			GroundTransform = GroundTransform?.Clone(),
			RotInterval = RotInterval,
			FirstTexture = FirstTexture,
			TexPos = TexPos?.Clone(),
			ColSelBoxesByHashkey = ColSelBoxesByHashkey.ToDictionary((KeyValuePair<long, Cuboidf[]> kv) => kv.Key, (KeyValuePair<long, Cuboidf[]> kv) => kv.Value?.Select((Cuboidf box) => box?.Clone()).ToArray()),
			ShapePath = ShapePath?.Clone(),
			ShapeResolved = ShapeResolved?.Clone(),
			Randomize = Randomize,
			Climbable = Climbable,
			LightHsv = LightHsv?.ToArray(),
			Textures = Textures?.ToDictionary((KeyValuePair<string, CompositeTexture> kv) => kv.Key, (KeyValuePair<string, CompositeTexture> kv) => kv.Value?.Clone()),
			TextureFlipCode = TextureFlipCode,
			TextureFlipGroupCode = TextureFlipGroupCode,
			SideAttachable = SideAttachable?.ToDictionary((KeyValuePair<string, bool> kv) => kv.Key, (KeyValuePair<string, bool> kv) => kv.Value),
			Drops = Drops?.Select((BlockDropItemStack drop) => drop.Clone()).ToArray(),
			Reparability = Reparability
		};
	}
}
