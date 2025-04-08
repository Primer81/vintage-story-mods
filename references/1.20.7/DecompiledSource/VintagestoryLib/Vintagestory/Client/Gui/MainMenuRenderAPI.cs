using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client.Gui;

public class MainMenuRenderAPI : RenderAPIBase
{
	private float[] mvMatrix;

	public float[] pMatrix;

	private MeshRef quadModel;

	private ScreenManager screenManager;

	public override ICoreClientAPI Api => screenManager.api;

	public MainMenuRenderAPI(ScreenManager screenManager)
		: base(screenManager.GamePlatform)
	{
		this.screenManager = screenManager;
		mvMatrix = Mat4f.Create();
		pMatrix = Mat4f.Create();
	}

	public override void GlTranslate(double x, double y, double z)
	{
		Mat4f.Translate(mvMatrix, mvMatrix, new float[3]
		{
			(float)x,
			(float)y,
			(float)z
		});
	}

	public override void GlTranslate(float x, float y, float z)
	{
		Mat4f.Translate(mvMatrix, mvMatrix, new float[3] { x, y, z });
	}

	public override void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		Render2DTexture(textureid, (float)bounds.renderX, (float)bounds.renderY, (float)bounds.OuterWidth, (float)bounds.OuterHeight, z, color);
	}

	public override void Render2DTexturePremultipliedAlpha(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
	{
		plat.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		Render2DTexture(textureid, posX, posY, width, height, z, color);
		plat.GlToggleBlend(on: true);
	}

	public override void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		plat.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		Render2DTexture(textureid, (int)bounds.renderX, (int)bounds.renderY, (int)bounds.OuterWidth, (int)bounds.OuterHeight, z, color);
		plat.GlToggleBlend(on: true);
	}

	public override void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		Render2DTexture(textureid, (float)posX, (float)posY, (float)width, (float)height, z, color);
	}

	public override void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		plat.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		Render2DTexture(textureid, (float)posX, (float)posY, (float)width, (float)height, z, color);
		plat.GlToggleBlend(on: true);
	}

	public override void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f)
	{
		Render2DTexture(textureid, x1, y1, width, height, z, null);
	}

	public override void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
	{
		ShaderProgramGui guiShaderProg = ShaderPrograms.Gui;
		if (quadModel == null)
		{
			quadModel = plat.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
		}
		if (guiShaderProg != null)
		{
			guiShaderProg.Use();
			guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
			guiShaderProg.ExtraGlow = 0;
			guiShaderProg.ApplyColor = ((color != null) ? 1 : 0);
			guiShaderProg.Tex2d2D = textureid;
			guiShaderProg.NoTexture = 0f;
			guiShaderProg.OverlayOpacity = 0f;
			guiShaderProg.DarkEdges = 0;
			guiShaderProg.NormalShaded = 0;
			guiShaderProg.DamageEffect = 0f;
			Mat4f.Identity(mvMatrix);
			Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(posX, posY, z - 20000f + 151f));
			Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
			Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
			Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(1f, 1f, 0f));
			guiShaderProg.ProjectionMatrix = pMatrix;
			guiShaderProg.ModelViewMatrix = mvMatrix;
			plat.BindTexture2d(textureid);
			plat.RenderMesh(quadModel);
			guiShaderProg.Stop();
		}
		else
		{
			ShaderProgramMinimalGui minimalGuiShader = plat.MinimalGuiShader;
			minimalGuiShader.Use();
			minimalGuiShader.Tex2d2D = textureid;
			Mat4f.Identity(mvMatrix);
			Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(posX, posY, 0f));
			Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
			Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
			Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(1f, 1f, z));
			minimalGuiShader.ProjectionMatrix = pMatrix;
			minimalGuiShader.ModelViewMatrix = mvMatrix;
			plat.BindTexture2d(textureid);
			plat.RenderMesh(quadModel);
			minimalGuiShader.Stop();
		}
	}

	public void Draw2DShadedEdges(float posX, float posY, float width, float height, float z = 50f)
	{
		ShaderProgramGui gui = ShaderPrograms.Gui;
		if (quadModel == null)
		{
			quadModel = plat.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
		}
		gui.Use();
		gui.RgbaIn = ColorUtil.WhiteArgbVec;
		gui.ExtraGlow = 0;
		gui.ApplyColor = 0;
		gui.Tex2d2D = 0;
		gui.NoTexture = 0f;
		gui.OverlayOpacity = 0f;
		gui.DarkEdges = 1;
		gui.NormalShaded = 0;
		Mat4f.Identity(mvMatrix);
		Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(posX, posY, 0f));
		Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
		Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
		Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(1f, 1f, z));
		gui.ProjectionMatrix = pMatrix;
		gui.ModelViewMatrix = mvMatrix;
		plat.RenderMesh(quadModel);
		gui.Stop();
	}

	public override void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f)
	{
		Render2DTexture(textTexture.TextureId, posX, posY, textTexture.Width, textTexture.Height, z);
	}

	public override void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color)
	{
		ShaderProgramGui gui = ShaderPrograms.Gui;
		gui.Use();
		if (whiteRectangleRef == null)
		{
			MeshData mesh = LineMeshUtil.GetRectangle(-1);
			whiteRectangleRef = plat.UploadMesh(mesh);
		}
		MeshRef modelRef = whiteRectangleRef;
		Vec4f vec = new Vec4f();
		gui.RgbaIn = ColorUtil.ToRGBAVec4f(color, ref vec);
		gui.ExtraGlow = 0;
		gui.ApplyColor = 0;
		gui.Tex2d2D = 0;
		gui.NoTexture = 1f;
		gui.OverlayOpacity = 0f;
		gui.DarkEdges = 0;
		gui.NormalShaded = 0;
		Mat4f.Identity(mvMatrix);
		Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(posX, posY, 0f));
		Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
		Mat4f.Scale(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
		Mat4f.Translate(mvMatrix, mvMatrix, Vec3Utilsf.FromValues(1f, 1f, posZ));
		gui.ProjectionMatrix = pMatrix;
		gui.ModelViewMatrix = mvMatrix;
		plat.GLLineWidth(1f);
		plat.SmoothLines(on: false);
		plat.RenderMesh(modelRef);
		gui.Stop();
	}
}
