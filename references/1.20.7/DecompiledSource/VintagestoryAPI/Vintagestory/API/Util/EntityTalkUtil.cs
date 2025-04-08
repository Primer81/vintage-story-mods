using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Util;

public class EntityTalkUtil
{
	public const int TalkPacketId = 1231;

	protected int lettersLeftToTalk;

	protected int totalLettersToTalk;

	protected int currentLetterInWord;

	protected int totalLettersTalked;

	protected float chordDelay;

	protected bool LongNote;

	protected Dictionary<EnumTalkType, float> TalkSpeed;

	protected EnumTalkType talkType;

	protected ICoreServerAPI sapi;

	protected ICoreClientAPI capi;

	protected Entity entity;

	public AssetLocation soundName = new AssetLocation("sounds/voice/saxophone");

	public float soundLength = -1f;

	protected List<SlidingPitchSound> slidingPitchSounds = new List<SlidingPitchSound>();

	protected List<SlidingPitchSound> stoppedSlidingSounds = new List<SlidingPitchSound>();

	public float chordDelayMul = 1f;

	public float pitchModifier = 1f;

	public float volumneModifier = 1f;

	public float idleTalkChance = 0.0005f;

	public bool AddSoundLengthChordDelay;

	public bool IsMultiSoundVoice;

	public bool ShouldDoIdleTalk = true;

	private List<IAsset> sounds;

	protected virtual Random Rand => capi.World.Rand;

	public EntityTalkUtil(ICoreAPI api, Entity atEntity, bool isMultiSoundVoice)
	{
		sapi = api as ICoreServerAPI;
		capi = api as ICoreClientAPI;
		entity = atEntity;
		IsMultiSoundVoice = isMultiSoundVoice;
		TalkSpeed = defaultTalkSpeeds();
		capi?.Event.RegisterRenderer(new DummyRenderer
		{
			action = OnRenderTick
		}, EnumRenderStage.Before, "talkfasttilk");
	}

	protected AssetLocation GetSoundLocation(float pitch, out float pitchOffset)
	{
		if (soundLength < 0f)
		{
			float pitchOffset2;
			SoundParams param = new SoundParams
			{
				Location = getSoundLocOnly(pitch, out pitchOffset2),
				DisposeOnFinish = true,
				ShouldLoop = false
			};
			ILoadedSound sound = capi.World.LoadSound(param);
			soundLength = sound?.SoundLengthSeconds ?? 0.1f;
			sound?.Dispose();
		}
		return getSoundLocOnly(pitch, out pitchOffset);
	}

	private AssetLocation getSoundLocOnly(float pitch, out float pitchOffset)
	{
		if (IsMultiSoundVoice)
		{
			if (sounds == null)
			{
				sounds = capi.Assets.GetMany(soundName.Path, soundName.Domain, loadAsset: false);
			}
			int len = sounds.Count;
			int index = (int)GameMath.Clamp((pitch - 0.65f) * (float)len, 0f, len - 1);
			float pitchPerStep = 1f / (float)len;
			float basePitch = (float)index * pitchPerStep + 0.65f;
			pitchOffset = pitch - basePitch;
			return sounds[index].Location;
		}
		pitchOffset = pitch;
		return soundName;
	}

	private void OnRenderTick(float dt)
	{
		for (int i = 0; i < slidingPitchSounds.Count; i++)
		{
			SlidingPitchSound sps = slidingPitchSounds[i];
			if (sps.sound.HasStopped)
			{
				stoppedSlidingSounds.Add(sps);
				continue;
			}
			float secondspassed = (float)(capi.World.ElapsedMilliseconds - sps.startMs) / 1000f;
			float len = sps.length;
			float progress = GameMath.Min(1f, secondspassed / len);
			float pitch = GameMath.Lerp(sps.startPitch, sps.endPitch, progress);
			float volume = GameMath.Lerp(sps.StartVolumne, sps.EndVolumne, progress);
			if (secondspassed > len)
			{
				volume -= (secondspassed - sps.length) * 5f;
			}
			sps.Vibrato = sps.TalkType == EnumTalkType.Death || sps.TalkType == EnumTalkType.Thrust;
			if (sps.TalkType == EnumTalkType.Thrust && (double)secondspassed > 0.15)
			{
				sps.sound.Stop();
				continue;
			}
			if (volume <= 0f)
			{
				sps.sound.FadeOutAndStop(0f);
				continue;
			}
			sps.sound.SetPitch(pitch + (sps.Vibrato ? ((float)Math.Sin(secondspassed * 8f) * 0.05f) : 0f));
			sps.sound.FadeTo(volume, 0.1f, delegate
			{
			});
		}
		foreach (SlidingPitchSound val in stoppedSlidingSounds)
		{
			slidingPitchSounds.Remove(val);
		}
	}

