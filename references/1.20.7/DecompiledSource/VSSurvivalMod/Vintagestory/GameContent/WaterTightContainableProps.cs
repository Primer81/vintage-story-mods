using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class WaterTightContainableProps
{
	public enum EnumSpilledAction
	{
		PlaceBlock,
		DropContents
	}

	public class WhenFilledProps
	{
		public JsonItemStack Stack;
	}

	public class WhenSpilledProps
	{
		public Dictionary<int, JsonItemStack> StackByFillLevel;

		public EnumSpilledAction Action;

		public JsonItemStack Stack;
	}

	public bool Containable;

	public float ItemsPerLitre = 1f;

	public AssetLocation FillSpillSound = new AssetLocation("sounds/block/water");

	public AssetLocation PourSound = new AssetLocation("sounds/effect/water-pour.ogg");

	public AssetLocation FillSound = new AssetLocation("sounds/effect/water-fill.ogg");

	public CompositeTexture Texture;

	public string ClimateColorMap;

	public bool AllowSpill = true;

	public bool IsOpaque;

	public WhenSpilledProps WhenSpilled;

	public WhenFilledProps WhenFilled;

	public int MaxStackSize;

	public int GlowLevel;

	public FoodNutritionProperties NutritionPropsPerLitre;
}
