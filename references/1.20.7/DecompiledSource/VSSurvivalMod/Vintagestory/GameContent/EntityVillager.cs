using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityVillager : EntityTradingHumanoid, ITalkUtil
{
	public EntityTalkUtil talkUtil;

	private MusicTrack track;

	private long handlerId;

	private long startLoadingMs;

	private bool wasStopped;

	public OrderedDictionary<string, TraderPersonality> Personalities => base.Properties.Attributes["personalities"].AsObject<OrderedDictionary<string, TraderPersonality>>();

	public string Personality
	{
		get
		{
			return WatchedAttributes.GetString("personality", "balanced");
		}
		set
		{
			OrderedDictionary<string, TraderPersonality> ps = Personalities;
			WatchedAttributes.SetString("personality", value);
			TalkUtil?.SetModifiers(ps[value].ChordDelayMul, ps[value].PitchModifier, ps[value].VolumneModifier);
		}
	}

	public string VoiceSound
	{
		get
		{
			if (!WatchedAttributes.HasAttribute("voiceSound"))
			{
				string[] sounds = base.Properties.Attributes["voiceSounds"].AsStringArray();
				int index = Api.World.Rand.Next(sounds.Length);
				string sound = sounds[index];
				WatchedAttributes.SetString("voiceSound", sound);
				TalkUtil.soundName = AssetLocation.Create(sound, Code.Domain);
				return sound;
			}
			return WatchedAttributes.GetString("voiceSound");
		}
		set
		{
			WatchedAttributes.SetString("voiceSound", value);
			TalkUtil.soundName = AssetLocation.Create(value, Code.Domain);
		}
	}

	public override EntityTalkUtil TalkUtil => talkUtil;

	public EntityVillager()
	{
		AnimManager = new PersonalizedAnimationManager();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		(AnimManager as PersonalizedAnimationManager).All = true;
		string personality = null;
		if (api.Side == EnumAppSide.Server)
		{
			JsonObject personalityAttribute = properties.Attributes["personality"];
			personality = ((!personalityAttribute.Exists) ? Personalities.GetKeyAtIndex(api.World.Rand.Next(Personalities.Count)) : personalityAttribute.AsString());
			(AnimManager as PersonalizedAnimationManager).Personality = personality;
			WatchedAttributes.SetString("personality", personality);
		}
		base.Initialize(properties, api, InChunkIndex3d);
		if (api.Side == EnumAppSide.Client)
		{
			personality = Personality;
			bool isMultiSoundVoice = true;
			talkUtil = new EntityTalkUtil(api as ICoreClientAPI, this, isMultiSoundVoice);
			TalkUtil.soundName = AssetLocation.Create(VoiceSound, Code.Domain);
		}
		Personality = personality;
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
		if (packetid == 199)
		{
			if (!Alive)
			{
				return;
			}
			TalkUtil.Talk(EnumTalkType.Hurt);
		}
		if (packetid == 198)
		{
			TalkUtil.Talk(EnumTalkType.Death);
		}
		if (packetid == 201)
		{
			if (track != null)
			{
				return;
			}
			SongPacket pkt = SerializerUtil.Deserialize<SongPacket>(data);
			ICoreClientAPI capi = Api as ICoreClientAPI;
			startLoadingMs = Api.World.ElapsedMilliseconds;
			wasStopped = false;
			track = capi.StartTrack(AssetLocation.Create(pkt.SoundLocation), 99f, EnumSoundType.MusicGlitchunaffected, delegate(ILoadedSound s)
			{
				onTrackLoaded(s, pkt.SecondsPassed);
			});
		}
		if (packetid == 202)
		{
			track?.Stop();
			track = null;
			Api.Event.UnregisterCallback(handlerId);
			wasStopped = true;
			TalkUtil.ShouldDoIdleTalk = true;
		}
		if (packetid == 203)
		{
			TalkUtil.Talk((EnumTalkType)SerializerUtil.Deserialize<int>(data));
		}
	}

	private void onTrackLoaded(ILoadedSound sound, float secondsPassed)
	{
		if (track == null)
		{
			sound?.Dispose();
		}
		else
		{
			if (sound == null)
			{
				return;
			}
			track.Sound = sound;
			TalkUtil.ShouldDoIdleTalk = false;
			Api.Event.EnqueueMainThreadTask(delegate
			{
				if (track != null)
				{
					track.loading = true;
				}
			}, "settrackloading");
			long longMsPassed = Api.World.ElapsedMilliseconds - startLoadingMs;
			handlerId = Api.Event.RegisterCallback(delegate
			{
				if (sound.IsDisposed)
				{
					handlerId = 0L;
					track = null;
				}
				else
				{
					if (!wasStopped)
					{
						sound.Start();
						sound.PlaybackPosition = secondsPassed;
					}
					track.loading = false;
				}
			}, (int)Math.Max(0L, 500 - longMsPassed));
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		track?.Stop();
		track = null;
		Api.Event.UnregisterCallback(handlerId);
		wasStopped = true;
		base.OnEntityDespawn(despawn);
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (World.Side != EnumAppSide.Client)
		{
			return;
		}
		TalkUtil.OnGameTick(dt);
		if (track?.Sound != null && track.Sound.IsPlaying)
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			Vec3d plrpos = capi.World.Player.Entity?.Pos?.XYZ;
			if (!(plrpos == null))
			{
				float dist = GameMath.Sqrt(plrpos.SquareDistanceTo(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5));
				float volume = GameMath.Clamp(1f / (float)Math.Log10(Math.Max(1.0, (double)dist * 0.7)) - 0.8f, 0f, 1f);
				track.Sound.SetVolume(volume);
				track.Sound.SetPitch(GameMath.Clamp(1f - capi.Render.ShaderUniforms.GlitchStrength, 0.1f, 1f));
			}
		}
		else
		{
			TalkUtil.ShouldDoIdleTalk = true;
		}
	}

	public override void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
	{
		if (!(type == "idle") || track == null || !track.IsActive)
		{
			base.PlayEntitySound(type, dualCallByPlayer, randomizePitch, range);
		}
	}

	public override void OnHurt(DamageSource dmgSource, float damage)
	{
		if (World.Side != EnumAppSide.Server)
		{
			return;
		}
		foreach (KeyValuePair<int, IEntityActivity> item in GetBehavior<EntityBehaviorActivityDriven>().ActivitySystem.ActiveActivitiesBySlot)
		{
			item.Value.CurrentAction?.OnHurt(dmgSource, damage);
		}
		base.OnHurt(dmgSource, damage);
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		(AnimManager as PersonalizedAnimationManager).Personality = Personality;
	}

	public override string GetInfoText()
	{
		string text = base.GetInfoText();
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			text = text.TrimEnd();
			text = text + "\n<font color=\"#bbbbbb\">Personality: " + Personality + "\nVoice: " + VoiceSound + "</font>";
		}
		return text;
	}
}