	public virtual void SetModifiers(float chordDelayMul = 1f, float pitchModifier = 1f, float volumneModifier = 1f)
	{
		this.chordDelayMul = chordDelayMul;
		this.pitchModifier = pitchModifier;
		this.volumneModifier = volumneModifier;
		TalkSpeed = defaultTalkSpeeds();
		EnumTalkType[] array = TalkSpeed.Keys.ToArray();
		foreach (EnumTalkType key in array)
		{
			TalkSpeed[key] = Math.Max(0.06f, TalkSpeed[key] * chordDelayMul);
		}
	}

	public virtual void OnGameTick(float dt)
	{
		float soundLen = 0.1f + (float)(capi.World.Rand.NextDouble() * capi.World.Rand.NextDouble()) / 2f;
		if (lettersLeftToTalk > 0)
		{
			chordDelay -= dt * (IsMultiSoundVoice ? 0.6f : 1f);
			if (!(chordDelay < 0f))
			{
				return;
			}
			chordDelay = TalkSpeed[talkType];
			switch (talkType)
			{
			case EnumTalkType.Purchase:
			{
				float startpitch = 1.5f;
				float endpitch = ((totalLettersTalked > 0) ? 0.9f : 1.5f);
				PlaySound(startpitch, endpitch, 1f, 0.8f, soundLen);
				chordDelay = 0.3f * chordDelayMul;
				break;
			}
			case EnumTalkType.Goodbye:
			{
				float pitch = 1.25f - 0.6f * (float)totalLettersTalked / (float)totalLettersToTalk;
				PlaySound(pitch, pitch * 0.9f, 0.7f, 0.6f, soundLen);
				chordDelay = 0.25f * chordDelayMul;
				break;
			}
			case EnumTalkType.Death:
				soundLen = 2.3f;
				PlaySound(0.75f, 0.3f, 1f, 0.2f, soundLen);
				break;
			case EnumTalkType.Thrust:
				soundLen = 0.12f;
				PlaySound(0.5f, 0.8f, 0.4f, 1f, soundLen);
				break;
			case EnumTalkType.Shrug:
				soundLen = 0.6f;
				PlaySound(0.9f, 1.5f, 0.8f, 0.8f, soundLen);
				break;
			case EnumTalkType.Meet:
			{
				float pitch4 = 0.75f + 0.5f * (float)Rand.NextDouble() + (float)totalLettersTalked / (float)totalLettersToTalk / 3f;
				PlaySound(pitch4, pitch4 * 1.5f, 0.75f, 0.75f, soundLen);
				if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
				{
					chordDelay = 0.15f * chordDelayMul;
					currentLetterInWord = 0;
				}
				break;
			}
			case EnumTalkType.Complain:
			{
				float startPitch3 = 0.75f + 0.5f * (float)Rand.NextDouble();
				float endPitch3 = startPitch3 + 0.15f;
				soundLen = 0.05f;
				PlaySound(startPitch3, endPitch3, startPitch3, endPitch3, soundLen);
				if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
				{
					chordDelay = 0.45f * chordDelayMul;
					currentLetterInWord = 0;
				}
				break;
			}
			case EnumTalkType.Idle:
			case EnumTalkType.IdleShort:
			{
				float startPitch2 = 0.75f + 0.25f * (float)Rand.NextDouble();
				float endPitch2 = 0.75f + 0.25f * (float)Rand.NextDouble();
				PlaySound(startPitch2, endPitch2, 0.75f, 0.75f, soundLen);
				if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
				{
					chordDelay = 0.35f * chordDelayMul;
					currentLetterInWord = 0;
				}
				break;
			}
			case EnumTalkType.Laugh:
			{
				float num = (float)Rand.NextDouble() * 0.1f;
				float pfac = (float)Math.Pow(Math.Min(1f, 1f / pitchModifier), 2.0);
				soundLen = 0.1f;
				float startPitch = num + 1.5f - (float)currentLetterInWord / (20f / pfac);
				float endPitch = startPitch - 0.05f;
				PlaySound(startPitch, endPitch, 1f, 0.8f, soundLen);
				chordDelay = 0.2f * chordDelayMul * pfac;
				break;
			}
			case EnumTalkType.Hurt:
			{
				float pitch3 = 0.75f + 0.5f * (float)Rand.NextDouble() + (1f - (float)totalLettersTalked / (float)totalLettersToTalk);
				soundLen /= 4f;
				float vol = 0.5f + (1f - (float)totalLettersTalked / (float)totalLettersToTalk);
				PlaySound(pitch3, pitch3 - 0.2f, vol, vol, soundLen);
				if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
				{
					chordDelay = 0.25f * chordDelayMul;
					currentLetterInWord = 0;
				}
				break;
			}
			case EnumTalkType.Hurt2:
			{
				float pitch2 = 0.75f + 0.4f * (float)Rand.NextDouble() + (1f - (float)totalLettersTalked / (float)totalLettersToTalk);
				PlaySound(pitch2, 0.5f + (1f - (float)totalLettersTalked / (float)totalLettersToTalk) / 1.25f, soundLen);
				if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
				{
					chordDelay = 0.2f * chordDelayMul;
					currentLetterInWord = 0;
				}
				chordDelay = 0f;
				break;
			}
			}
			if (AddSoundLengthChordDelay)
			{
				chordDelay += Math.Min(soundLength, soundLen) * chordDelayMul;
			}
			lettersLeftToTalk--;
			currentLetterInWord++;
			totalLettersTalked++;
		}
		else if (lettersLeftToTalk == 0 && capi.World.Rand.NextDouble() < (double)idleTalkChance && entity.Alive && ShouldDoIdleTalk)
		{
			Talk(EnumTalkType.Idle);
		}
	}

