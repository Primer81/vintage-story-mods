using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BookShelfVariantGroup
{
	public bool DoubleSided;

	public BookShelfTypeProps[] types;

	public OrderedDictionary<string, BookShelfTypeProps> typesByCode = new OrderedDictionary<string, BookShelfTypeProps>();

	public BlockClutterBookshelf block;

	public TextureAtlasPosition texPos { get; set; }

	public Vec3f Rotation { get; set; } = new Vec3f();


	public Cuboidf[] ColSelBoxes { get; set; }

	public ModelTransform GuiTf { get; set; } = ModelTransform.BlockDefaultGui().EnsureDefaultValues().WithRotation(new Vec3f(-22.6f, -135.3f, 0f));


	public ModelTransform FpTf { get; set; }

	public ModelTransform TpTf { get; set; }

	public ModelTransform GroundTf { get; set; }

	public string RotInterval { get; set; } = "22.5deg";


	public Dictionary<long, Cuboidf[]> ColSelBoxesByHashkey { get; set; } = new Dictionary<long, Cuboidf[]>();

}
