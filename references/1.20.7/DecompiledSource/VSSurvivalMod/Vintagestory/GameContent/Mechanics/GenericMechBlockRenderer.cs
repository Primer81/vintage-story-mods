using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class GenericMechBlockRenderer : MechBlockRenderer
{
	private CustomMeshDataPartFloat matrixAndLightFloats;

	private MeshRef blockMeshRef;

	public GenericMechBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		AssetLocation loc = shapeLoc.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = Shape.TryGet(capi, loc);
		Vec3f rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var blockMesh, rot);
		if (shapeLoc.Overlays != null)
		{
			for (int i = 0; i < shapeLoc.Overlays.Length; i++)
			{
				CompositeShape ovShapeCmp = shapeLoc.Overlays[i];
				rot = new Vec3f(ovShapeCmp.rotateX, ovShapeCmp.rotateY, ovShapeCmp.rotateZ);
				Shape ovshape = Shape.TryGet(capi, ovShapeCmp.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
				capi.Tesselator.TesselateShape(textureSoureBlock, ovshape, out var overlayMesh, rot);
				blockMesh.AddMeshData(overlayMesh);
			}
		}
		blockMesh.CustomFloats = (matrixAndLightFloats = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		blockMesh.CustomFloats.SetAllocationSize(202000);
		blockMeshRef = capi.Render.UploadMesh(blockMesh);
	}

	protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotation, IMechanicalPowerRenderable dev)
	{
		float rotX = rotation * (float)dev.AxisSign[0];
		float rotY = rotation * (float)dev.AxisSign[1];
		float rotZ = rotation * (float)dev.AxisSign[2];
		if (dev is BEBehaviorMPToggle tog && ((rotX == 0f) ^ tog.isRotationReversed()))
		{
			rotY = (float)Math.PI;
			rotZ = 0f - rotZ;
		}
		UpdateLightAndTransformMatrix(matrixAndLightFloats.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ);
	}

	public override void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
		if (quantityBlocks > 0)
		{
			matrixAndLightFloats.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = matrixAndLightFloats;
			capi.Render.UpdateMesh(blockMeshRef, updateMesh);
			capi.Render.RenderMeshInstanced(blockMeshRef, quantityBlocks);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		blockMeshRef?.Dispose();
	}
}
