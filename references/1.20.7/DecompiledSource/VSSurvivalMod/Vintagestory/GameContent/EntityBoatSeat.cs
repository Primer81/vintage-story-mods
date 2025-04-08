using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBoatSeat : EntityRideableSeat
{
	public string actionAnim;

	private Dictionary<string, string> animations => (Entity as EntityBoat).MountAnimations;

	public override AnimationMetaData SuggestedAnimation
	{
		get
		{
			if (actionAnim == null)
			{
				return null;
			}
			Entity passenger = base.Passenger;
			AnimationMetaData ameta = default(AnimationMetaData);
			if (passenger != null && (passenger.Properties?.Client.AnimationsByMetaCode?.TryGetValue(actionAnim, out ameta)).GetValueOrDefault())
			{
				return ameta;
			}
			return null;
		}
	}

	public EntityBoatSeat(IMountable mountablesupplier, string seatId, SeatConfig config)
		: base(mountablesupplier, seatId, config)
	{
		RideableClassName = "boat";
	}

	public override bool CanMount(EntityAgent entityAgent)
	{
		JsonObject attributes = config.Attributes;
		if (attributes != null && attributes["ropeTieablesOnly"].AsBool())
		{
			return entityAgent.HasBehavior<EntityBehaviorRopeTieable>();
		}
		return base.CanMount(entityAgent);
	}

	public override void DidMount(EntityAgent entityAgent)
	{
		base.DidMount(entityAgent);
		entityAgent.AnimManager.StartAnimation(animations["idle"]);
	}

	public override void DidUnmount(EntityAgent entityAgent)
	{
		if (base.Passenger != null)
		{
			base.Passenger.AnimManager?.StopAnimation(animations["ready"]);
			base.Passenger.AnimManager?.StopAnimation(animations["forwards"]);
			base.Passenger.AnimManager?.StopAnimation(animations["backwards"]);
			base.Passenger.AnimManager?.StopAnimation(animations["idle"]);
			base.Passenger.SidedPos.Roll = 0f;
		}
		base.DidUnmount(entityAgent);
	}

	protected override void tryTeleportToFreeLocation()
	{
		IWorldAccessor world = base.Passenger.World;
		IBlockAccessor ba = base.Passenger.World.BlockAccessor;
		double shortestDistance = 99.0;
		Vec3d shortestTargetPos = null;
		for (int dx2 = -4; dx2 <= 4; dx2++)
		{
			for (int dy = 0; dy < 2; dy++)
			{
				for (int dz2 = -4; dz2 <= 4; dz2++)
				{
					Vec3d targetPos2 = base.Passenger.ServerPos.XYZ.AsBlockPos.ToVec3d().Add((double)dx2 + 0.5, (double)dy + 0.1, (double)dz2 + 0.5);
					Block block = ba.GetMostSolidBlock((int)targetPos2.X, (int)(targetPos2.Y - 0.15), (int)targetPos2.Z);
					if (ba.GetBlock((int)targetPos2.X, (int)targetPos2.Y, (int)targetPos2.Z, 2).Id == 0 && block.SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(ba, base.Passenger.CollisionBox, targetPos2, alsoCheckTouch: false))
					{
						float dist = targetPos2.DistanceTo(base.Passenger.ServerPos.XYZ);
						if ((double)dist < shortestDistance)
						{
							shortestDistance = dist;
							shortestTargetPos = targetPos2;
						}
					}
				}
			}
		}
		if (shortestTargetPos != null)
		{
			base.Passenger.TeleportTo(shortestTargetPos);
			return;
		}
		bool found = false;
		int dx = -1;
		while (!found && dx <= 1)
		{
			int dz = -1;
			while (!found && dz <= 1)
			{
				Vec3d targetPos = base.Passenger.ServerPos.XYZ.AsBlockPos.ToVec3d().Add((double)dx + 0.5, 1.1, (double)dz + 0.5);
				if (!world.CollisionTester.IsColliding(ba, base.Passenger.CollisionBox, targetPos, alsoCheckTouch: false))
				{
					base.Passenger.TeleportTo(targetPos);
					found = true;
					break;
				}
				dz++;
			}
			dx++;
		}
	}
}
