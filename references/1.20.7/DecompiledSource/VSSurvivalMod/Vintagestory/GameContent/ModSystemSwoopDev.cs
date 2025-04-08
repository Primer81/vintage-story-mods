using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemSwoopDev : ModSystem
{
	private ICoreServerAPI sapi;

	private Vec3d[] points = new Vec3d[4];

	private bool plot;

	private Vec3f zero = Vec3f.Zero;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		sapi.ChatCommands.GetOrCreate("dev").BeginSub("swoop").WithDesc("Bezier test thing")
			.BeginSub("start1")
			.WithDesc("Set a bezier point")
			.WithAlias("end1", "start2", "end2")
			.HandleWith(cmdPoint)
			.EndSub()
			.BeginSub("plot")
			.WithDesc("Plot bezier curves with particles")
			.HandleWith(delegate
			{
				plot = !plot;
				return TextCommandResult.Success("plot now " + (plot ? "on" : "off"));
			})
			.EndSub()
			.EndSub();
		sapi.Event.RegisterGameTickListener(onTick1s, 1001, 12);
	}

	private TextCommandResult cmdPoint(TextCommandCallingArgs args)
	{
		string[] names = new string[4] { "start1", "end1", "start2", "end2" };
		points[names.IndexOf(args.SubCmdCode)] = args.Caller.Pos;
		return TextCommandResult.Success("ok set");
	}

	private void onTick1s(float dt)
	{
		if (!(points[0] == null) && !(points[1] == null) && !(points[2] == null) && !(points[2] == null) && plot)
		{
			Vec3d delta1 = points[1] - points[0];
			Vec3d delta2 = points[3] - points[2];
			int its = 50;
			for (int i = 0; i < its; i++)
			{
				double p = (double)i / (double)its;
				Vec3d mid1 = points[0] + p * delta1;
				Vec3d mid2 = points[2] + p * delta2;
				Vec3d bez = (1.0 - p) * mid1 + p * mid2;
				sapi.World.SpawnParticles(1f, -1, bez, bez, zero, zero, 1f, 0f);
			}
			sapi.World.SpawnParticles(1f, ColorUtil.ColorFromRgba(0, 0, 128, 255), points[0], points[0], zero, zero, 1f, 0f);
			sapi.World.SpawnParticles(1f, ColorUtil.ColorFromRgba(0, 0, 255, 255), points[1], points[1], zero, zero, 1f, 0f);
			sapi.World.SpawnParticles(1f, ColorUtil.ColorFromRgba(0, 128, 0, 255), points[2], points[2], zero, zero, 1f, 0f);
			sapi.World.SpawnParticles(1f, ColorUtil.ColorFromRgba(0, 255, 0, 255), points[3], points[3], zero, zero, 1f, 0f);
		}
	}
}
