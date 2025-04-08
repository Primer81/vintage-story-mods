using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class PlayerCamera : Camera
{
	private ClientMain game;

	public float CameraShakeStrength;

	public bool UpdateCameraPos = true;

	private double physicsUnsimulatedSeconds;

	private Vec3d prevPos = new Vec3d();

	private Vec3d curPos = new Vec3d();

	private float targetCameraDistance;

	public double deltaSum;

	public PlayerCamera(ClientMain game)
	{
		this.game = game;
		HotkeyManager hotkeyManager = ScreenManager.hotkeyManager;
		hotkeyManager.SetHotKeyHandler("zoomout", KeyZoomOut);
		hotkeyManager.SetHotKeyHandler("zoomin", KeyZoomIn);
		hotkeyManager.SetHotKeyHandler("cyclecamera", KeyCycleCameraModes);
		game.eventManager.RegisterRenderer(OnBeforeRenderFrame3D, EnumRenderStage.Before, "camera", 0.0);
		targetCameraDistance = Tppcameradistance;
	}

	public void OnPlayerPhysicsTick(float nextAccum, Vec3d prevPos)
	{
		physicsUnsimulatedSeconds = nextAccum;
		this.prevPos.Set(prevPos);
	}

	public void OnBeforeRenderFrame3D(float dt)
	{
		Tppcameradistance = GameMath.Clamp(Tppcameradistance + (targetCameraDistance - Tppcameradistance) * dt * 5f, TppCameraDistanceMin, TppCameraDistanceMax);
		if (!game.IsPaused)
		{
			game.EntityPlayer.OnSelfBeforeRender(dt);
			base.Yaw = game.mouseYaw;
			base.Pitch = game.mousePitch;
			PlayerHeight = game.EntityPlayer.SelectionBox.Y2;
			CameraShakeStrength = Math.Max(0f, CameraShakeStrength - dt);
			deltaSum += dt;
			deltaSum = GameMath.Mod(deltaSum, Math.PI * 2.0);
			float physicsStepTime = 1f / 60f;
			double alpha = physicsUnsimulatedSeconds / (double)physicsStepTime;
			physicsUnsimulatedSeconds += dt;
			Vec3d camPos = game.EntityPlayer.CameraPos;
			curPos.Set(game.EntityPlayer.Pos.X, game.EntityPlayer.Pos.Y, game.EntityPlayer.Pos.Z);
			if (UpdateCameraPos)
			{
				Vec3d offset = game.EntityPlayer.CameraPosOffset;
				camPos.Set(prevPos.X + (curPos.X - prevPos.X) * alpha + (double)CameraShakeStrength * GameMath.Cos(deltaSum * 100.0) / 10.0 + offset.X, prevPos.Y + (curPos.Y - prevPos.Y) * alpha - (double)CameraShakeStrength * GameMath.Cos(deltaSum * 100.0) / 10.0 + offset.Y, prevPos.Z + (curPos.Z - prevPos.Z) * alpha + (double)CameraShakeStrength * GameMath.Sin(deltaSum * 100.0) / 10.0 + offset.Z);
			}
			camPos.Y += game.EntityPlayer.Pos.DimensionYAdjustment;
			base.CamSourcePosition.Set(camPos.X, camPos.Y, camPos.Z);
			base.OriginPosition.Set(0.0, 0.0, 0.0);
			Update(dt, game.interesectionTester);
			Vec3d pos = camPos;
			if (game.shUniforms.playerReferencePos == null)
			{
				game.shUniforms.playerReferencePos = new Vec3d(game.BlockAccessor.MapSizeX / 2, 0.0, game.BlockAccessor.MapSizeZ / 2);
				game.shUniforms.playerReferencePosForFoam = new Vec3d(game.BlockAccessor.MapSizeX / 2, 0.0, game.BlockAccessor.MapSizeZ / 2);
			}
			if ((double)game.shUniforms.playerReferencePos.HorizontalSquareDistanceTo(pos.X, pos.Z) > 400000000.0)
			{
				game.shUniforms.playerReferencePos.Set((float)pos.X, 0.0, (float)pos.Z);
			}
			if ((double)game.shUniforms.playerReferencePosForFoam.HorizontalSquareDistanceTo(pos.X, pos.Z) > 40000.0)
			{
				game.shUniforms.playerReferencePosForFoam.Set((float)pos.X, 0.0, (float)pos.Z);
			}
			game.shUniforms.PlayerPos.Set(pos.SubCopy(game.shUniforms.playerReferencePos));
			game.shUniforms.PlayerPosForFoam.Set(pos.SubCopy(game.shUniforms.playerReferencePosForFoam));
		}
	}

	internal override void SetMode(EnumCameraMode mode)
	{
		CameraMode = mode;
	}

	public void CycleMode()
	{
		if (CameraMode == EnumCameraMode.FirstPerson)
		{
			CameraMode = EnumCameraMode.ThirdPerson;
		}
		else if (CameraMode == EnumCameraMode.ThirdPerson)
		{
			CameraMode = EnumCameraMode.Overhead;
			if (!game.EntityPlayer.Controls.TriesToMove)
			{
				game.EntityPlayer.WalkYaw = game.EntityPlayer.Pos.Yaw;
			}
		}
		else
		{
			CameraMode = EnumCameraMode.FirstPerson;
		}
	}

	private bool KeyZoomOut(KeyCombination viaKeyComb)
	{
		if (CameraMode != 0)
		{
			targetCameraDistance += ((!game.api.inputapi.KeyboardKeyState[3]) ? 1 : 10);
		}
		targetCameraDistance = GameMath.Clamp(targetCameraDistance, TppCameraDistanceMin, TppCameraDistanceMax);
		return true;
	}

	private bool KeyZoomIn(KeyCombination viaKeyComb)
	{
		if (CameraMode != 0)
		{
			targetCameraDistance -= ((!game.api.inputapi.KeyboardKeyState[3]) ? 1 : 10);
		}
		targetCameraDistance = GameMath.Clamp(targetCameraDistance, TppCameraDistanceMin, TppCameraDistanceMax);
		return true;
	}

	private bool KeyCycleCameraModes(KeyCombination viaKeyComb)
	{
		CycleMode();
		return true;
	}
}
