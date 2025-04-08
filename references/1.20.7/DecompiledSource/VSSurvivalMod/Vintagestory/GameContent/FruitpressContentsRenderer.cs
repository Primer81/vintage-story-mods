using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitpressContentsRenderer : IRenderer, IDisposable, ITexPositionSource
{
	private ICoreClientAPI api;

	private BlockPos pos;

	private Matrixf ModelMat = new Matrixf();

	private MultiTextureMeshRef mashMeshref;

	private BlockEntityFruitPress befruitpress;

	private AssetLocation textureLocation;

	private TextureAtlasPosition texPos;

	public TextureAtlasPosition juiceTexPos;

	public double RenderOrder => 0.65;

	public int RenderRange => 48;

	public Size2i AtlasSize => api.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation texturePath = textureLocation;
			TextureAtlasPosition texpos = ((!(texturePath == null)) ? api.BlockTextureAtlas[texturePath] : texPos);
			if (texpos == null)
			{
				IAsset texAsset = api.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if (texAsset != null)
				{
					BitmapRef bmp = texAsset.ToBitmap(api);
					api.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texpos, () => bmp);
				}
				else
				{
					texpos = api.BlockTextureAtlas.UnknownTexturePosition;
				}
			}
			return texpos;
		}
	}

	public FruitpressContentsRenderer(ICoreClientAPI api, BlockPos pos, BlockEntityFruitPress befruitpress)
	{
		this.api = api;
		this.pos = pos;
		this.befruitpress = befruitpress;
	}

	public void reloadMeshes(JuiceableProperties props, bool mustReload)
	{
		if (befruitpress.Inventory.Empty)
		{
			mashMeshref = null;
		}
		else
		{
			if (!mustReload && mashMeshref != null)
			{
				return;
			}
			mashMeshref?.Dispose();
			ItemStack stack = befruitpress.Inventory[0].Itemstack;
			if (stack == null)
			{
				return;
			}
			int y;
			if (stack.Collectible.Code.Path == "rot")
			{
				textureLocation = new AssetLocation("block/wood/barrel/rot");
				y = GameMath.Clamp(stack.StackSize / 2, 1, 9);
			}
			else
			{
				textureLocation = props.PressedStack.ResolvedItemstack.Item.Textures.First().Value.Base;
				y = ((!stack.Attributes.HasAttribute("juiceableLitresLeft")) ? GameMath.Clamp(stack.StackSize, 1, 9) : ((int)GameMath.Clamp((float)stack.Attributes.GetDecimal("juiceableLitresLeft") + (float)stack.Attributes.GetDecimal("juiceableLitresTransfered"), 1f, 9f)));
			}
			Shape mashShape = Shape.TryGet(api, "shapes/block/wood/fruitpress/part-mash-" + y + ".json");
			api.Tesselator.TesselateShape("fruitpress-mash", mashShape, out var mashMesh, this, null, 0, 0, 0);
			juiceTexPos = api.BlockTextureAtlas[textureLocation];
			if (stack.Collectible.Code.Path != "rot")
			{
				Shape.TryGet(api, "shapes/block/wood/fruitpress/part-juice.json");
				AssetLocation loc = AssetLocation.Create("juiceportion-" + stack.Collectible.Variant["fruit"], stack.Collectible.Code.Domain);
				Item item = api.World.GetItem(loc);
				textureLocation = null;
				if (item?.FirstTexture.Baked == null)
				{
					texPos = api.BlockTextureAtlas.UnknownTexturePosition;
				}
				else
				{
					texPos = api.BlockTextureAtlas.Positions[item.FirstTexture.Baked.TextureSubId];
				}
			}
			mashMeshref = api.Render.UploadMultiTextureMesh(mashMesh);
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (mashMeshref != null && !mashMeshref.Disposed)
		{
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.GlDisableCullFace();
			rpi.GlToggleBlend(blend: true);
			IStandardShaderProgram standardShader = rpi.StandardShader;
			standardShader.Use();
			standardShader.DontWarpVertices = 0;
			standardShader.AddRenderFlags = 0;
			standardShader.RgbaAmbientIn = rpi.AmbientColor;
			standardShader.RgbaFogIn = rpi.FogColor;
			standardShader.FogMinIn = rpi.FogMin;
			standardShader.FogDensityIn = rpi.FogDensity;
			standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
			standardShader.NormalShaded = 1;
			standardShader.ExtraGodray = 0f;
			standardShader.ExtraGlow = 0;
			standardShader.SsaoAttn = 0f;
			standardShader.AlphaTest = 0.05f;
			standardShader.OverlayOpacity = 0f;
			Vec4f lightrgbs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			standardShader.RgbaLightIn = lightrgbs;
			double squeezeRel = (befruitpress.MashSlot.Itemstack?.Attributes?.GetDouble("squeezeRel", 1.0)).GetValueOrDefault(1.0);
			standardShader.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Translate(0f, 0.8f, 0f)
				.Scale(1f, (float)squeezeRel, 1f)
				.Values;
			standardShader.ViewMatrix = rpi.CameraMatrixOriginf;
			standardShader.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMultiTextureMesh(mashMeshref, "tex");
			standardShader.Stop();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		mashMeshref?.Dispose();
	}
}
