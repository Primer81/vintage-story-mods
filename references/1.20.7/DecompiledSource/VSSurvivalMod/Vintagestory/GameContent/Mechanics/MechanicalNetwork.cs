using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent.Mechanics;

[ProtoContract]
public class MechanicalNetwork
{
	public Dictionary<BlockPos, IMechanicalPowerNode> nodes = new Dictionary<BlockPos, IMechanicalPowerNode>();

	internal MechanicalPowerMod mechanicalPowerMod;

	[ProtoMember(1)]
	public long networkId;

	[ProtoMember(2)]
	protected float totalAvailableTorque;

	[ProtoMember(3)]
	protected float networkResistance;

	[ProtoMember(4)]
	protected float speed;

	[ProtoMember(7)]
	protected float serverSideAngle;

	[ProtoMember(8)]
	protected float angle;

	[ProtoMember(9)]
	public Dictionary<Vec3i, int> inChunks = new Dictionary<Vec3i, int>();

	[ProtoMember(10)]
	private float networkTorque;

	public float clientSpeed;

	private const int chunksize = 32;

	public bool fullyLoaded;

	private bool firstTick = true;

	[ProtoMember(11)]
	public EnumRotDirection TurnDir { get; set; }

	public bool Valid { get; set; } = true;


	public float AngleRad
	{
		get
		{
			return angle;
		}
		set
		{
			angle = value;
		}
	}

	public float Speed
	{
		get
		{
			return speed;
		}
		set
		{
			speed = value;
		}
	}

	public bool DirectionHasReversed { get; set; }

	public float TotalAvailableTorque
	{
		get
		{
			return totalAvailableTorque;
		}
		set
		{
			totalAvailableTorque = value;
		}
	}

	public float NetworkTorque
	{
		get
		{
			return networkTorque;
		}
		set
		{
			networkTorque = value;
		}
	}

	public float NetworkResistance
	{
		get
		{
			return networkResistance;
		}
		set
		{
			networkResistance = value;
		}
	}

	public MechanicalNetwork()
	{
	}

	public MechanicalNetwork(MechanicalPowerMod mechanicalPowerMod, long networkId)
	{
		this.networkId = networkId;
		Init(mechanicalPowerMod);
	}

	public void Init(MechanicalPowerMod mechanicalPowerMod)
	{
		this.mechanicalPowerMod = mechanicalPowerMod;
	}

	public void Join(IMechanicalPowerNode node)
	{
		BlockPos pos = node.GetPosition();
		nodes[pos] = node;
		Vec3i chunkpos = new Vec3i(pos.X / 32, pos.Y / 32, pos.Z / 32);
		inChunks.TryGetValue(chunkpos, out var q);
		inChunks[chunkpos] = q + 1;
	}

	public void DidUnload(IMechanicalPowerDevice node)
	{
		fullyLoaded = false;
	}

	public void Leave(IMechanicalPowerNode node)
	{
		BlockPos pos = node.GetPosition();
		nodes.Remove(pos);
		Vec3i chunkpos = new Vec3i(pos.X / 32, pos.Y / 32, pos.Z / 32);
		inChunks.TryGetValue(chunkpos, out var q);
		if (q <= 1)
		{
			inChunks.Remove(chunkpos);
		}
		else
		{
			inChunks[chunkpos] = q - 1;
		}
	}

	internal void AwaitChunkThenDiscover(Vec3i missingChunkPos)
	{
		inChunks[missingChunkPos] = 1;
		fullyLoaded = false;
	}

	public void ClientTick(float dt)
	{
		if (firstTick)
		{
			firstTick = false;
			mechanicalPowerMod.SendNetworkBlocksUpdateRequestToServer(networkId);
		}
		if (!(speed < 0.001f))
		{
			float f = dt * 50f;
			clientSpeed += GameMath.Clamp(speed - clientSpeed, f * -0.01f, f * 0.01f);
			UpdateAngle(f * (((TurnDir == EnumRotDirection.Clockwise) ^ DirectionHasReversed) ? clientSpeed : (0f - clientSpeed)));
			float diff = f * GameMath.AngleRadDistance(angle, serverSideAngle);
			angle += GameMath.Clamp(diff, -0.002f * Math.Abs(diff), 0.002f * Math.Abs(diff));
		}
	}

	public void ServerTick(float dt, long tickNumber)
	{
		UpdateAngle(speed * dt * 50f);
		if (tickNumber % 5 == 0L)
		{
			updateNetwork(tickNumber);
		}
		if (tickNumber % 40 == 0L)
		{
			broadcastData();
		}
	}

