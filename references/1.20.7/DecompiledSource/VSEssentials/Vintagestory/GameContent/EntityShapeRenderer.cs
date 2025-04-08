using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityShapeRenderer : EntityRenderer, ITexPositionSource
{
	protected long listenerId;

	protected LoadedTexture debugTagTexture;

	protected MultiTextureMeshRef meshRefOpaque;

	protected Vec4f color = new Vec4f(1f, 1f, 1f, 1f);

	protected long lastDebugInfoChangeMs;

	protected bool isSpectator;

	protected IClientPlayer player;

	public float bodyYawLerped;

	public Vec3f OriginPos = new Vec3f();

	public float[] ModelMat = Mat4f.Create();

	protected float[] tmpMvMat = Mat4f.Create();

	protected Matrixf ItemModelMat = new Matrixf();

	public bool DoRenderHeldItem;

	public int AddRenderFlags;

	public double WindWaveIntensity = 1.0;

	public bool glitchFlicker;

	public bool frostable;

	public float frostAlpha;

	public float targetFrostAlpha;

	public OnGetFrostAlpha getFrostAlpha;

	public float frostAlphaAccum;

	protected List<MessageTexture> messageTextures;

	protected EntityAgent eagent;

	public CompositeShape OverrideCompositeShape;

	public Shape OverrideEntityShape;

	public string[] OverrideSelectiveElements;

	public bool glitchAffected;

	protected IInventory gearInv;

	protected ITexPositionSource defaultTexSource;

	protected Vec4f lightrgbs;

	protected float intoxIntensity;

	public TextureAtlasPosition skinTexPos;

	protected bool loaded;

	private float accum;

	protected float[] pMatrixHandFov;

	protected float[] pMatrixNormalFov;

	private double stepPitch;

	private double prevY;

	private double prevYAccum;

	public float xangle;

	public float yangle;

	public float zangle;

	private IMountable ims;

	private float stepingAccum;

	private float fallingAccum;

	public float targetSwivelRad;

	public float nowSwivelRad;

	protected double prevAngleSwing;

	protected double prevPosXSwing;

	protected double prevPosZSwing;

	private float swivelaccum;

	public long LastJumpMs;

	public bool shouldSwivelFromMotion = true;

	public virtual bool DisplayChatMessages { get; set; }

	public Size2i AtlasSize => capi.EntityTextureAtlas.Size;

	public virtual TextureAtlasPosition this[string textureCode] => defaultTexSource[textureCode] ?? skinTexPos;

	public EntityShapeRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		EntityShapeRenderer entityShapeRenderer = this;
		eagent = entity as EntityAgent;
		DoRenderHeldItem = true;
		glitchAffected = true;
		glitchFlicker = entity.Properties.Attributes?["glitchFlicker"].AsBool() ?? false;
		frostable = entity.Properties.Attributes?["frostable"].AsBool(defaultValue: true) ?? true;
		shouldSwivelFromMotion = entity.Properties.Attributes?["shouldSwivelFromMotion"].AsBool(defaultValue: true) ?? true;
		frostAlphaAccum = (float)api.World.Rand.NextDouble();
		listenerId = api.Event.RegisterGameTickListener(UpdateDebugInfo, 250);
		OnDebugInfoChanged();
		if (DisplayChatMessages)
		{
			messageTextures = new List<MessageTexture>();
			api.Event.ChatMessage += OnChatMessage;
		}
		api.Event.ReloadShapes += entity.MarkShapeModified;
		getFrostAlpha = delegate
		{
			BlockPos asBlockPos = entity.Pos.AsBlockPos;
			ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(asBlockPos);
			if (climateAt == null)
			{
				return entityShapeRenderer.targetFrostAlpha;
			}
			float num = 1f - GameMath.Clamp((float)(api.World.BlockAccessor.GetDistanceToRainFall(asBlockPos, 5) - 2) / 3f, 0f, 1f);
			float num2 = GameMath.Clamp((Math.Max(0f, 0f - climateAt.Temperature) - 2f) / 5f, 0f, 1f) * num;
			if (num2 > 0f)
			{
				float num3 = Math.Max(api.World.BlockAccessor.GetClimateAt(asBlockPos, EnumGetClimateMode.ForSuppliedDateValues, api.World.Calendar.TotalDays - (double)(4f / api.World.Calendar.HoursPerDay)).Rainfall, climateAt.Rainfall);
				num2 *= num3;
			}
			return Math.Max(0f, num2);
		};
	}

	public override void OnEntityLoaded()
	{
		loaded = true;
		prevY = entity.Pos.Y;
		prevPosXSwing = entity.Pos.X;
		prevPosZSwing = entity.Pos.Z;
	}

	protected void OnChatMessage(int groupId, string message, EnumChatType chattype, string data)
	{
		if (data == null || !data.Contains("from:") || !(entity.Pos.SquareDistanceTo(capi.World.Player.Entity.Pos.XYZ) < 400.0) || message.Length <= 0)
		{
			return;
		}
		string[] parts = data.Split(new char[1] { ',' }, 2);
		if (parts.Length < 2)
		{
			return;
		}
		string[] partone = parts[0].Split(new char[1] { ':' }, 2);
		string[] parttwo = parts[1].Split(new char[1] { ':' }, 2);
		if (!(partone[0] != "from"))
		{
			int.TryParse(partone[1], out var entityid);
			if (entity.EntityId == entityid)
			{
				message = parttwo[1];
				message = message.Replace("&lt;", "<").Replace("&gt;", ">");
				LoadedTexture tex = capi.Gui.TextTexture.GenTextTexture(message, new CairoFont(25.0, GuiStyle.StandardFontName, ColorUtil.WhiteArgbDouble), 350, new TextBackground
				{
					FillColor = GuiStyle.DialogLightBgColor,
					Padding = 3,
					Radius = GuiStyle.ElementBGRadius
				}, EnumTextOrientation.Center);
				messageTextures.Insert(0, new MessageTexture
				{
					tex = tex,
					message = message,
					receivedTime = capi.World.ElapsedMilliseconds
				});
			}
		}
	}

	public virtual void TesselateShape()
	{
		if (loaded)
		{
			ims = entity.GetInterface<IMountable>();
			TesselateShape(onMeshReady);
		}
	}

	protected virtual void onMeshReady(MeshData meshData)
	{
		if (meshRefOpaque != null)
		{
			meshRefOpaque.Dispose();
			meshRefOpaque = null;
		}
		if (!capi.IsShuttingDown && meshData.VerticesCount > 0)
		{
			meshRefOpaque = capi.Render.UploadMultiTextureMesh(meshData);
		}
	}

	public virtual void TesselateShape(Action<MeshData> onMeshDataReady, string[] overrideSelectiveElements = null)
	{
		if (!loaded)
		{
			return;
		}
		CompositeShape compositeShape = ((OverrideCompositeShape != null) ? OverrideCompositeShape : entity.Properties.Client.Shape);
		Shape entityShape = ((OverrideEntityShape != null) ? OverrideEntityShape : entity.Properties.Client.LoadedShapeForEntity);
		if (entityShape == null)
		{
			return;
		}
		entity.OnTesselation(ref entityShape, compositeShape.Base.ToString());
		defaultTexSource = GetTextureSource();
		string[] ovse = overrideSelectiveElements ?? OverrideSelectiveElements;
		TyronThreadPool.QueueTask(delegate
		{
			MeshData meshdata;
			if (entity.Properties.Client.Shape.VoxelizeTexture)
			{
				int @int = entity.WatchedAttributes.GetInt("textureIndex");
				TextureAtlasPosition atlasPos = defaultTexSource["all"];
				CompositeTexture firstTexture = entity.Properties.Client.FirstTexture;
				CompositeTexture[] alternates = firstTexture.Alternates;
				CompositeTexture texture = ((@int == 0) ? firstTexture : alternates[@int % alternates.Length]);
				meshdata = capi.Tesselator.VoxelizeTexture(texture, capi.EntityTextureAtlas.Size, atlasPos);
				for (int i = 0; i < meshdata.xyz.Length; i += 3)
				{
					meshdata.xyz[i] -= 0.125f;
					meshdata.xyz[i + 1] -= 0.5f;
					meshdata.xyz[i + 2] += 0.0625f;
				}
			}
			else
			{
				try
				{
					TesselationMetaData meta = new TesselationMetaData
					{
						QuantityElements = compositeShape.QuantityElements,
						SelectiveElements = (ovse ?? compositeShape.SelectiveElements),
						IgnoreElements = compositeShape.IgnoreElements,
						TexSource = this,
						WithJointIds = true,
						WithDamageEffect = true,
						TypeForLogging = "entity",
						Rotation = new Vec3f(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ)
					};
					capi.Tesselator.TesselateShape(meta, entityShape, out meshdata);
					meshdata.Translate(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
				}
				catch (Exception e)
				{
					capi.World.Logger.Fatal("Failed tesselating entity {0} with id {1}. Entity will probably be invisible!.", entity.Code, entity.EntityId);
					capi.World.Logger.Fatal(e);
					return;
				}
			}
			capi.Event.EnqueueMainThreadTask(delegate
			{
				onMeshDataReady(meshdata);
				entity.OnTesselated();
			}, "uploadentitymesh");
			capi.TesselatorManager.ThreadDispose();
		});
	}

	protected virtual ITexPositionSource GetTextureSource()
	{
		return entity.GetTextureSource();
	}

	protected void UpdateDebugInfo(float dt)
	{
		OnDebugInfoChanged();
		entity.DebugAttributes.MarkClean();
	}

	protected void OnDebugInfoChanged()
	{
		bool showDebuginfo = capi.Settings.Bool["showEntityDebugInfo"];
		if (showDebuginfo && !entity.DebugAttributes.AllDirty && !entity.DebugAttributes.PartialDirty && debugTagTexture != null)
		{
			return;
		}
		if (debugTagTexture != null)
		{
			if (showDebuginfo && capi.World.Player.Entity.Pos.SquareDistanceTo(entity.Pos) > 225f && debugTagTexture.Width > 10)
			{
				return;
			}
			debugTagTexture.Dispose();
			debugTagTexture = null;
		}
		if (!showDebuginfo)
		{
			return;
		}
		StringBuilder text = new StringBuilder();
		foreach (KeyValuePair<string, IAttribute> val in entity.DebugAttributes)
		{
			text.AppendLine(val.Key + ": " + val.Value.ToString());
		}
		debugTagTexture = capi.Gui.TextTexture.GenUnscaledTextTexture(text.ToString(), new CairoFont(20.0, GuiStyle.StandardFontName, ColorUtil.WhiteArgbDouble), new TextBackground
		{
			FillColor = GuiStyle.DialogDefaultBgColor,
			Padding = 3,
			Radius = GuiStyle.ElementBGRadius
		});
		lastDebugInfoChangeMs = entity.World.ElapsedMilliseconds;
	}

	public override void BeforeRender(float dt)
	{
		if (!entity.ShapeFresh)
		{
			TesselateShape();
			capi.World.FrameProfiler.Mark("esr-tesseleateshape");
		}
		lightrgbs = capi.World.BlockAccessor.GetLightRGBs((int)(entity.Pos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1), (int)entity.Pos.InternalY, (int)(entity.Pos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1));
		if (entity.SelectionBox.Y2 > 1f)
		{
			Vec4f lightrgbs2 = capi.World.BlockAccessor.GetLightRGBs((int)(entity.Pos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1), (int)entity.Pos.InternalY + 1, (int)(entity.Pos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1));
			if (lightrgbs2.W > lightrgbs.W)
			{
				lightrgbs = lightrgbs2;
			}
		}
		if (meshRefOpaque == null)
		{
			return;
		}
		if (player == null && entity is EntityPlayer)
		{
			player = capi.World.PlayerByUid((entity as EntityPlayer).PlayerUID) as IClientPlayer;
		}
		if (capi.IsGamePaused)
		{
			return;
		}
		frostAlphaAccum += dt;
		if (frostAlphaAccum > 5f)
		{
			frostAlphaAccum = 0f;
			targetFrostAlpha = getFrostAlpha();
		}
		isSpectator = player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator;
		if (isSpectator)
		{
			return;
		}
		if (DisplayChatMessages && messageTextures.Count > 0)
		{
			MessageTexture tex = messageTextures.Last();
			if (capi.World.ElapsedMilliseconds > tex.receivedTime + 3500 + 100 * (tex.message.Length - 10))
			{
				messageTextures.RemoveAt(messageTextures.Count - 1);
				tex.tex.Dispose();
			}
		}
		determineSidewaysSwivel(dt);
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (!isSpectator)
		{
			loadModelMatrix(entity, dt, isShadowPass);
			Vec3d camPos = capi.World.Player.Entity.CameraPos;
			OriginPos.Set((float)(entity.Pos.X - camPos.X), (float)(entity.Pos.InternalY - camPos.Y), (float)(entity.Pos.Z - camPos.Z));
			if (isShadowPass)
			{
				DoRender3DAfterOIT(dt, isShadowPass: true);
			}
			if (DoRenderHeldItem && !entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("lie") && !isSpectator)
			{
				RenderHeldItem(dt, isShadowPass, right: false);
				RenderHeldItem(dt, isShadowPass, right: true);
			}
		}
	}

	protected virtual IShaderProgram getReadyShader()
	{
		IStandardShaderProgram standardShader = capi.Render.StandardShader;
		standardShader.Use();
		return standardShader;
	}

	protected virtual void RenderHeldItem(float dt, bool isShadowPass, bool right)
	{
		ItemSlot slot = ((!right) ? eagent?.LeftHandItemSlot : eagent?.RightHandItemSlot);
		ItemStack stack = slot?.Itemstack;
		if (stack != null && !(slot is ItemSlotSkill))
		{
			AttachmentPointAndPose apap = entity.AnimManager?.Animator?.GetAttachmentPointPose(right ? "RightHand" : "LeftHand");
			if (apap != null)
			{
				ItemRenderInfo renderInfo = capi.Render.GetItemStackRenderInfo(slot, right ? EnumItemRenderTarget.HandTp : EnumItemRenderTarget.HandTpOff, dt);
				RenderItem(dt, isShadowPass, stack, apap, renderInfo);
			}
		}
	}

	protected virtual void RenderItem(float dt, bool isShadowPass, ItemStack stack, AttachmentPointAndPose apap, ItemRenderInfo renderInfo)
	{
		IRenderAPI rapi = capi.Render;
		AttachmentPoint ap = apap.AttachPoint;
		IShaderProgram prog = null;
		if (renderInfo?.Transform == null)
		{
			return;
		}
		ModelTransform modelTransform = renderInfo.Transform.EnsureDefaultValues();
		Vec3f itemOrigin = modelTransform.Origin;
		Vec3f itemTranslation = modelTransform.Translation;
		Vec3f itemRotation = modelTransform.Rotation;
		Vec3f itemScaleXYZ = modelTransform.ScaleXYZ;
		ItemModelMat.Set(ModelMat).Mul(apap.AnimModelMatrix).Translate(itemOrigin.X, itemOrigin.Y, itemOrigin.Z)
			.Scale(itemScaleXYZ.X, itemScaleXYZ.Y, itemScaleXYZ.Z)
			.Translate(ap.PosX / 16.0 + (double)itemTranslation.X, ap.PosY / 16.0 + (double)itemTranslation.Y, ap.PosZ / 16.0 + (double)itemTranslation.Z)
			.RotateX((float)(ap.RotationX + (double)itemRotation.X) * ((float)Math.PI / 180f))
			.RotateY((float)(ap.RotationY + (double)itemRotation.Y) * ((float)Math.PI / 180f))
			.RotateZ((float)(ap.RotationZ + (double)itemRotation.Z) * ((float)Math.PI / 180f))
			.Translate(0f - itemOrigin.X, 0f - itemOrigin.Y, 0f - itemOrigin.Z);
		string samplername = "tex";
		if (isShadowPass)
		{
			samplername = "tex2d";
			rapi.CurrentActiveShader.BindTexture2D("tex2d", renderInfo.TextureId, 0);
			float[] mvpMat = Mat4f.Mul(ItemModelMat.Values, capi.Render.CurrentModelviewMatrix, ItemModelMat.Values);
			Mat4f.Mul(mvpMat, capi.Render.CurrentProjectionMatrix, mvpMat);
			capi.Render.CurrentActiveShader.UniformMatrix("mvpMatrix", mvpMat);
			capi.Render.CurrentActiveShader.Uniform("origin", new Vec3f());
		}
		else
		{
			prog = getReadyShader();
			prog.Uniform("dontWarpVertices", 0);
			prog.Uniform("addRenderFlags", 0);
			prog.Uniform("normalShaded", 1);
			prog.Uniform("tempGlowMode", stack.ItemAttributes?["tempGlowMode"].AsInt() ?? 0);
			prog.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
			prog.Uniform("alphaTest", renderInfo.AlphaTest);
			prog.Uniform("damageEffect", renderInfo.DamageEffect);
			prog.Uniform("overlayOpacity", renderInfo.OverlayOpacity);
			if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0f)
			{
				prog.BindTexture2D("tex2dOverlay", renderInfo.OverlayTexture.TextureId, 1);
				prog.Uniform("overlayTextureSize", new Vec2f(renderInfo.OverlayTexture.Width, renderInfo.OverlayTexture.Height));
				prog.Uniform("baseTextureSize", new Vec2f(renderInfo.TextureSize.Width, renderInfo.TextureSize.Height));
				TextureAtlasPosition texPos = rapi.GetTextureAtlasPosition(stack);
				prog.Uniform("baseUvOrigin", new Vec2f(texPos.x1, texPos.y1));
			}
			int num = (int)stack.Collectible.GetTemperature(capi.World, stack);
			float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int gi = GameMath.Clamp((num - 500) / 3, 0, 255);
			BakedCompositeTexture baked = (stack.Item?.FirstTexture ?? stack.Block?.FirstTextureInventory)?.Baked;
			Vec4f vec = ((baked == null) ? new Vec4f(1f, 1f, 1f, 1f) : ColorUtil.ToRGBAVec4f(capi.BlockTextureAtlas.GetAverageColor(baked.TextureSubId)));
			prog.Uniform("averageColor", vec);
			prog.Uniform("extraGlow", gi);
			prog.Uniform("rgbaAmbientIn", rapi.AmbientColor);
			prog.Uniform("rgbaLightIn", lightrgbs);
			prog.Uniform("rgbaGlowIn", new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)gi / 255f));
			prog.Uniform("rgbaFogIn", rapi.FogColor);
			prog.Uniform("fogMinIn", rapi.FogMin);
			prog.Uniform("fogDensityIn", rapi.FogDensity);
			prog.Uniform("normalShaded", renderInfo.NormalShaded ? 1 : 0);
			prog.UniformMatrix("projectionMatrix", rapi.CurrentProjectionMatrix);
			prog.UniformMatrix("viewMatrix", rapi.CameraMatrixOriginf);
			prog.UniformMatrix("modelMatrix", ItemModelMat.Values);
		}
		if (!renderInfo.CullFaces)
		{
			rapi.GlDisableCullFace();
		}
		rapi.RenderMultiTextureMesh(renderInfo.ModelRef, samplername);
		if (!isShadowPass)
		{
			prog.Uniform("tempGlowMode", 0);
		}
		if (!renderInfo.CullFaces)
		{
			rapi.GlEnableCullFace();
		}
		if (isShadowPass)
		{
			return;
		}
		prog.Uniform("damageEffect", 0f);
		prog.Stop();
		float windAffectednessAtPos = Math.Max(0f, 1f - (float)capi.World.BlockAccessor.GetDistanceToRainFall(entity.Pos.AsBlockPos) / 5f);
		AdvancedParticleProperties[] ParticleProperties = stack.Collectible?.ParticleProperties;
		if (stack.Collectible == null || capi.IsGamePaused)
		{
			return;
		}
		Vec4f pos = ItemModelMat.TransformVector(new Vec4f(stack.Collectible.TopMiddlePos.X, stack.Collectible.TopMiddlePos.Y, stack.Collectible.TopMiddlePos.Z, 1f));
		if (pMatrixHandFov != null)
		{
			Vec4f screenSpaceCoordNormalFov = new Matrixf().Set(pMatrixHandFov).Mul(rapi.CameraMatrixOriginf).TransformVector(pos);
			pos = new Matrixf(rapi.CameraMatrixOriginf).Invert().Mul(new Matrixf(pMatrixNormalFov).Invert()).TransformVector(screenSpaceCoordNormalFov);
		}
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		accum += dt;
		if (ParticleProperties != null && ParticleProperties.Length != 0 && accum > 0.05f)
		{
			accum %= 0.025f;
			foreach (AdvancedParticleProperties bps in ParticleProperties)
			{
				bps.WindAffectednesAtPos = windAffectednessAtPos;
				bps.WindAffectednes = windAffectednessAtPos;
				bps.basePos.X = (double)pos.X + entityPlayer.CameraPos.X;
				bps.basePos.Y = (double)pos.Y + entityPlayer.CameraPos.Y;
				bps.basePos.Z = (double)pos.Z + entityPlayer.CameraPos.Z;
				eagent.World.SpawnParticles(bps);
			}
		}
	}

	public override void RenderToGui(float dt, double posX, double posY, double posZ, float yawDelta, float size)
	{
		loadModelMatrixForGui(entity, posX, posY, posZ, yawDelta, size);
		if (meshRefOpaque != null)
		{
			capi.Render.CurrentActiveShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			capi.Render.CurrentActiveShader.UniformMatrix("modelViewMatrix", Mat4f.Mul(ModelMat, capi.Render.CurrentModelviewMatrix, ModelMat));
			capi.Render.RenderMultiTextureMesh(meshRefOpaque, "tex2d");
		}
		if (!entity.ShapeFresh)
		{
			TesselateShape();
		}
	}

	public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
	{
		if (!isSpectator && meshRefOpaque != null)
		{
			IShaderProgram prog = capi.Render.CurrentActiveShader;
			if (isShadowPass)
			{
				Mat4f.Mul(tmpMvMat, capi.Render.CurrentModelviewMatrix, ModelMat);
				prog.UniformMatrix("modelViewMatrix", tmpMvMat);
			}
			else
			{
				frostAlpha += (targetFrostAlpha - frostAlpha) * dt / 6f;
				float fa = (float)Math.Round(GameMath.Clamp(frostAlpha, 0f, 1f), 4);
				prog.Uniform("rgbaLightIn", lightrgbs);
				prog.Uniform("extraGlow", entity.Properties.Client.GlowLevel);
				prog.UniformMatrix("modelMatrix", ModelMat);
				prog.UniformMatrix("viewMatrix", capi.Render.CurrentModelviewMatrix);
				prog.Uniform("addRenderFlags", AddRenderFlags);
				prog.Uniform("windWaveIntensity", (float)WindWaveIntensity);
				prog.Uniform("entityId", (int)entity.EntityId);
				prog.Uniform("glitchFlicker", glitchFlicker ? 1 : 0);
				prog.Uniform("frostAlpha", fa);
				prog.Uniform("waterWaveCounter", capi.Render.ShaderUniforms.WaterWaveCounter);
				color.R = (float)((entity.RenderColor >> 16) & 0xFF) / 255f;
				color.G = (float)((entity.RenderColor >> 8) & 0xFF) / 255f;
				color.B = (float)(entity.RenderColor & 0xFF) / 255f;
				color.A = (float)((entity.RenderColor >> 24) & 0xFF) / 255f;
				prog.Uniform("renderColor", color);
				double @double = entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
				double plrStab = capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
				double stabMin = Math.Min(@double, plrStab);
				float strength = (float)(glitchAffected ? Math.Max(0.0, 1.0 - 2.5 * stabMin) : 0.0);
				prog.Uniform("glitchEffectStrength", strength);
			}
			prog.UBOs["Animation"].Update((object)entity.AnimManager.Animator.Matrices, 0, entity.AnimManager.Animator.MaxJointId * 16 * 4);
			if (meshRefOpaque != null)
			{
				capi.Render.RenderMultiTextureMesh(meshRefOpaque, "entityTex");
			}
		}
	}

	public override void DoRender2D(float dt)
	{
		if (isSpectator || (debugTagTexture == null && messageTextures == null))
		{
			return;
		}
		EntityPlayer obj = entity as EntityPlayer;
		if (obj != null && obj.ServerControls.Sneak && debugTagTexture == null)
		{
			return;
		}
		IRenderAPI rapi = capi.Render;
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Vec3d pos = MatrixToolsd.Project(getAboveHeadPosition(entityPlayer), rapi.PerspectiveProjectionMat, rapi.PerspectiveViewMat, rapi.FrameWidth, rapi.FrameHeight);
		if (pos.Z < 0.0)
		{
			return;
		}
		float scale = 4f / Math.Max(1f, (float)pos.Z);
		float cappedScale = Math.Min(1f, scale);
		if (cappedScale > 0.75f)
		{
			cappedScale = 0.75f + (cappedScale - 0.75f) / 2f;
		}
		float offY = 0f;
		entityPlayer.Pos.SquareDistanceTo(entity.Pos);
		if (debugTagTexture != null)
		{
			float posx2 = (float)pos.X - cappedScale * (float)debugTagTexture.Width / 2f;
			float posy2 = (float)rapi.FrameHeight - (float)pos.Y - (offY + (float)debugTagTexture.Height) * Math.Max(0f, cappedScale);
			rapi.Render2DTexture(debugTagTexture.TextureId, posx2, posy2 - offY, cappedScale * (float)debugTagTexture.Width, cappedScale * (float)debugTagTexture.Height, 20f);
		}
		if (messageTextures == null)
		{
			return;
		}
		offY += 0f;
		foreach (MessageTexture mt in messageTextures)
		{
			offY += (float)mt.tex.Height * cappedScale + 4f;
			float posx = (float)pos.X - cappedScale * (float)mt.tex.Width / 2f;
			float posy = (float)pos.Y + offY;
			rapi.Render2DTexture(mt.tex.TextureId, posx, (float)rapi.FrameHeight - posy, cappedScale * (float)mt.tex.Width, cappedScale * (float)mt.tex.Height, 20f);
		}
	}

	public virtual Vec3d getAboveHeadPosition(EntityPlayer entityPlayer)
	{
		IMountableSeat thisMount = (entity as EntityAgent)?.MountedOn;
		IMountableSeat selfMount = entityPlayer.MountedOn;
		Vec3d aboveHeadPos;
		if (thisMount?.MountSupplier != null && thisMount.MountSupplier == selfMount?.MountSupplier)
		{
			Vec3d mpos = thisMount.SeatPosition.XYZ - selfMount.SeatPosition.XYZ;
			aboveHeadPos = new Vec3d(entityPlayer.CameraPos.X + entityPlayer.LocalEyePos.X, entityPlayer.CameraPos.Y + 0.4 + entityPlayer.LocalEyePos.Y, entityPlayer.CameraPos.Z + entityPlayer.LocalEyePos.Z);
			aboveHeadPos.Add(mpos);
		}
		else
		{
			aboveHeadPos = new Vec3d(entity.Pos.X, entity.Pos.InternalY + (double)entity.SelectionBox.Y2 + 0.2, entity.Pos.Z);
		}
		double offX = entity.SelectionBox.X2 - entity.OriginSelectionBox.X2;
		double offZ = entity.SelectionBox.Z2 - entity.OriginSelectionBox.Z2;
		aboveHeadPos.Add(offX, 0.0, offZ);
		return aboveHeadPos;
	}

	public void loadModelMatrix(Entity entity, float dt, bool isShadowPass)
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		IMountableSeat seat;
		if (ims != null && (seat = ims.GetSeatOfMountedEntity(entityPlayer)) != null)
		{
			Vec3d offset = seat.SeatPosition.XYZ - seat.MountSupplier.Position.XYZ;
			ModelMat = Mat4f.Translate(ModelMat, ModelMat, 0f - (float)offset.X, 0f - (float)offset.Y, 0f - (float)offset.Z);
		}
		else
		{
			seat = eagent?.MountedOn;
			if (ims != null && seat != null)
			{
				if (entityPlayer.MountedOn?.Entity == eagent.MountedOn.Entity)
				{
					EntityPos selfMountPos = entityPlayer.MountedOn.SeatPosition;
					Mat4f.Translate(ModelMat, ModelMat, (float)(seat.SeatPosition.X - selfMountPos.X), (float)(seat.SeatPosition.InternalY - selfMountPos.Y), (float)(seat.SeatPosition.Z - selfMountPos.Z));
				}
				else
				{
					Mat4f.Translate(ModelMat, ModelMat, (float)(seat.SeatPosition.X - entityPlayer.CameraPos.X), (float)(seat.SeatPosition.InternalY - entityPlayer.CameraPos.Y), (float)(seat.SeatPosition.Z - entityPlayer.CameraPos.Z));
				}
			}
			else
			{
				Mat4f.Translate(ModelMat, ModelMat, (float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));
			}
		}
		float rotX = entity.Properties.Client.Shape?.rotateX ?? 0f;
		float rotY = entity.Properties.Client.Shape?.rotateY ?? 0f;
		float rotZ = entity.Properties.Client.Shape?.rotateZ ?? 0f;
		Mat4f.Translate(ModelMat, ModelMat, 0f, entity.SelectionBox.Y2 / 2f, 0f);
		if (!isShadowPass)
		{
			updateStepPitch(dt);
		}
		double[] quat = Quaterniond.Create();
		float bodyPitch = ((entity is EntityPlayer) ? 0f : entity.Pos.Pitch);
		float yaw = entity.Pos.Yaw + (rotY + 90f) * ((float)Math.PI / 180f);
		BlockFacing climbonfacing = entity.ClimbingOnFace;
		int num;
		if (entity.Properties.RotateModelOnClimb)
		{
			BlockFacing climbingOnFace = entity.ClimbingOnFace;
			num = ((climbingOnFace != null && climbingOnFace.Axis == EnumAxis.X) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool fuglyHack = (byte)num != 0;
		float sign = -1f;
		Quaterniond.RotateX(quat, quat, bodyPitch + rotX * ((float)Math.PI / 180f) + (fuglyHack ? (yaw * sign) : 0f));
		Quaterniond.RotateY(quat, quat, fuglyHack ? 0f : yaw);
		Quaterniond.RotateZ(quat, quat, (double)entity.Pos.Roll + stepPitch + (double)(rotZ * ((float)Math.PI / 180f)) + (double)(fuglyHack ? ((float)Math.PI / 2f * (float)((climbonfacing != BlockFacing.WEST) ? 1 : (-1))) : 0f));
		Quaterniond.RotateX(quat, quat, xangle);
		Quaterniond.RotateY(quat, quat, yangle);
		Quaterniond.RotateZ(quat, quat, zangle);
		float[] qf = new float[quat.Length];
		for (int i = 0; i < quat.Length; i++)
		{
			qf[i] = (float)quat[i];
		}
		Mat4f.Mul(ModelMat, ModelMat, Mat4f.FromQuat(Mat4f.Create(), qf));
		if (shouldSwivelFromMotion)
		{
			Mat4f.RotateX(ModelMat, ModelMat, nowSwivelRad);
		}
		float scale = entity.Properties.Client.Size;
		Mat4f.Translate(ModelMat, ModelMat, 0f, (0f - entity.SelectionBox.Y2) / 2f, 0f);
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { scale, scale, scale });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	protected void loadModelMatrixForGui(Entity entity, double posX, double posY, double posZ, double yawDelta, float size)
	{
		Mat4f.Identity(ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, (float)posX, (float)posY, (float)posZ);
		Mat4f.Translate(ModelMat, ModelMat, size, 2f * size, 0f);
		float rotX = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateX : 0f);
		float rotY = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateY : 0f);
		float rotZ = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateZ : 0f);
		Mat4f.RotateX(ModelMat, ModelMat, (float)Math.PI + rotX * ((float)Math.PI / 180f));
		Mat4f.RotateY(ModelMat, ModelMat, (float)yawDelta + rotY * ((float)Math.PI / 180f));
		Mat4f.RotateZ(ModelMat, ModelMat, rotZ * ((float)Math.PI / 180f));
		float scale = entity.Properties.Client.Size * size;
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { scale, scale, scale });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	private void updateStepPitch(float dt)
	{
		if (!entity.CanStepPitch)
		{
			stepPitch = 0.0;
			return;
		}
		double targetPitch = 0.0;
		if (LastJumpMs > 0)
		{
			targetPitch = 0.0;
			if (capi.InWorldEllapsedMilliseconds - LastJumpMs > 500 && entity.OnGround)
			{
				LastJumpMs = -1L;
			}
		}
		else
		{
			prevYAccum += dt;
			if (prevYAccum > 0.20000000298023224)
			{
				prevYAccum = 0.0;
				prevY = entity.Pos.Y;
			}
			EntityAgent entityAgent = eagent;
			if (entityAgent != null && !entityAgent.Alive)
			{
				stepPitch = Math.Max(0.0, stepPitch - (double)(2f * dt));
			}
			if (eagent == null || entity.Properties.CanClimbAnywhere || !eagent.Alive || entity.Attributes.GetInt("dmgkb") != 0 || !entity.Properties.Client.PitchStep)
			{
				return;
			}
			if (entity.Properties.Habitat == EnumHabitat.Air || eagent.Controls.IsClimbing)
			{
				stepPitch = GameMath.Clamp(entity.Pos.Y - prevY + 0.1, 0.0, 0.3) - GameMath.Clamp(prevY - entity.Pos.Y - 0.1, 0.0, 0.3);
				return;
			}
			double num = entity.Pos.Y - prevY;
			bool steppingUp = num > 0.02 && !entity.FeetInLiquid && !entity.Swimming && !entity.OnGround;
			bool num2 = num < 0.0 && !entity.OnGround && !entity.FeetInLiquid && !entity.Swimming;
			stepingAccum = Math.Max(0f, stepingAccum - dt);
			fallingAccum = Math.Max(0f, fallingAccum - dt);
			if (steppingUp)
			{
				stepingAccum = 0.2f;
			}
			if (num2)
			{
				fallingAccum = 0.2f;
			}
			if (stepingAccum > 0f)
			{
				targetPitch = -0.5;
			}
			else if (fallingAccum > 0f)
			{
				targetPitch = 0.5;
			}
		}
		stepPitch += (targetPitch - stepPitch) * (double)dt * 5.0;
	}

	protected virtual void determineSidewaysSwivel(float dt)
	{
		if (!shouldSwivelFromMotion)
		{
			if (eagent != null)
			{
				eagent.sidewaysSwivelAngle = 0f;
			}
			return;
		}
		if (!entity.CanSwivel)
		{
			nowSwivelRad = 0f;
			targetSwivelRad = 0f;
			if (eagent != null)
			{
				eagent.sidewaysSwivelAngle = 0f;
			}
			return;
		}
		swivelaccum += dt;
		if ((double)swivelaccum > 0.1 && entity.CanSwivelNow)
		{
			double dx = entity.Pos.X - prevPosXSwing;
			double dz = entity.Pos.Z - prevPosZSwing;
			IMountable im = eagent?.GetInterface<IMountable>();
			if (im?.Controller != null)
			{
				IMountableSeat seat = im.GetSeatOfMountedEntity(im.Controller);
				if (seat != null && !seat.Controls.Left && !seat.Controls.Right)
				{
					dx = 0.0;
					dz = 0.0;
				}
			}
			double nowAngle = Math.Atan2(dz, dx);
			double speed = Math.Sqrt(dx * dx + dz * dz);
			swivelaccum = 0f;
			float anglechange = GameMath.AngleRadDistance((float)nowAngle, (float)prevAngleSwing);
			if (Math.Abs(anglechange) < (float)Math.PI / 2f)
			{
				targetSwivelRad = GameMath.Clamp((float)speed * anglechange * 3f, -0.4f, 0.4f);
			}
			else
			{
				targetSwivelRad = 0f;
			}
			prevAngleSwing = nowAngle;
			prevPosXSwing = entity.Pos.X;
			prevPosZSwing = entity.Pos.Z;
		}
		float diff = GameMath.AngleRadDistance(nowSwivelRad, targetSwivelRad);
		nowSwivelRad += GameMath.Clamp(diff * dt * 2f, -0.15f, 0.15f);
		if (eagent != null)
		{
			eagent.sidewaysSwivelAngle = nowSwivelRad;
		}
	}

	public override void Dispose()
	{
		capi.World.UnregisterGameTickListener(listenerId);
		listenerId = 0L;
		if (meshRefOpaque != null)
		{
			meshRefOpaque.Dispose();
			meshRefOpaque = null;
		}
		if (debugTagTexture != null)
		{
			debugTagTexture.Dispose();
			debugTagTexture = null;
		}
		capi.Event.ReloadShapes -= entity.MarkShapeModified;
		if (DisplayChatMessages)
		{
			capi.Event.ChatMessage -= OnChatMessage;
		}
	}
}
