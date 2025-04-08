using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class PlayerHeadController : EntityHeadController
{
	protected IPlayer player;

	private EntityPlayer entityPlayer;

	protected bool turnOpposite;

	protected bool rotateTpYawNow;

	public PlayerHeadController(IAnimationManager animator, EntityPlayer entity, Shape entityShape)
		: base(animator, entity, entityShape)
	{
		entityPlayer = entity;
	}

	public override void OnFrame(float dt)
	{
		if (player == null)
		{
			player = entityPlayer.Player;
		}
		ICoreClientAPI capi = entity.Api as ICoreClientAPI;
		if (capi.World.Player.Entity.EntityId != entity.EntityId)
		{
			base.OnFrame(dt);
			return;
		}
		float diff = GameMath.AngleRadDistance(entity.BodyYaw, entity.Pos.Yaw);
		if (Math.Abs(diff) > 1.8849558f)
		{
			turnOpposite = true;
		}
		if (turnOpposite)
		{
			if (Math.Abs(diff) < (float)Math.PI * 9f / 20f)
			{
				turnOpposite = false;
			}
			else
			{
				diff = 0f;
			}
		}
		EnumCameraMode cameraMode = (player as IClientPlayer).CameraMode;
		bool overheadLookAtMode = capi.Settings.Bool["overheadLookAt"] && cameraMode == EnumCameraMode.Overhead;
		if (!overheadLookAtMode && capi.Input.MouseGrabbed)
		{
			entity.Pos.HeadYaw += (diff - entity.Pos.HeadYaw) * dt * 6f;
			entity.Pos.HeadYaw = GameMath.Clamp(entity.Pos.HeadYaw, -0.75f, 0.75f);
			entity.Pos.HeadPitch = GameMath.Clamp((entity.Pos.Pitch - (float)Math.PI) * 0.75f, -1.2f, 1.2f);
		}
		EnumMountAngleMode angleMode = EnumMountAngleMode.Unaffected;
		IMountableSeat mount = player.Entity.MountedOn;
		if (player.Entity.MountedOn != null)
		{
			angleMode = mount.AngleMode;
		}
		if (player?.Entity == null || angleMode == EnumMountAngleMode.Fixate || angleMode == EnumMountAngleMode.FixateYaw || cameraMode == EnumCameraMode.Overhead)
		{
			if (capi.Input.MouseGrabbed)
			{
				entity.BodyYaw = entity.Pos.Yaw;
				if (overheadLookAtMode)
				{
					float dist = 0f - GameMath.AngleRadDistance((entity.Api as ICoreClientAPI).Input.MouseYaw, entity.Pos.Yaw);
					float targetHeadYaw = (float)Math.PI + dist;
					float targetpitch = GameMath.Clamp(0f - entity.Pos.Pitch - (float)Math.PI + (float)Math.PI * 2f, -1f, 0.8f);
					if (targetHeadYaw > (float)Math.PI)
					{
						targetHeadYaw -= (float)Math.PI * 2f;
					}
					if (targetHeadYaw < -1f || targetHeadYaw > 1f)
					{
						targetHeadYaw = 0f;
						entity.Pos.HeadPitch += (GameMath.Clamp((entity.Pos.Pitch - (float)Math.PI) * 0.75f, -1.2f, 1.2f) - entity.Pos.HeadPitch) * dt * 6f;
					}
					else
					{
						entity.Pos.HeadPitch += (targetpitch - entity.Pos.HeadPitch) * dt * 6f;
					}
					entity.Pos.HeadYaw += (targetHeadYaw - entity.Pos.HeadYaw) * dt * 6f;
				}
			}
		}
		else
		{
			IPlayer obj = player;
			if (obj != null && obj.Entity.Alive)
			{
				float yawDist = GameMath.AngleRadDistance(entity.BodyYaw, entity.Pos.Yaw);
				bool ismoving = player.Entity.Controls.TriesToMove || player.Entity.ServerControls.TriesToMove;
				bool attachedToClimbWall = false;
				float threshold = 1.2f - (ismoving ? 1.19f : 0f) + (float)(attachedToClimbWall ? 3 : 0);
				if (entity.Controls.Gliding)
				{
					threshold = 0f;
				}
				if (player.PlayerUID == capi.World.Player.PlayerUID && !capi.Settings.Bool["immersiveFpMode"] && cameraMode != 0)
				{
					if (Math.Abs(yawDist) > threshold || rotateTpYawNow)
					{
						float speed = 0.05f + Math.Abs(yawDist) * 3.5f;
						entity.BodyYaw += GameMath.Clamp(yawDist, (0f - dt) * speed, dt * speed);
						rotateTpYawNow = Math.Abs(yawDist) > 0.01f;
					}
				}
				else
				{
					entity.BodyYaw = entity.Pos.Yaw;
				}
			}
		}
		base.OnFrame(dt);
	}
}
