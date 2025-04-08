using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemSoundEngine : ClientSystem, IRenderer, IDisposable
{
	public static Vec3f Zero = new Vec3f();

	public static float NowReverbness = 0f;

	public static float TargetReverbness = 0f;

	private bool scanning;

	public static Cuboidi RoomLocation = new Cuboidi();

	private AABBIntersectionTest intersectionTester;

	private bool glitchActive;

	private bool prevSubmerged;

	private int prevReverbKey = -999;

	public override string Name => "soen";

	public double RenderOrder => 1.0;

	public int RenderRange => 1;

	public SystemSoundEngine(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(this, EnumRenderStage.Before, "updateAudioListener");
		game.RegisterGameTickListener(OnGameTick100ms, 100);
		game.RegisterGameTickListener(OnGameTick500ms, 500);
		ClientSettings.Inst.AddWatcher<int>("soundLevel", OnSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("entitySoundLevel", OnSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("ambientSoundLevel", OnSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("weatherSoundLevel", OnSoundLevelChanged);
		game.api.ChatCommands.GetOrCreate("debug").BeginSub("sound").BeginSub("list")
			.HandleWith(onListSounds)
			.EndSub()
			.EndSub();
		intersectionTester = new AABBIntersectionTest(new OffthreadBaSupplier(game));
	}

	private TextCommandResult onListSounds(TextCommandCallingArgs args)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Active sounds: ");
		sb.AppendLine("IsPlaying | Location | Sound path");
		foreach (ILoadedSound val in game.ActiveSounds)
		{
			if (!val.IsDisposed)
			{
				sb.AppendLine($"{val.IsPlaying} | {val.Params.Position} | {val.Params.Location.ToShortString()}");
			}
		}
		game.Logger.Notification(sb.ToString());
		return TextCommandResult.Success("Active sounds printed to client-main.txt");
	}

	public override void OnBlockTexturesLoaded()
	{
		game.SoundConfig = game.Platform.AssetManager.TryGet("sounds/soundconfig.json")?.ToObject<SoundConfig>();
		if (game.SoundConfig == null)
		{
			game.SoundConfig = new SoundConfig();
		}
		for (int i = 0; i < game.Blocks.Count; i++)
		{
			Block block = game.Blocks[i];
			if (block != null && !(block.Code == null))
			{
				if (block.Sounds == null)
				{
					block.Sounds = new BlockSounds();
				}
				if (block.Sounds.Walk == null)
				{
					block.Sounds.Walk = game.SoundConfig.defaultBlockSounds.Walk;
				}
				if (block.Sounds.Place == null)
				{
					block.Sounds.Place = game.SoundConfig.defaultBlockSounds.Place;
				}
				if (block.Sounds.Hit == null)
				{
					block.Sounds.Hit = game.SoundConfig.defaultBlockSounds.Hit;
				}
				if (block.Sounds.Break == null)
				{
					block.Sounds.Break = game.SoundConfig.defaultBlockSounds.Break;
				}
			}
		}
	}

	private void OnGameTick500ms(float dt)
	{
		if (game.IsPaused)
		{
			return;
		}
		int count = game.ActiveSounds.Count;
		while (count-- > 0)
		{
			ILoadedSound sound = game.ActiveSounds.Dequeue();
			if (sound == null)
			{
				game.Logger.Error("Found a null sound in the ActiveSounds queue, something is incorrectly programmed. Skipping over it.");
				continue;
			}
			SoundParams @params = sound.Params;
			if (@params != null && @params.DisposeOnFinish && sound.HasStopped && sound.HasReverbStopped(game.ElapsedMilliseconds))
			{
				sound.Dispose();
			}
			else if (!sound.IsDisposed)
			{
				game.ActiveSounds.Enqueue(sound);
			}
		}
		if (!scanning)
		{
			TyronThreadPool.QueueLongDurationTask(scanReverbnessOffthread);
		}
	}

	private void scanReverbnessOffthread()
	{
		scanning = true;
		EntityPos entitypos = game.player.Entity.Pos.Copy().Add(game.player.Entity.LocalEyePos.X, game.player.Entity.LocalEyePos.Y, game.player.Entity.LocalEyePos.Z);
		Vec3d plrpos = game.player.Entity.Pos.XYZ.Add(game.player.Entity.LocalEyePos);
		Vec3d minpos = plrpos.Clone();
		Vec3d maxpos = plrpos.Clone();
		BlockSelection blocksel = new BlockSelection();
		new EntitySelection();
		double nowreverbness = 0.0;
		_ = game.World.BlockAccessor;
		for (float yaw = 0f; yaw < 360f; yaw += 45f)
		{
			for (float pitch = -90f; pitch <= 90f; pitch += 45f)
			{
				int faceIndex = 0;
				faceIndex = ((pitch <= -45f) ? BlockFacing.UP.Index : ((!(pitch >= 45f)) ? BlockFacing.HorizontalFromYaw(yaw).Opposite.Index : BlockFacing.DOWN.Index));
				Ray ray = Ray.FromAngles(plrpos, pitch * ((float)Math.PI / 180f), yaw * ((float)Math.PI / 180f), 35f);
				intersectionTester.LoadRayAndPos(ray);
				float range = (float)ray.Length;
				blocksel = intersectionTester.GetSelectedBlock(range, (BlockPos pos, Block block) => true, testCollide: true);
				Block block2 = blocksel?.Block;
				if (block2 != null && (block2.BlockMaterial == EnumBlockMaterial.Metal || block2.BlockMaterial == EnumBlockMaterial.Ore || block2.BlockMaterial == EnumBlockMaterial.Mantle || block2.BlockMaterial == EnumBlockMaterial.Ice || block2.BlockMaterial == EnumBlockMaterial.Ceramic || block2.BlockMaterial == EnumBlockMaterial.Brick || block2.BlockMaterial == EnumBlockMaterial.Stone) && block2.SideIsSolid(blocksel.Position, faceIndex))
				{
					Vec3d pos2 = blocksel.FullPosition;
					float distance = pos2.DistanceTo(plrpos);
					nowreverbness += (Math.Log(distance + 1f) / 18.0 - 0.07) * 3.0;
					minpos.Set(Math.Min(minpos.X, pos2.X), Math.Min(minpos.Y, pos2.Y), Math.Min(minpos.Z, pos2.Z));
					maxpos.Set(Math.Max(maxpos.X, pos2.X), Math.Max(maxpos.Y, pos2.Y), Math.Max(maxpos.Z, pos2.Z));
				}
				else
				{
					nowreverbness -= 0.2;
					entitypos.Yaw = yaw;
					entitypos.Pitch = pitch;
					entitypos.AheadCopy(35.0);
					minpos.Set(Math.Min(minpos.X, entitypos.X), Math.Min(minpos.Y, entitypos.InternalY), Math.Min(minpos.Z, entitypos.Z));
					maxpos.Set(Math.Max(maxpos.X, entitypos.X), Math.Max(maxpos.Y, entitypos.InternalY), Math.Max(maxpos.Z, entitypos.Z));
				}
			}
		}
		TargetReverbness = (float)nowreverbness;
		RoomLocation = new Cuboidi(minpos.AsBlockPos, maxpos.AsBlockPos).GrowBy(10, 10, 10);
		scanning = false;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		Vec3f look = game.EntityPlayer.Pos.GetViewVector();
		Vec3d eyesPos = game.EntityPlayer.Pos.XYZ.Add(game.EntityPlayer.LocalEyePos);
		game.Platform.UpdateAudioListener((float)eyesPos.X, (float)eyesPos.Y, (float)eyesPos.Z, look.X, 0f, look.Z);
		NowReverbness += (TargetReverbness - NowReverbness) * deltaTime / 1.5f;
	}

	private void OnGameTick100ms(float dt)
	{
		if (game.api.renderapi.ShaderUniforms.GlitchStrength > 0.5f)
		{
			glitchActive = true;
			float str = GameMath.Clamp(game.api.renderapi.ShaderUniforms.GlitchStrength * 2f, 0f, 1f);
			foreach (ILoadedSound val4 in game.ActiveSounds)
			{
				if (val4.Params.SoundType != EnumSoundType.SoundGlitchunaffected && val4.Params.SoundType != EnumSoundType.AmbientGlitchunaffected && val4.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
				{
					float rnd = (float)game.Rand.NextDouble() * 0.75f;
					int dir = game.Rand.Next(2) * 2 - 1;
					val4.SetPitchOffset(GameMath.Mix(0f, rnd * (float)dir - 0.2f, str));
				}
			}
		}
		else if (glitchActive)
		{
			glitchActive = false;
			foreach (ILoadedSound val5 in game.ActiveSounds)
			{
				if (val5.Params.SoundType != EnumSoundType.SoundGlitchunaffected && val5.Params.SoundType != EnumSoundType.AmbientGlitchunaffected && val5.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
				{
					val5.SetPitchOffset(0f);
				}
			}
		}
		if (submerged() && !prevSubmerged)
		{
			prevSubmerged = true;
			foreach (ILoadedSound val2 in game.ActiveSounds)
			{
				if (!val2.IsDisposed)
				{
					val2.SetLowPassfiltering(0.06f);
					if (!glitchActive && val2.Params.SoundType != EnumSoundType.Music && val2.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
					{
						val2.SetPitchOffset(-0.15f);
					}
				}
			}
		}
		else if (prevSubmerged && !submerged())
		{
			prevSubmerged = false;
			foreach (ILoadedSound val3 in game.ActiveSounds)
			{
				if (!val3.IsDisposed)
				{
					val3.SetLowPassfiltering(1f);
					if (!glitchActive)
					{
						val3.SetPitchOffset(0f);
					}
				}
			}
		}
		if (prevReverbKey == reverbKey())
		{
			return;
		}
		prevReverbKey = reverbKey();
		foreach (ILoadedSound val in game.ActiveSounds)
		{
			if (!val.IsDisposed && val.IsReady)
			{
				if (val.Params.Position == null || val.Params.Position == Zero || RoomLocation.ContainsOrTouches(val.Params.Position))
				{
					val.SetReverb(Math.Max(0f, NowReverbness));
				}
				else
				{
					val.SetReverb(0f);
				}
			}
		}
	}

	private int reverbKey()
	{
		if (!submerged())
		{
			return (int)(NowReverbness * 10f);
		}
		return 0;
	}

	private bool submerged()
	{
		if (!(game.EyesInWaterDepth() > 0f))
		{
			return game.EyesInLavaDepth() > 0f;
		}
		return true;
	}

	public override void Dispose(ClientMain game)
	{
		while (game.ActiveSounds.Count > 0)
		{
			ILoadedSound loadedSound = game.ActiveSounds.Dequeue();
			loadedSound?.Stop();
			loadedSound?.Dispose();
		}
	}

	private void OnSoundLevelChanged(int newValue)
	{
		int count = game.ActiveSounds.Count;
		while (count-- > 0)
		{
			ILoadedSound sound = game.ActiveSounds.Dequeue();
			sound.SetVolume();
			game.ActiveSounds.Enqueue(sound);
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public void Dispose()
	{
	}
}
