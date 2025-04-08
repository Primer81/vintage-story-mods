using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitRenderer
{
	private Dictionary<Vec3d, FruitData> positions = new Dictionary<Vec3d, FruitData>();

	private bool onGround;

	protected ICoreClientAPI capi;

	protected MeshData itemMesh;

	protected MeshRef meshref;

	private CustomMeshDataPartFloat matrixAndLightFloats;

	protected Vec3f tmp = new Vec3f();

	protected float[] tmpMat = Mat4f.Create();

	protected double[] quat = Quaterniond.Create();

	protected float[] qf = new float[4];

	protected float[] rotMat = Mat4f.Create();

	private static Vec3f noRotation = new Vec3f(0f, 0f, 0f);

	private Vec3f v = new Vec3f();

	private static int nextID = 0;

	private int id;

	private Vec3f i = new Vec3f();

	private Vec3f f = new Vec3f();

	public FruitRenderer(ICoreClientAPI capi, Item item)
	{
		this.capi = capi;
		id = nextID++;
		CompositeShape shapeLoc = item.Shape;
		if (item.Attributes != null && item.Attributes["onGround"].AsBool())
		{
			onGround = true;
		}
		AssetLocation loc = shapeLoc.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = Shape.TryGet(capi, loc);
		Vec3f rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(item, shape, out itemMesh, rot, null, shapeLoc.SelectiveElements);
		itemMesh.CustomFloats = (matrixAndLightFloats = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		itemMesh.CustomFloats.SetAllocationSize(202000);
		meshref = capi.Render.UploadMesh(itemMesh);
	}

	internal void Dispose()
	{
		meshref?.Dispose();
	}

	internal void AddFruit(Vec3d position, FruitData data)
	{
		positions[position] = data;
	}

	internal void RemoveFruit(Vec3d position)
	{
		positions.Remove(position);
	}

	internal void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
		if (positions.Count > 0)
		{
			matrixAndLightFloats.Count = positions.Count * 20;
			itemMesh.CustomFloats = matrixAndLightFloats;
			capi.Render.UpdateMesh(meshref, itemMesh);
			capi.Render.RenderMeshInstanced(meshref, positions.Count);
		}
	}

	protected virtual void UpdateCustomFloatBuffer()
	{
		Vec3d camera = capi.World.Player.Entity.CameraPos;
		float windSpeed = GlobalConstants.CurrentWindSpeedClient.X;
		float windWaveIntensity = 1f;
		float div = 105f;
		DefaultShaderUniforms shaderUniforms = capi.Render.ShaderUniforms;
		float wwaveHighFreq = shaderUniforms.WindWaveCounterHighFreq;
		float counter = shaderUniforms.WindWaveCounter;
		int i = 0;
		foreach (KeyValuePair<Vec3d, FruitData> fruit in positions)
		{
			Vec3d key = fruit.Key;
			Vec3f rot = fruit.Value.rotation;
			double posX = key.X;
			double posY = key.Y;
			double posZ = key.Z;
			float rotY = rot.Y;
			float rotX = rot.X;
			float rotZ = rot.Z;
			if (onGround)
			{
				BlockPos blockPos = fruit.Value.behavior.Blockentity.Pos;
				posY = (double)blockPos.Y - 0.0625;
				posX += 1.1 * (posX - (double)blockPos.X - 0.5);
				posZ += 1.1 * (posZ - (double)blockPos.Z - 0.5);
				rot = noRotation;
				rotY = (float)((posX + posZ) * 40.0 % 90.0);
			}
			else
			{
				double x = posX;
				double y = posY;
				double z = posZ;
				float heightBend = 0.7f * (0.5f + (float)y - (float)(int)y);
				double strength = (double)(windWaveIntensity * (1f + windSpeed)) * (0.5 + (posY - (double)fruit.Value.behavior.Blockentity.Pos.Y)) / 2.0;
				v.Set((float)x % 4096f / 10f, (float)z % 4096f / 10f, counter % 1024f / 4f);
				float bendNoise = windSpeed * 0.2f + 1.4f * gnoise(v);
				float bend = windSpeed * (0.8f + bendNoise) * heightBend * windWaveIntensity;
				bend = Math.Min(4f, bend) * 0.2857143f / 2.8f;
				x += (double)wwaveHighFreq;
				y += (double)wwaveHighFreq;
				z += (double)wwaveHighFreq;
				strength *= 0.25;
				double dx = strength * (Math.Sin(x * 10.0) / 120.0 + (2.0 * Math.Sin(x / 2.0) + Math.Sin(x + y) + Math.Sin(0.5 + 4.0 * x + 2.0 * y) + Math.Sin(1.0 + 6.0 * x + 3.0 * y) / 3.0) / (double)div);
				double dz = strength * ((2.0 * Math.Sin(z / 4.0) + Math.Sin(z + 3.0 * y) + Math.Sin(0.5 + 4.0 * z + 2.0 * y) + Math.Sin(1.0 + 6.0 * z + y) / 3.0) / (double)div);
				posX += dx;
				posY += strength * (Math.Sin(5.0 * y) / 15.0 + Math.Cos(10.0 * x) / 10.0 + Math.Sin(3.0 * z) / 2.0 + Math.Cos(x * 2.0) / 2.2) / (double)div;
				posZ += dz;
				rotX += (float)(dz * 6.0 + (double)(bend / 2f));
				rotZ += (float)(dx * 6.0 + (double)(bend / 2f));
				posX += (double)bend;
			}
			tmp.Set((float)(posX - camera.X), (float)(posY - camera.Y), (float)(posZ - camera.Z));
			UpdateLightAndTransformMatrix(matrixAndLightFloats.Values, i, tmp, fruit.Value.behavior.LightRgba, rotX, rotY, rotZ);
			i++;
		}
	}

	private float ghashDot(Vec3f p, Vec3f q, float oX, float oY, float oZ)
	{
		float num = q.X - oX;
		float qY = q.Y - oY;
		float qZ = q.Z - oZ;
		oX += p.X;
		oY += p.Y;
		oZ += p.Z;
		float pX = 127.1f * oX + 311.7f * oY + 74.7f * oZ;
		float pY = 269.5f * oX + 183.3f * oY + 246.1f * oZ;
		float pZ = 113.5f * oX + 271.9f * oY + 124.6f * oZ;
		return (float)((double)num * (-1.0 + 2.0 * fract((double)GameMath.Mod((pX * 0.025f + 8f) * pX, 289f) / 41.0)) + (double)qY * (-1.0 + 2.0 * fract((double)GameMath.Mod((pY * 0.025f + 8f) * pY, 289f) / 41.0)) + (double)qZ * (-1.0 + 2.0 * fract((double)GameMath.Mod((pZ * 0.025f + 8f) * pZ, 289f) / 41.0)));
	}

	private double fract(double v)
	{
		return v - Math.Floor(v);
	}

	private float gnoise(Vec3f p)
	{
		int ix = (int)p.X;
		int iy = (int)p.Y;
		int iz = (int)p.Z;
		i.Set(ix, iy, iz);
		f.Set(p.X - (float)ix, p.Y - (float)iy, p.Z - (float)iz);
		float ux = f.X * f.X * (3f - 2f * f.X);
		float uy = f.Y * f.Y * (3f - 2f * f.Y);
		float uz = f.Z * f.Z * (3f - 2f * f.Z);
		float ab1 = ghashDot(i, f, 0f, 0f, 0f);
		float ab2 = ghashDot(i, f, 0f, 0f, 1f);
		float at1 = ghashDot(i, f, 0f, 1f, 0f);
		float at2 = ghashDot(i, f, 0f, 1f, 1f);
		float bb1 = ghashDot(i, f, 1f, 0f, 0f);
		float bb2 = ghashDot(i, f, 1f, 0f, 1f);
		float bt1 = ghashDot(i, f, 1f, 1f, 0f);
		float bt2 = ghashDot(i, f, 1f, 1f, 1f);
		float rg1 = mix(mix(ab1, bb1, ux), mix(at1, bt1, ux), uy);
		float rg2 = mix(mix(ab2, bb2, ux), mix(at2, bt2, ux), uy);
		return 1.2f * mix(rg1, rg2, uz);
	}

	private float mix(float x, float y, float a)
	{
		return x * (1f - a) + y * a;
	}

	protected virtual void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotY, float rotZ)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X, distToCamera.Y, distToCamera.Z);
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
}
