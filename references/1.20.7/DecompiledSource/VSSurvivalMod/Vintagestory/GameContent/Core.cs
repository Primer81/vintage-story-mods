using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class Core : ModSystem
{
	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		api.RegisterMountable("elevator", EntityRideableSeat.GetMountable);
		api.RegisterMountable("boat", EntityRideableSeat.GetMountable);
		api.RegisterMountable("rideableanimal", EntityRideableSeat.GetMountable);
		api.RegisterItemClass("ItemBoat", typeof(ItemBoat));
		api.RegisterEntity("EntityBoat", typeof(EntityBoat));
		api.RegisterEntity("EntityElevator", typeof(EntityElevator));
		api.RegisterItemClass("ItemRoller", typeof(ItemRoller));
		api.RegisterEntity("EntityBoatConstruction", typeof(EntityBoatConstruction));
	}
}