	public void broadcastData()
	{
		mechanicalPowerMod.broadcastNetwork(new MechNetworkPacket
		{
			angle = angle,
			networkId = networkId,
			speed = speed,
			direction = ((speed >= 0f) ? 1 : (-1)),
			totalAvailableTorque = totalAvailableTorque,
			networkResistance = networkResistance,
			networkTorque = networkTorque
		});
	}

	public void UpdateAngle(float speed)
	{
		angle += speed / 10f;
		serverSideAngle += speed / 10f;
	}

	public void updateNetwork(long tick)
	{
		if (DirectionHasReversed)
		{
			speed = 0f - speed;
			DirectionHasReversed = false;
		}
		float totalTorque = 0f;
		float totalResistance = 0f;
		float speedTmp = speed;
		foreach (IMechanicalPowerNode powerNode in nodes.Values)
		{
			float r = powerNode.GearedRatio;
			totalTorque += r * powerNode.GetTorque(tick, speedTmp * r, out var resistance);
			totalResistance += r * resistance;
			totalResistance += speed * speed * r * r / 1000f;
		}
		networkTorque = totalTorque;
		networkResistance = totalResistance;
		float unusedTorque = Math.Abs(totalTorque) - networkResistance;
		float torqueSign = ((totalTorque >= 0f) ? 1f : (-1f));
		float drag = Math.Max(1f, (float)Math.Pow(nodes.Count, 0.25));
		float step = 1f / drag;
		bool wrongTurnSense = speed * torqueSign < 0f;
		if (unusedTorque > 0f && !wrongTurnSense)
		{
			speed += Math.Min(0.05f, step * unusedTorque) * torqueSign;
		}
		else
		{
			float change = unusedTorque;
			if (wrongTurnSense)
			{
				change = 0f - networkResistance;
			}
			if (change < 0f - Math.Abs(speed))
			{
				change = 0f - Math.Abs(speed);
			}
			if (change < -1E-06f || Math.Abs(speed) > 1E-06f)
			{
				float speedSign = ((speed < 0f) ? (-1f) : 1f);
				speed = Math.Max(1E-06f, Math.Abs(speed) + step * change) * speedSign;
			}
			else if (Math.Abs(unusedTorque) > 0f)
			{
				speed = torqueSign / 1000000f;
			}
		}
		if (unusedTorque > Math.Abs(totalAvailableTorque))
		{
			if (totalTorque > 0f)
			{
				totalAvailableTorque = Math.Min(totalTorque, totalAvailableTorque + step);
			}
			else
			{
				totalAvailableTorque = Math.Max(Math.Min(totalTorque, -1E-08f), totalAvailableTorque - step);
			}
		}
		else
		{
			totalAvailableTorque *= 0.9f;
		}
		TurnDir = ((!(speed >= 0f)) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
	}

	public void UpdateFromPacket(MechNetworkPacket packet, bool isNew)
	{
		totalAvailableTorque = packet.totalAvailableTorque;
		networkResistance = packet.networkResistance;
		networkTorque = packet.networkTorque;
		speed = Math.Abs(packet.speed);
		if (isNew)
		{
			angle = packet.angle;
			clientSpeed = speed;
		}
		serverSideAngle = packet.angle;
		TurnDir = ((packet.direction < 0) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
		DirectionHasReversed = false;
	}

	public bool testFullyLoaded(ICoreAPI api)
	{
		foreach (Vec3i chunkpos in inChunks.Keys)
		{
			if (api.World.BlockAccessor.GetChunk(chunkpos.X, chunkpos.Y, chunkpos.Z) == null)
			{
				return false;
			}
		}
		return true;
	}

	public void ReadFromTreeAttribute(ITreeAttribute tree)
	{
		networkId = tree.GetLong("networkId", 0L);
		totalAvailableTorque = tree.GetFloat("totalAvailableTorque");
		networkResistance = tree.GetFloat("totalResistance");
		speed = tree.GetFloat("speed");
		angle = tree.GetFloat("angle");
		TurnDir = (EnumRotDirection)tree.GetInt("rot");
	}

	public void WriteToTreeAttribute(ITreeAttribute tree)
	{
		tree.SetLong("networkId", networkId);
		tree.SetFloat("totalAvailableTorque", totalAvailableTorque);
		tree.SetFloat("totalResistance", networkResistance);
		tree.SetFloat("speed", speed);
		tree.SetFloat("angle", angle);
		tree.SetInt("rot", (int)TurnDir);
	}

	public void SendBlocksUpdateToClient(IServerPlayer player)
	{
		foreach (IMechanicalPowerNode value in nodes.Values)
		{
			if (value is BEBehaviorMPBase bemp)
			{
				bemp.Blockentity.MarkDirty();
			}
		}
	}
}
