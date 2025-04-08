using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public class StackMatrix4
{
	private double[][] values;

	private const int max = 1024;

	private int count;

	public double[] Top => values[count - 1];

	public int Count => count;

	public StackMatrix4(int max = 1024)
	{
		values = new double[max][];
		for (int i = 0; i < max; i++)
		{
			values[i] = Mat4d.Create();
		}
	}

	public void PushIdentity()
	{
		Mat4d.Identity(values[count]);
		count++;
		if (count >= values.Length)
		{
			throw new Exception("Stack matrix overflow");
		}
	}

	public void Push(double[] p)
	{
		Mat4d.Copy(values[count], p);
		count++;
		if (count >= values.Length)
		{
			throw new Exception("Stack matrix overflow");
		}
	}

	public void Push()
	{
		Mat4d.Copy(values[count], Top);
		count++;
		if (count >= values.Length)
		{
			throw new Exception("Stack matrix overflow");
		}
	}

	public double[] Pop()
	{
		double[] result = values[count - 1];
		count--;
		if (count < 0)
		{
			throw new Exception("Stack matrix underflow");
		}
		return result;
	}

	public void Clear()
	{
		count = 0;
	}

	public void Rotate(double rad, double x, double y, double z)
	{
		Mat4d.Rotate(Top, Top, rad, x, y, z);
	}

	public void Translate(double x, double y, double z)
	{
		Mat4d.Translate(Top, Top, x, y, z);
	}

	public void Scale(double x, double y, double z)
	{
		Mat4d.Scale(Top, x, y, z);
	}

	public void Translate(double[] rotationOrigin)
	{
		Mat4d.Translate(Top, Top, rotationOrigin);
	}
}
