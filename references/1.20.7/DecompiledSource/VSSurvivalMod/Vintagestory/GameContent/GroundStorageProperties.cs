using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GroundStorageProperties
{
	public EnumGroundStorageLayout Layout;

	public int WallOffY = 1;

	public AssetLocation PlaceRemoveSound = new AssetLocation("sounds/player/build");

	public bool RandomizeSoundPitch;

	public AssetLocation StackingModel;

	public float ModelItemsToStackSizeRatio = 1f;

	public Dictionary<string, AssetLocation> StackingTextures;

	public int MaxStackingHeight = 1;

	public int StackingCapacity = 1;

	public int TransferQuantity = 1;

	public int BulkTransferQuantity = 4;

	public bool CtrlKey;

	[Obsolete("Use CtrlKey instead. SprintKey maintained for compatibility with existing JSONs")]
	public bool SprintKey;

	public bool UpSolid;

	public Cuboidf CollisionBox;

	public Cuboidf SelectionBox;

	public float CbScaleYByLayer;

	public int MaxFireable = 9999;

	[Obsolete("Use ModelItemsToStackSizeRatio instead, which is now a float instead of int?")]
	public int? TessQuantityElements
	{
		get
		{
			return (int)ModelItemsToStackSizeRatio;
		}
		set
		{
			ModelItemsToStackSizeRatio = value.GetValueOrDefault();
		}
	}

	public GroundStorageProperties Clone()
	{
		return new GroundStorageProperties
		{
			Layout = Layout,
			WallOffY = WallOffY,
			PlaceRemoveSound = PlaceRemoveSound,
			RandomizeSoundPitch = RandomizeSoundPitch,
			StackingCapacity = StackingCapacity,
			StackingModel = StackingModel,
			StackingTextures = StackingTextures,
			MaxStackingHeight = MaxStackingHeight,
			TransferQuantity = TransferQuantity,
			BulkTransferQuantity = BulkTransferQuantity,
			CollisionBox = CollisionBox,
			SelectionBox = SelectionBox,
			CbScaleYByLayer = CbScaleYByLayer,
			MaxFireable = MaxFireable,
			CtrlKey = CtrlKey,
			UpSolid = UpSolid
		};
	}
}
