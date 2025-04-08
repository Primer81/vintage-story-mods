using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityPlayerShapeRenderer : EntityShapeRenderer
{
	private MultiTextureMeshRef firstPersonMeshRef;

	private MultiTextureMeshRef thirdPersonMeshRef;

	private bool watcherRegistered;

	private EntityPlayer entityPlayer;

	private ModSystemFpHands modSys;

	private RenderMode renderMode;

	private float smoothedBodyYaw;

	private bool previfpMode;

	private static ModelTransform DefaultTongTransform = new ModelTransform
	{
		Translation = new Vec3f(-0.68f, -0.52f, -0.6f),
		Rotation = new Vec3f(-26f, -13f, -88f),
		Origin = new Vec3f(0.5f, 0f, 0.5f),
		Scale = 0.7f
	};

	public float? HeldItemPitchFollowOverride { get; set; }

	protected bool IsSelf => entity.EntityId == capi.World.Player.Entity.EntityId;

	public override bool DisplayChatMessages => true;

	public virtual float HandRenderFov => (float)capi.Settings.Int["fpHandsFoV"] * ((float)Math.PI / 180f);

	public EntityPlayerShapeRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		entityPlayer = entity as EntityPlayer;
		modSys = api.ModLoader.GetModSystem<ModSystemFpHands>();
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
	}

	public override void TesselateShape()
	{
		if (entityPlayer.GetBehavior<EntityBehaviorPlayerInventory>().Inventory == null)
		{
			return;
		}
		defaultTexSource = GetTextureSource();
		Tesselate();
		if (watcherRegistered)
		{
			return;
		}
		previfpMode = capi.Settings.Bool["immersiveFpMode"];
		if (IsSelf)
		{
			capi.Settings.Bool.AddWatcher("immersiveFpMode", delegate(bool on)
			{
				entity.MarkShapeModified();
				(entityPlayer.AnimManager as PlayerAnimationManager).OnIfpModeChanged(previfpMode, on);
			});
		}
		watcherRegistered = true;
	}

	protected override void onMeshReady(MeshData meshData)
	{
		base.onMeshReady(meshData);
		if (!IsSelf)
		{
			thirdPersonMeshRef = meshRefOpaque;
		}
	}

	public void Tesselate()
	{
		if (!IsSelf)
		{
			base.TesselateShape();
		}
		else
		{
			if (!loaded)
			{
				return;
			}
			TesselateShape(delegate(MeshData meshData)
			{
				disposeMeshes();
				if (!capi.IsShuttingDown && meshData.VerticesCount > 0)
				{
					MeshData meshData2 = meshData.EmptyClone();
					thirdPersonMeshRef = capi.Render.UploadMultiTextureMesh(meshData);
					determineRenderMode();
					if (renderMode == RenderMode.ImmersiveFirstPerson)
					{
						HashSet<int> skipJointIds = new HashSet<int>();
						loadJointIdsRecursive(entity.AnimManager.Animator.GetPosebyName("Neck"), skipJointIds);
						meshData2.AddMeshData(meshData, (int i) => !skipJointIds.Contains(meshData.CustomInts.Values[i * 4]));
					}
					else
					{
						HashSet<int> includeJointIds = new HashSet<int>();
						loadJointIdsRecursive(entity.AnimManager.Animator.GetPosebyName("UpperArmL"), includeJointIds);
						loadJointIdsRecursive(entity.AnimManager.Animator.GetPosebyName("UpperArmR"), includeJointIds);
						meshData2.AddMeshData(meshData, (int i) => includeJointIds.Contains(meshData.CustomInts.Values[i * 4]));
					}
					firstPersonMeshRef = capi.Render.UploadMultiTextureMesh(meshData2);
				}
			});
		}
	}

	private void loadJointIdsRecursive(ElementPose elementPose, HashSet<int> outList)
	{
		outList.Add(elementPose.ForElement.JointId);
		foreach (ElementPose childpose in elementPose.ChildElementPoses)
		{
			loadJointIdsRecursive(childpose, outList);
		}
	}

	private void disposeMeshes()
	{
		if (firstPersonMeshRef != null)
		{
			firstPersonMeshRef.Dispose();
			firstPersonMeshRef = null;
		}
		if (thirdPersonMeshRef != null)
		{
			thirdPersonMeshRef.Dispose();
			thirdPersonMeshRef = null;
		}
		meshRefOpaque = null;
	}

	public override void BeforeRender(float dt)
	{
		RenderMode prevRenderMode = renderMode;
		determineRenderMode();
		if ((prevRenderMode == RenderMode.FirstPerson && renderMode == RenderMode.ImmersiveFirstPerson) || (prevRenderMode == RenderMode.ImmersiveFirstPerson && renderMode == RenderMode.FirstPerson))
		{
			entity.MarkShapeModified();
			(entityPlayer.AnimManager as PlayerAnimationManager).OnIfpModeChanged(previfpMode, renderMode == RenderMode.ImmersiveFirstPerson);
		}
		base.BeforeRender(dt);
	}

	private void determineRenderMode()
	{
		if (IsSelf && capi.Render.CameraType == EnumCameraMode.FirstPerson)
		{
			if (capi.Settings.Bool["immersiveFpMode"] && !capi.Render.CameraStuck)
			{
				ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("tiredness");
				if (treeAttribute == null || treeAttribute.GetInt("isSleeping") != 1)
				{
					renderMode = RenderMode.ImmersiveFirstPerson;
					return;
				}
			}
			renderMode = RenderMode.FirstPerson;
		}
		else
		{
			renderMode = RenderMode.ThirdPerson;
		}
	}

	public override void RenderToGui(float dt, double posX, double posY, double posZ, float yawDelta, float size)
	{
		if (IsSelf)
		{
			meshRefOpaque = thirdPersonMeshRef;
		}
		base.RenderToGui(dt, posX, posY, posZ, yawDelta, size);
	}

	public override void DoRender2D(float dt)
	{
		if (!IsSelf || capi.Render.CameraType != 0)
		{
			base.DoRender2D(dt);
		}
	}

	public override Vec3d getAboveHeadPosition(EntityPlayer entityPlayer)
	{
		if (IsSelf)
		{
			return new Vec3d(entityPlayer.CameraPos.X + entityPlayer.LocalEyePos.X, entityPlayer.CameraPos.Y + 0.4 + entityPlayer.LocalEyePos.Y, entityPlayer.CameraPos.Z + entityPlayer.LocalEyePos.Z);
		}
		return base.getAboveHeadPosition(entityPlayer);
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (IsSelf)
		{
			entityPlayer.selfNowShadowPass = isShadowPass;
		}
		bool isHandRender = renderMode == RenderMode.FirstPerson && !isShadowPass;
		loadModelMatrixForPlayer(entity, IsSelf, dt, isShadowPass);
		if (IsSelf && (renderMode == RenderMode.ImmersiveFirstPerson || isShadowPass))
		{
			OriginPos.Set(0f, 0f, 0f);
		}
		if (isHandRender && capi.HideGuis)
		{
			return;
		}
		if (isHandRender)
		{
			pMatrixNormalFov = (float[])capi.Render.CurrentProjectionMatrix.Clone();
			capi.Render.Set3DProjection(capi.Render.ShaderUniforms.ZFar, HandRenderFov);
			pMatrixHandFov = (float[])capi.Render.CurrentProjectionMatrix.Clone();
		}
		else
		{
			pMatrixHandFov = null;
			pMatrixNormalFov = null;
		}
		if (isShadowPass)
		{
			DoRender3DAfterOIT(dt, isShadowPass: true);
		}
		if (DoRenderHeldItem && !entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("lie") && !isSpectator)
		{
			RenderHeldItem(dt, isShadowPass, right: false);
			RenderHeldItem(dt, isShadowPass, right: true);
		}
		if (isHandRender)
		{
			if (!capi.Settings.Bool["hideFpHands"] && !entityPlayer.GetBehavior<EntityBehaviorTiredness>().IsSleeping)
			{
				IShaderProgram prog = modSys.fpModeHandShader;
				meshRefOpaque = firstPersonMeshRef;
				prog.Use();
				prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
				prog.Uniform("rgbaFogIn", capi.Render.FogColor);
				prog.Uniform("fogMinIn", capi.Render.FogMin);
				prog.Uniform("fogDensityIn", capi.Render.FogDensity);
				prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
				prog.Uniform("alphaTest", 0.05f);
				prog.Uniform("lightPosition", capi.Render.ShaderUniforms.LightPosition3D);
				prog.Uniform("depthOffset", -0.3f - GameMath.Max(0f, (float)capi.Settings.Int["fieldOfView"] / 90f - 1f) / 2f);
				capi.Render.GlPushMatrix();
				capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
				base.DoRender3DOpaqueBatched(dt, isShadowPass: false);
				capi.Render.GlPopMatrix();
				prog.Stop();
			}
			capi.Render.Reset3DProjection();
		}
	}

	protected override IShaderProgram getReadyShader()
	{
		if (!entityPlayer.selfNowShadowPass && renderMode == RenderMode.FirstPerson)
		{
			IShaderProgram prog = modSys.fpModeItemShader;
			prog.Use();
			prog.Uniform("depthOffset", -0.3f - GameMath.Max(0f, (float)capi.Settings.Int["fieldOfView"] / 90f - 1f) / 2f);
			prog.Uniform("ssaoAttn", 1f);
			return prog;
		}
		return base.getReadyShader();
	}

	protected override void RenderHeldItem(float dt, bool isShadowPass, bool right)
	{
		if (IsSelf)
		{
			entityPlayer.selfNowShadowPass = isShadowPass;
		}
		if (right)
		{
			ItemSlot slot = eagent?.RightHandItemSlot;
			if (slot is ItemSlotSkill)
			{
				return;
			}
			ItemStack stack = slot?.Itemstack;
			ItemStack tongStack = eagent?.LeftHandItemSlot?.Itemstack;
			if (stack != null && stack.Collectible.GetTemperature(entity.World, stack) > 200f && tongStack != null && (tongStack.ItemAttributes?.IsTrue("heatResistant")).GetValueOrDefault())
			{
				AttachmentPointAndPose apap = entity.AnimManager?.Animator?.GetAttachmentPointPose("LeftHand");
				ItemRenderInfo renderInfo = capi.Render.GetItemStackRenderInfo(slot, EnumItemRenderTarget.HandTpOff, dt);
				renderInfo.Transform = stack.ItemAttributes?["onTongTransform"].AsObject(DefaultTongTransform) ?? DefaultTongTransform;
				RenderItem(dt, isShadowPass, stack, apap, renderInfo);
				return;
			}
		}
		bool ishandrender = renderMode == RenderMode.FirstPerson;
		if ((ishandrender && !capi.Settings.Bool["hideFpHands"]) || !ishandrender)
		{
			base.RenderHeldItem(dt, isShadowPass, right);
		}
	}

	public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
	{
		if (renderMode != 0 || isShadowPass)
		{
			if (isShadowPass)
			{
				meshRefOpaque = thirdPersonMeshRef;
			}
			else
			{
				meshRefOpaque = ((renderMode == RenderMode.ImmersiveFirstPerson) ? firstPersonMeshRef : thirdPersonMeshRef);
			}
			base.DoRender3DOpaqueBatched(dt, isShadowPass);
		}
	}

	public void loadModelMatrixForPlayer(Entity entity, bool isSelf, float dt, bool isShadowPass)
	{
		EntityPlayer selfEplr = capi.World.Player.Entity;
		Mat4f.Identity(ModelMat);
		if (isSelf)
		{
			Matrixf tf = selfEplr.MountedOn?.RenderTransform;
			if (tf != null)
			{
				ModelMat = Mat4f.Mul(ModelMat, ModelMat, tf.Values);
			}
		}
		if (!isSelf)
		{
			Vec3f off = GetOtherPlayerRenderOffset();
			Mat4f.Translate(ModelMat, ModelMat, off.X, off.Y, off.Z);
		}
		float rotX = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateX : 0f);
		float rotY = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateY : 0f);
		float rotZ = ((entity.Properties.Client.Shape != null) ? entity.Properties.Client.Shape.rotateZ : 0f);
		float mdt = Math.Min(0.05f, dt);
		if (!isSelf || capi.World.Player.CameraMode != 0)
		{
			float yawDist = GameMath.AngleRadDistance(bodyYawLerped, eagent.BodyYaw);
			bodyYawLerped += GameMath.Clamp(yawDist, (0f - mdt) * 8f, mdt * 8f);
			float bodyYaw = bodyYawLerped;
			smoothedBodyYaw = bodyYaw;
		}
		else
		{
			float bodyYaw = ((renderMode != RenderMode.ThirdPerson) ? eagent.BodyYaw : eagent.Pos.Yaw);
			if (!isShadowPass)
			{
				smoothCameraTurning(bodyYaw, mdt);
			}
		}
		float bodyPitch = ((entityPlayer == null) ? 0f : entityPlayer.WalkPitch);
		Mat4f.RotateX(ModelMat, ModelMat, entity.Pos.Roll + rotX * ((float)Math.PI / 180f));
		Mat4f.RotateY(ModelMat, ModelMat, smoothedBodyYaw + (90f + rotY) * ((float)Math.PI / 180f));
		if ((!isSelf || !eagent.Swimming || renderMode != 0) && (((selfEplr == null || !selfEplr.Controls.Gliding) && selfEplr.MountedOn == null) || renderMode != 0))
		{
			Mat4f.RotateZ(ModelMat, ModelMat, bodyPitch + rotZ * ((float)Math.PI / 180f));
		}
		Mat4f.RotateX(ModelMat, ModelMat, nowSwivelRad);
		if (selfEplr != null && renderMode == RenderMode.FirstPerson && !isShadowPass)
		{
			float itemSpecificPitchFollow = (eagent.RightHandItemSlot?.Itemstack?.ItemAttributes?["heldItemPitchFollow"].AsFloat(0.75f)).GetValueOrDefault(0.75f);
			float ridingSpecificPitchFollow = eagent.MountedOn?.FpHandPitchFollow ?? 1f;
			float f = ((selfEplr != null && selfEplr.Controls.IsFlying) ? 1f : (HeldItemPitchFollowOverride ?? (itemSpecificPitchFollow * ridingSpecificPitchFollow)));
			Mat4f.Translate(ModelMat, ModelMat, 0f, (float)entity.LocalEyePos.Y, 0f);
			Mat4f.RotateZ(ModelMat, ModelMat, (entity.Pos.Pitch - (float)Math.PI) * f);
			Mat4f.Translate(ModelMat, ModelMat, 0f, 0f - (float)entity.LocalEyePos.Y, 0f);
		}
		if (renderMode == RenderMode.FirstPerson && !isShadowPass)
		{
			Mat4f.Translate(ModelMat, ModelMat, 0f, capi.Settings.Float["fpHandsYOffset"], 0f);
		}
		float targetIntensity = entity.WatchedAttributes.GetFloat("intoxication");
		intoxIntensity += (targetIntensity - intoxIntensity) * dt / 3f;
		capi.Render.PerceptionEffects.ApplyToTpPlayer(entity as EntityPlayer, ModelMat, intoxIntensity);
		float scale = entity.Properties.Client.Size;
		Mat4f.Scale(ModelMat, ModelMat, new float[3] { scale, scale, scale });
		Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
	}

	private void smoothCameraTurning(float bodyYaw, float mdt)
	{
		float yawDist = GameMath.AngleRadDistance(smoothedBodyYaw, bodyYaw);
		smoothedBodyYaw += Math.Max(0f, Math.Abs(yawDist) - 0.6f) * (float)Math.Sign(yawDist);
		yawDist = GameMath.AngleRadDistance(smoothedBodyYaw, eagent.BodyYaw);
		smoothedBodyYaw += yawDist * mdt * 25f;
	}

	protected Vec3f GetOtherPlayerRenderOffset()
	{
		EntityPlayer selfEplr = capi.World.Player.Entity;
		IMountable selfMountedOn = selfEplr.MountedOn?.MountSupplier;
		IMountable heMountedOn = (entity as EntityAgent).MountedOn?.MountSupplier;
		if (selfMountedOn != null && selfMountedOn == heMountedOn)
		{
			EntityPos selfMountPos = selfEplr.MountedOn.SeatPosition;
			EntityPos heMountPos = (entity as EntityAgent).MountedOn.SeatPosition;
			return new Vec3f((float)(0.0 - selfMountPos.X + heMountPos.X), (float)(0.0 - selfMountPos.Y + heMountPos.Y), (float)(0.0 - selfMountPos.Z + heMountPos.Z));
		}
		return new Vec3f((float)(entity.Pos.X - selfEplr.CameraPos.X), (float)(entity.Pos.InternalY - selfEplr.CameraPos.Y), (float)(entity.Pos.Z - selfEplr.CameraPos.Z));
	}

	protected override void determineSidewaysSwivel(float dt)
	{
		if (entityPlayer.MountedOn != null)
		{
			entityPlayer.sidewaysSwivelAngle = (nowSwivelRad = 0f);
			return;
		}
		double nowAngle = Math.Atan2(entity.Pos.Motion.Z, entity.Pos.Motion.X);
		double walkspeedsq = entity.Pos.Motion.LengthSq();
		if (walkspeedsq > 0.001 && entity.OnGround)
		{
			float angledist = GameMath.AngleRadDistance((float)prevAngleSwing, (float)nowAngle);
			float num = nowSwivelRad;
			float num2 = GameMath.Clamp(angledist, -0.05f, 0.05f) * dt * 40f * (float)Math.Min(0.02500000037252903, walkspeedsq) * 80f;
			EntityAgent entityAgent = eagent;
			nowSwivelRad = num - num2 * (float)((entityAgent == null || !entityAgent.Controls.Backward) ? 1 : (-1));
			nowSwivelRad = GameMath.Clamp(nowSwivelRad, -0.3f, 0.3f);
		}
		nowSwivelRad *= Math.Min(0.99f, 1f - 0.1f * dt * 60f);
		prevAngleSwing = nowAngle;
		entityPlayer.sidewaysSwivelAngle = nowSwivelRad;
	}

	public override void Dispose()
	{
		base.Dispose();
		firstPersonMeshRef?.Dispose();
		thirdPersonMeshRef?.Dispose();
	}
}
