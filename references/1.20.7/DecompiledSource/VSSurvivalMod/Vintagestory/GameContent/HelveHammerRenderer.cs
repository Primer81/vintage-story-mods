using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class HelveHammerRenderer : IRenderer, IDisposable
{
	internal bool ShouldRender;

	internal bool ShouldRotateManual;

	internal bool ShouldRotateAutomated;

	private BEHelveHammer be;

	private ICoreClientAPI api;

	private BlockPos pos;

	private MultiTextureMeshRef meshref;

	public Matrixf ModelMat = new Matrixf();

	public float AngleRad;

	internal bool Obstructed;

	private Matrixf shadowMvpMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public HelveHammerRenderer(ICoreClientAPI coreClientAPI, BEHelveHammer be, BlockPos pos, MeshData mesh)
	{
		api = coreClientAPI;
		this.pos = pos;
		this.be = be;
		meshref = coreClientAPI.Render.UploadMultiTextureMesh(mesh);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (meshref != null && be.HammerStack != null)
		{
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.GlDisableCullFace();
			float rotY = be.facing.HorizontalAngleIndex * 90;
			float offx = ((be.facing == BlockFacing.NORTH || be.facing == BlockFacing.WEST) ? (-0.0625f) : 1.0625f);
			ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).RotateYDeg(rotY)
				.Translate(offx, 25f / 32f, 0.5f)
				.RotateZ(AngleRad)
				.Translate(0f - offx, -25f / 32f, -0.5f)
				.RotateYDeg(0f - rotY);
			if (stage == EnumRenderStage.Opaque)
			{
				IStandardShaderProgram standardShaderProgram = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
				standardShaderProgram.ModelMatrix = ModelMat.Values;
				standardShaderProgram.ViewMatrix = rpi.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = rpi.CurrentProjectionMatrix;
				rpi.RenderMultiTextureMesh(meshref, "tex");
				standardShaderProgram.Stop();
				AngleRad = be.Angle;
			}
			else
			{
				IRenderAPI rapi = api.Render;
				shadowMvpMat.Set(rapi.CurrentProjectionMatrix).Mul(rapi.CurrentModelviewMatrix).Mul(ModelMat.Values);
				rapi.CurrentActiveShader.UniformMatrix("mvpMatrix", shadowMvpMat.Values);
				rapi.CurrentActiveShader.Uniform("origin", new Vec3f());
				rpi.RenderMultiTextureMesh(meshref, "tex2d");
			}
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		api.Event.UnregisterRenderer(this, EnumRenderStage.ShadowFar);
		api.Event.UnregisterRenderer(this, EnumRenderStage.ShadowNear);
		meshref.Dispose();
	}
}
