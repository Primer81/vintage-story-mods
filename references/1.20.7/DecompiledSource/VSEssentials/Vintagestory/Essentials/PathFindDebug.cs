using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Essentials;

public class PathFindDebug : ModSystem
{
	private BlockPos start;

	private BlockPos end;

	private ICoreServerAPI sapi;

	private EnumAICreatureType ct;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.ChatCommands.GetOrCreate("debug").BeginSub("astar").WithDesc("A* path finding debug testing tool")
			.RequiresPrivilege(Privilege.controlserver)
			.RequiresPlayer()
			.WithArgs(api.ChatCommands.Parsers.WordRange("command", "start", "end", "bench", "clear", "ct"), api.ChatCommands.Parsers.OptionalWord("creature type"))
			.HandleWith(onAstarCmd)
			.EndSub();
	}

	private TextCommandResult onAstarCmd(TextCommandCallingArgs args)
	{
		string subcmd = (string)args[0];
		IPlayer player = args.Caller.Player;
		BlockPos plrPos = player.Entity.ServerPos.XYZ.AsBlockPos;
		PathfindSystem pfs = sapi.ModLoader.GetModSystem<PathfindSystem>();
		Cuboidf cuboidf = new Cuboidf(-0.4f, 0f, -0.4f, 0.4f, 1.5f, 0.4f);
		new Cuboidf(-0.2f, 0f, -0.2f, 0.2f, 1.5f, 0.2f);
		new Cuboidf(-0.6f, 0f, -0.6f, 0.6f, 1.5f, 0.6f);
		Cuboidf collbox = cuboidf;
		int maxFallHeight = 3;
		float stepHeight = 1.01f;
		switch (subcmd)
		{
		case "ct":
		{
			string ct = (string)args[1];
			if (ct == null)
			{
				return TextCommandResult.Success($"Current creature type is {ct}");
			}
			if (Enum.TryParse<EnumAICreatureType>(ct, out var ect))
			{
				this.ct = ect;
				return TextCommandResult.Success($"Creature type set to {ct}");
			}
			return TextCommandResult.Error($"Not a vaild enum type");
		}
		case "start":
			start = plrPos.Copy();
			sapi.World.HighlightBlocks(player, 26, new List<BlockPos> { start }, new List<int> { ColorUtil.ColorFromRgba(255, 255, 0, 128) });
			break;
		case "end":
			end = plrPos.Copy();
			sapi.World.HighlightBlocks(player, 27, new List<BlockPos> { end }, new List<int> { ColorUtil.ColorFromRgba(255, 0, 255, 128) });
			break;
		case "bench":
		{
			if (start == null || end == null)
			{
				return TextCommandResult.Error("Start/End not set");
			}
			Stopwatch sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < 15; i++)
			{
				pfs.FindPath(start, end, maxFallHeight, stepHeight, collbox);
			}
			sw.Stop();
			float timeMs2 = (float)sw.ElapsedMilliseconds / 15f;
			return TextCommandResult.Success($"15 searches average: {(int)timeMs2} ms");
		}
		case "clear":
			start = null;
			end = null;
			sapi.World.HighlightBlocks(player, 2, new List<BlockPos>());
			sapi.World.HighlightBlocks(player, 26, new List<BlockPos>());
			sapi.World.HighlightBlocks(player, 27, new List<BlockPos>());
			break;
		}
		if (start == null || end == null)
		{
			sapi.World.HighlightBlocks(player, 2, new List<BlockPos>());
		}
		if (start != null && end != null)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			List<PathNode> nodes = pfs.FindPath(start, end, maxFallHeight, stepHeight, collbox, this.ct);
			stopwatch.Stop();
			int timeMs = (int)stopwatch.ElapsedMilliseconds;
			string message = $"Search took {timeMs} ms, {pfs.astar.NodesChecked} nodes checked";
			if (nodes == null)
			{
				sapi.World.HighlightBlocks(player, 2, new List<BlockPos>());
				sapi.World.HighlightBlocks(player, 3, new List<BlockPos>());
				return TextCommandResult.Success(message + "\nNo path found");
			}
			List<BlockPos> poses = new List<BlockPos>();
			foreach (PathNode node2 in nodes)
			{
				poses.Add(node2);
			}
			sapi.World.HighlightBlocks(player, 2, poses, new List<int> { ColorUtil.ColorFromRgba(128, 128, 128, 30) });
			List<Vec3d> list = pfs.ToWaypoints(nodes);
			poses = new List<BlockPos>();
			foreach (Vec3d node in list)
			{
				poses.Add(node.AsBlockPos);
			}
			sapi.World.HighlightBlocks(player, 3, poses, new List<int> { ColorUtil.ColorFromRgba(128, 0, 0, 100) });
			return TextCommandResult.Success(message);
		}
		return TextCommandResult.Success();
	}
}
