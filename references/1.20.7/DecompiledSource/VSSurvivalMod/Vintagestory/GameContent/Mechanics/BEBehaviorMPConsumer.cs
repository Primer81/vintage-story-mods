using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPConsumer : BEBehaviorMPBase
{
	protected float resistance = 0.1f;

	public Action OnConnected;

	public Action OnDisconnected;

	public float TrueSpeed => Math.Abs((base.Network?.Speed * base.GearedRatio).GetValueOrDefault());

	public BEBehaviorMPConsumer(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		Shape = properties["mechPartShape"].AsObject<CompositeShape>();
		Shape?.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		resistance = properties["resistance"].AsFloat(0.1f);
	}

	public override void JoinNetwork(MechanicalNetwork network)
	{
		base.JoinNetwork(network);
		OnConnected?.Invoke();
	}

	public override void LeaveNetwork()
	{
		base.LeaveNetwork();
		OnDisconnected?.Invoke();
	}

	public override float GetResistance()
	{
		return resistance;
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath fromExitTurnDir)
	{
		return new MechPowerPath[0];
	}
}