	protected virtual void PlaySound(float startpitch, float volume, float length)
	{
		PlaySound(startpitch, startpitch, volume, volume, length);
	}

	protected virtual void PlaySound(float startPitch, float endPitch, float startvolume, float endvolumne, float length)
	{
		startPitch *= pitchModifier;
		endPitch *= pitchModifier;
		startvolume *= volumneModifier;
		endvolumne *= volumneModifier;
		float pitchOffset;
		AssetLocation loc = GetSoundLocation(startPitch, out pitchOffset);
		SoundParams param = new SoundParams
		{
			Location = loc,
			DisposeOnFinish = true,
			Pitch = (IsMultiSoundVoice ? pitchOffset : startPitch),
			Volume = startvolume,
			Position = entity.Pos.XYZ.ToVec3f().Add(0f, (float)entity.LocalEyePos.Y, 0f),
			ShouldLoop = false,
			Range = 8f
		};
		ILoadedSound sound = capi.World.LoadSound(param);
		slidingPitchSounds.Add(new SlidingPitchSound
		{
			TalkType = talkType,
			startPitch = (IsMultiSoundVoice ? (1f + pitchOffset) : startPitch),
			endPitch = (IsMultiSoundVoice ? (1f + (endPitch - startPitch) + pitchOffset) : endPitch),
			sound = sound,
			startMs = capi.World.ElapsedMilliseconds,
			length = length,
			StartVolumne = startvolume,
			EndVolumne = endvolumne
		});
		sound.Start();
	}

	public virtual void Talk(EnumTalkType talkType)
	{
		if (sapi != null)
		{
			sapi.Network.BroadcastEntityPacket(entity.EntityId, 1231, SerializerUtil.Serialize(talkType));
			return;
		}
		IClientWorldAccessor world = capi.World;
		this.talkType = talkType;
		totalLettersTalked = 0;
		currentLetterInWord = 0;
		chordDelay = TalkSpeed[talkType];
		LongNote = false;
		if (talkType == EnumTalkType.Meet)
		{
			lettersLeftToTalk = 2 + world.Rand.Next(10);
		}
		if (talkType == EnumTalkType.Hurt || talkType == EnumTalkType.Hurt2)
		{
			lettersLeftToTalk = 2 + world.Rand.Next(3);
		}
		if (talkType == EnumTalkType.Idle)
		{
			lettersLeftToTalk = 3 + world.Rand.Next(12);
		}
		if (talkType == EnumTalkType.IdleShort)
		{
			lettersLeftToTalk = 3 + world.Rand.Next(4);
		}
		if (talkType == EnumTalkType.Laugh)
		{
			lettersLeftToTalk = (int)((float)(4 + world.Rand.Next(4)) * Math.Max(1f, pitchModifier));
		}
		if (talkType == EnumTalkType.Purchase)
		{
			lettersLeftToTalk = 2 + world.Rand.Next(2);
		}
		if (talkType == EnumTalkType.Complain)
		{
			lettersLeftToTalk = 10 + world.Rand.Next(12);
		}
		if (talkType == EnumTalkType.Goodbye)
		{
			lettersLeftToTalk = 2 + world.Rand.Next(2);
		}
		if (talkType == EnumTalkType.Death)
		{
			lettersLeftToTalk = 1;
		}
		if (talkType == EnumTalkType.Shrug)
		{
			lettersLeftToTalk = 1;
		}
		if (talkType == EnumTalkType.Thrust)
		{
			lettersLeftToTalk = 1;
		}
		totalLettersToTalk = lettersLeftToTalk;
	}

	private Dictionary<EnumTalkType, float> defaultTalkSpeeds()
	{
		return new Dictionary<EnumTalkType, float>
		{
			{
				EnumTalkType.Meet,
				0.13f
			},
			{
				EnumTalkType.Death,
				0.3f
			},
			{
				EnumTalkType.Idle,
				0.1f
			},
			{
				EnumTalkType.IdleShort,
				0.1f
			},
			{
				EnumTalkType.Laugh,
				0.2f
			},
			{
				EnumTalkType.Hurt,
				0.07f
			},
			{
				EnumTalkType.Hurt2,
				0.07f
			},
			{
				EnumTalkType.Goodbye,
				0.07f
			},
			{
				EnumTalkType.Complain,
				0.09f
			},
			{
				EnumTalkType.Purchase,
				0.15f
			},
			{
				EnumTalkType.Thrust,
				0.15f
			},
			{
				EnumTalkType.Shrug,
				0.15f
			}
		};
	}
}
