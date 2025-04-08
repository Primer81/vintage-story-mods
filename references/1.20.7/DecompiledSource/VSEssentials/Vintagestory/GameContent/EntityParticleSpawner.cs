using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityParticleSpawner : ModSystem
{
	private ICoreClientAPI capi;

	private Random rand = new Random();

	private NormalizedSimplexNoise grasshopperNoise;

	private NormalizedSimplexNoise cicadaNoise;

	private NormalizedSimplexNoise matingGnatsSwarmNoise;

	private NormalizedSimplexNoise coquiNoise;

	private NormalizedSimplexNoise waterstriderNoise;

	private Queue<Action> SimTickExecQueue = new Queue<Action>();

	public HashSet<string> disabledInsects;

	private EntityParticleSystem sys;

	private float accum;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		grasshopperNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 100);
		coquiNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.0025, 0.9, api.World.Seed * 101);
		waterstriderNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 102);
		matingGnatsSwarmNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 103);
		cicadaNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 0.01, 0.9, api.World.Seed * 104);
		sys = api.ModLoader.GetModSystem<EntityParticleSystem>();
		sys.OnSimTick += Sys_OnSimTick;
		disabledInsects = new HashSet<string>();
		List<string> dis = capi.Settings.Strings["disabledInsects"];
		if (dis != null)
		{
			disabledInsects.AddRange(dis);
		}
		api.ChatCommands.GetOrCreate("insectconfig").WithArgs(api.ChatCommands.Parsers.WordRange("type", "grasshopper", "cicada", "gnats", "coqui", "waterstrider"), api.ChatCommands.Parsers.OptionalBool("enable/disable")).HandleWith(onCmdInsectConfig);
		api.ChatCommands.GetOrCreate("debug").BeginSub("eps").BeginSub("testspawn")
			.WithArgs(api.ChatCommands.Parsers.WordRange("type", "gh", "ws", "coq", "mg", "cic", "fis"))
			.HandleWith(handleSpawn)
			.EndSub()
			.BeginSub("count")
			.HandleWith(handleCount)
			.EndSub()
			.BeginSub("clear")
			.HandleWith(handleClear)
			.EndSub()
			.BeginSub("testnoise")
			.HandleWith(handleTestnoise)
			.WithArgs(api.ChatCommands.Parsers.OptionalWordRange("clear", "clear"))
			.EndSub()
			.EndSub();
	}

	private TextCommandResult onCmdInsectConfig(TextCommandCallingArgs args)
	{
		string type = (string)args[0];
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success(Lang.Get("{0} are currently {1}", type, disabledInsects.Contains(type) ? Lang.Get("disabled") : Lang.Get("enabled")));
		}
		bool disabled = !(bool)args[1];
		if (disabled)
		{
			disabledInsects.Add(type);
		}
		else
		{
			disabledInsects.Remove(type);
		}
		capi.Settings.Strings["disabledInsects"] = disabledInsects.ToList();
		return TextCommandResult.Success(Lang.Get("{0} are now {1}", type, disabled ? Lang.Get("disabled") : Lang.Get("enabled")));
	}

	private TextCommandResult handleCount(TextCommandCallingArgs args)
	{
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<string, int> val in sys.Count.Dict)
		{
			sb.AppendLine($"{val.Key}: {val.Value}");
		}
		if (sb.Length == 0)
		{
			return TextCommandResult.Success("No entityparticle alive");
		}
		return TextCommandResult.Success(sb.ToString());
	}

	private TextCommandResult handleTestnoise(TextCommandCallingArgs args)
	{
		BlockPos pos = capi.World.Player.Entity.Pos.XYZ.AsBlockPos;
		Block block = capi.World.GetBlock(new AssetLocation("creativeblock-35"));
		bool clear = !args.Parsers[0].IsMissing;
		for (int dx = -200; dx <= 200; dx++)
		{
			for (int dz = -200; dz <= 200; dz++)
			{
				double noise = matingGnatsSwarmNoise.Noise(pos.X + dx, pos.Z + dz);
				if (clear || noise < 0.65)
				{
					capi.World.BlockAccessor.SetBlock(0, new BlockPos(pos.X + dx, 160, pos.Z + dz));
				}
				else
				{
					capi.World.BlockAccessor.SetBlock(block.Id, new BlockPos(pos.X + dx, 160, pos.Z + dz));
				}
			}
		}
		return TextCommandResult.Success("testnoise");
	}

	private TextCommandResult handleClear(TextCommandCallingArgs args)
	{
		sys.Clear();
		return TextCommandResult.Success("cleared");
	}

	private TextCommandResult handleSpawn(TextCommandCallingArgs args)
	{
		string type = args[0] as string;
		SimTickExecQueue.Enqueue(delegate
		{
			EntityPos pos = capi.World.Player.Entity.Pos;
			ClimateCondition climateAt = capi.World.BlockAccessor.GetClimateAt(pos.AsBlockPos);
			float cohesion = (float)GameMath.Max(rand.NextDouble() * 1.1, 0.25);
			Vec3d vec3d = pos.XYZ.AddCopy(0f, 1.5f, 0f);
			for (int i = 0; i < 20; i++)
			{
				double num = pos.X + (rand.NextDouble() - 0.5) * 10.0;
				double num2 = pos.Z + (rand.NextDouble() - 0.5) * 10.0;
				double num3 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num, (int)num2);
				if (type == "gh")
				{
					EntityParticleGrasshopper eparticle = new EntityParticleGrasshopper(capi, num, num3 + 1.0 + rand.NextDouble() * 0.25, num2);
					sys.SpawnParticle(eparticle);
				}
				if (type == "coq")
				{
					EntityParticleCoqui eparticle2 = new EntityParticleCoqui(capi, num, num3 + 1.0 + rand.NextDouble() * 0.25, num2);
					sys.SpawnParticle(eparticle2);
				}
				if (type == "ws")
				{
					Block block = capi.World.BlockAccessor.GetBlock((int)num, (int)num3, (int)num2, 2);
					if (block.LiquidCode == "water" && block.PushVector == null)
					{
						EntityParticleWaterStrider eparticle3 = new EntityParticleWaterStrider(capi, num, num3 + (double)((float)block.LiquidLevel / 8f), num2);
						sys.SpawnParticle(eparticle3);
					}
				}
				if (type == "fis")
				{
					num = pos.X + (rand.NextDouble() - 0.5) * 2.0;
					num2 = pos.Z + (rand.NextDouble() - 0.5) * 2.0;
					Block block2 = capi.World.BlockAccessor.GetBlock((int)num, (int)num3, (int)num2, 2);
					if (block2.LiquidCode == "saltwater" && block2.PushVector == null)
					{
						EntityParticleFish eparticle4 = new EntityParticleFish(capi, num, num3 - (double)block2.LiquidLevel, num2);
						sys.SpawnParticle(eparticle4);
					}
				}
				if (type == "mg")
				{
					sys.SpawnParticle(new EntityParticleMatingGnats(capi, cohesion, vec3d.X, vec3d.Y, vec3d.Z));
				}
				if (type == "cic")
				{
					spawnCicadas(pos, climateAt);
				}
			}
		});
		return TextCommandResult.Success(type + " spawned.");
	}

	private void Sys_OnSimTick(float dt)
	{
		accum += dt;
		while (SimTickExecQueue.Count > 0)
		{
			SimTickExecQueue.Dequeue()();
		}
		if (accum > 0.5f)
		{
			accum = 0f;
			EntityPos pos = capi.World.Player.Entity.Pos;
			ClimateCondition climate = capi.World.BlockAccessor.GetClimateAt(pos.AsBlockPos);
			if (!disabledInsects.Contains("grasshopper"))
			{
				spawnGrasshoppers(pos, climate);
			}
			if (!disabledInsects.Contains("cicada"))
			{
				spawnCicadas(pos, climate);
			}
			if (!disabledInsects.Contains("gnats"))
			{
				spawnMatingGnatsSwarm(pos, climate);
			}
			if (!disabledInsects.Contains("coqui"))
			{
				spawnCoquis(pos, climate);
			}
			if (!disabledInsects.Contains("waterstrider"))
			{
				spawnWaterStriders(pos, climate);
			}
		}
	}

	private void spawnWaterStriders(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature > 35f || climate.Temperature < 19f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.5 || waterstriderNoise.Noise(pos.X, pos.Z) < 0.5 || sys.Count["waterStrider"] > 50)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double x = pos.X + (rand.NextDouble() - 0.5) * 60.0;
			double z = pos.Z + (rand.NextDouble() - 0.5) * 60.0;
			double y = capi.World.BlockAccessor.GetRainMapHeightAt((int)x, (int)z);
			if (!(pos.HorDistanceTo(new Vec3d(x, y, z)) < 3.0))
			{
				Block block = capi.World.BlockAccessor.GetBlock((int)x, (int)y, (int)z, 2);
				Block belowblock = capi.World.BlockAccessor.GetBlock((int)x, (int)y - 1, (int)z);
				Block aboveblock = capi.World.BlockAccessor.GetBlock((int)x, (int)y + 1, (int)z);
				if (block.LiquidCode == "water" && block.PushVector == null && belowblock.Replaceable < 6000 && aboveblock.Id == 0)
				{
					EntityParticleWaterStrider ws = new EntityParticleWaterStrider(capi, x, y + (double)((float)block.LiquidLevel / 8f), z);
					sys.SpawnParticle(ws);
				}
			}
		}
	}

	private void spawnFish(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature > 40f || climate.Temperature < 0f)
		{
			return;
		}
		BlockPos bpos = new BlockPos();
		if (sys.Count["fish"] > 100)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double x = pos.X + (rand.NextDouble() - 0.5) * 40.0;
			double z = pos.Z + (rand.NextDouble() - 0.5) * 40.0;
			bpos.Set((int)x, 0, (int)z);
			int y = capi.World.BlockAccessor.GetTerrainMapheightAt(bpos);
			bpos.Y = Math.Min(capi.World.SeaLevel - 1, y + 2);
			if (bpos.HorDistanceSqTo(pos.X, pos.Z) < 3f)
			{
				continue;
			}
			Block block = capi.World.BlockAccessor.GetBlock(bpos, 2);
			Block belowBlock = capi.World.BlockAccessor.GetBlock(bpos.DownCopy());
			if (block.LiquidCode == "saltwater" && belowBlock.Code.PathStartsWith("coral"))
			{
				int fishAmount = 10 + rand.Next(10);
				for (int j = 0; j < fishAmount; j++)
				{
					double offX = rand.NextDouble() - 0.5;
					double offZ = rand.NextDouble() - 0.5;
					EntityParticleFish ws = new EntityParticleFish(capi, x + offX, bpos.Y, z + offZ);
					sys.SpawnParticle(ws);
				}
			}
		}
	}

	private void spawnGrasshoppers(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature >= 30f || climate.Temperature < 18f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.5 || grasshopperNoise.Noise(pos.X, pos.Z) < 0.7 || sys.Count["grassHopper"] > 40)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double x = pos.X + (rand.NextDouble() - 0.5) * 60.0;
			double z = pos.Z + (rand.NextDouble() - 0.5) * 60.0;
			double y = capi.World.BlockAccessor.GetRainMapHeightAt((int)x, (int)z);
			if (!(pos.HorDistanceTo(new Vec3d(x, y, z)) < 3.0))
			{
				Block block = capi.World.BlockAccessor.GetBlock((int)x, (int)y + 1, (int)z);
				Block belowblock = capi.World.BlockAccessor.GetBlock((int)x, (int)y, (int)z);
				if (block.BlockMaterial == EnumBlockMaterial.Plant && belowblock.BlockMaterial == EnumBlockMaterial.Soil)
				{
					EntityParticleGrasshopper gh = new EntityParticleGrasshopper(capi, x, y + 1.01 + rand.NextDouble() * 0.25, z);
					sys.SpawnParticle(gh);
				}
			}
		}
	}

	private void spawnCicadas(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature > 33f || climate.Temperature < 22f || climate.WorldGenTemperature < 10f || climate.WorldGenTemperature > 22f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.5 || cicadaNoise.Noise(pos.X, pos.Z, capi.World.Calendar.Year) < 0.7 || sys.Count["cicada"] > 40)
		{
			return;
		}
		for (int i = 0; i < 400; i++)
		{
			double x = pos.X + (rand.NextDouble() - 0.5) * 50.0;
			double z = pos.Z + (rand.NextDouble() - 0.5) * 50.0;
			double y = pos.Y + (rand.NextDouble() - 0.5) * 10.0;
			if (pos.HorDistanceTo(new Vec3d(x, y, z)) < 2.0)
			{
				continue;
			}
			Block block = capi.World.BlockAccessor.GetBlock((int)x, (int)y, (int)z);
			Block blockbelow = capi.World.BlockAccessor.GetBlock((int)x, (int)y - 1, (int)z);
			if (block.BlockMaterial != EnumBlockMaterial.Wood || !(block.Variant["type"] == "grown") || blockbelow.Id != block.Id)
			{
				continue;
			}
			Vec3f face = BlockFacing.HORIZONTALS[rand.Next(4)].Normalf;
			double sx = (float)(int)x + 0.5f + face.X * 0.52f;
			double sy = y + 0.1 + rand.NextDouble() * 0.8;
			double sz = (float)(int)z + 0.5f + face.Z * 0.52f;
			if (capi.World.BlockAccessor.GetBlock((int)sx, (int)sy, (int)sz).Replaceable >= 6000)
			{
				EntityParticleCicada gh = new EntityParticleCicada(capi, sx, sy, sz);
				sys.SpawnParticle(gh);
				continue;
			}
			sx += (double)face.X;
			sz += (double)face.Z;
			if (capi.World.BlockAccessor.GetBlock((int)sx, (int)sy, (int)sz).Replaceable >= 6000)
			{
				EntityParticleCicada gh2 = new EntityParticleCicada(capi, sx, sy, sz);
				sys.SpawnParticle(gh2);
			}
		}
	}

	private void spawnCoquis(EntityPos pos, ClimateCondition climate)
	{
		if (climate.WorldGenTemperature < 30f || (double)climate.WorldgenRainfall < 0.7 || coquiNoise.Noise(pos.X, pos.Z) < 0.8 || sys.Count["coqui"] > 60)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			double x = pos.X + (rand.NextDouble() - 0.5) * 60.0;
			double z = pos.Z + (rand.NextDouble() - 0.5) * 60.0;
			double y = capi.World.BlockAccessor.GetRainMapHeightAt((int)x, (int)z);
			if (!(pos.HorDistanceTo(new Vec3d(x, y, z)) < 3.0))
			{
				Block block = capi.World.BlockAccessor.GetBlock((int)x, (int)y + 1, (int)z);
				Block belowblock = capi.World.BlockAccessor.GetBlock((int)x, (int)y, (int)z);
				if (block.BlockMaterial == EnumBlockMaterial.Plant && belowblock.BlockMaterial == EnumBlockMaterial.Soil)
				{
					EntityParticleCoqui gh = new EntityParticleCoqui(capi, x, y + 1.01 + rand.NextDouble() * 0.25, z);
					sys.SpawnParticle(gh);
				}
			}
		}
	}

	private void spawnMatingGnatsSwarm(EntityPos pos, ClimateCondition climate)
	{
		if (climate.Temperature < 17f || climate.Rainfall > 0.1f || (double)climate.WorldgenRainfall < 0.6 || GlobalConstants.CurrentWindSpeedClient.Length() > 0.35f || matingGnatsSwarmNoise.Noise(pos.X, pos.Z) < 0.5 || sys.Count["matinggnats"] > 200)
		{
			return;
		}
		int spawns = 0;
		for (int i = 0; i < 100; i++)
		{
			if (spawns >= 6)
			{
				break;
			}
			double x = pos.X + (rand.NextDouble() - 0.5) * 24.0;
			double z = pos.Z + (rand.NextDouble() - 0.5) * 24.0;
			double y = capi.World.BlockAccessor.GetRainMapHeightAt((int)x, (int)z);
			if (pos.HorDistanceTo(new Vec3d(x, y, z)) < 2.0)
			{
				continue;
			}
			Block ab2block = capi.World.BlockAccessor.GetBlock((int)x, (int)y + 2, (int)z);
			Block abblock = capi.World.BlockAccessor.GetBlock((int)x, (int)y + 1, (int)z);
			Block block = capi.World.BlockAccessor.GetBlock((int)x, (int)y, (int)z, 2);
			Block belowf2block = capi.World.BlockAccessor.GetBlock((int)x, (int)y - 2, (int)z, 2);
			if (block.LiquidCode == "water" && abblock.Id == 0 && ab2block.Id == 0 && belowf2block.Id == 0)
			{
				float cohesion = (float)GameMath.Max(rand.NextDouble() * 1.1, 0.25) / 2f;
				int cnt = 10 + rand.Next(21);
				for (int j = 0; j < cnt; j++)
				{
					sys.SpawnParticle(new EntityParticleMatingGnats(capi, cohesion, (double)(int)x + 0.5, y + 1.5 + rand.NextDouble() * 0.5, (double)(int)z + 0.5));
				}
				spawns++;
			}
		}
	}
}
