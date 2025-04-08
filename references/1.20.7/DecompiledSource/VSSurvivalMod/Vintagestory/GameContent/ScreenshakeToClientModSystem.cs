using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ScreenshakeToClientModSystem : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("screenshake").RegisterMessageType<ScreenshakePacket>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
	}

	public void ShakeScreen(Vec3d pos, float strength, float range)
	{
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer plr = (IServerPlayer)allOnlinePlayers[i];
			if (plr.ConnectionState == EnumClientState.Playing)
			{
				float dist = (float)plr.Entity.ServerPos.DistanceTo(pos);
				float str = Math.Min(1f, (range - dist) / dist) * strength;
				if ((double)str > 0.05)
				{
					sapi.Network.GetChannel("screenshake").SendPacket(new ScreenshakePacket
					{
						Strength = str
					}, plr);
				}
			}
		}
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Network.GetChannel("screenshake").SetMessageHandler<ScreenshakePacket>(onScreenshakePacket);
	}

	private void onScreenshakePacket(ScreenshakePacket packet)
	{
		capi.World.AddCameraShake(packet.Strength);
	}
}
