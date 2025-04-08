using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShadowBox
{
	public static double ShadowBoxZExtend = 100.0;

	public static double ShadowBoxYExtend = 0.0;

	public static Vec4d UP = new Vec4d(0.0, 1.0, 0.0, 0.0);

	public static Vec4d FORWARD = new Vec4d(0.0, 0.0, -1.0, 0.0);

	public static double SHADOW_DISTANCE = 100.0;

	public double minX;

	public double maxX;

	public double minY;

	public double maxY;

	public double minZ;

	public double maxZ;

	public double[] lightViewMatrix;

	private Camera camera;

	private ClientMain game;

	public double farHeight;

	public double farWidth;

	public double nearHeight;

	public double nearWidth;

	private Vec4d forwardVector = new Vec4d();

	private Vec4d[] points = new Vec4d[8]
	{
		new Vec4d(),
		new Vec4d(),
		new Vec4d(),
		new Vec4d(),
		new Vec4d(),
		new Vec4d(),
		new Vec4d(),
		new Vec4d()
	};

	private Vec4d upVector = new Vec4d();

	private Vec3d rightVector = new Vec3d();

	private Vec3d downVector = new Vec3d();

	private Vec3d leftVector = new Vec3d();

	private Vec3d farTop = new Vec3d();

	private Vec3d farBottom = new Vec3d();

	private Vec3d nearTop = new Vec3d();

	private Vec3d nearBottom = new Vec3d();

	private double[] vec4f = new double[4] { 0.0, 0.0, 0.0, 1.0 };

	private double[] rotation = Mat4d.Create();

	public double Width => maxX - minX;

	public double Height => maxY - minY;

	public double Length => maxZ - minZ;

	public ShadowBox(double[] lightViewMatrix, ClientMain game)
	{
		this.lightViewMatrix = lightViewMatrix;
		camera = game.MainCamera;
		this.game = game;
		calculateWidthsAndHeights();
	}

	public void update()
	{
		double[] rotationMat = getCameraRotationMatrix();
		Mat4d.MulWithVec4(rotationMat, FORWARD, forwardVector);
		Vec3d vec3d = new Vec3d(forwardVector);
		vec3d.Mul(SHADOW_DISTANCE);
		Vec3d vec3d2 = new Vec3d(forwardVector);
		vec3d2.Mul(camera.ZNear);
		Vec3d centerNear = vec3d2 + camera.OriginPosition;
		Vec3d centerFar = vec3d + camera.OriginPosition;
		Vec4d[] array = calculateFrustumVertices(rotationMat, forwardVector, centerNear, centerFar);
		bool first = true;
		Vec4d[] array2 = array;
		foreach (Vec4d point in array2)
		{
			if (first)
			{
				minX = point.X;
				maxX = point.X;
				minY = point.Y;
				maxY = point.Y;
				minZ = point.Z;
				maxZ = point.Z;
				first = false;
				continue;
			}
			if (point.X > maxX)
			{
				maxX = point.X;
			}
			else if (point.X < minX)
			{
				minX = point.X;
			}
			if (point.Y > maxY)
			{
				maxY = point.Y;
			}
			else if (point.Y < minY)
			{
				minY = point.Y;
			}
			if (point.Z > maxZ)
			{
				maxZ = point.Z;
			}
			else if (point.Z < minZ)
			{
				minZ = point.Z;
			}
		}
		minZ += 0.0;
		maxZ += ShadowBoxZExtend;
	}

	public Vec4d[] calculateFrustumVertices(double[] rotation, Vec4d forwardVector, Vec3d centerNear, Vec3d centerFar)
	{
		Mat4d.MulWithVec4(rotation, UP, upVector);
		rightVector.Cross(forwardVector, upVector);
		downVector.Set(0.0 - upVector.X, 0.0 - upVector.Y, 0.0 - upVector.Z);
		leftVector.Set(0.0 - rightVector.X, 0.0 - rightVector.Y, 0.0 - rightVector.Z);
		farTop.Set(centerFar.X + upVector.X * farHeight, centerFar.Y + upVector.Y * farHeight, centerFar.Z + upVector.Z * farHeight);
		farBottom.Set(centerFar.X + downVector.X * farHeight, centerFar.Y + downVector.Y * farHeight, centerFar.Z + downVector.Z * farHeight);
		nearTop.Set(centerNear.X + upVector.X * nearHeight, centerNear.Y + upVector.Y * nearHeight, centerNear.Z + upVector.Z * nearHeight);
		nearBottom.Set(centerNear.X + downVector.X * nearHeight, centerNear.Y + downVector.Y * nearHeight, centerNear.Z + downVector.Z * nearHeight);
		calculateLightSpaceFrustumCorner(farTop, rightVector, farWidth, points[0]);
		calculateLightSpaceFrustumCorner(farTop, leftVector, farWidth, points[1]);
		calculateLightSpaceFrustumCorner(farBottom, rightVector, farWidth, points[2]);
		calculateLightSpaceFrustumCorner(farBottom, leftVector, farWidth, points[3]);
		calculateLightSpaceFrustumCorner(nearTop, rightVector, nearWidth, points[4]);
		calculateLightSpaceFrustumCorner(nearTop, leftVector, nearWidth, points[5]);
		calculateLightSpaceFrustumCorner(nearBottom, rightVector, nearWidth, points[6]);
		calculateLightSpaceFrustumCorner(nearBottom, leftVector, nearWidth, points[7]);
		return points;
	}

	public void calculateLightSpaceFrustumCorner(Vec3d startPoint, Vec3d direction, double width, Vec4d target)
	{
		vec4f[0] = startPoint.X + direction.X * width;
		vec4f[1] = startPoint.Y + direction.Y * width;
		vec4f[2] = startPoint.Z + direction.Z * width;
		Mat4d.MulWithVec4(lightViewMatrix, vec4f, target);
	}

	public double[] getCameraRotationMatrix()
	{
		Mat4d.Identity(rotation);
		return rotation;
	}

	public void calculateWidthsAndHeights()
	{
		float fowMul = Math.Min(1f, (float)ClientSettings.FieldOfView / 90f);
		farWidth = (float)(SHADOW_DISTANCE * (double)fowMul);
		nearWidth = camera.ZNear * fowMul;
		farHeight = farWidth / (double)getAspectRatio();
		nearHeight = nearWidth / (double)getAspectRatio();
	}

	private float getAspectRatio()
	{
		return (float)game.Width / (float)game.Height;
	}
}
