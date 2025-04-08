using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class AnimatableRenderer : IRenderer, IDisposable
{
	protected Vec3d pos;

	protected ICoreClientAPI capi;

	protected AnimatorBase animator;

	protected Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode = new Dictionary<string, AnimationMetaData>();

	public MultiTextureMeshRef mtmeshref;

	public MeshRef meshref;

	public int textureId;

	public float[] ModelMat = Mat4f.Create();

	public float[] CustomTransform;

	public bool ShouldRender;

	public bool StabilityAffected = true;

	public bool LightAffected = true;

	public float FogAffectedness = 1f;

	public Vec3f rotationDeg;

	public float ScaleX = 1f;

	public float ScaleY = 1f;

	public float ScaleZ = 1f;

	public Vec4f renderColor = ColorUtil.WhiteArgbVec.Clone();

	public bool backfaceCulling = true;

	public double RenderOrder => 1.0;

	public int RenderRange => 99;

	[Obsolete("Use constructor with MultiTextureMeshRef instead, the standard MeshRef texturing breaks when there is multiple texture atlasses")]
	public AnimatableRenderer(ICoreClientAPI capi, Vec3d pos, Vec3f rotationDeg, AnimatorBase animator, Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, MeshRef meshref, int textureId, EnumRenderStage renderStage = EnumRenderStage.Opaque)
	{
		AnimatableRenderer renderer = this;
		this.pos = pos;
		this.capi = capi;
		this.animator = animator;
		this.activeAnimationsByAnimCode = activeAnimationsByAnimCode;
		this.meshref = meshref;
		this.rotationDeg = rotationDeg;
		this.textureId = textureId;
		if (rotationDeg == null)
		{
			this.rotationDeg = new Vec3f();
		}
		capi.Event.EnqueueMainThreadTask(delegate
		{
			capi.Event.RegisterRenderer(renderer, renderStage, "animatable");
			capi.Event.RegisterRenderer(renderer, EnumRenderStage.ShadowFar, "animatable");
			capi.Event.RegisterRenderer(renderer, EnumRenderStage.ShadowNear, "animatable");
		}, "registerrenderers");
	}

	public AnimatableRenderer(ICoreClientAPI capi, Vec3d pos, Vec3f rotationDeg, AnimatorBase animator, Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, MultiTextureMeshRef meshref, EnumRenderStage renderStage = EnumRenderStage.Opaque)
	{
		AnimatableRenderer renderer = this;
		this.pos = pos;
		this.capi = capi;
		this.animator = animator;
		this.activeAnimationsByAnimCode = activeAnimationsByAnimCode;
		mtmeshref = meshref;
		this.rotationDeg = rotationDeg;
		if (rotationDeg == null)
		{
			this.rotationDeg = new Vec3f();
		}
		capi.Event.EnqueueMainThreadTask(delegate
		{
			capi.Event.RegisterRenderer(renderer, renderStage, "animatable");
			capi.Event.RegisterRenderer(renderer, EnumRenderStage.ShadowFar, "animatable");
			capi.Event.RegisterRenderer(renderer, EnumRenderStage.ShadowNear, "animatable");
		}, "registerrenderers");
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (!ShouldRender || (mtmeshref != null && (mtmeshref.Disposed || !mtmeshref.Initialized)) || (meshref != null && (meshref.Disposed || !meshref.Initialized)))
		{
			return;
		}
		bool shadowPass = stage != EnumRenderStage.Opaque;
		capi.Render.GLDepthMask(on: true);
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		Mat4f.Translate(ModelMat, ModelMat, (float)(pos.X - entityPlayer.CameraPos.X), (float)(pos.Y - entityPlayer.CameraPos.Y), (float)(pos.Z - entityPlayer.CameraPos.Z));
		if (CustomTransform != null)
		{
			Mat4f.Multiply(ModelMat, ModelMat, CustomTransform);
		}
		else
		{
			Mat4f.Translate(ModelMat, ModelMat, 0.5f, 0f, 0.5f);
			Mat4f.Scale(ModelMat, ModelMat, ScaleX, ScaleY, ScaleZ);
			Mat4f.RotateY(ModelMat, ModelMat, rotationDeg.Y * ((float)Math.PI / 180f));
			Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
		}
		IRenderAPI rpi = capi.Render;
		IShaderProgram currentActiveShader = rpi.CurrentActiveShader;
		currentActiveShader?.Stop();
		capi.Render.GlDisableCullFace();
		IShaderProgram prog = rpi.GetEngineShader(shadowPass ? EnumShaderProgram.Shadowmapentityanimated : EnumShaderProgram.Entityanimated);
		prog.Use();
		Vec4f lightrgbs = (LightAffected ? capi.World.BlockAccessor.GetLightRGBs((int)pos.X, (int)pos.Y, (int)pos.Z) : ColorUtil.WhiteArgbVec);
		rpi.GlToggleBlend(blend: true);
		if (!shadowPass)
		{
			prog.Uniform("extraGlow", 0);
			prog.Uniform("rgbaAmbientIn", rpi.AmbientColor);
			prog.Uniform("rgbaFogIn", rpi.FogColor);
			prog.Uniform("fogMinIn", rpi.FogMin * FogAffectedness);
			prog.Uniform("fogDensityIn", rpi.FogDensity * FogAffectedness);
			prog.Uniform("rgbaLightIn", lightrgbs);
			prog.Uniform("renderColor", renderColor);
			prog.Uniform("alphaTest", 0.1f);
			prog.UniformMatrix("modelMatrix", ModelMat);
			prog.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
			prog.Uniform("windWaveIntensity", 0f);
			prog.Uniform("glitchEffectStrength", 0f);
			prog.Uniform("frostAlpha", 0f);
			if (!StabilityAffected)
			{
				prog.Uniform("globalWarpIntensity", 0f);
				prog.Uniform("glitchWaviness", 0f);
			}
		}
		else
		{
			prog.UniformMatrix("modelViewMatrix", Mat4f.Mul(new float[16], capi.Render.CurrentModelviewMatrix, ModelMat));
		}
		prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
		prog.Uniform("addRenderFlags", 0);
		prog.UBOs["Animation"].Update((object)animator.Matrices, 0, animator.MaxJointId * 16 * 4);
		if ((stage == EnumRenderStage.Opaque || stage == EnumRenderStage.ShadowNear) && !backfaceCulling)
		{
			capi.Render.GlDisableCullFace();
		}
		if (meshref != null)
		{
			prog.BindTexture2D("entityTex", textureId, 0);
			capi.Render.RenderMesh(meshref);
		}
		else
		{
			capi.Render.RenderMultiTextureMesh(mtmeshref, "entityTex");
		}
		if ((stage == EnumRenderStage.Opaque || stage == EnumRenderStage.ShadowNear) && !backfaceCulling)
		{
			capi.Render.GlEnableCullFace();
		}
		prog.Stop();
		currentActiveShader?.Use();
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		capi.Event.UnregisterRenderer(this, EnumRenderStage.ShadowFar);
		capi.Event.UnregisterRenderer(this, EnumRenderStage.ShadowNear);
		mtmeshref?.Dispose();
		meshref?.Dispose();
	}
}
