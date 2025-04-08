using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

internal class SystemRenderFrameBufferDebug : ClientSystem
{
	private bool framebufferDebug;

	private MeshRef coloredPlanesRef;

	private MeshRef quadModel;

	private LoadedTexture[] labels;

	public override string Name => "debwt";

	public SystemRenderFrameBufferDebug(ClientMain game)
		: base(game)
	{
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("fbdeb").WithDescription("Toggle Framebuffer/WOIT Debug mode")
			.HandleWith(CmdWoit)
			.EndSubCommand();
		float sc = RuntimeEnv.GUIScale;
		MeshData redQuad = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, sc * 10f, sc * 10f, 220, 0, 0, 191);
		MeshData yellowQuad = QuadMeshUtilExt.GetCustomQuadModelData(sc * 2f, sc * 2f, sc * 2f, sc * 10f, sc * 10f, 220, 220, 0, 191);
		MeshData blueQuad = QuadMeshUtilExt.GetCustomQuadModelData(sc * 4f, sc * 4f, sc * 4f, sc * 10f, sc * 10f, 0, 0, 220, 191);
		MeshData quads = new MeshData(12, 12);
		quads.AddMeshData(redQuad);
		quads.AddMeshData(yellowQuad);
		quads.AddMeshData(blueQuad);
		quads.Uv = null;
		coloredPlanesRef = game.Platform.UploadMesh(quads);
		quadModel = game.Platform.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
		game.eventManager.RegisterRenderer(OnRenderFrame3DTransparent, EnumRenderStage.OIT, "debwt-oit", 0.2);
		game.eventManager.RegisterRenderer(OnRenderFrame2DOverlay, EnumRenderStage.Ortho, "debwt-ortho", 0.2);
		CairoFont font = CairoFont.WhiteDetailText();
		TextBackground bg = new TextBackground
		{
			FillColor = new double[4] { 0.2, 0.2, 0.2, 0.3 },
			Padding = (int)(sc * 2f)
		};
		labels = new LoadedTexture[14]
		{
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Shadow Map Far", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("WOIT Accum", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("WOIT Reveal", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Findbright (A.Bloom)", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Color", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Depth", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Depthlinear", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Luma", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Glow (red=bloom,green=godray)", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Shadow Map Near", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB GNormal", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB GPosition", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("SSAO", font, bg),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Liquid depth", font, bg)
		};
	}

	public void OnRenderFrame3DTransparent(float deltaTime)
	{
		if (framebufferDebug)
		{
			game.Platform.GlDisableDepthTest();
			ShaderProgramWoittest woittest = ShaderPrograms.Woittest;
			woittest.Use();
			woittest.ProjectionMatrix = game.CurrentProjectionMatrix;
			game.GlMatrixModeModelView();
			game.GlPushMatrix();
			game.GlTranslate(5000.0, 120.0, 5000.0);
			woittest.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(coloredPlanesRef);
			game.GlPopMatrix();
			woittest.Stop();
			game.Platform.GlEnableDepthTest();
		}
	}

	public void OnRenderFrame2DOverlay(float deltaTime)
	{
		if (framebufferDebug)
		{
			game.Platform.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
			float sc = RuntimeEnv.GUIScale;
			FrameBufferRef fb = game.Platform.FrameBuffers[1];
			game.Render2DTextureFlipped(fb.ColorTextureIds[0], sc * 10f, sc * 10f, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[1], sc * 10f, sc * 10f);
			game.Render2DTextureFlipped(fb.ColorTextureIds[1], sc * 10f, sc * 160f, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[2], sc * 10f, sc * 160f);
			fb = game.Platform.FrameBuffers[10];
			game.Render2DTextureFlipped(fb.ColorTextureIds[0], sc * 10f, sc * 310f, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[7], sc * 10f, sc * 310f);
			fb = game.Platform.FrameBuffers[4];
			game.Render2DTextureFlipped(fb.ColorTextureIds[0], sc * 10f, sc * 460f, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[3], sc * 10f, sc * 460f);
			fb = game.Platform.FrameBuffers[0];
			game.Render2DTextureFlipped(fb.ColorTextureIds[1], sc * 10f, sc * 610f, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[8], sc * 10f, sc * 610f);
			fb = game.Platform.FrameBuffers[0];
			int y = 10;
			game.Render2DTextureFlipped(fb.ColorTextureIds[0], (float)game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[4], (float)game.Width - sc * 160f, sc * (float)y);
			y += 155;
			if (ClientSettings.SSAOQuality > 0)
			{
				game.Render2DTextureFlipped(game.Platform.FrameBuffers[13].ColorTextureIds[0], (float)game.Width - sc * 320f, sc * 10f, sc * 150f, sc * 150f);
				game.Render2DLoadedTexture(labels[12], (float)game.Width - sc * 320f, sc * 10f);
				game.Render2DTextureFlipped(fb.ColorTextureIds[2], (float)game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f);
				game.Render2DLoadedTexture(labels[10], (float)game.Width - sc * 160f, sc * (float)y);
				y += 155;
				game.Render2DTextureFlipped(fb.ColorTextureIds[3], (float)game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f);
				game.Render2DLoadedTexture(labels[11], (float)game.Width - sc * 160f, sc * (float)y);
				y += 155;
			}
			game.Render2DTextureFlipped(fb.DepthTextureId, (float)game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f);
			game.Render2DLoadedTexture(labels[5], (float)game.Width - sc * 160f, sc * (float)y);
			y += 155;
			game.guiShaderProg.Stop();
			ShaderProgramDebugdepthbuffer prog = ShaderPrograms.Debugdepthbuffer;
			prog.Use();
			prog.DepthSampler2D = fb.DepthTextureId;
			game.GlPushMatrix();
			game.GlTranslate((float)game.Width - sc * 160f, sc * (float)y, sc * 50f);
			game.GlScale(sc * 150f, sc * 150f, 0.0);
			game.GlScale(0.5, 0.5, 0.0);
			game.GlTranslate(1.0, 1.0, 0.0);
			game.GlRotate(180f, 1.0, 0.0, 0.0);
			prog.ProjectionMatrix = game.CurrentProjectionMatrix;
			prog.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(quadModel);
			game.GlPopMatrix();
			int shadowMapQuality = ClientSettings.ShadowMapQuality;
			if (shadowMapQuality > 0)
			{
				fb = game.Platform.FrameBuffers[11];
				prog.DepthSampler2D = fb.DepthTextureId;
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
				game.GlPushMatrix();
				game.GlTranslate(sc * 170f, sc * 10f, sc * 50f);
				game.GlScale(sc * 300f, sc * 300f, 0.0);
				game.GlScale(0.5, 0.5, 0.0);
				game.GlTranslate(1.0, 1.0, 0.0);
				game.GlRotate(180f, 1.0, 0.0, 0.0);
				prog.ProjectionMatrix = game.CurrentProjectionMatrix;
				prog.ModelViewMatrix = game.CurrentModelViewMatrix;
				game.Platform.RenderMesh(quadModel);
				game.GlPopMatrix();
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
			}
			if (shadowMapQuality > 1)
			{
				fb = game.Platform.FrameBuffers[12];
				prog.DepthSampler2D = fb.DepthTextureId;
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
				game.GlPushMatrix();
				game.GlTranslate(sc * 170f, sc * 320f, sc * 50f);
				game.GlScale(sc * 300f, sc * 300f, 0.0);
				game.GlScale(0.5, 0.5, 0.0);
				game.GlTranslate(1.0, 1.0, 0.0);
				game.GlRotate(180f, 1.0, 0.0, 0.0);
				prog.ProjectionMatrix = game.CurrentProjectionMatrix;
				prog.ModelViewMatrix = game.CurrentModelViewMatrix;
				game.Platform.RenderMesh(quadModel);
				game.GlPopMatrix();
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
			}
			fb = game.Platform.FrameBuffers[5];
			prog.DepthSampler2D = fb.DepthTextureId;
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
			game.GlPushMatrix();
			game.GlTranslate(sc * 170f, sc * 630f, sc * 50f);
			game.GlScale(sc * 300f, sc * 300f, 0.0);
			game.GlScale(0.5, 0.5, 0.0);
			game.GlTranslate(1.0, 1.0, 0.0);
			game.GlRotate(180f, 1.0, 0.0, 0.0);
			prog.ProjectionMatrix = game.CurrentProjectionMatrix;
			prog.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(quadModel);
			game.GlPopMatrix();
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
			prog.Stop();
			game.guiShaderProg.Use();
			game.Platform.GlDisableDepthTest();
			game.Render2DLoadedTexture(labels[13], sc * 170f, sc * 630f);
			if (shadowMapQuality > 0)
			{
				game.Render2DLoadedTexture(labels[0], sc * 170f, sc * 10f);
			}
			if (shadowMapQuality > 1)
			{
				game.Render2DLoadedTexture(labels[9], sc * 170f, sc * 320f);
			}
			game.Platform.GlEnableDepthTest();
			game.Render2DLoadedTexture(labels[6], (float)game.Width - sc * 170f, sc * (float)y);
			game.Platform.GlToggleBlend(on: true);
		}
	}

	private TextCommandResult CmdWoit(TextCommandCallingArgs textCommandCallingArgs)
	{
		framebufferDebug = !framebufferDebug;
		return TextCommandResult.Success();
	}

	public override void Dispose(ClientMain game)
	{
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i]?.Dispose();
		}
		quadModel?.Dispose();
		coloredPlanesRef?.Dispose();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
