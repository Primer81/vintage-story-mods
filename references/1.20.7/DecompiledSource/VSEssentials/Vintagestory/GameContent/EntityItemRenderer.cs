using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityItemRenderer : EntityRenderer
{
	public static bool RunWittySkipRenderAlgorithm;

	public static BlockPos LastPos = new BlockPos();

	public static int LastCollectibleId;

	public static int RenderCount;

	public static int RenderModulo;

	private EntityItem entityitem;

	private long touchGroundMS;

	public float[] ModelMat = Mat4f.Create();

	private float scaleRand;

	private float yRotRand;

	private Vec3d lerpedPos = new Vec3d();

	private ItemSlot inslot;

	private float accum;

	private Vec4f particleOutTransform = new Vec4f();

	private Vec4f glowRgb = new Vec4f();

	private bool rotateWhenFalling;

	private float xangle;

	private float yangle;

	private float zangle;

	public EntityItemRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		entityitem = (EntityItem)entity;
		inslot = entityitem.Slot;
		rotateWhenFalling = (inslot.Itemstack?.Collectible?.Attributes?["rotateWhenFalling"].AsBool(defaultValue: true)).GetValueOrDefault(true);
		scaleRand = (float)api.World.Rand.NextDouble() / 20f - 0.025f;
		touchGroundMS = entityitem.itemSpawnedMilliseconds - api.World.Rand.Next(5000);
		yRotRand = (float)api.World.Rand.NextDouble() * ((float)Math.PI * 2f);
		lerpedPos = entity.Pos.XYZ;
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (isShadowPass && !entity.IsRendered)
		{
			return;
		}
		if (RunWittySkipRenderAlgorithm)
		{
			int x = (int)entity.Pos.X;
			int y = (int)entity.Pos.Y;
			int z = (int)entity.Pos.Z;
			int collId = ((entityitem.Itemstack.Class != 0) ? 1 : (-1)) * entityitem.Itemstack.Id;
			if (LastPos.X == x && LastPos.Y == y && LastPos.Z == z && LastCollectibleId == collId)
			{
				if (entity.EntityId % RenderModulo != 0L)
				{
					return;
				}
			}
			else
			{
				LastPos.Set(x, y, z);
			}
			LastCollectibleId = collId;
		}
		IRenderAPI rapi = capi.Render;
		lerpedPos.X += (entity.Pos.X - lerpedPos.X) * 22.0 * (double)dt;
		lerpedPos.Y += (entity.Pos.InternalY - lerpedPos.Y) * 22.0 * (double)dt;
		lerpedPos.Z += (entity.Pos.Z - lerpedPos.Z) * 22.0 * (double)dt;
		ItemRenderInfo renderInfo = rapi.GetItemStackRenderInfo(inslot, EnumItemRenderTarget.Ground, dt);
		if (renderInfo.ModelRef == null || renderInfo.Transform == null)
		{
			return;
		}
		IStandardShaderProgram prog = null;
		LoadModelMatrix(renderInfo, isShadowPass, dt);
		string textureSampleName = "tex";
		if (isShadowPass)
		{
			textureSampleName = "tex2d";
			float[] mvpMat = Mat4f.Mul(ModelMat, capi.Render.CurrentModelviewMatrix, ModelMat);
			Mat4f.Mul(mvpMat, capi.Render.CurrentProjectionMatrix, mvpMat);
			capi.Render.CurrentActiveShader.UniformMatrix("mvpMatrix", mvpMat);
			capi.Render.CurrentActiveShader.Uniform("origin", new Vec3f());
		}
		else
		{
			prog = rapi.StandardShader;
			prog.Use();
			prog.RgbaTint = (entity.Swimming ? new Vec4f(0.5f, 0.5f, 0.5f, 1f) : ColorUtil.WhiteArgbVec);
			prog.DontWarpVertices = 0;
			prog.NormalShaded = 1;
			prog.AlphaTest = renderInfo.AlphaTest;
			prog.DamageEffect = renderInfo.DamageEffect;
			if (entity.Swimming)
			{
				prog.AddRenderFlags = (int)(((entityitem.Itemstack.Collectible.MaterialDensity <= 1000) ? 1u : 0u) << 12);
				prog.WaterWaveCounter = capi.Render.ShaderUniforms.WaterWaveCounter;
			}
			else
			{
				prog.AddRenderFlags = 0;
			}
			prog.OverlayOpacity = renderInfo.OverlayOpacity;
			if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0f)
			{
				prog.Tex2dOverlay2D = renderInfo.OverlayTexture.TextureId;
				prog.OverlayTextureSize = new Vec2f(renderInfo.OverlayTexture.Width, renderInfo.OverlayTexture.Height);
				prog.BaseTextureSize = new Vec2f(renderInfo.TextureSize.Width, renderInfo.TextureSize.Height);
				TextureAtlasPosition texPos = rapi.GetTextureAtlasPosition(entityitem.Itemstack);
				prog.BaseUvOrigin = new Vec2f(texPos.x1, texPos.y1);
			}
			BlockPos pos = entityitem.Pos.AsBlockPos;
			Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.InternalY, pos.Z);
			int num = (int)entityitem.Itemstack.Collectible.GetTemperature(capi.World, entityitem.Itemstack);
			float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
			glowRgb.R = glowColor[0];
			glowRgb.G = glowColor[1];
			glowRgb.B = glowColor[2];
			glowRgb.A = (float)extraGlow / 255f;
			prog.ExtraGlow = extraGlow;
			prog.RgbaAmbientIn = rapi.AmbientColor;
			prog.RgbaLightIn = lightrgbs;
			prog.RgbaGlowIn = glowRgb;
			prog.RgbaFogIn = rapi.FogColor;
			prog.FogMinIn = rapi.FogMin;
			prog.FogDensityIn = rapi.FogDensity;
			prog.ExtraGodray = 0f;
			prog.NormalShaded = (renderInfo.NormalShaded ? 1 : 0);
			prog.ProjectionMatrix = rapi.CurrentProjectionMatrix;
			prog.ViewMatrix = rapi.CameraMatrixOriginf;
			prog.ModelMatrix = ModelMat;
			ItemStack stack = entityitem.Itemstack;
			AdvancedParticleProperties[] ParticleProperties = stack.Block?.ParticleProperties;
			if (stack.Block != null && !capi.IsGamePaused)
			{
				Mat4f.MulWithVec4(ModelMat, new Vec4f(stack.Block.TopMiddlePos.X, stack.Block.TopMiddlePos.Y - 0.4f, stack.Block.TopMiddlePos.Z - 0.5f, 0f), particleOutTransform);
				accum += dt;
				if (ParticleProperties != null && ParticleProperties.Length != 0 && accum > 0.025f)
				{
					accum %= 0.025f;
					foreach (AdvancedParticleProperties bps in ParticleProperties)
					{
						bps.basePos.X = (double)particleOutTransform.X + entity.Pos.X;
						bps.basePos.Y = (double)particleOutTransform.Y + entity.Pos.InternalY;
						bps.basePos.Z = (double)particleOutTransform.Z + entity.Pos.Z;
						entityitem.World.SpawnParticles(bps);
					}
				}
			}
		}
		if (!renderInfo.CullFaces)
		{
			rapi.GlDisableCullFace();
		}
		rapi.RenderMultiTextureMesh(renderInfo.ModelRef, textureSampleName);
		if (!renderInfo.CullFaces)
		{
			rapi.GlEnableCullFace();
		}
		if (!isShadowPass)
		{
			prog.AddRenderFlags = 0;
			prog.DamageEffect = 0f;
			prog.Stop();
		}
	}

	private void LoadModelMatrix(ItemRenderInfo renderInfo, bool isShadowPass, float dt)
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, (float)(lerpedPos.X - entityPlayer.CameraPos.X), (float)(lerpedPos.Y - entityPlayer.CameraPos.Y), (float)(lerpedPos.Z - entityPlayer.CameraPos.Z));
		float sizeX = 0.2f * renderInfo.Transform.ScaleXYZ.X;
		float sizeY = 0.2f * renderInfo.Transform.ScaleXYZ.Y;
		float sizeZ = 0.2f * renderInfo.Transform.ScaleXYZ.Z;
		float dx = 0f;
		float dz = 0f;
		if (!isShadowPass)
		{
			long ellapseMs = capi.World.ElapsedMilliseconds;
			bool freefall = !entity.Collided && !entity.Swimming && !capi.IsGamePaused;
			if (!freefall)
			{
				touchGroundMS = ellapseMs;
			}
			if (entity.Collided)
			{
				xangle *= 0.55f;
				yangle *= 0.55f;
				zangle *= 0.55f;
			}
			else if (rotateWhenFalling)
			{
				float easeIn = Math.Min(1L, (ellapseMs - touchGroundMS) / 200);
				float angleGain = (freefall ? (1000f * dt / 7f * easeIn) : 0f);
				yangle += angleGain;
				xangle += angleGain;
				zangle += angleGain;
			}
			if (entity.Swimming)
			{
				float diff = 1f;
				if (entityitem.Itemstack.Collectible.MaterialDensity > 1000)
				{
					dx = GameMath.Sin((float)((double)ellapseMs / 1000.0)) / 50f;
					dz = (0f - GameMath.Sin((float)((double)ellapseMs / 3000.0))) / 50f;
					diff = 0.1f;
				}
				xangle = GameMath.Sin((float)((double)ellapseMs / 1000.0)) * 8f * diff;
				yangle = GameMath.Cos((float)((double)ellapseMs / 2000.0)) * 3f * diff;
				zangle = (0f - GameMath.Sin((float)((double)ellapseMs / 3000.0))) * 8f * diff;
			}
		}
		ModelTransform itemTransform = renderInfo.Transform;
		Vec3f itemTranslation = itemTransform.Translation;
		Vec3f itemRotation = itemTransform.Rotation;
		Mat4f.Translate(ModelMat, ModelMat, dx + itemTranslation.X, itemTranslation.Y, dz + itemTranslation.Z);
		Mat4f.Scale(ModelMat, ModelMat, new float[3]
		{
			sizeX + scaleRand,
			sizeY + scaleRand,
			sizeZ + scaleRand
		});
		Mat4f.RotateY(ModelMat, ModelMat, (float)Math.PI / 180f * (itemRotation.Y + yangle) + (itemTransform.Rotate ? yRotRand : 0f));
		Mat4f.RotateZ(ModelMat, ModelMat, (float)Math.PI / 180f * (itemRotation.Z + zangle));
		Mat4f.RotateX(ModelMat, ModelMat, (float)Math.PI / 180f * (itemRotation.X + xangle));
		Mat4f.Translate(ModelMat, ModelMat, 0f - itemTransform.Origin.X, 0f - itemTransform.Origin.Y, 0f - itemTransform.Origin.Z);
	}

	public override void Dispose()
	{
	}
}
