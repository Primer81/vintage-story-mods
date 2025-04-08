using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class AngledGearsBlockRenderer : MechBlockRenderer
{
	private MeshRef gearboxCage;

	private MeshRef gearboxPeg;

	private CustomMeshDataPartFloat floatsPeg;

	private CustomMeshDataPartFloat floatsCage;

	public AngledGearsBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc)
		: base(capi, mechanicalPowerMod)
	{
		Vec3f rot = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
		capi.Tesselator.TesselateShape(textureSoureBlock, Shape.TryGet(capi, "shapes/block/wood/mechanics/angledgearbox-cage.json"), out var gearboxCageMesh, rot);
		capi.Tesselator.TesselateShape(textureSoureBlock, Shape.TryGet(capi, "shapes/block/wood/mechanics/angledgearbox-peg.json"), out var gearboxPegMesh, rot);
		gearboxPegMesh.CustomFloats = (floatsPeg = new CustomMeshDataPartFloat(202000)
		{
			Instanced = true,
			InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
			InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
			InterleaveStride = 80,
			StaticDraw = false
		});
		gearboxPegMesh.CustomFloats.SetAllocationSize(202000);
		gearboxCageMesh.CustomFloats = (floatsCage = floatsPeg.Clone());
		gearboxPeg = capi.Render.UploadMesh(gearboxPegMesh);
		gearboxCage = capi.Render.UploadMesh(gearboxCageMesh);
	}

	protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotationRad, IMechanicalPowerRenderable dev)
	{
		if (dev is BEBehaviorMPAngledGears gear)
		{
			BlockFacing inTurn = gear.GetPropagationDirection();
			if (inTurn == gear.axis1 || inTurn == gear.axis2)
			{
				rotationRad = 0f - rotationRad;
			}
		}
		float rotX = rotationRad * (float)dev.AxisSign[0];
		float rotY = rotationRad * (float)dev.AxisSign[1];
		float rotZ = rotationRad * (float)dev.AxisSign[2];
		UpdateLightAndTransformMatrix(floatsPeg.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ);
		if (dev.AxisSign.Length >= 4)
		{
			rotX = rotationRad * (float)dev.AxisSign[3];
			rotY = rotationRad * (float)dev.AxisSign[4];
			rotZ = rotationRad * (float)dev.AxisSign[5];
			UpdateLightAndTransformMatrix(floatsCage.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ);
		}
	}

	public override void OnRenderFrame(float deltaTime, IShaderProgram prog)
	{
		UpdateCustomFloatBuffer();
		if (quantityBlocks > 0)
		{
			floatsPeg.Count = quantityBlocks * 20;
			floatsCage.Count = quantityBlocks * 20;
			updateMesh.CustomFloats = floatsPeg;
			capi.Render.UpdateMesh(gearboxPeg, updateMesh);
			updateMesh.CustomFloats = floatsCage;
			capi.Render.UpdateMesh(gearboxCage, updateMesh);
			capi.Render.RenderMeshInstanced(gearboxPeg, quantityBlocks);
			capi.Render.RenderMeshInstanced(gearboxCage, quantityBlocks);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		gearboxCage?.Dispose();
		gearboxPeg?.Dispose();
	}
}
