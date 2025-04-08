using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class PulverizerRenderer : MechBlockRenderer, ITexPositionSource
{
	public static string[] metals = new string[7] { "nometal", "tinbronze", "bismuthbronze", "blackbronze", "iron", "meteoriciron", "steel" };

	private CustomMeshDataPartFloat matrixAndLightFloatsAxle;

	private CustomMeshDataPartFloat[] matrixAndLightFloatsLPounder = new CustomMeshDataPartFloat[metals.Length];

	private CustomMeshDataPartFloat[] matrixAndLightFloatsRPounder = new CustomMeshDataPartFloat[metals.Length];

	private readonly MeshRef toggleMeshref;

	private readonly MeshRef[] lPoundMeshrefs = new MeshRef[metals.Length];

	private readonly MeshRef[] rPounderMeshrefs = new MeshRef[metals.Length];

	private readonly Vec3f axisCenter = new Vec3f(0.5f, 0.5f, 0.5f);

	private int quantityAxles;

	private int[] quantityLPounders = new int[metals.Length];

	private int[] quantityRPounders = new int[metals.Length];

	private ITexPositionSource texSource;

	private string metal;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "cap")
			{
				return texSource["capmetal-" + metal];
			}
			return texSource[textureCode];
		}
	}

	public PulverizerRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		int count = 4000;
		AssetLocation loc = new AssetLocation("shapes/block/wood/mechanics/pulverizer-moving.json");
		Shape shape = Shape.TryGet(capi, loc);
		Vec3f rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY + 90f, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var toggleMesh, rot);
		toggleMesh.CustomFloats = (matrixAndLightFloatsAxle = createCustomFloats(count));
		toggleMeshref = capi.Render.UploadMesh(toggleMesh);
		AssetLocation locPounderL = new AssetLocation("shapes/block/wood/mechanics/pulverizer-pounder-l.json");
		AssetLocation locPounderR = new AssetLocation("shapes/block/wood/mechanics/pulverizer-pounder-r.json");
		Shape shapel = Shape.TryGet(capi, locPounderL);
		Shape shaper = Shape.TryGet(capi, locPounderR);
		texSource = capi.Tesselator.GetTextureSource(textureSoureBlock);
		for (int i = 0; i < metals.Length; i++)
		{
			metal = metals[i];
			matrixAndLightFloatsLPounder[i] = createCustomFloats(count);
			matrixAndLightFloatsRPounder[i] = createCustomFloats(count);
			capi.Tesselator.TesselateShape("pulverizer-pounder-l", shapel, out var lPounderMesh, this, rot, 0, 0, 0);
			capi.Tesselator.TesselateShape("pulverizer-pounder-r", shaper, out var rPounderMesh, this, rot, 0, 0, 0);
			lPounderMesh.CustomFloats = matrixAndLightFloatsLPounder[i];
			rPounderMesh.CustomFloats = matrixAndLightFloatsRPounder[i];
			lPoundMeshrefs[i] = capi.Render.UploadMesh(lPounderMesh);
			rPounderMeshrefs[i] = capi.Render.UploadMesh(rPounderMesh);
		}
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
		BEBehaviorMPPulverizer bhpu = dev as BEBehaviorMPPulverizer;
		float rot = (bhpu.bepu.hasAxle ? dev.AngleRad : 0f);
		float axX = -Math.Abs(dev.AxisSign[0]);
		float axZ = -Math.Abs(dev.AxisSign[2]);
		if (bhpu.bepu.hasAxle)
		{
			float rotX = rot * axX;
			float rotZ = rot * axZ;
			UpdateLightAndTransformMatrix(matrixAndLightFloatsAxle.Values, quantityAxles, distToCamera, dev.LightRgba, rotX, rotZ, axisCenter, 0f);
			quantityAxles++;
		}
		if (bhpu.isRotationReversed())
		{
			rot = 0f - rot;
		}
		int metalIndexLeft = bhpu.bepu.CapMetalIndexL;
		if (bhpu.bepu.hasLPounder && metalIndexLeft >= 0)
		{
			bool leftEmpty = bhpu.bepu.Inventory[1].Empty;
			float progress2 = GetProgress(bhpu.bepu.hasAxle ? (rot - 0.45f + (float)Math.PI / 4f) : 0f, 0f);
			UpdateLightAndTransformMatrix(matrixAndLightFloatsLPounder[metalIndexLeft].Values, quantityLPounders[metalIndexLeft], distToCamera, dev.LightRgba, 0f, 0f, axisCenter, Math.Max(progress2 / 6f + 0.0071f, leftEmpty ? (-1f) : (1f / 32f)));
			if (progress2 < bhpu.prevProgressLeft && progress2 < 0.25f)
			{
				if (bhpu.leftDir == 1)
				{
					bhpu.OnClientSideImpact(right: false);
				}
				bhpu.leftDir = -1;
			}
			else
			{
				bhpu.leftDir = 1;
			}
			bhpu.prevProgressLeft = progress2;
			quantityLPounders[metalIndexLeft]++;
		}
		int metalIndexRight = bhpu.bepu.CapMetalIndexR;
		if (!bhpu.bepu.hasRPounder || metalIndexRight < 0)
		{
			return;
		}
		bool rightEmpty = bhpu.bepu.Inventory[0].Empty;
		float progress = GetProgress(bhpu.bepu.hasAxle ? (rot - 0.45f) : 0f, 0f);
		UpdateLightAndTransformMatrix(matrixAndLightFloatsRPounder[metalIndexRight].Values, quantityRPounders[metalIndexRight], distToCamera, dev.LightRgba, 0f, 0f, axisCenter, Math.Max(progress / 6f + 0.0071f, rightEmpty ? (-1f) : (1f / 32f)));
		if (progress < bhpu.prevProgressRight && progress < 0.25f)
		{
			if (bhpu.rightDir == 1)
			{
				bhpu.OnClientSideImpact(right: true);
			}
			bhpu.rightDir = -1;
		}
		else
		{
			bhpu.rightDir = 1;
		}
		bhpu.prevProgressRight = progress;
		quantityRPounders[metalIndexRight]++;
	}

	private float GetProgress(float rot, float offset)
	{
		float progress = rot % ((float)Math.PI / 2f) / ((float)Math.PI / 2f);
		if (progress < 0f)
		{
			progress += 1f;
		}
		progress = 0.6355f * (float)Math.Atan(2.2f * progress - 1.2f) + 0.5f;
		if (progress > 0.9f)
		{
			progress = 2.7f - 3f * progress;
			progress = 0.9f - progress * progress * 10f;
		}
		if (progress < 0f)
		{
			progress = 0f;
		}
		return progress;
	}

	protected void UpdateLightAndTransformMatrix(float[] values, int index, Vec3f distToCamera, Vec4f lightRgba, float rotX, float rotZ, Vec3f axis, float translate)
	{
		Mat4f.Identity(tmpMat);
		Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y + translate, distToCamera.Z + axis.Z);
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
		quantityAxles = 0;
		for (int j = 0; j < metals.Length; j++)
		{
			quantityLPounders[j] = 0;
			quantityRPounders[j] = 0;
		}
		UpdateCustomFloatBuffer();
		if (quantityAxles > 0)
		{
			matrixAndLightFloatsAxle.Count = quantityAxles * 20;
			updateMesh.CustomFloats = matrixAndLightFloatsAxle;
			capi.Render.UpdateMesh(toggleMeshref, updateMesh);
			capi.Render.RenderMeshInstanced(toggleMeshref, quantityAxles);
		}
		for (int i = 0; i < metals.Length; i++)
		{
			int qLpounder = quantityLPounders[i];
			int qRpounder = quantityRPounders[i];
			if (qLpounder > 0)
			{
				matrixAndLightFloatsLPounder[i].Count = qLpounder * 20;
				updateMesh.CustomFloats = matrixAndLightFloatsLPounder[i];
				capi.Render.UpdateMesh(lPoundMeshrefs[i], updateMesh);
				capi.Render.RenderMeshInstanced(lPoundMeshrefs[i], qLpounder);
			}
			if (qRpounder > 0)
			{
				matrixAndLightFloatsRPounder[i].Count = qRpounder * 20;
				updateMesh.CustomFloats = matrixAndLightFloatsRPounder[i];
				capi.Render.UpdateMesh(rPounderMeshrefs[i], updateMesh);
				capi.Render.RenderMeshInstanced(rPounderMeshrefs[i], qRpounder);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		toggleMeshref?.Dispose();
		for (int i = 0; i < metals.Length; i++)
		{
			lPoundMeshrefs[i]?.Dispose();
			rPounderMeshrefs[i]?.Dispose();
		}
	}
}
