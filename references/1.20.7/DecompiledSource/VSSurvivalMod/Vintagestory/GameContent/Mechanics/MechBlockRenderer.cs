using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public abstract class MechBlockRenderer
{
	protected ICoreClientAPI capi;

	protected MeshData updateMesh = new MeshData();

	protected int quantityBlocks;

	protected float[] tmpMat = Mat4f.Create();

	protected double[] quat = Quaterniond.Create();

	protected float[] qf = new float[4];

	protected float[] rotMat = Mat4f.Create();

	protected MechanicalPowerMod mechanicalPowerMod;

	protected Dictionary<BlockPos, IMechanicalPowerRenderable> renderedDevices = new Dictionary<BlockPos, IMechanicalPowerRenderable>();

	protected Vec3f tmp = new Vec3f();

	public MechBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod)
	{
		this.mechanicalPowerMod = mechanicalPowerMod;
		this.capi = capi;
	}

	public void AddDevice(IMechanicalPowerRenderable device)
	{
		renderedDevices[device.Position] = device;
		quantityBlocks = renderedDevices.Count;
	}

	public bool RemoveDevice(IMechanicalPowerRenderable device)
	{
		bool result = renderedDevices.Remove(device.Position);
		quantityBlocks = renderedDevices.Count;
		return result;
	}

	protected virtual void UpdateCustomFloatBuffer()
	{
		Vec3d pos = capi.World.Player.Entity.CameraPos;
		int i = 0;
		foreach (IMechanicalPowerRenderable dev in renderedDevices.Values)
		{
			tmp.Set((float)((double)dev.Position.X - pos.X), (float)((double)dev.Position.Y - pos.Y), (float)((double)dev.Position.Z - pos.Z));
			UpdateLightAndTransformMatrix(i, tmp, dev.AngleRad % ((float)Math.PI * 2f), dev);
			i++;
		}
	}

	protected abstract void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotRad, IMechanicalPowerRenderable dev);

	protected virtual void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotY, float rotZ)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + 0.5f, distToCamera.Y + 0.5f, distToCamera.Z + 0.5f);
		quat[0] = 0.0;
		quat[1] = 0.0;
		quat[2] = 0.0;
		quat[3] = 1.0;
		if (rotX != 0f)
		{
			Quaterniond.RotateX(quat, quat, rotX);
		}
		if (rotY != 0f)
		{
			Quaterniond.RotateY(quat, quat, rotY);
		}
		if (rotZ != 0f)
		{
			Quaterniond.RotateZ(quat, quat, rotZ);
		}
		for (int j = 0; j < quat.Length; j++)
		{
			qf[j] = (float)quat[j];
		}
		Mat4f.Mul(tmpMat, tmpMat, Mat4f.FromQuat(rotMat, qf));
		Mat4f.Translate(tmpMat, tmpMat, -0.5f, -0.5f, -0.5f);
		int k = index * 20;
		values[k] = lightRgba.R;
		values[++k] = lightRgba.G;
		values[++k] = lightRgba.B;
		values[++k] = lightRgba.A;
		for (int i = 0; i < 16; i++)
		{
			values[++k] = tmpMat[i];
		}
	}

	public virtual void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
	}

	public virtual void Dispose()
	{
	}
}
