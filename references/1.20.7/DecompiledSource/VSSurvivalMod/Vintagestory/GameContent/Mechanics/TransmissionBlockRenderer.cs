using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class TransmissionBlockRenderer : MechBlockRenderer
{
	private CustomMeshDataPartFloat matrixAndLightFloats1;

	private CustomMeshDataPartFloat matrixAndLightFloats2;

	private MeshRef blockMeshRef1;

	private MeshRef blockMeshRef2;

	public TransmissionBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		MeshData blockMesh2 = null;
		AssetLocation loc = shapeLoc.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = Shape.TryGet(capi, loc);
		Vec3f rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, shape, out var blockMesh1, rot);
		CompositeShape ovShapeCmp = new CompositeShape
		{
			Base = new AssetLocation("shapes/block/wood/mechanics/transmission-rightgear.json")
		};
		rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		Shape ovshape = Shape.TryGet(capi, ovShapeCmp.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
		capi.Tesselator.TesselateShape(textureSoureBlock, ovshape, out blockMesh2, rot);
		blockMesh1.CustomFloats = (matrixAndLightFloats1 = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		blockMesh1.CustomFloats.SetAllocationSize(202000);
		blockMesh2.CustomFloats = (matrixAndLightFloats2 = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		blockMesh2.CustomFloats.SetAllocationSize(202000);
		blockMeshRef1 = capi.Render.UploadMesh(blockMesh1);
		blockMeshRef2 = capi.Render.UploadMesh(blockMesh2);
	}

	protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotation, IMechanicalPowerRenderable dev)
	{
		if (dev is BEBehaviorMPTransmission trans)
		{
			float rot1 = trans.RotationNeighbour(1, allowIndirect: true);
			float num = trans.RotationNeighbour(0, allowIndirect: true);
			UpdateLightAndTransformMatrix(rotX: rot1 * (float)dev.AxisSign[0], rotY: rot1 * (float)dev.AxisSign[1], rotZ: rot1 * (float)dev.AxisSign[2], values: matrixAndLightFloats1.Values, index: index, distToCamera: distToCamera, lightRgba: dev.LightRgba);
			float rotX = num * (float)dev.AxisSign[0];
			float rotY = num * (float)dev.AxisSign[1];
			float rotZ = num * (float)dev.AxisSign[2];
			UpdateLightAndTransformMatrix(matrixAndLightFloats2.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ);
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
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		blockMeshRef1?.Dispose();
		blockMeshRef2?.Dispose();
	}
}
