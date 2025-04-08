using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.Datastructures;

public class NaturalShape
{
	private readonly Dictionary<Vec2i, ShapeCell> outline;

	private readonly HashSet<Vec2i> inside;

	private readonly IRandom rand;

	private readonly NatFloat natFloat;

	private bool hasSquareStart;

	public NaturalShape(IRandom rand)
	{
		this.rand = rand;
		outline = new Dictionary<Vec2i, ShapeCell>();
		inside = new HashSet<Vec2i>();
		natFloat = NatFloat.createGauss(1f, 1f);
		Init();
	}

	private void Init()
	{
		Vec2i tmpVec = new Vec2i();
		outline.Add(tmpVec, new ShapeCell(tmpVec, new bool[4] { true, true, true, true }));
	}

	public void InitSquare(int sizeX, int sizeZ)
	{
		hasSquareStart = true;
		inside.Clear();
		outline.Clear();
		for (int x = 0; x < sizeX; x++)
		{
			for (int z = 0; z < sizeZ; z++)
			{
				Vec2i pos = new Vec2i(x, z);
				bool[] openSide = new bool[4];
				if (z == 0)
				{
					openSide[0] = true;
				}
				else if (z == sizeZ - 1)
				{
					openSide[2] = true;
				}
				if (x == 0)
				{
					openSide[3] = true;
				}
				else if (x == sizeX - 1)
				{
					openSide[1] = true;
				}
				ShapeCell shapeCell = new ShapeCell(pos, openSide);
				if (!openSide.Any((bool s) => s))
				{
					inside.Add(pos);
				}
				else
				{
					outline.Add(pos, shapeCell);
				}
			}
		}
	}

	public bool[] GetOpenSides(Vec2i c)
	{
		bool[] openSides = new bool[4];
		for (int i = 0; i < 4; i++)
		{
			Vec2i offsetByIndex = c + GetOffsetByIndex(i);
			bool hasONe = outline.ContainsKey(offsetByIndex);
			bool hasINe = inside.Contains(offsetByIndex);
			openSides[i] = !hasONe && !hasINe;
		}
		return openSides;
	}

	public ShapeCell GetBySide(ShapeCell cell, int index)
	{
		Vec2i offset = GetOffsetByIndex(index);
		Vec2i offPos = cell.Position + offset;
		bool[] openSides = GetOpenSides(offPos);
		return new ShapeCell(offPos, openSides);
	}

	private static Vec2i GetOffsetByIndex(int index)
	{
		Vec2i offset = new Vec2i();
		switch (index)
		{
		case 0:
			offset.Set(0, -1);
			break;
		case 1:
			offset.Set(1, 0);
			break;
		case 2:
			offset.Set(0, 1);
			break;
		case 3:
			offset.Set(-1, 0);
			break;
		}
		return offset;
	}

	public void Grow(int steps)
	{
		for (int i = 0; i < steps; i++)
		{
			if (hasSquareStart)
			{
				natFloat.avg = (float)outline.Count * 0.5f;
				natFloat.var = (float)outline.Count * 0.5f;
			}
			else
			{
				natFloat.avg = (float)outline.Count * 0.85f;
				natFloat.var = (float)outline.Count * 0.15f;
			}
			int next = (int)natFloat.nextFloat(1f, rand);
			KeyValuePair<Vec2i, ShapeCell> cell = outline.ElementAt(next);
			next = rand.NextInt(4);
			ShapeCell newCell = null;
			for (int k = next; k < next + 4; k++)
			{
				if (cell.Value.OpenSides[k % 4])
				{
					newCell = GetBySide(cell.Value, k % 4);
					if (newCell.OpenSides.Any((bool s) => s))
					{
						outline.TryAdd(newCell.Position, newCell);
					}
					else
					{
						inside.Add(newCell.Position);
					}
					break;
				}
			}
			if (newCell == null)
			{
				continue;
			}
			for (int j = 0; j < 4; j++)
			{
				Vec2i offset = GetOffsetByIndex(j);
				Vec2i newCellPosition = newCell.Position + offset;
				if (outline.TryGetValue(newCellPosition, out var foundCell))
				{
					foundCell.OpenSides = GetOpenSides(newCellPosition);
					if (!GetOpenSides(newCellPosition).Any((bool s) => s))
					{
						outline.Remove(newCellPosition);
						inside.Add(new Vec2i(newCellPosition.X, newCellPosition.Y));
					}
				}
			}
		}
	}

	public List<BlockPos> GetPositions(BlockPos start)
	{
		List<BlockPos> list = new List<BlockPos>();
		foreach (var (pos2, _) in outline)
		{
			list.Add(new BlockPos(start.X + pos2.X, start.Y, start.Z + pos2.Y, 0));
		}
		foreach (Vec2i pos in inside)
		{
			list.Add(new BlockPos(start.X + pos.X, start.Y, start.Z + pos.Y, 0));
		}
		return list;
	}

	public List<Vec2i> GetPositions()
	{
		List<Vec2i> list = new List<Vec2i>();
		foreach (var (pos2, _) in outline)
		{
			list.Add(pos2);
		}
		foreach (Vec2i pos in inside)
		{
			list.Add(pos);
		}
		return list;
	}
}
