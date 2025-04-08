using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityTestShip : EntityChunky
{
	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
	}

	public static EntityChunky CreateShip(ICoreServerAPI sapi, IMiniDimension dimension)
	{
		EntityChunky obj = (EntityChunky)sapi.World.ClassRegistry.CreateEntity("EntityTestShip");
		obj.Code = new AssetLocation("testship");
		obj.AssociateWithDimension(dimension);
		return obj;
	}

	public override void OnGameTick(float dt)
	{
		if (blocks != null && base.SidedPos != null)
		{
			base.OnGameTick(dt);
			base.SidedPos.Motion.X = 0.01;
			Pos.Y = (double)(int)Pos.Y + 0.5;
			Pos.Yaw = (float)(Pos.X % 6.3) / 20f;
			Pos.Pitch = (float)GameMath.Sin(Pos.X % 6.3) / 5f;
			Pos.Roll = (float)GameMath.Sin(Pos.X % 12.6) / 3f;
			base.SidedPos.Pitch = Pos.Pitch;
			base.SidedPos.Roll = Pos.Roll;
			base.SidedPos.Y = Pos.Y;
			ServerPos.SetFrom(Pos);
		}
	}
}
