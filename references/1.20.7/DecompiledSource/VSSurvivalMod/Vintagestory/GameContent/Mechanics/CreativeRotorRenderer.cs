using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class CreativeRotorRenderer : MechBlockRenderer
{
	private CustomMeshDataPartFloat matrixAndLightFloats1;

	private CustomMeshDataPartFloat matrixAndLightFloats2;

	private CustomMeshDataPartFloat matrixAndLightFloats3;

	private CustomMeshDataPartFloat matrixAndLightFloats4;

	private CustomMeshDataPartFloat matrixAndLightFloats5;

	private MeshRef blockMeshRef1;

	private MeshRef blockMeshRef2;

	private MeshRef blockMeshRef3;

	private MeshRef blockMeshRef4;

	private Vec3f axisCenter = new Vec3f(0.5f, 0.5f, 0.5f);

	public CreativeRotorRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		MeshData blockMesh2 = null;
		MeshData blockMesh3 = null;
		MeshData blockMesh4 = null;
		AssetLocation loc = new AssetLocation("shapes/block/metal/mechanics/creativerotor-axle.json");
		Shape shape = Shape.TryGet(capi, loc);
		Vec3f rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var blockMesh1, rot);
		rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		Shape ovshape = Shape.TryGet(capi, new AssetLocation("shapes/block/metal/mechanics/creativerotor-contra.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, ovshape, out blockMesh2, rot);
		Shape ovshape2 = Shape.TryGet(capi, new AssetLocation("shapes/block/metal/mechanics/creativerotor-spinbar.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, ovshape2, out blockMesh3, rot);
		Shape ovshape3 = Shape.TryGet(capi, new AssetLocation("shapes/block/metal/mechanics/creativerotor-spinball.json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, ovshape3, out blockMesh4, rot);
		int count = 42000;
		blockMesh1.CustomFloats = (matrixAndLightFloats1 = createCustomFloats(count));
		blockMesh2.CustomFloats = (matrixAndLightFloats2 = createCustomFloats(count));
		blockMesh3.CustomFloats = (matrixAndLightFloats3 = createCustomFloats(count));
		blockMesh4.CustomFloats = (matrixAndLightFloats4 = createCustomFloats(count));
		matrixAndLightFloats5 = createCustomFloats(count);
		blockMeshRef1 = capi.Render.UploadMesh(blockMesh1);
		blockMeshRef2 = capi.Render.UploadMesh(blockMesh2);
		blockMeshRef3 = capi.Render.UploadMesh(blockMesh3);
		blockMeshRef4 = capi.Render.UploadMesh(blockMesh4);
	}

	private CustomMeshDataPartFloat createCustomFloats(int count)
	{
		CustomMeshDataPartFloat customMeshDataPartFloat = new CustomMeshDataPartFloat(count);
		customMeshDataPartFloat.Instanced = true;
		customMeshDataPartFloat.InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 };
		customMeshDataPartFloat.InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 };
		customMeshDataPartFloat.InterleaveStride = 80;
		customMeshDataPartFloat.StaticDraw = false;
		customMeshDataPartFloat.SetAllocationSize(count);
		return customMeshDataPartFloat;
	}

	protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotation, IMechanicalPowerRenderable dev)
	{
		float rot1 = dev.AngleRad;
		float num = (float)Math.PI * 2f - dev.AngleRad;
		float rot2 = rot1 * 2f;
		float axX = -Math.Abs(dev.AxisSign[0]);
		float axZ = -Math.Abs(dev.AxisSign[2]);
		float rotX = rot1 * axX;
		float rotZ = rot1 * axZ;
		UpdateLightAndTransformMatrix(matrixAndLightFloats1.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, null);
		rotX = num * axX;
		rotZ = num * axZ;
		UpdateLightAndTransformMatrix(matrixAndLightFloats2.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, null);
		rotX = rot2 * axX;
		rotZ = rot2 * axZ;
		UpdateLightAndTransformMatrix(matrixAndLightFloats3.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, null);
		rotX = (rot2 + (float)Math.PI / 4f) * axX;
		rotZ = (rot2 + (float)Math.PI / 4f) * axZ;
		TransformMatrix(distToCamera, rotX, rotZ, axisCenter);
		rotX = ((axX == 0f) ? (rot1 * 2f) : 0f);
		rotZ = ((axZ == 0f) ? ((0f - rot1) * 2f) : 0f);
		axX = (float)dev.AxisSign[0] * 0.05f;
		axZ = (float)dev.AxisSign[2] * 0.05f;
		UpdateLightAndTransformMatrix(matrixAndLightFloats4.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, new Vec3f(0.5f + axX, 0.5f, 0.5f + axZ), (float[])tmpMat.Clone());
		rotX = (rot2 + 3.926991f) * (float)(-Math.Abs(dev.AxisSign[0]));
		rotZ = (rot2 + 3.926991f) * (float)(-Math.Abs(dev.AxisSign[2]));
		TransformMatrix(distToCamera, rotX, rotZ, axisCenter);
		rotX = ((axX == 0f) ? (rot1 * 2f) : 0f);
		rotZ = ((axZ == 0f) ? ((0f - rot1) * 2f) : 0f);
		UpdateLightAndTransformMatrix(matrixAndLightFloats5.Values, index, distToCamera, dev.LightRgba, rotX, rotZ, new Vec3f(0.5f + axX, 0.5f, 0.5f + axZ), (float[])tmpMat.Clone());
	}

	private void TransformMatrix(Vec3f distToCamera, float rotX, float rotZ, Vec3f axis)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y, distToCamera.Z + axis.Z);
		quat[0] = 0.0;
		quat[1] = 0.0;
		quat[2] = 0.0;
		quat[3] = 1.0;
		if (rotX != 0f)
		{
			Quaterniond.RotateX(quat, quat, rotX);
		}
		if (rotZ != 0f)
		{
			Quaterniond.RotateZ(quat, quat, rotZ);
		}
		for (int i = 0; i < quat.Length; i++)
		{
			qf[i] = (float)quat[i];
		}
		Mat4f.Mul(tmpMat, tmpMat, Mat4f.FromQuat(rotMat, qf));
		Mat4f.Translate(tmpMat, tmpMat, 0f - axis.X, 0f - axis.Y, 0f - axis.Z);
	}

	protected void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotZ, Vec3f axis, float[] initialTransform)
	{
		if (initialTransform == null)
		{
			Mat4f.Identity(tmpMat);
			Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y, distToCamera.Z + axis.Z);
		}
		else
		{
			Mat4f.Translate(tmpMat, tmpMat, axis.X, axis.Y, axis.Z);
		}
		quat[0] = 0.0;
		quat[1] = 0.0;
		quat[2] = 0.0;
		quat[3] = 1.0;
		if (rotX != 0f)
		{
			Quaterniond.RotateX(quat, quat, rotX);
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
		Mat4f.Translate(tmpMat, tmpMat, 0f - axis.X, 0f - axis.Y, 0f - axis.Z);
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

	public override void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
		if (quantityBlocks > 0)
		{
			matrixAndLightFloats1.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats1;
			capi.Render.UpdateMesh(blockMeshRef1, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef1, quantityBlocks);
			matrixAndLightFloats2.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats2;
			capi.Render.UpdateMesh(blockMeshRef2, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef2, quantityBlocks);
			matrixAndLightFloats3.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats3;
			capi.Render.UpdateMesh(blockMeshRef3, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef3, quantityBlocks);
			matrixAndLightFloats4.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats4;
			capi.Render.UpdateMesh(blockMeshRef4, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef4, quantityBlocks);
			matrixAndLightFloats5.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats5;
			capi.Render.UpdateMesh(blockMeshRef4, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef4, quantityBlocks);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		blockMeshRef1?.Dispose();
		blockMeshRef2?.Dispose();
		blockMeshRef3?.Dispose();
		blockMeshRef4?.Dispose();
	}
}
