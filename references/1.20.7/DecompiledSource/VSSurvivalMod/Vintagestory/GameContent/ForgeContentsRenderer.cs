using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ForgeContentsRenderer : IRenderer, IDisposable, ITexPositionSource
{
	private ICoreClientAPI capi;

	private BlockPos pos;

	private MeshRef workItemMeshRef;

	private MeshRef emberQuadRef;

	private MeshRef coalQuadRef;

	private ItemStack stack;

	private float fuelLevel;

	private bool burning;

	private TextureAtlasPosition coaltexpos;

	private TextureAtlasPosition embertexpos;

	private int textureId;

	private string tmpMetal;

	private ITexPositionSource tmpTextureSource;

	private Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpMetal];

	public ForgeContentsRenderer(BlockPos pos, ICoreClientAPI capi)
	{
		this.pos = pos;
		this.capi = capi;
		Block block = capi.World.GetBlock(new AssetLocation("forge"));
		coaltexpos = capi.BlockTextureAtlas.GetPosition(block, "coal");
		embertexpos = capi.BlockTextureAtlas.GetPosition(block, "ember");
		MeshData emberMesh = QuadMeshUtil.GetCustomQuadHorizontal(0.1875f, 0f, 0.1875f, 0.625f, 0.625f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		for (int j = 0; j < emberMesh.Uv.Length; j += 2)
		{
			emberMesh.Uv[j] = embertexpos.x1 + emberMesh.Uv[j] * 32f / (float)AtlasSize.Width;
			emberMesh.Uv[j + 1] = embertexpos.y1 + emberMesh.Uv[j + 1] * 32f / (float)AtlasSize.Height;
		}
		emberMesh.Flags = new int[4] { 128, 128, 128, 128 };
		MeshData coalMesh = QuadMeshUtil.GetCustomQuadHorizontal(0.1875f, 0f, 0.1875f, 0.625f, 0.625f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		for (int i = 0; i < coalMesh.Uv.Length; i += 2)
		{
			coalMesh.Uv[i] = coaltexpos.x1 + coalMesh.Uv[i] * 32f / (float)AtlasSize.Width;
			coalMesh.Uv[i + 1] = coaltexpos.y1 + coalMesh.Uv[i + 1] * 32f / (float)AtlasSize.Height;
		}
		emberQuadRef = capi.Render.UploadMesh(emberMesh);
		coalQuadRef = capi.Render.UploadMesh(coalMesh);
	}

	public void SetContents(ItemStack stack, float fuelLevel, bool burning, bool regen)
	{
		this.stack = stack;
		this.fuelLevel = fuelLevel;
		this.burning = burning;
		if (regen)
		{
			RegenMesh();
		}
	}

	private void RegenMesh()
	{
		workItemMeshRef?.Dispose();
		workItemMeshRef = null;
		if (stack == null)
		{
			return;
		}
		tmpMetal = stack.Collectible.LastCodePart();
		MeshData mesh = null;
		switch (stack.Collectible.FirstCodePart())
		{
		case "metalplate":
		{
			tmpTextureSource = capi.Tesselator.GetTextureSource(capi.World.GetBlock(new AssetLocation("platepile")));
			Shape shape = Shape.TryGet(capi, "shapes/block/stone/forge/platepile.json");
			textureId = tmpTextureSource[tmpMetal].atlasTextureId;
			capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);
			break;
		}
		case "workitem":
		{
			MeshData workItemMesh = ItemWorkItem.GenMesh(capi, stack, ItemWorkItem.GetVoxels(stack), out textureId);
			if (workItemMesh != null)
			{
				workItemMesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
				workItemMesh.Translate(0f, -0.5625f, 0f);
				workItemMeshRef = capi.Render.UploadMesh(workItemMesh);
			}
			break;
		}
		case "ingot":
		{
			tmpTextureSource = capi.Tesselator.GetTextureSource(capi.World.GetBlock(new AssetLocation("ingotpile")));
			Shape shape = Shape.TryGet(capi, "shapes/block/stone/forge/ingotpile.json");
			textureId = tmpTextureSource[tmpMetal].atlasTextureId;
			capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);
			break;
		}
		default:
		{
			JsonObject attributes = stack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("forgable"))
			{
				if (stack.Class == EnumItemClass.Block)
				{
					mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
					textureId = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;
				}
				else
				{
					capi.Tesselator.TesselateItem(stack.Item, out mesh);
					textureId = capi.ItemTextureAtlas.AtlasTextures[0].TextureId;
				}
				ModelTransform tf = stack.Collectible.Attributes["inForgeTransform"].AsObject<ModelTransform>();
				if (tf != null)
				{
					tf.EnsureDefaultValues();
					mesh.ModelTransform(tf);
				}
			}
			break;
		}
		}
		if (mesh != null)
		{
			workItemMeshRef = capi.Render.UploadMesh(mesh);
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (stack == null && fuelLevel == 0f)
		{
			return;
		}
		IRenderAPI rpi = capi.Render;
		Vec3d camPos = capi.World.Player.Entity.CameraPos;
		rpi.GlDisableCullFace();
		IStandardShaderProgram prog = rpi.StandardShader;
		prog.Use();
		prog.RgbaAmbientIn = rpi.AmbientColor;
		prog.RgbaFogIn = rpi.FogColor;
		prog.FogMinIn = rpi.FogMin;
		prog.FogDensityIn = rpi.FogDensity;
		prog.RgbaTint = ColorUtil.WhiteArgbVec;
		prog.DontWarpVertices = 0;
		prog.AddRenderFlags = 0;
		prog.ExtraGodray = 0f;
		prog.OverlayOpacity = 0f;
		if (stack != null && workItemMeshRef != null)
		{
			int num = (int)stack.Collectible.GetTemperature(capi.World, stack);
			Vec4f lightrgbs2 = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			float[] glowColor2 = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
			prog.NormalShaded = 1;
			prog.RgbaLightIn = lightrgbs2;
			prog.RgbaGlowIn = new Vec4f(glowColor2[0], glowColor2[1], glowColor2[2], (float)extraGlow / 255f);
			prog.ExtraGlow = extraGlow;
			prog.Tex2D = textureId;
			prog.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y + 0.625 + (double)(fuelLevel * 0.65f), (double)pos.Z - camPos.Z).Values;
			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMesh(workItemMeshRef);
		}
		if (fuelLevel > 0f)
		{
			Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			long seed = capi.World.ElapsedMilliseconds + pos.GetHashCode();
			float flicker = (float)(Math.Sin((double)seed / 40.0) * 0.20000000298023224 + Math.Sin((double)seed / 220.0) * 0.6000000238418579 + Math.Sin((double)seed / 100.0) + 1.0) / 2f;
			if (burning)
			{
				float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(1200);
				glowColor[0] *= 1f - flicker * 0.15f;
				glowColor[1] *= 1f - flicker * 0.15f;
				glowColor[2] *= 1f - flicker * 0.15f;
				prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], 1f);
			}
			else
			{
				prog.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
			}
			prog.NormalShaded = 0;
			prog.RgbaLightIn = lightrgbs;
			prog.TempGlowMode = 1;
			int glow = 255 - (int)(flicker * 50f);
			prog.ExtraGlow = (burning ? glow : 0);
			rpi.BindTexture2d(burning ? embertexpos.atlasTextureId : coaltexpos.atlasTextureId);
			prog.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y + 0.625 + (double)(fuelLevel * 0.65f), (double)pos.Z - camPos.Z).Values;
			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMesh(burning ? emberQuadRef : coalQuadRef);
		}
		prog.Stop();
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		emberQuadRef?.Dispose();
		coalQuadRef?.Dispose();
		workItemMeshRef?.Dispose();
	}
}
