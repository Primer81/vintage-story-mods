using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class FrustumCulling
{
	public int ViewDistanceSq;

	internal BlockPos playerPos;

	public float lod0BiasSq;

	/// <summary>If distance squared is above this fraction of ViewDistanceSq, switch to LOD2.  Default value corresponds to distances beyond around 67% of the player's view distance.   At default, approximately 55% of the total rendered chunks will use LOD2.</summary>
	public double lod2BiasSq = 0.45;

	public double shadowRangeX;

	public double shadowRangeZ;

	/// <summary>
	/// Index order: Near 0, Left 1, Right 2, Top 3, Bottom 4, Far 5
	/// </summary>
	private Plane[] frustum = new Plane[6];

	public void UpdateViewDistance(int newValue)
	{
		ViewDistanceSq = newValue * newValue + 400;
	}

	public bool SphereInFrustum(double x, double y, double z, double radius)
	{
		if (frustum[0].distanceOfPoint(x, y, z) <= 0.0 - radius)
		{
			return false;
		}
		if (frustum[1].distanceOfPoint(x, y, z) <= 0.0 - radius)
		{
			return false;
		}
		if (frustum[2].distanceOfPoint(x, y, z) <= 0.0 - radius)
		{
			return false;
		}
		if (frustum[3].distanceOfPoint(x, y, z) <= 0.0 - radius)
		{
			return false;
		}
		if (frustum[4].distanceOfPoint(x, y, z) <= 0.0 - radius)
		{
			return false;
		}
		if (frustum[5].distanceOfPoint(x, y, z) <= 0.0 - radius)
		{
			return false;
		}
		return true;
	}

	public bool InFrustum(Sphere sphere)
	{
		if (frustum[0].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[1].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[2].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[3].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[4].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[5].AABBisOutside(sphere))
		{
			return false;
		}
		return true;
	}

	public bool InFrustumShadowPass(Sphere sphere)
	{
		if ((double)Math.Abs((float)playerPos.X - sphere.x) >= shadowRangeX)
		{
			return false;
		}
		if ((double)Math.Abs((float)playerPos.Z - sphere.z) >= shadowRangeZ)
		{
			return false;
		}
		if (frustum[0].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[1].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[2].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[3].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[4].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[5].AABBisOutside(sphere))
		{
			return false;
		}
		return true;
	}

	public bool InFrustumAndRange(Sphere sphere, bool nowVisible, int lodLevel = 0)
	{
		if (frustum[0].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[1].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[2].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[3].AABBisOutside(sphere))
		{
			return false;
		}
		if (frustum[4].AABBisOutside(sphere))
		{
			return false;
		}
		double distance = playerPos.HorDistanceSqTo(sphere.x, sphere.z);
		switch (lodLevel)
		{
		case 0:
			if (lod0BiasSq > 0f)
			{
				return distance < (double)(lod0BiasSq + 1024f);
			}
			return false;
		case 1:
			return distance < (double)ViewDistanceSq;
		case 2:
			return distance <= lod2BiasSq;
		case 3:
			if (distance > lod2BiasSq)
			{
				return distance < (double)ViewDistanceSq;
			}
			return false;
		default:
			return false;
		}
	}

	public void CalcFrustumEquations(BlockPos playerPos, double[] projectionMatrix, double[] cameraMatrix)
	{
		this.playerPos = playerPos;
		double[] matFrustum = Mat4d.Create();
		Mat4d.Multiply(matFrustum, projectionMatrix, cameraMatrix);
		CalcFrustumEquations(matFrustum);
	}

	/// <summary>
	/// Calculating the frustum planes.
	/// </summary>
	/// <remarks>
	/// From the current OpenGL modelview and projection matrices,
	/// calculate the frustum plane equations (Ax+By+Cz+D=0, normal=(A,B,C))
	/// The equations can then be used to see on which side points are.
	/// </remarks>
	private void CalcFrustumEquations(double[] matrix)
	{
		double x = matrix[3] - matrix[0];
		double y = matrix[7] - matrix[4];
		double z = matrix[11] - matrix[8];
		double d = matrix[15] - matrix[12];
		frustum[2] = new Plane(x, y, z, d);
		x = matrix[3] + matrix[0];
		y = matrix[7] + matrix[4];
		z = matrix[11] + matrix[8];
		d = matrix[15] + matrix[12];
		frustum[1] = new Plane(x, y, z, d);
		x = matrix[3] + matrix[1];
		y = matrix[7] + matrix[5];
		z = matrix[11] + matrix[9];
		d = matrix[15] + matrix[13];
		frustum[4] = new Plane(x, y, z, d);
		x = matrix[3] - matrix[1];
		y = matrix[7] - matrix[5];
		z = matrix[11] - matrix[9];
		d = matrix[15] - matrix[13];
		frustum[3] = new Plane(x, y, z, d);
		x = matrix[3] - matrix[2];
		y = matrix[7] - matrix[6];
		z = matrix[11] - matrix[10];
		d = matrix[15] - matrix[14];
		frustum[5] = new Plane(x, y, z, d);
		x = matrix[3] + matrix[2];
		y = matrix[7] + matrix[6];
		z = matrix[11] + matrix[10];
		d = matrix[15] + matrix[14];
		frustum[0] = new Plane(x, y, z, d);
	}
}
