using System;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemCalendar : ClientSystem
{
	private bool started;

	public override string Name => "cal";

	public SystemCalendar(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[13] = HandleCalendarPacket;
		game.api.ChatCommands.Create("time").WithDescription("Show the the current client time and speed").WithArgs(game.api.ChatCommands.Parsers.OptionalBool("speed"))
			.HandleWith(OnTimeCommand);
		game.RegisterGameTickListener(OnGameTick, 20);
	}

	public override void OnBlockTexturesLoaded()
	{
		game.GameWorldCalendar = new ClientGameCalendar(game, ScreenManager.Platform.AssetManager.Get("textures/environment/sunlight.png"), game.Seed, 28000L);
		game.api.eventapi.LevelFinalize += Eventapi_LevelFinalize;
	}

	private void Eventapi_LevelFinalize()
	{
		game.GameWorldCalendar.Update();
		game.GameWorldCalendar.Update();
	}

	private void OnGameTick(float dt)
	{
		game.GameWorldCalendar.Tick();
	}

	private void HandleCalendarPacket(Packet_Server packet)
	{
		if (!game.ignoreServerCalendarUpdates && game.GameWorldCalendar != null)
		{
			if (!started)
			{
				game.GameWorldCalendar.Start();
				started = true;
			}
			int drift = (int)(packet.Calendar.TotalSeconds - game.GameWorldCalendar.TotalSeconds);
			if (Math.Abs(drift) > 900 && game.GameWorldCalendar.TotalSeconds > 28000)
			{
				ScreenManager.Platform.Logger.Notification("Wow, client daytime drifted off significantly from server daytime ({0} mins)", Math.Round((float)drift / 60f, 1));
			}
			game.GameWorldCalendar.UpdateFromPacket(packet);
		}
	}

	private TextCommandResult OnTimeCommand(TextCommandCallingArgs args)
	{
		GameCalendar cal = game.GameWorldCalendar;
		game.ShowChatMessage("Client time: " + cal.PrettyDate());
		if ((bool)args[0])
		{
			game.ShowChatMessage("Game speed: " + Math.Round(game.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1) + " IRL minutes");
		}
		return TextCommandResult.Success();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
