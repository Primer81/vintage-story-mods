using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorUnstable : BlockBehavior
{
	private BlockFacing[] AttachedToFaces;

	private Dictionary<string, Cuboidi> attachmentAreas;

	public BlockBehaviorUnstable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		AttachedToFaces = new BlockFacing[1] { BlockFacing.DOWN };
		if (properties["attachedToFaces"].Exists)
		{
			string[] faces = properties["attachedToFaces"].AsArray<string>();
			AttachedToFaces = new BlockFacing[faces.Length];
			for (int i = 0; i < faces.Length; i++)
			{
				AttachedToFaces[i] = BlockFacing.FromCode(faces[i]);
			}
		}
		Dictionary<string, RotatableCube> areas = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		if (areas == null)
		{
			return;
		}
		attachmentAreas = new Dictionary<string, Cuboidi>();
		foreach (KeyValuePair<string, RotatableCube> val in areas)
		{
			val.Value.Origin.Set(8.0, 8.0, 8.0);
			attachmentAreas[val.Key] = val.Value.RotatedCopy().ConvertToCuboidi();
		}
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		if (!IsAttached(world.BlockAccessor, blockSel.Position))
		{
			handling = EnumHandling.PreventSubsequent;
			failureCode = "requireattachable";
			return false;
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		if (!IsAttached(world.BlockAccessor, pos))
		{
			handled = EnumHandling.PreventDefault;
			world.BlockAccessor.BreakBlock(pos, null);
		}
		else
		{
			base.OnNeighbourBlockChange(world, pos, neibpos, ref handled);
		}
	}

	public virtual bool IsAttached(IBlockAccessor blockAccessor, BlockPos pos)
	{
		for (int i = 0; i < AttachedToFaces.Length; i++)
		{
			BlockFacing face = AttachedToFaces[i];
			Block obj = blockAccessor.GetBlock(pos.AddCopy(face));
			Cuboidi attachmentArea = null;
			attachmentAreas?.TryGetValue(face.Code, out attachmentArea);
			if (obj.CanAttachBlockAt(blockAccessor, block, pos.AddCopy(face), face.Opposite, attachmentArea))
			{
				return true;
			}
		}
		return false;
	}
}
