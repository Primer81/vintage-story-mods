using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ClothConstraint
{
	private static Random Rand = new Random();

	[ProtoMember(1)]
	public int PointIndex1;

	[ProtoMember(2)]
	public int PointIndex2;

	[ProtoMember(3)]
	private float squared_rest_length;

	private ClothPoint p1;

	private ClothPoint p2;

	private float rest_length;

	private float inverse_length;

	private Vec3f tensionDirection = new Vec3f();

	private float StretchStiffness = 200f;

	private double springLength;

	public Vec3d renderCenterPos;

	private double extension;

	public ClothPoint Point1 => p1;

	public ClothPoint Point2 => p2;

	public double SpringLength => springLength;

	public double Extension => extension;

	public ClothConstraint()
	{
	}

	public ClothConstraint(ClothPoint p1, ClothPoint p2)
	{
		this.p1 = p1;
		this.p2 = p2;
		PointIndex1 = p1.PointIndex;
		PointIndex2 = p2.PointIndex;
		squared_rest_length = p1.Pos.SquareDistanceTo(p2.Pos);
		rest_length = GameMath.Sqrt(squared_rest_length);
		inverse_length = 1f / rest_length;
		renderCenterPos = p1.Pos + (p1.Pos - p2.Pos) / 2f;
	}

	public void RestorePoints(Dictionary<int, ClothPoint> pointsByIndex)
	{
		p1 = pointsByIndex[PointIndex1];
		p2 = pointsByIndex[PointIndex2];
		rest_length = GameMath.Sqrt(squared_rest_length);
		inverse_length = 1f / rest_length;
		renderCenterPos = p1.Pos + (p1.Pos - p2.Pos) / 2f;
	}

	public void satisfy(float pdt)
	{
		tensionDirection.Set((float)(p1.Pos.X - p2.Pos.X), (float)(p1.Pos.Y - p2.Pos.Y), (float)(p1.Pos.Z - p2.Pos.Z));
		springLength = tensionDirection.Length();
		if (springLength == 0.0)
		{
			tensionDirection.Set((float)Rand.NextDouble() / 100f - 0.02f, (float)Rand.NextDouble() / 100f - 0.02f, (float)Rand.NextDouble() / 100f - 0.02f);
			springLength = tensionDirection.Length();
		}
		extension = springLength - (double)rest_length;
		double tension = (double)StretchStiffness * (extension * (double)inverse_length);
		tensionDirection *= (float)(tension / springLength);
		p2.Tension.Add(tensionDirection);
		p1.Tension.Sub(tensionDirection);
		p2.TensionDirection.Set(tensionDirection);
		p1.TensionDirection.Set(0f - tensionDirection.X, 0f - tensionDirection.Y, 0f - tensionDirection.Z);
		p1.extension = extension;
		p2.extension = extension;
	}
}
