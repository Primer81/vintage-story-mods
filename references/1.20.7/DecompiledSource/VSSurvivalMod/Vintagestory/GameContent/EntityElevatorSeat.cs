using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityElevatorSeat : EntityRideableSeat
{
	public string actionAnim;

	public override EnumMountAngleMode AngleMode => EnumMountAngleMode.Unaffected;

	private Dictionary<string, string> animations => (Entity as EntityElevator).MountAnimations;

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

	public EntityElevatorSeat(IMountable mountablesupplier, string seatId, SeatConfig config)
		: base(mountablesupplier, seatId, config)
	{
		RideableClassName = "elevator";
	}

	public override bool CanUnmount(EntityAgent entityAgent)
	{
		if (Entity is EntityElevator { IsMoving: not false })
		{
			return false;
		}
		return true;
	}

	public override bool CanMount(EntityAgent entityAgent)
	{
		if (Entity is EntityElevator { IsMoving: not false })
		{
			return false;
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
			base.Passenger.AnimManager?.StopAnimation(animations["idle"]);
		}
		base.DidUnmount(entityAgent);
	}

	protected override void tryTeleportToFreeLocation()
	{
		base.Passenger?.TeleportTo(base.Passenger.ServerPos.Add(0.0, 0.1, 0.0));
	}
}
