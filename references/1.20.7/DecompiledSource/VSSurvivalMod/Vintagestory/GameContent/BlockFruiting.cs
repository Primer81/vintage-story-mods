using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFruiting : BlockCrop
{
	private double[] FruitPoints { get; set; }

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		return base.GetColor(capi, pos);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		JsonObject attributes = Attributes;
		if (attributes == null || !attributes["pickPrompt"].AsBool())
		{
			return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-fruiting-harvest",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = BlockUtil.GetKnifeStacks(api)
			}
		};
	}

	public virtual double[] GetFruitingPoints()
	{
		if (FruitPoints == null)
		{
			SetUpFruitPoints();
		}
		return FruitPoints;
	}

	public virtual void SetUpFruitPoints()
	{
		ShapeElement[] elements = (api as ICoreClientAPI).TesselatorManager.GetCachedShape(Shape.Base).Elements;
		double offsetX = 0.0;
		double offsetY = 0.0;
		double offsetZ = 0.0;
		float scaleFactor = Shape.Scale;
		if (elements.Length == 1 && elements[0].Children != null)
		{
			offsetX = (elements[0].From[0] + elements[0].To[0]) / 32.0;
			offsetY = (elements[0].From[1] + elements[0].To[1]) / 32.0;
			offsetZ = (elements[0].From[2] + elements[0].To[2]) / 32.0;
			elements = elements[0].Children;
		}
		int count = 0;
		ShapeElement[] array = elements;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Name.StartsWithOrdinal("fruit"))
			{
				count++;
			}
		}
		FruitPoints = new double[count * 3];
		double[] matrix = new double[16];
		double[] triple = new double[3];
		double[] pos = new double[4];
		count = 0;
		array = elements;
		foreach (ShapeElement element in array)
		{
			if (!element.Name.StartsWithOrdinal("fruit"))
			{
				continue;
			}
			double mainX = element.From[0] / 16.0;
			double mainY = element.From[1] / 16.0;
			double mainZ = element.From[2] / 16.0;
			double highestX = (element.To[0] - element.From[0]) / 32.0;
			double highestY = (element.To[1] - element.From[1]) / 16.0;
			double highestZ = (element.To[2] - element.From[2]) / 32.0;
			if (element.Children != null)
			{
				ShapeElement[] children = element.Children;
				foreach (ShapeElement child in children)
				{
					pos[0] = (child.To[0] - child.From[0]) / 32.0;
					pos[1] = (child.To[1] - child.From[1]) / 16.0;
					pos[2] = (child.To[2] - child.From[2]) / 32.0;
					pos[3] = 1.0;
					double[] actual = Rotate(pos, child, matrix, triple);
					if (actual[1] > highestY)
					{
						highestX = actual[0];
						highestY = actual[1];
						highestZ = actual[2];
					}
				}
			}
			pos[0] = highestX;
			pos[1] = highestY;
			pos[2] = highestZ;
			pos[3] = 0.0;
			double[] mainActual = Rotate(pos, element, matrix, triple);
			FruitPoints[count * 3] = (mainActual[0] + mainX + offsetX - 0.5) * (double)scaleFactor + 0.5 + (double)Shape.offsetX;
			FruitPoints[count * 3 + 1] = (mainActual[1] + mainY + offsetY) * (double)scaleFactor + (double)Shape.offsetY;
			FruitPoints[count * 3 + 2] = (mainActual[2] + mainZ + offsetZ - 0.5) * (double)scaleFactor + 0.5 + (double)Shape.offsetZ;
			count++;
		}
	}

	private double[] Rotate(double[] pos, ShapeElement element, double[] matrix, double[] triple)
	{
		Mat4d.Identity(matrix);
		Mat4d.Translate(matrix, matrix, element.RotationOrigin[0] / 16.0, element.RotationOrigin[1] / 16.0, element.RotationOrigin[2] / 16.0);
		if (element.RotationX != 0.0)
		{
			triple[0] = 1.0;
			triple[1] = 0.0;
			triple[2] = 0.0;
			Mat4d.Rotate(matrix, matrix, element.RotationX * 0.01745329238474369, triple);
		}
		if (element.RotationY != 0.0)
		{
			triple[0] = 0.0;
			triple[1] = 1.0;
			triple[2] = 0.0;
			Mat4d.Rotate(matrix, matrix, element.RotationY * 0.01745329238474369, triple);
		}
		if (element.RotationZ != 0.0)
		{
			triple[0] = 0.0;
			triple[1] = 0.0;
			triple[2] = 1.0;
			Mat4d.Rotate(matrix, matrix, element.RotationZ * 0.01745329238474369, triple);
		}
		Mat4d.Translate(matrix, matrix, (element.From[0] - element.RotationOrigin[0]) / 16.0, (element.From[1] - element.RotationOrigin[1]) / 16.0, (element.From[2] - element.RotationOrigin[2]) / 16.0);
		return Mat4d.MulWithVec4(matrix, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is BlockEntityFarmland befarmland && befarmland.OnBlockInteract(byPlayer))
		{
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorFruiting>() != null)
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorFruiting>())?.OnPlayerInteract(secondsUsed, byPlayer, blockSel.HitPosition) ?? base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		(world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorFruiting>())?.OnPlayerInteractStop(secondsUsed, byPlayer, blockSel.HitPosition);
	}
}
