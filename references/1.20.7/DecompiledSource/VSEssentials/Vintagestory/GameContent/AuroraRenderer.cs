using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AuroraRenderer : IRenderer, IDisposable
{
	private bool renderAurora = true;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private MeshRef quadTilesRef;

	private Matrixf mvMat = new Matrixf();

	private Vec4f col = new Vec4f(1f, 1f, 1f, 1f);

	private float quarterSecAccum;

	public ClimateCondition clientClimateCond;

	private BlockPos plrPos = new BlockPos();

	public double RenderOrder => 0.35;

	public int RenderRange => 9999;

	public AuroraRenderer(ICoreClientAPI capi, WeatherSystemClient wsys)
	{
		this.capi = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "aurora");
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
		renderAurora = capi.Settings.Bool["renderAurora"];
		renderAurora = true;
	}

	public bool LoadShader()
	{
		InitQuads();
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("aurora", prog);
		return prog.Compile();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!renderAurora || prog.LoadError || capi.Render.FrameWidth == 0)
		{
			return;
		}
		Vec3d campos = capi.World.Player.Entity.CameraPos;
		quarterSecAccum += deltaTime;
		if (quarterSecAccum > 0.51f)
		{
			plrPos.X = (int)campos.X;
			plrPos.Y = capi.World.SeaLevel;
			plrPos.Z = (int)campos.Z;
			clientClimateCond = capi.World.BlockAccessor.GetClimateAt(plrPos, EnumGetClimateMode.WorldGenValues);
			quarterSecAccum = 0f;
		}
		if (clientClimateCond != null)
		{
			float tempfac = GameMath.Clamp((Math.Max(0f, 0f - clientClimateCond.Temperature) - 5f) / 15f, 0f, 1f);
			col.W = GameMath.Clamp(1f - 1.5f * capi.World.Calendar.DayLightStrength, 0f, 1f) * tempfac;
			if (!(col.W <= 0f))
			{
				prog.Use();
				prog.Uniform("color", col);
				prog.Uniform("rgbaFogIn", capi.Ambient.BlendedFogColor);
				prog.Uniform("fogMinIn", capi.Ambient.BlendedFogMin);
				prog.Uniform("fogDensityIn", capi.Ambient.BlendedFogDensity);
				prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
				prog.Uniform("flatFogDensity", capi.Ambient.BlendedFlatFogDensity);
				prog.Uniform("flatFogStart", capi.Ambient.BlendedFlatFogYPosForShader - (float)campos.Y);
				float speedmul = capi.World.Calendar.SpeedOfTime / 60f;
				prog.Uniform("auroraCounter", (float)((double)capi.InWorldEllapsedMilliseconds / 5000.0 * (double)speedmul) % 579f);
				mvMat.Set(capi.Render.MvMatrix.Top).FollowPlayer().Translate(0.0, (double)(1.1f * (float)capi.World.BlockAccessor.MapSizeY) + 0.5 - capi.World.Player.Entity.CameraPos.Y, 0.0);
				prog.UniformMatrix("modelViewMatrix", mvMat.Values);
				capi.Render.RenderMesh(quadTilesRef);
				prog.Stop();
			}
		}
	}

	public void InitQuads()
	{
		quadTilesRef?.Dispose();
		float height = 200f;
		MeshData mesh = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		mesh.CustomFloats = new CustomMeshDataPartFloat(4);
		mesh.CustomFloats.InterleaveStride = 4;
		mesh.CustomFloats.InterleaveOffsets = new int[1];
		mesh.CustomFloats.InterleaveSizes = new int[1] { 1 };
		Random rnd = new Random();
		float resolution = 1.5f;
		float spread = 1.5f;
		float parts = 20f * resolution;
		float advance = 1f / resolution;
		for (int i = 0; i < 15; i++)
		{
			Vec3f dir = new Vec3f((float)rnd.NextDouble() * 20f - 10f, (float)rnd.NextDouble() * 5f - 3f, (float)rnd.NextDouble() * 20f - 10f);
			dir.Normalize();
			dir.Mul(advance);
			float x = spread * ((float)rnd.NextDouble() * 800f - 400f);
			float y = spread * ((float)rnd.NextDouble() * 80f - 40f);
			float z = spread * ((float)rnd.NextDouble() * 800f - 400f);
			for (int j = 0; (float)j < parts + 2f; j++)
			{
				float lngx = (float)rnd.NextDouble() * 5f + 20f;
				float lngy = (float)rnd.NextDouble() * 4f + 4f;
				float lngz = (float)rnd.NextDouble() * 5f + 20f;
				x += dir.X * lngx;
				y += dir.Y * lngy;
				z += dir.Z * lngz;
				int lastelement = mesh.VerticesCount;
				mesh.AddVertex(x, y + height, z, j % 2, 1f);
				mesh.AddVertex(x, y, z, j % 2, 0f);
				float f = (float)j / (parts - 1f);
				mesh.CustomFloats.Add(f, f);
				if (j > 0 && (float)j < parts - 1f)
				{
					mesh.AddIndex(lastelement + 1);
					mesh.AddIndex(lastelement + 3);
					mesh.AddIndex(lastelement + 2);
					mesh.AddIndex(lastelement);
					mesh.AddIndex(lastelement + 1);
					mesh.AddIndex(lastelement + 2);
				}
			}
		}
		quadTilesRef = capi.Render.UploadMesh(mesh);
	}

	public void Dispose()
	{
		capi.Render.DeleteMesh(quadTilesRef);
	}
}
