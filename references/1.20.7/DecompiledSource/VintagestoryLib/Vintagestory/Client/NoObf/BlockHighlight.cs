using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class BlockHighlight
{
	public MeshRef modelRef;

	public BlockPos origin;

	public BlockPos[] attachmentPoints;

	public Vec3i Size;

	public EnumHighlightBlocksMode mode;

	public EnumHighlightShape shape;

	public float Scale = 1f;

	private int defaultColor = ColorUtil.ToRgba(96, (int)(GuiStyle.DialogDefaultBgColor[2] * 255.0), (int)(GuiStyle.DialogDefaultBgColor[1] * 255.0), (int)(GuiStyle.DialogDefaultBgColor[0] * 255.0));

	public void TesselateModel(ClientMain game, BlockPos[] positions, int[] colors)
	{
		if (modelRef != null)
		{
			game.Platform.DeleteMesh(modelRef);
			modelRef = null;
		}
		if (positions.Length == 0)
		{
			return;
		}
		switch (shape)
		{
		case EnumHighlightShape.Arbitrary:
		case EnumHighlightShape.Cylinder:
			TesselateArbitraryModel(game, positions, colors);
			break;
		case EnumHighlightShape.Cube:
			if (positions.Length == 0)
			{
				modelRef = null;
			}
			else if (positions.Length == 2)
			{
				int color2 = defaultColor;
				if (colors != null && colors.Length != 0)
				{
					color2 = colors[0];
				}
				TesselateCubeModel(game, positions[0], positions[1], color2);
			}
			else
			{
				TesselateArbitraryModel(game, positions, colors);
			}
			break;
		case EnumHighlightShape.Cubes:
		{
			if (positions.Length < 2 || positions.Length % 2 != 0)
			{
				modelRef = null;
				break;
			}
			MeshData modeldata = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
			int color = defaultColor;
			if (colors != null && colors.Length != 0)
			{
				color = colors[0];
			}
			bool manyColors = colors != null && colors.Length >= positions.Length / 2;
			BlockPos start = positions[0];
			BlockPos end = positions[1];
			origin = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
			for (int i = 0; i < positions.Length; i += 2)
			{
				GenCubeModel(game, modeldata, positions[i], positions[i + 1], manyColors ? colors[i / 2] : color);
			}
			modelRef = game.Platform.UploadMesh(modeldata);
			break;
		}
		case EnumHighlightShape.Ball:
			break;
		}
	}

	private void TesselateCubeModel(ClientMain game, BlockPos start, BlockPos end, int color)
	{
		origin = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.InternalY, end.InternalY), Math.Min(start.Z, end.Z));
		MeshData modeldata = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		GenCubeModel(game, modeldata, start, end, color);
		modelRef = game.Platform.UploadMesh(modeldata);
	}

	private void GenCubeModel(ClientMain game, MeshData intoMesh, BlockPos start, BlockPos end, int color)
	{
		BlockPos minPos = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.InternalY, end.InternalY), Math.Min(start.Z, end.Z));
		int widthX = Math.Max(start.X, end.X) - minPos.X;
		int widthY = Math.Max(start.InternalY, end.InternalY) - minPos.InternalY;
		int widthZ = Math.Max(start.Z, end.Z) - minPos.Z;
		if (widthX == 0 || widthY == 0 || widthZ == 0)
		{
			game.Logger.Error("Cannot generate block highlight. Highlight width, height and length must be above 0");
			return;
		}
		if (mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
		{
			origin.X = 0;
			origin.Y = 0;
			origin.Z = 0;
			attachmentPoints = new BlockPos[6];
			for (int j = 0; j < 6; j++)
			{
				Vec3i k = BlockFacing.ALLNORMALI[j];
				attachmentPoints[j] = new BlockPos(widthX / 2 * k.X, widthY / 2 * k.Y, widthZ / 2 * k.Z);
			}
		}
		Vec3f centerPos = new Vec3f((float)widthX / 2f + (float)minPos.X - (float)origin.X, (float)widthY / 2f + (float)minPos.InternalY - (float)origin.Y, (float)widthZ / 2f + (float)minPos.Z - (float)origin.Z);
		Vec3f cubeSize = new Vec3f(widthX, widthY, widthZ);
		float[] shadings = CubeMeshUtil.DefaultBlockSideShadingsByFacing;
		for (int i = 0; i < 6; i++)
		{
			BlockFacing face = BlockFacing.ALLFACES[i];
			ModelCubeUtilExt.AddFaceSkipTex(intoMesh, face, centerPos, cubeSize, color, shadings[face.Index]);
		}
	}

	private void TesselateArbitraryModel(ClientMain game, BlockPos[] positions, int[] colors)
	{
		Dictionary<BlockPos, int> faceDrawFlags = new Dictionary<BlockPos, int>();
		BlockPos min = positions[0].Copy();
		BlockPos max = positions[0].Copy();
		foreach (BlockPos cur in positions)
		{
			min.X = Math.Min(min.X, cur.X);
			min.Y = Math.Min(min.Y, cur.Y);
			min.Z = Math.Min(min.Z, cur.Z);
			max.X = Math.Max(max.X, cur.X);
			max.Y = Math.Max(max.Y, cur.Y);
			max.Z = Math.Max(max.Z, cur.Z);
			faceDrawFlags[cur] = 0;
		}
		foreach (BlockPos cur in positions)
		{
			int flags2 = 0;
			if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.NORTH)))
			{
				flags2 |= BlockFacing.NORTH.Flag;
			}
			if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.EAST)))
			{
				flags2 |= BlockFacing.EAST.Flag;
			}
			if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.SOUTH)))
			{
				flags2 |= BlockFacing.SOUTH.Flag;
			}
			if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.WEST)))
			{
				flags2 |= BlockFacing.WEST.Flag;
			}
			if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.UP)))
			{
				flags2 |= BlockFacing.UP.Flag;
			}
			if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.DOWN)))
			{
				flags2 |= BlockFacing.DOWN.Flag;
			}
			faceDrawFlags[cur] = flags2;
		}
		origin = min.Copy();
		if (mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || mode == EnumHighlightBlocksMode.AttachedToSelectedBlock || mode == EnumHighlightBlocksMode.CenteredToBlockSelectionIndex || mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex)
		{
			origin.X = 0;
			origin.Y = 0;
			origin.Z = 0;
			if (shape == EnumHighlightShape.Cube)
			{
				Size = new Vec3i(max.X - min.X + 1, max.Y - min.Y + 1, max.Z - min.Z + 1);
			}
			else
			{
				Size = new Vec3i(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
			}
			attachmentPoints = new BlockPos[6];
			for (int j = 0; j < 6; j++)
			{
				Vec3i m = BlockFacing.ALLNORMALI[j];
				if (shape == EnumHighlightShape.Cylinder)
				{
					attachmentPoints[j] = new BlockPos((int)((float)Size.X / 2f * (float)m.X), (int)Math.Ceiling((float)Size.Y / 2f * (float)m.Y), (int)((float)Size.Z / 2f * (float)m.Z));
					if (j == BlockFacing.DOWN.Index)
					{
						attachmentPoints[j].Y--;
					}
					if (j == BlockFacing.WEST.Index)
					{
						attachmentPoints[j].X--;
					}
					if (j == BlockFacing.NORTH.Index)
					{
						attachmentPoints[j].Z--;
					}
				}
				else if (shape == EnumHighlightShape.Cube)
				{
					attachmentPoints[j] = new BlockPos((int)((float)Size.X / 2f * (float)m.X), (int)((float)Size.Y / 2f * (float)m.Y), (int)((float)Size.Z / 2f * (float)m.Z));
					if (Size.Y == 1 && j == BlockFacing.DOWN.Index)
					{
						attachmentPoints[j].Y--;
					}
					if (Size.X == 1 && j == BlockFacing.WEST.Index)
					{
						attachmentPoints[j].X--;
					}
					if (Size.Z == 1 && j == BlockFacing.NORTH.Index)
					{
						attachmentPoints[j].Z--;
					}
				}
				else
				{
					attachmentPoints[j] = new BlockPos((int)((float)Size.X / 2f * (float)m.X), (int)((float)Size.Y / 2f * (float)m.Y), (int)((float)Size.Z / 2f * (float)m.Z));
					if (j == BlockFacing.DOWN.Index)
					{
						attachmentPoints[j].Y--;
					}
					if (j == BlockFacing.WEST.Index)
					{
						attachmentPoints[j].X--;
					}
					if (j == BlockFacing.NORTH.Index)
					{
						attachmentPoints[j].Z--;
					}
				}
			}
		}
		MeshData modeldata = new MeshData(positions.Length * 4 * 6, positions.Length * 6 * 6, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		Vec3f centerPos = new Vec3f();
		Vec3f cubeSize = new Vec3f(1f, 1f, 1f);
		int color = defaultColor;
		if (colors != null && colors.Length != 0)
		{
			color = colors[0];
		}
		bool manyColors = colors != null && colors.Length >= positions.Length && colors.Length > 1;
		float[] shadings = CubeMeshUtil.DefaultBlockSideShadingsByFacing;
		int posIndex = 0;
		foreach (KeyValuePair<BlockPos, int> val in faceDrawFlags)
		{
			int flags = val.Value;
			centerPos.X = (float)(val.Key.X - origin.X) + 0.5f;
			centerPos.Y = (float)(val.Key.InternalY - origin.Y) + 0.5f;
			centerPos.Z = (float)(val.Key.Z - origin.Z) + 0.5f;
			for (int i = 0; i < 6; i++)
			{
				BlockFacing face = BlockFacing.ALLFACES[i];
				if ((flags & face.Flag) != 0)
				{
					ModelCubeUtilExt.AddFaceSkipTex(modeldata, face, centerPos, cubeSize, manyColors ? colors[posIndex] : color, shadings[face.Index]);
				}
			}
			posIndex++;
		}
		modelRef = game.Platform.UploadMesh(modeldata);
	}

	internal void Dispose(ClientMain game)
	{
		if (modelRef != null)
		{
			game.Platform.DeleteMesh(modelRef);
		}
	}
}
