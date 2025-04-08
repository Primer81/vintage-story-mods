using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

public class EntityBehaviorInterpolatePosition : EntityBehavior, IRenderer, IDisposable
{
	public ICoreClientAPI capi;

	public EntityAgent agent;

	public IMountable mountableSupplier;

	public float dtAccum;

	public PositionSnapshot pL;

	public PositionSnapshot pN;

	public Queue<PositionSnapshot> positionQueue = new Queue<PositionSnapshot>();

	public float currentYaw;

	public float targetYaw;

	public float currentPitch;

	public float targetPitch;

	public float currentRoll;

	public float targetRoll;

	public float currentHeadYaw;

	public float targetHeadYaw;

	public float currentHeadPitch;

	public float targetHeadPitch;

	public float currentBodyYaw;

	public float targetBodyYaw;

	public float interval = 1f / 15f;

	public int queueCount;

	public IRemotePhysics physics;

	public int wait;

	public float targetSpeed = 0.6f;

	public double RenderOrder => 0.0;

	public int RenderRange => 9999;

	public EntityBehaviorInterpolatePosition(Entity entity)
		: base(entity)
	{
		if (entity.World.Side == EnumAppSide.Server)
		{
			throw new Exception("Remove server interpolation behavior from " + entity.Code.Path + ".");
		}
		capi = entity.Api as ICoreClientAPI;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "interpolateposition");
		agent = entity as EntityAgent;
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		mountableSupplier = entity.GetInterface<IMountable>();
	}

	public void PushQueue(PositionSnapshot snapshot)
	{
		positionQueue.Enqueue(snapshot);
		queueCount++;
	}

	public void PopQueue(bool clear)
	{
		dtAccum -= pN.interval;
		if (dtAccum < 0f)
		{
			dtAccum = 0f;
		}
		if (dtAccum > 1f)
		{
			dtAccum = 0f;
		}
		pL = pN;
		pN = positionQueue.Dequeue();
		queueCount--;
		if (clear && queueCount > 1)
		{
			PopQueue(clear: true);
		}
		IMountable mountable = mountableSupplier;
		if (mountable == null || !mountable.IsBeingControlled())
		{
			entity.ServerPos.SetPos(pN.x, pN.y, pN.z);
			physics?.HandleRemotePhysics(Math.Max(pN.interval, interval), pN.isTeleport);
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		currentYaw = entity.ServerPos.Yaw;
		targetYaw = entity.ServerPos.Yaw;
		PushQueue(new PositionSnapshot(entity.ServerPos, 0f, isTeleport: false));
		targetYaw = entity.ServerPos.Yaw;
		targetPitch = entity.ServerPos.Pitch;
		targetRoll = entity.ServerPos.Roll;
		currentYaw = entity.ServerPos.Yaw;
		currentPitch = entity.ServerPos.Pitch;
		currentRoll = entity.ServerPos.Roll;
		if (agent != null)
		{
			targetHeadYaw = entity.ServerPos.HeadYaw;
			targetHeadPitch = entity.ServerPos.HeadPitch;
			targetBodyYaw = agent.BodyYawServer;
			currentHeadYaw = entity.ServerPos.HeadYaw;
			currentHeadPitch = entity.ServerPos.HeadPitch;
			currentBodyYaw = agent.BodyYawServer;
		}
		foreach (EntityBehavior behavior in entity.SidedProperties.Behaviors)
		{
			if (behavior is IRemotePhysics)
			{
				physics = behavior as IRemotePhysics;
				break;
			}
		}
	}

	public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
	{
		float tickInterval = (float)entity.Attributes.GetInt("tickDiff", 1) * interval;
		PushQueue(new PositionSnapshot(entity.ServerPos, tickInterval, isTeleport));
		if (isTeleport)
		{
			dtAccum = 0f;
			positionQueue.Clear();
			queueCount = 0;
			PushQueue(new PositionSnapshot(entity.ServerPos, tickInterval, isTeleport: false));
			PushQueue(new PositionSnapshot(entity.ServerPos, tickInterval, isTeleport: false));
			PopQueue(clear: false);
			PopQueue(clear: false);
		}
		targetYaw = entity.ServerPos.Yaw;
		targetPitch = entity.ServerPos.Pitch;
		targetRoll = entity.ServerPos.Roll;
		if (agent != null)
		{
			targetHeadYaw = entity.ServerPos.HeadYaw;
			targetHeadPitch = entity.ServerPos.HeadPitch;
			targetBodyYaw = agent.BodyYawServer;
		}
		if (queueCount > 20)
		{
			PopQueue(clear: true);
		}
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
		if (queueCount < wait)
		{
			if (mountableSupplier == null)
			{
				entity.Pos.Yaw = LerpRotation(ref currentYaw, targetYaw, dt);
				entity.Pos.Pitch = LerpRotation(ref currentPitch, targetPitch, dt);
				entity.Pos.Roll = LerpRotation(ref currentRoll, targetRoll, dt);
				if (agent != null)
				{
					entity.Pos.HeadYaw = LerpRotation(ref currentHeadYaw, targetHeadYaw, dt);
					entity.Pos.HeadPitch = LerpRotation(ref currentHeadPitch, targetHeadPitch, dt);
					agent.BodyYaw = LerpRotation(ref currentBodyYaw, targetBodyYaw, dt);
				}
			}
			return;
		}
		dtAccum += dt * targetSpeed;
		while (dtAccum > pN.interval)
		{
			if (queueCount > 0)
			{
				if (entity == capi.World.Player.Entity)
				{
					capi.Event.UnregisterRenderer(this, EnumRenderStage.Before);
					return;
				}
				PopQueue(clear: false);
				wait = 0;
				continue;
			}
			wait = 1;
			break;
		}
		float speed = (float)queueCount * 0.2f + 0.8f;
		targetSpeed = GameMath.Lerp(targetSpeed, speed, dt * 4f);
		if (mountableSupplier != null)
		{
			IMountableSeat[] seats = mountableSupplier.Seats;
			foreach (IMountableSeat seat in seats)
			{
				if (seat.Passenger != capi.World.Player.Entity)
				{
					seat.Passenger?.Pos.SetFrom(seat.SeatPosition);
				}
				else if (mountableSupplier.Controller == capi.World.Player.Entity)
				{
					currentYaw = entity.Pos.Yaw;
					currentPitch = entity.Pos.Pitch;
					currentRoll = entity.Pos.Roll;
					return;
				}
			}
		}
		float delta = dtAccum / pN.interval;
		if (wait != 0)
		{
			delta = 1f;
		}
		entity.Pos.Yaw = LerpRotation(ref currentYaw, targetYaw, dt);
		entity.Pos.Pitch = LerpRotation(ref currentPitch, targetPitch, dt);
		entity.Pos.Roll = LerpRotation(ref currentRoll, targetRoll, dt);
		if (agent != null)
		{
			entity.Pos.HeadYaw = LerpRotation(ref currentHeadYaw, targetHeadYaw, dt);
			entity.Pos.HeadPitch = LerpRotation(ref currentHeadPitch, targetHeadPitch, dt);
			agent.BodyYaw = LerpRotation(ref currentBodyYaw, targetBodyYaw, dt);
		}
		if (agent == null || agent.MountedOn == null)
		{
			entity.Pos.X = GameMath.Lerp(pL.x, pN.x, delta);
			entity.Pos.Y = GameMath.Lerp(pL.y, pN.y, delta);
			entity.Pos.Z = GameMath.Lerp(pL.z, pN.z, delta);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float LerpRotation(ref float current, float target, float dt)
	{
		float pDiff = Math.Abs(GameMath.AngleRadDistance(current, target)) * dt / 0.1f;
		int signY = Math.Sign(pDiff);
		current += 0.6f * Math.Clamp(GameMath.AngleRadDistance(current, target), (float)(-signY) * pDiff, (float)signY * pDiff);
		current %= (float)Math.PI * 2f;
		return current;
	}

	public override string PropertyName()
	{
		return "entityinterpolation";
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Before);
	}

	public void Dispose()
	{
	}
}
