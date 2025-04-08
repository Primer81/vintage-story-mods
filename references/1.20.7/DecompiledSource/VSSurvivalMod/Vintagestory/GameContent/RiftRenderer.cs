using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class RiftRenderer : IRenderer, IDisposable
{
	private MeshRef meshref;

	private Matrixf matrixf;

	private float counter;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	public Dictionary<int, Rift> rifts;

	private ModSystemRifts modsys;

	private int cnt;

	public double RenderOrder => 0.05;

	public int RenderRange => 100;

	public RiftRenderer(ICoreClientAPI capi, Dictionary<int, Rift> rifts)
	{
		this.capi = capi;
		this.rifts = rifts;
		capi.Event.RegisterRenderer(this, EnumRenderStage.AfterBlit, "riftrenderer");
		MeshData mesh = QuadMeshUtil.GetQuad();
		meshref = capi.Render.UploadMesh(mesh);
		matrixf = new Matrixf();
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
		modsys = capi.ModLoader.GetModSystem<ModSystemRifts>();
	}

	public bool LoadShader()
	{
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("rift", prog);
		return prog.Compile();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		EntityPos plrPos = capi.World.Player.Entity.Pos;
		EntityBehaviorTemporalStabilityAffected bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
		if (bh != null)
		{
			bh.stabilityOffset = 0.0;
		}
		if (modsys.nearestRifts.Length != 0)
		{
			Rift rift2 = modsys.nearestRifts[0];
			float dist = Math.Max(0f, GameMath.Sqrt(plrPos.SquareDistanceTo(rift2.Position)) - 1f - rift2.Size / 2f);
			float f = Math.Max(0f, 1f - dist / 3f);
			GlobalConstants.GuiGearRotJitter = ((capi.World.Rand.NextDouble() < 0.25) ? (f * ((float)capi.World.Rand.NextDouble() - 0.5f) / 1f) : 0f);
			capi.ModLoader.GetModSystem<SystemTemporalStability>().modGlitchStrength = Math.Min(1f, f * 1.3f);
			if (bh != null)
			{
				bh.stabilityOffset = (0.0 - Math.Pow(Math.Max(0f, 1f - dist / 3f), 2.0)) * 20.0;
			}
		}
		else
		{
			capi.ModLoader.GetModSystem<SystemTemporalStability>().modGlitchStrength = 0f;
		}
		counter += deltaTime;
		if (capi.World.Rand.NextDouble() < 0.012)
		{
			counter += 20f * (float)capi.World.Rand.NextDouble();
		}
		capi.Render.GLDepthMask(on: false);
		prog.Use();
		prog.Uniform("rgbaFogIn", capi.Render.FogColor);
		prog.Uniform("fogMinIn", capi.Render.FogMin);
		prog.Uniform("fogDensityIn", capi.Render.FogDensity);
		prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
		prog.Uniform("rgbaLightIn", new Vec4f(1f, 1f, 1f, 1f));
		prog.BindTexture2D("primaryFb", capi.Render.FrameBuffers[0].ColorTextureIds[0], 0);
		prog.BindTexture2D("depthTex", capi.Render.FrameBuffers[0].DepthTextureId, 1);
		prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		int width = capi.Render.FrameWidth;
		int height = capi.Render.FrameHeight;
		prog.Uniform("counter", counter);
		float bf = 200f + (float)GameMath.Sin((double)capi.InWorldEllapsedMilliseconds / 24000.0) * 100f;
		prog.Uniform("counterSmooth", bf);
		prog.Uniform("invFrameSize", new Vec2f(1f / (float)width, 1f / (float)height));
		int riftIndex = 0;
		cnt = (cnt + 1) % 3;
		foreach (Rift rift in rifts.Values)
		{
			if (cnt == 0)
			{
				rift.Visible = capi.World.BlockAccessor.GetChunkAtBlockPos((int)rift.Position.X, (int)rift.Position.Y, (int)rift.Position.Z) != null;
			}
			riftIndex++;
			matrixf.Identity();
			float dx = (float)(rift.Position.X - plrPos.X);
			float dy = (float)(rift.Position.Y - plrPos.Y);
			float dz = (float)(rift.Position.Z - plrPos.Z);
			matrixf.Translate(dx, dy, dz);
			matrixf.ReverseMul(capi.Render.CameraMatrixOriginf);
			matrixf.Values[0] = 1f;
			matrixf.Values[1] = 0f;
			matrixf.Values[2] = 0f;
			matrixf.Values[8] = 0f;
			matrixf.Values[9] = 0f;
			matrixf.Values[10] = 1f;
			float size = rift.GetNowSize(capi);
			matrixf.Scale(size, size, size);
			prog.UniformMatrix("modelViewMatrix", matrixf.Values);
			prog.Uniform("worldPos", new Vec4f(dx, dy, dz, 0f));
			prog.Uniform("riftIndex", riftIndex);
			capi.Render.RenderMesh(meshref);
			if (dx * dx + dy * dy + dz * dz < 1600f)
			{
				Vec3d ppos = rift.Position;
				capi.World.SpawnParticles(0.1f, ColorUtil.ColorFromRgba(10, 35, 58, 128), ppos, ppos, new Vec3f(-0.125f, -0.125f, -0.125f), new Vec3f(0.125f, 0.125f, 0.125f), 5f, 0f, (0.0625f + (float)capi.World.Rand.NextDouble() * 0.25f) / 2f);
			}
		}
		counter = GameMath.Mod(counter + deltaTime, (float)Math.PI * 200f);
		prog.Stop();
		capi.Render.GLDepthMask(on: true);
	}

	public void Dispose()
	{
		meshref?.Dispose();
	}
}
