using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class RenderAPIGame : RenderAPIBase
{
	private ClientMain game;

	internal InventoryItemRenderer inventoryItemRenderer;

	internal PerceptionEffects perceptionEffects;

	public override DefaultShaderUniforms ShaderUniforms => game.shUniforms;

	public override ICoreClientAPI Api => game.api;

	public override int TextureSize => game.textureSize;

	public override PerceptionEffects PerceptionEffects => perceptionEffects;

	public override ModelTransform CameraOffset => game.MainCamera.CameraOffset;

	public override double[] CameraMatrixOrigin => game.MainCamera.CameraMatrixOrigin;

	public override float[] CameraMatrixOriginf => game.MainCamera.CameraMatrixOriginf;

	public override Vec3f AmbientColor => game.AmbientManager.BlendedAmbientColor;

	public override Vec4f FogColor => game.AmbientManager.BlendedFogColor;

	public override float FogMin => game.AmbientManager.BlendedFogMin;

	public override float FogDensity => game.AmbientManager.BlendedFogDensity;

	public override EnumCameraMode CameraType => game.MainCamera.CameraMode;

	public override StackMatrix4 MvMatrix => game.MvMatrix;

	public override StackMatrix4 PMatrix => game.PMatrix;

	public override double[] PerspectiveViewMat => game.PerspectiveViewMat;

	public override double[] PerspectiveProjectionMat => game.PerspectiveProjectionMat;

	public override float[] CurrentModelviewMatrix => game.CurrentModelViewMatrix;

	public override float[] CurrentProjectionMatrix => game.CurrentProjectionMatrix;

	public override EnumRenderStage CurrentRenderStage => game.currentRenderStage;

	public override float[] CurrentShadowProjectionMatrix => game.shadowMvpMatrix;

	public override FrustumCulling DefaultFrustumCuller => game.frustumCuller;

	public RenderAPIGame(ICoreClientAPI capi, ClientMain game)
		: base(game.Platform)
	{
		this.game = game;
		inventoryItemRenderer = new InventoryItemRenderer(game);
		perceptionEffects = new PerceptionEffects(capi);
	}

	public override void GlLoadMatrix(double[] matrix)
	{
		game.GlLoadMatrix(matrix);
	}

	public override void GlMatrixModeModelView()
	{
		game.GlMatrixModeModelView();
	}

	public override void GlPopMatrix()
	{
		game.GlPopMatrix();
	}

	public override void GlPushMatrix()
	{
		game.GlPushMatrix();
	}

	public override void GlRotate(float angle, float x, float y, float z)
	{
		game.GlRotate(angle, x, y, z);
	}

	public override void GlScale(float x, float y, float z)
	{
		game.GlScale(x, y, z);
	}

	public override void GlTranslate(float x, float y, float z)
	{
		game.GlTranslate(x, y, z);
	}

	public override void GlTranslate(double x, double y, double z)
	{
		game.GlTranslate(x, y, z);
	}

	public override void GetOrLoadTexture(AssetLocation name, ref LoadedTexture intoTexture)
	{
		game.GetOrLoadCachedTexture(name, ref intoTexture);
	}

	public override int GetOrLoadTexture(AssetLocation name)
	{
		return game.GetOrLoadCachedTexture(name);
	}

	public override void GetOrLoadTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture)
	{
		game.GetOrLoadCachedTexture(name, bmp, ref intoTexture);
	}

	public override bool RemoveTexture(AssetLocation name)
	{
		return game.DeleteCachedTexture(name);
	}

	public override void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f)
	{
		game.Render2DTexture(textureid, x1, y1, width, height, z);
	}

	public override ItemRenderInfo GetItemStackRenderInfo(ItemStack stack, EnumItemRenderTarget target, float dt)
	{
		return InventoryItemRenderer.GetItemStackRenderInfo(game, new DummySlot(stack), target, dt);
	}

	public override TextureAtlasPosition GetTextureAtlasPosition(ItemStack itemstack)
	{
		return InventoryItemRenderer.GetTextureAtlasPosition(game, itemstack);
	}

	public override ItemRenderInfo GetItemStackRenderInfo(ItemSlot inSlot, EnumItemRenderTarget target, float dt)
	{
		return InventoryItemRenderer.GetItemStackRenderInfo(game, inSlot, target, dt);
	}

	public override IStandardShaderProgram PreparedStandardShader(int posX, int posY, int posZ, Vec4f colorMul = null)
	{
		Vec4f lightrgbs = game.WorldMap.GetLightRGBSVec4f(posX, posY, posZ);
		IStandardShaderProgram standardShader = base.StandardShader;
		standardShader.Use();
		standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
		standardShader.RgbaAmbientIn = AmbientColor;
		standardShader.RgbaLightIn = ((colorMul == null) ? lightrgbs : lightrgbs.Mul(colorMul));
		standardShader.RgbaFogIn = FogColor;
		standardShader.NormalShaded = 1;
		standardShader.ExtraGlow = 0;
		standardShader.FogMinIn = FogMin;
		standardShader.FogDensityIn = FogDensity;
		standardShader.DontWarpVertices = 0;
		standardShader.AddRenderFlags = 0;
		standardShader.ExtraZOffset = 0f;
		standardShader.OverlayOpacity = 0f;
		standardShader.DamageEffect = 0f;
		standardShader.ExtraGodray = 0f;
		standardShader.ProjectionMatrix = CurrentProjectionMatrix;
		return standardShader;
	}

	public override void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f)
	{
		game.RenderTextureIntoTexture(fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, intoTexture, targetX, targetY, alphaTest);
	}

	public override Vec4f GetLightRGBs(int x, int y, int z)
	{
		return game.WorldMap.GetLightRGBSVec4f(x, y, z);
	}

	internal override void Dispose()
	{
		base.Dispose();
		inventoryItemRenderer.Dispose();
	}

	public override bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
	{
		return inventoryItemRenderer.RenderItemStackToAtlas(stack, atlas, size, onComplete, color, sepiaLevel, scale);
	}

	public override void RenderItemstackToGui(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStacksize = true)
	{
		inventoryItemRenderer.RenderItemstackToGui(new DummySlot(itemstack), posX, posY, posZ, size, color, shading, rotate, showStacksize);
	}

	public override void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool rotate = false, bool showStackSize = true)
	{
		inventoryItemRenderer.RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, dt, shading, rotate, showStackSize);
	}

	public override void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStacksize = true)
	{
		inventoryItemRenderer.RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, shading, rotate, showStacksize);
	}

	public override void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
	{
		inventoryItemRenderer.RenderEntityToGui(dt, entity, posX, posY, posZ, yawDelta, size, color);
	}

	public override void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		Render2DTexture(textureid, (int)bounds.renderX, (int)bounds.renderY, bounds.OuterWidthInt, bounds.OuterHeightInt, z, color);
	}

	public override void Render2DTexture(MeshRef quadModel, int textureid, float x1, float y1, float width, float height, float z = 50f)
	{
		game.Render2DTexture(quadModel, textureid, x1, y1, width, height, z);
	}

	public override void Render2DTexture(MultiTextureMeshRef quadModel, float x1, float y1, float width, float height, float z = 50f)
	{
		game.Render2DTexture(quadModel, x1, y1, width, height, z);
	}

	public override void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		plat.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		Render2DTexture(textureid, (int)bounds.renderX, (int)bounds.renderY, bounds.OuterWidthInt, bounds.OuterHeightInt, z, color);
		plat.GlToggleBlend(on: true);
	}

	public override void Render2DTexturePremultipliedAlpha(int textureid, float x1, float y1, float width, float height, float z = 50f, Vec4f color = null)
	{
		plat.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		game.Render2DTexture(textureid, (int)x1, (int)y1, width, height, z, color);
		plat.GlToggleBlend(on: true);
	}

	public override void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		plat.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		Render2DTexture(textureid, (int)posX, (int)posY, (float)width, (float)height, z, color);
		plat.GlToggleBlend(on: true);
	}

	public override void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f, Vec4f color = null)
	{
		game.Render2DTexture(textureid, x1, y1, width, height, z, color);
	}

	public override void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		Render2DTexture(textureid, (int)posX, (int)posY, (float)width, (float)height, z, color);
	}

	public override void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color)
	{
		MeshRef modelRef = whiteRectangleRef;
		Vec4f vec = new Vec4f();
		game.guiShaderProg.RgbaIn = ColorUtil.ToRGBAVec4f(color, ref vec);
		game.guiShaderProg.ExtraGlow = 0;
		game.guiShaderProg.ApplyColor = 0;
		game.guiShaderProg.Tex2d2D = 0;
		game.guiShaderProg.NoTexture = 1f;
		game.guiShaderProg.OverlayOpacity = 0f;
		game.GlPushMatrix();
		game.GlTranslate(posX, posY, posZ);
		game.GlScale(width, height, 0.0);
		game.GlScale(0.5, 0.5, 0.0);
		game.GlTranslate(1.0, 1.0, 0.0);
		plat.GLLineWidth(1f);
		plat.SmoothLines(on: false);
		plat.GlToggleBlend(on: true);
		game.guiShaderProg.ProjectionMatrix = game.CurrentProjectionMatrix;
		game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
		plat.RenderMesh(modelRef);
		game.GlPopMatrix();
		game.guiShaderProg.NoTexture = 0f;
	}

	public override void RenderLine(BlockPos origin, float posX1, float posY1, float posZ1, float posX2, float posY2, float posZ2, int color)
	{
		MeshData mesh = new MeshData(4, 4, withNormals: false, withUv: false);
		mesh.SetMode(EnumDrawMode.LineStrip);
		int vertexIndex = 0;
		mesh.AddVertexSkipTex(posX1, posY1, posZ1, color);
		mesh.AddIndex(vertexIndex++);
		mesh.AddVertexSkipTex(posX2, posY2, posZ2, color);
		mesh.AddIndex(vertexIndex++);
		MeshRef meshref = game.api.renderapi.UploadMesh(mesh);
		ShaderProgramAutocamera autocamera = ShaderPrograms.Autocamera;
		autocamera.Use();
		game.Platform.GLLineWidth(2f);
		game.Platform.BindTexture2d(0);
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		game.GlTranslate((float)((double)origin.X - cameraPos.X), (float)((double)origin.Y - cameraPos.Y), (float)((double)origin.Z - cameraPos.Z));
		autocamera.ProjectionMatrix = game.CurrentProjectionMatrix;
		autocamera.ModelViewMatrix = game.CurrentModelViewMatrix;
		plat.RenderMesh(meshref);
		autocamera.Stop();
		meshref.Dispose();
		game.GlPopMatrix();
	}

	public override void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f)
	{
		game.Render2DLoadedTexture(textTexture, posX, posY, z);
	}

	public override void AddPointLight(IPointLight pointlight)
	{
		game.pointlights.Add(pointlight);
	}

	public override void RemovePointLight(IPointLight pointlight)
	{
		game.pointlights.Remove(pointlight);
	}

	public override void Reset3DProjection()
	{
		game.Reset3DProjection();
	}

	public override void Set3DProjection(float zfar, float fov)
	{
		game.Set3DProjection(zfar, fov);
	}
}
