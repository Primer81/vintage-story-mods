using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemWorldAmbient : ServerSystem
{
	private AmbientModifier serverSettings;

	public ServerSystemWorldAmbient(ServerMain server)
		: base(server)
	{
		serverSettings = new AmbientModifier().EnsurePopulated();
		server.EventManager.OnGameWorldBeingSaved += OnSaving;
		server.api.ChatCommands.Create("setambient").WithDescription("Sets the server controlled ambient for everyone. Json format.").RequiresPrivilege(Privilege.controlserver)
			.WithArgs(server.api.ChatCommands.Parsers.All("json_code"))
			.HandleWith(OnSetAmbient);
	}

	private TextCommandResult OnSetAmbient(TextCommandCallingArgs args)
	{
		try
		{
			serverSettings = JsonConvert.DeserializeObject<AmbientModifier>(args[0] as string).EnsurePopulated();
			server.BroadcastPacket(GetAmbientPacket());
		}
		catch
		{
			return TextCommandResult.Success("Failed parsing the json");
		}
		return TextCommandResult.Success();
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		server.SendPacket(player.ClientId, GetAmbientPacket());
	}

	private Packet_Server GetAmbientPacket()
	{
		Packet_Server p = new Packet_Server
		{
			Id = 65,
			Ambient = new Packet_Ambient()
		};
		using MemoryStream ms = new MemoryStream();
		serverSettings.ToBytes(new BinaryWriter(ms));
		p.Ambient.SetData(ms.ToArray());
		return p;
	}

	private void OnSaving()
	{
		using MemoryStream ms = new MemoryStream();
		serverSettings.ToBytes(new BinaryWriter(ms));
		server.SaveGameData.ModData["ambient"] = ms.ToArray();
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		if (savegame.ModData.TryGetValue("ambient", out var data))
		{
			try
			{
				using (MemoryStream ms = new MemoryStream(data))
				{
					serverSettings.FromBytes(new BinaryReader(ms));
				}
				serverSettings.EnsurePopulated();
			}
			catch
			{
			}
		}
		base.OnBeginGameReady(savegame);
		if (savegame.IsNewWorld)
		{
			serverSettings = AmbientModifier.DefaultAmbient;
			float newWeight = 0f;
			serverSettings.AmbientColor.Weight = 0f;
			serverSettings.FogColor.Weight = newWeight;
			serverSettings.FogDensity.Weight = newWeight;
			serverSettings.FogMin.Weight = newWeight;
			serverSettings.CloudBrightness.Weight = newWeight;
			serverSettings.CloudDensity.Weight = newWeight;
		}
	}
}
