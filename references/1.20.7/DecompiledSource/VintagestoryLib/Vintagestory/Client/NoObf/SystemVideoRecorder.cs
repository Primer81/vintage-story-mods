using System;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class SystemVideoRecorder : ClientSystem
{
	public bool Recording;

	public IAviWriter avi;

	public float writeAccum;

	public bool firstFrameDone;

	public string videoFileName;

	public override string Name => "vrec";

	public SystemVideoRecorder(ClientMain game)
		: base(game)
	{
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		game.api.ChatCommands.Create("vrec").WithDescription("Video Recorder Tools").BeginSubCommand("start")
			.WithDescription("start")
			.HandleWith(VrecCmdStart)
			.EndSubCommand()
			.BeginSubCommand("stop")
			.WithDescription("stop")
			.HandleWith(VrecCmdStop)
			.EndSubCommand()
			.BeginSubCommand("toggle")
			.WithDescription("toggle")
			.HandleWith(VrecCmdToggle)
			.EndSubCommand()
			.BeginSubCommand("codec")
			.WithDescription("codec")
			.WithArgs(parsers.Word("codec", (from c in game.Platform.GetAvailableCodecs()
				select c.Code).ToArray()))
			.HandleWith(VrecCmdCodec)
			.EndSubCommand()
			.BeginSubCommand("videofps")
			.WithDescription("videofps")
			.WithArgs(parsers.OptionalInt("videofps"))
			.HandleWith(VrecCmdVideofps)
			.EndSubCommand()
			.BeginSubCommand("tickfps")
			.WithDescription("tickfps")
			.WithArgs(parsers.OptionalInt("tickfps"))
			.HandleWith(VrecCmdTickfps)
			.EndSubCommand()
			.BeginSubCommand("filetarget")
			.WithDescription("filetarget")
			.WithArgs(parsers.OptionalWord("file"))
			.HandleWith(VrecCmdFiletarget)
			.EndSubCommand();
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, Name, 1.0);
	}

	private TextCommandResult VrecCmdFiletarget(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			string target = ClientSettings.VideoFileTarget;
			if (target == null)
			{
				target = GamePaths.Videos;
			}
			game.ShowChatMessage("Current file target: " + target);
			return TextCommandResult.Success("Use file target '-' to reset to default target (=Videos folder)");
		}
		string target2 = args[0] as string;
		if (target2 == "-")
		{
			target2 = null;
		}
		ClientSettings.VideoFileTarget = target2;
		if (target2 == null)
		{
			target2 = GamePaths.Videos;
		}
		return TextCommandResult.Success("Video File Target set to " + target2);
	}

	private TextCommandResult VrecCmdVideofps(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current Framerate: " + ClientSettings.RecordingFrameRate);
		}
		ClientSettings.RecordingFrameRate = (int)args[0];
		return TextCommandResult.Success("Framerate: " + ClientSettings.RecordingFrameRate + " set.");
	}

	private TextCommandResult VrecCmdTickfps(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current Game Tick Framerate: " + ClientSettings.GameTickFrameRate);
		}
		ClientSettings.GameTickFrameRate = (int)args[0];
		return TextCommandResult.Success("Current Game Tick Framerate: " + ClientSettings.GameTickFrameRate + " set.");
	}

	private TextCommandResult VrecCmdCodec(TextCommandCallingArgs args)
	{
		AvailableCodec[] codecs = null;
		try
		{
			codecs = game.Platform.GetAvailableCodecs();
		}
		catch (Exception e)
		{
			game.Logger.Error("Failed retrieving codecs:");
			game.Logger.Error(e);
			return TextCommandResult.Success("Could not retrieve codecs. Check log files.");
		}
		if (args.Parsers[0].IsMissing)
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine("List of available codecs:");
			for (int i = 0; i < codecs.Length; i++)
			{
				text.AppendLine(codecs[i].Code + " =&gt; " + codecs[i].Name);
			}
			game.ShowChatMessage(text.ToString());
			if (ClientSettings.RecordingCodec != null)
			{
				game.ShowChatMessage("Currently selected codec: " + ClientSettings.RecordingCodec);
			}
			return TextCommandResult.Success();
		}
		string code = args[0] as string;
		for (int j = 0; j < codecs.Length; j++)
		{
			if (code == codecs[j].Code)
			{
				ClientSettings.RecordingCodec = code;
				return TextCommandResult.Success("Codec " + code + " (" + codecs[j].Name + ") set.");
			}
		}
		return TextCommandResult.Success("No such video codec supported.");
	}

	private TextCommandResult VrecCmdToggle(TextCommandCallingArgs args)
	{
		if (!Recording)
		{
			return VrecCmdStart(args);
		}
		return VrecCmdStop(args);
	}

	private TextCommandResult VrecCmdStop(TextCommandCallingArgs args)
	{
		if (!Recording)
		{
			return TextCommandResult.Success();
		}
		Recording = false;
		if (avi != null)
		{
			avi.Close();
			avi = null;
		}
		string path = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
		game.ShowChatMessage(videoFileName + " saved to " + path);
		game.DeltaTimeLimiter = -1f;
		return TextCommandResult.Success("Ok, Video Recording stopped");
	}

	private TextCommandResult VrecCmdStart(TextCommandCallingArgs args)
	{
		if (Recording)
		{
			return TextCommandResult.Success();
		}
		Recording = true;
		string path = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
		try
		{
			if (new DirectoryInfo(path).Parent != null && !Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			avi = game.Platform.CreateAviWriter(ClientSettings.RecordingFrameRate, ClientSettings.RecordingCodec);
			string time = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
			avi.Open(Path.Combine(path, videoFileName = $"{time}.avi"), game.Width, game.Height);
		}
		catch (Exception e)
		{
			Recording = false;
			return TextCommandResult.Success("Could not start recorder: " + e.Message);
		}
		if (ClientSettings.GameTickFrameRate > 0f)
		{
			game.DeltaTimeLimiter = 1f / ClientSettings.GameTickFrameRate;
		}
		return TextCommandResult.Success("Ok, Video Recording now");
	}

	public void OnFinalizeFrame(float dt)
	{
		if (!Recording || avi == null)
		{
			return;
		}
		if (!firstFrameDone)
		{
			firstFrameDone = true;
			return;
		}
		writeAccum += dt;
		float frameRate = ClientSettings.RecordingFrameRate;
		if (writeAccum >= 1f / frameRate)
		{
			writeAccum -= 1f / frameRate;
			avi.AddFrame();
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
