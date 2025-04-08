using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class PickingRayUtil
{
	private Unproject unproject;

	private double[] tempViewport;

	private double[] tempRay;

	private double[] tempRayStartPoint;

	public PickingRayUtil()
	{
		unproject = new Unproject();
		tempViewport = new double[4];
		tempRay = new double[4];
		tempRayStartPoint = new double[4];
	}

	public Ray GetPickingRayByMouseCoordinates(ClientMain game)
	{
		int mouseX = game.MouseCurrentX;
		int mouseY = game.MouseCurrentY;
		tempViewport[0] = 0.0;
		tempViewport[1] = 0.0;
		tempViewport[2] = game.Width;
		tempViewport[3] = game.Height;
		unproject.UnProject(mouseX, game.Height - mouseY, 1, game.MvMatrix.Top, game.PMatrix.Top, tempViewport, tempRay);
		unproject.UnProject(mouseX, game.Height - mouseY, 0, game.MvMatrix.Top, game.PMatrix.Top, tempViewport, tempRayStartPoint);
		double raydirX = tempRay[0] - tempRayStartPoint[0];
		double raydirY = tempRay[1] - tempRayStartPoint[1];
		double raydirZ = tempRay[2] - tempRayStartPoint[2];
		float raydirLength = Length((float)raydirX, (float)raydirY, (float)raydirZ);
		raydirX /= (double)raydirLength;
		raydirY /= (double)raydirLength;
		raydirZ /= (double)raydirLength;
		float pickDistance1 = game.player.WorldData.PickingRange;
		bool doOffsetOrigin = game.MainCamera.CameraMode != 0 && (game.MouseGrabbed || game.mouseWorldInteractAnyway);
		Ray ray = new Ray(new Vec3d(tempRayStartPoint[0] + (doOffsetOrigin ? (raydirX * (double)game.MainCamera.Tppcameradistance) : 0.0), tempRayStartPoint[1] + (doOffsetOrigin ? (raydirY * (double)game.MainCamera.Tppcameradistance) : 0.0), tempRayStartPoint[2] + (doOffsetOrigin ? (raydirZ * (double)game.MainCamera.Tppcameradistance) : 0.0)), new Vec3d(raydirX * (double)pickDistance1, raydirY * (double)pickDistance1, raydirZ * (double)pickDistance1));
		if (double.IsNaN(ray.origin.X))
		{
			return null;
		}
		return ray;
	}

	internal float Length(float x, float y, float z)
	{
		return (float)Math.Sqrt(x * x + y * y + z * z);
	}
}
