using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GearRenderer : IRenderer, IDisposable
{
	private MeshRef gearMeshref;

	private Matrixf matrixf;

	private float counter;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private List<MachineGear> mgears = new List<MachineGear>();

	private AnimationUtil tripodAnim;

	private Vec3d tripodPos = new Vec3d();

	private double tripodAccum;

	private LoadedTexture rustTexture;

	private EntityBehaviorTemporalStabilityAffected bh;

	private float raiseyRelGears;

	private float raiseyRelTripod;

	public double RenderOrder => 1.0;

	public int RenderRange => 100;

	public GearRenderer(ICoreClientAPI capi)
	{
		this.capi = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "machinegearrenderer");
		matrixf = new Matrixf();
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
	}

	public void Init()
	{
		Shape shape = Shape.TryGet(capi, "shapes/block/machine/machinegear2.json");
		Block block = capi.World.GetBlock(new AssetLocation("platepile"));
		if (block != null)
		{
			capi.Tesselator.TesselateShape(block, shape, out var mesh);
			gearMeshref = capi.Render.UploadMesh(mesh);
			genGears();
			rustTexture = new LoadedTexture(capi);
			AssetLocation loc = new AssetLocation("textures/block/metal/tarnished/rust.png");
			capi.Render.GetOrLoadTexture(loc, ref rustTexture);
			shape = Shape.TryGet(capi, "shapes/entity/supermech/thunderlord.json");
			tripodAnim = new AnimationUtil(capi, tripodPos);
			tripodAnim.InitializeShapeAndAnimator("tripod", shape, capi.Tesselator.GetTextureSource(block), null, out var _);
			tripodAnim.StartAnimation(new AnimationMetaData
			{
				Animation = "walk",
				Code = "walk",
				BlendMode = EnumAnimationBlendMode.Average,
				AnimationSpeed = 0.1f
			});
			tripodAnim.renderer.ScaleX = 30f;
			tripodAnim.renderer.ScaleY = 30f;
			tripodAnim.renderer.ScaleZ = 30f;
			tripodAnim.renderer.FogAffectedness = 0.15f;
			tripodAnim.renderer.LightAffected = false;
			tripodAnim.renderer.StabilityAffected = false;
			tripodAnim.renderer.ShouldRender = true;
			bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
		}
	}

	private void genGears()
	{
		Random rnd = capi.World.Rand;
		mgears.Clear();
		double angle = rnd.NextDouble() * 6.2831854820251465;
		int cnt = 6;
		float angleStep = (float)Math.PI * 2f / (float)cnt;
		for (int i = 0; i < cnt; i++)
		{
			double dist = 150.0 + rnd.NextDouble() * 300.0;
			dist *= 5.0;
			angle += (double)angleStep + rnd.NextDouble() * (double)angleStep * 0.1 - (double)angleStep * 0.05;
			float size = 20f + (float)rnd.NextDouble() * 30f;
			size *= 15f;
			MachineGear mg = new MachineGear
			{
				Position = new Vec3d(GameMath.Sin(angle) * dist, size / 2f, GameMath.Cos(angle) * dist),
				Rot = new Vec3d(0.0, rnd.NextDouble() * 6.2831854820251465, rnd.NextDouble() - 0.5),
				Velocity = (float)rnd.NextDouble() * 0.2f,
				Size = size
			};
			mgears.Add(mg);
		}
	}

	public bool LoadShader()
	{
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("machinegear", prog);
		return prog.Compile();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (tripodAnim != null)
		{
			if (capi.IsGamePaused)
			{
				deltaTime = 0f;
			}
			float targetRaiseyRel = 0f;
			if (bh != null)
			{
				targetRaiseyRel = GameMath.Clamp((float)bh.GlichEffectStrength * 5f - 3f, 0f, 1f);
			}
			raiseyRelGears += (targetRaiseyRel - raiseyRelGears) * deltaTime;
			capi.Render.GlToggleBlend(blend: true);
			if (raiseyRelGears >= 0.01f)
			{
				renderGears(deltaTime);
			}
			if (!capi.IsGamePaused)
			{
				tripodAnim.renderer.ShouldRender = raiseyRelTripod > 0.01f;
				updateSuperMechState(deltaTime, stage);
			}
			capi.Render.GlToggleBlend(blend: false);
		}
	}

	private void updateSuperMechState(float deltaTime, EnumRenderStage stage)
	{
		float targetRaiseyRel = 0f;
		if (bh != null)
		{
			targetRaiseyRel = GameMath.Clamp((float)bh.GlichEffectStrength * 5f - 1.75f, 0f, 1f);
		}
		raiseyRelTripod += (targetRaiseyRel - raiseyRelTripod) * deltaTime / 3f;
		EntityPos plrPos = capi.World.Player.Entity.Pos;
		tripodAccum += (double)deltaTime / 50.0 * (double)(0.33f + raiseyRelTripod) * 1.2000000476837158;
		tripodAccum %= 500000.0;
		float d = (1f - raiseyRelTripod) * 900f;
		tripodPos.X = plrPos.X + Math.Sin(tripodAccum) * (300.0 + (double)d);
		tripodPos.Y = capi.World.SeaLevel;
		tripodPos.Z = plrPos.Z + Math.Cos(tripodAccum) * (300.0 + (double)d);
		tripodAnim.renderer.rotationDeg.Y = (float)(tripodAccum % 6.2831854820251465 + 3.1415927410125732) * (180f / (float)Math.PI);
		tripodAnim.renderer.renderColor.Set(0.5f, 0.5f, 0.5f, Math.Min(1f, raiseyRelTripod * 2f));
		tripodAnim.renderer.FogAffectedness = 1f - GameMath.Clamp(raiseyRelGears * 2.2f - 0.5f, 0f, 0.9f);
		tripodAnim.OnRenderFrame(deltaTime, stage);
	}

	private void renderGears(float deltaTime)
	{
		prog.Use();
		prog.Uniform("rgbaFogIn", capi.Render.FogColor);
		prog.Uniform("fogMinIn", capi.Render.FogMin);
		prog.Uniform("fogDensityIn", capi.Render.FogDensity);
		prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
		prog.Uniform("rgbaLightIn", new Vec4f(1f, 1f, 1f, 1f));
		prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		prog.Uniform("counter", counter);
		int riftIndex = 0;
		EntityPos plrPos = capi.World.Player.Entity.Pos;
		foreach (MachineGear gear in mgears)
		{
			riftIndex++;
			matrixf.Identity();
			gear.Position.Y = Math.Max(gear.Position.Y, capi.World.BlockAccessor.GetTerrainMapheightAt(new BlockPos((int)(gear.Position.X + plrPos.X), 0, (int)(gear.Position.Z + plrPos.Z))));
			float dx = (float)gear.Position.X;
			float dy = (float)(gear.Position.Y - plrPos.Y - (double)((1f - raiseyRelGears) * gear.Size * 1.5f));
			float dz = (float)gear.Position.Z;
			GameMath.Sqrt(dx * dx + dz * dz);
			matrixf.Mul(capi.Render.CameraMatrixOriginf);
			matrixf.Translate(dx, dy, dz);
			matrixf.RotateY((float)gear.Rot.Y);
			matrixf.RotateX((float)gear.Rot.Z + (float)Math.PI / 2f);
			float size = gear.Size;
			matrixf.Scale(size, size, size);
			matrixf.Translate(0.5f, 0.5f, 0.5f);
			matrixf.RotateY(counter * gear.Velocity);
			matrixf.Translate(-0.5f, -0.5f, -0.5f);
			prog.Uniform("alpha", 1f);
			prog.UniformMatrix("modelViewMatrix", matrixf.Values);
			prog.Uniform("worldPos", new Vec4f(dx, dy, dz, 0f));
			prog.Uniform("riftIndex", riftIndex);
			capi.Render.RenderMesh(gearMeshref);
		}
		counter = GameMath.Mod(counter + deltaTime, (float)Math.PI * 200f);
		prog.Stop();
	}

	public void Dispose()
	{
		gearMeshref?.Dispose();
	}
}
