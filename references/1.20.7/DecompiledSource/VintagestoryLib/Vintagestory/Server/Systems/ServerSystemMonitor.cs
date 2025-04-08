using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server.Network;

namespace Vintagestory.Server.Systems;

public class ServerSystemMonitor : ServerSystem
{
	private int errors;

	private long listener;

	private Process? currentProcess;

	private int accumTick;

	public ServerSystemMonitor(ServerMain server)
		: base(server)
	{
	}

	public override void OnBeginConfiguration()
	{
		server.api.ChatCommands.GetOrCreate("ipblock").WithDescription("Manage the ip block list. This list will be cleared automatically every 10 minutes.").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("clear")
			.WithDescription("Clear the current ip block list.")
			.HandleWith(OnClearList)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDescription("Print the current ip block list.")
			.HandleWith(OnList)
			.EndSubCommand();
	}

	private TextCommandResult OnList(TextCommandCallingArgs args)
	{
		StringBuilder sb = new StringBuilder();
		string[] array = TcpNetConnection.blockedIps.ToArray();
		foreach (string ip in array)
		{
			sb.AppendLine(ip);
		}
		return TextCommandResult.Success(sb.ToString());
	}

	private TextCommandResult OnClearList(TextCommandCallingArgs args)
	{
		int count = TcpNetConnection.blockedIps.Count;
		TcpNetConnection.blockedIps.Clear();
		return TextCommandResult.Success($"Cleared {count} IPs from the block list.");
	}

	private void OnEvery60sec(float obj)
	{
		currentProcess.Refresh();
		float num = (float)currentProcess.WorkingSet64 / 1024f / 1024f;
		if ((double)num > (double)server.Config.DieAboveMemoryUsageMb * 0.9)
		{
			ServerMain.Logger.Warning("The server is currently using more than 90% of its maximum allowed memory. If usage reaches 100% (" + server.Config.DieAboveMemoryUsageMb + " MB), the server will shut down automatically.");
			server.BroadcastMessageToAllGroups("<strong><font color=\"orange\">The server is currently using more than 90% of its maximum allowed memory. If usage reaches 100%, the server will shut down automatically.</font></strong>", EnumChatType.AllGroups);
		}
		if (num > (float)server.Config.DieAboveMemoryUsageMb)
		{
			ServerMain.Logger.Notification(TcpNetConnection.blockedIps.Count + " ips were blocked.");
			server.Stop("Server is consuming too much RAM", "Server is consuming more then " + server.Config.DieAboveMemoryUsageMb + " MB of RAM", EnumLogType.Error);
		}
		accumTick++;
		if (accumTick % 15 == 0)
		{
			if (TcpNetConnection.blockedIps.Count > 0)
			{
				ServerMain.Logger.Notification(TcpNetConnection.blockedIps.Count + " IP's were blocked. Clearing the temporary block list now.");
				TcpNetConnection.blockedIps.Clear();
			}
			if (server.RecentClientLogins.Count > 0)
			{
				ServerMain.Logger.Notification(server.RecentClientLogins.Count + " IP's send Connection Attempts too fast. Clearing the list now.");
				server.RecentClientLogins.Clear();
			}
			accumTick = 0;
		}
	}

	public override void OnLoadAssets()
	{
		server.api.Logger.EntryAdded += OnEntryAdded;
		if (server.IsDedicatedServer)
		{
			currentProcess = Process.GetCurrentProcess();
			listener = server.RegisterGameTickListener(OnEvery60sec, 60000);
		}
	}

	public override void Dispose()
	{
		server.api.Logger.EntryAdded -= OnEntryAdded;
		server.UnregisterGameTickListener(listener);
		currentProcess?.Dispose();
		currentProcess = null;
	}

	private void OnEntryAdded(EnumLogType logtype, string message, object[] args)
	{
		if (logtype == EnumLogType.Error || logtype == EnumLogType.Fatal)
		{
			errors++;
			if (errors > server.Config.DieAboveErrorCount)
			{
				string msg = $"More then {server.Config.DieAboveErrorCount} errors detected. Shutting down now. Threshold can be changed in serverconfig.json \"DieAboveErrorCount\"";
				server.Stop("Too many errors detected. See server-main.log file", msg, EnumLogType.Error);
			}
		}
	}
}
