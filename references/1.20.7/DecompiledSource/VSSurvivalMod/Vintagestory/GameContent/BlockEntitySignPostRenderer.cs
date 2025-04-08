using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntitySignPostRenderer : IRenderer, IDisposable
{
	protected static int TextWidth = 200;

	protected static int TextHeight = 25;

	protected static float QuadWidth = 0.7f;

	protected static float QuadHeight = 0.1f;

	protected CairoFont font;

	protected BlockPos pos;

	protected ICoreClientAPI api;

	protected LoadedTexture loadedTexture;

	protected MeshRef quadModelRef;

	public Matrixf ModelMat = new Matrixf();

	protected float rotY;

	protected float translateX;

	protected float translateY = 0.5625f;

	protected float translateZ;

	private string[] textByCardinal;

	private double fontSize;

	public double RenderOrder => 0.5;

	public int RenderRange => 48;

	public BlockEntitySignPostRenderer(BlockPos pos, ICoreClientAPI api, CairoFont font)
	{
		this.api = api;
		this.pos = pos;
		this.font = font;
		fontSize = font.UnscaledFontsize;
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "signpost");
	}

	private void genMesh()
	{
		MeshData allMeshes = new MeshData(4, 6);
		int qsigns = 0;
		for (int j = 0; j < 8; j++)
		{
			if (textByCardinal[j].Length != 0)
			{
				qsigns++;
			}
		}
		if (qsigns == 0)
		{
			quadModelRef?.Dispose();
			quadModelRef = null;
			return;
		}
		int snum = 0;
		for (int i = 0; i < 8; i++)
		{
			if (textByCardinal[i].Length != 0)
			{
				Cardinal dir = Cardinal.ALL[i];
				MeshData modeldata = QuadMeshUtil.GetQuad();
				float vStart = (float)snum / (float)qsigns;
				float vEnd = (float)(snum + 1) / (float)qsigns;
				snum++;
				modeldata.Uv = new float[8] { 1f, vEnd, 0f, vEnd, 0f, vStart, 1f, vStart };
				modeldata.Rgba = new byte[16];
				modeldata.Rgba.Fill(byte.MaxValue);
				Vec3f orig = new Vec3f(0.5f, 0.5f, 0.5f);
				switch (dir.Index)
				{
				case 0:
					rotY = 90f;
					break;
				case 1:
					rotY = 45f;
					break;
				case 2:
					rotY = 0f;
					break;
				case 3:
					rotY = 315f;
					break;
				case 4:
					rotY = 270f;
					break;
				case 5:
					rotY = 225f;
					break;
				case 6:
					rotY = 180f;
					break;
				case 7:
					rotY = 135f;
					break;
				}
				modeldata.Translate(1.6f, 0f, 0.375f);
				MeshData front = modeldata.Clone();
				front.Scale(orig, 0.5f * QuadWidth, 0.4f * QuadHeight, 0.5f * QuadWidth);
				front.Rotate(orig, 0f, rotY * ((float)Math.PI / 180f), 0f);
				front.Translate(0f, 1.39f, 0f);
				allMeshes.AddMeshData(front);
				MeshData back = modeldata;
				back.Uv = new float[8] { 0f, vEnd, 1f, vEnd, 1f, vStart, 0f, vStart };
				back.Translate(0f, 0f, 0.26f);
				back.Scale(orig, 0.5f * QuadWidth, 0.4f * QuadHeight, 0.5f * QuadWidth);
				back.Rotate(orig, 0f, rotY * ((float)Math.PI / 180f), 0f);
				back.Translate(0f, 1.39f, 0f);
				allMeshes.AddMeshData(back);
			}
		}
		quadModelRef?.Dispose();
		quadModelRef = api.Render.UploadMesh(allMeshes);
	}

	public virtual void SetNewText(string[] textByCardinal, int color)
	{
		this.textByCardinal = textByCardinal;
		font.WithColor(ColorUtil.ToRGBADoubles(color));
		font.UnscaledFontsize = fontSize / (double)RuntimeEnv.GUIScale;
		int lines = 0;
		for (int i = 0; i < textByCardinal.Length; i++)
		{
			if (textByCardinal[i].Length > 0)
			{
				lines++;
			}
		}
		if (lines == 0)
		{
			loadedTexture?.Dispose();
			loadedTexture = null;
			return;
		}
		ImageSurface surface = new ImageSurface(Format.Argb32, TextWidth, TextHeight * lines);
		Context ctx = new Context(surface);
		font.SetupContext(ctx);
		int line = 0;
		for (int j = 0; j < textByCardinal.Length; j++)
		{
			if (textByCardinal[j].Length > 0)
			{
				double linewidth = font.GetTextExtents(textByCardinal[j]).Width;
				ctx.MoveTo(((double)TextWidth - linewidth) / 2.0, (double)(line * TextHeight) + ctx.FontExtents.Ascent);
				ctx.ShowText(textByCardinal[j]);
				line++;
			}
		}
		if (loadedTexture == null)
		{
			loadedTexture = new LoadedTexture(api);
		}
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag: true, ref loadedTexture);
		surface.Dispose();
		ctx.Dispose();
		genMesh();
	}

	public virtual void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (loadedTexture != null)
		{
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.GlDisableCullFace();
			rpi.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
			IStandardShaderProgram standardShaderProgram = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			standardShaderProgram.Tex2D = loadedTexture.TextureId;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Values;
			standardShaderProgram.ViewMatrix = rpi.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			standardShaderProgram.NormalShaded = 0;
			standardShaderProgram.ExtraGodray = 0f;
			standardShaderProgram.SsaoAttn = 0f;
			standardShaderProgram.AlphaTest = 0.05f;
			standardShaderProgram.OverlayOpacity = 0f;
			rpi.RenderMesh(quadModelRef);
			standardShaderProgram.Stop();
			rpi.GlToggleBlend(blend: true);
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		loadedTexture?.Dispose();
		quadModelRef?.Dispose();
	}
}
