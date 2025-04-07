#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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
            ILoadedSound loadedSound = capi.World.LoadSound(param);
            soundLength = loadedSound?.SoundLengthSeconds ?? 0.1f;
            loadedSound?.Dispose();
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

            int count = sounds.Count;
            int num = (int)GameMath.Clamp((pitch - 0.65f) * (float)count, 0f, count - 1);
            float num2 = 1f / (float)count;
            float num3 = (float)num * num2 + 0.65f;
            pitchOffset = pitch - num3;
            return sounds[num].Location;
        }

        pitchOffset = pitch;
        return soundName;
    }

    private void OnRenderTick(float dt)
    {
        for (int i = 0; i < slidingPitchSounds.Count; i++)
        {
            SlidingPitchSound slidingPitchSound = slidingPitchSounds[i];
            if (slidingPitchSound.sound.HasStopped)
            {
                stoppedSlidingSounds.Add(slidingPitchSound);
                continue;
            }

            float num = (float)(capi.World.ElapsedMilliseconds - slidingPitchSound.startMs) / 1000f;
            float length = slidingPitchSound.length;
            float t = GameMath.Min(1f, num / length);
            float num2 = GameMath.Lerp(slidingPitchSound.startPitch, slidingPitchSound.endPitch, t);
            float num3 = GameMath.Lerp(slidingPitchSound.StartVolumne, slidingPitchSound.EndVolumne, t);
            if (num > length)
            {
                num3 -= (num - slidingPitchSound.length) * 5f;
            }

            slidingPitchSound.Vibrato = slidingPitchSound.TalkType == EnumTalkType.Death || slidingPitchSound.TalkType == EnumTalkType.Thrust;
            if (slidingPitchSound.TalkType == EnumTalkType.Thrust && (double)num > 0.15)
            {
                slidingPitchSound.sound.Stop();
                continue;
            }

            if (num3 <= 0f)
            {
                slidingPitchSound.sound.FadeOutAndStop(0f);
                continue;
            }

            slidingPitchSound.sound.SetPitch(num2 + (slidingPitchSound.Vibrato ? ((float)Math.Sin(num * 8f) * 0.05f) : 0f));
            slidingPitchSound.sound.FadeTo(num3, 0.1f, delegate
            {
            });
        }

        foreach (SlidingPitchSound stoppedSlidingSound in stoppedSlidingSounds)
        {
            slidingPitchSounds.Remove(stoppedSlidingSound);
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
        float num = 0.1f + (float)(capi.World.Rand.NextDouble() * capi.World.Rand.NextDouble()) / 2f;
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
                        float startPitch2 = 1.5f;
                        float endPitch3 = ((totalLettersTalked > 0) ? 0.9f : 1.5f);
                        PlaySound(startPitch2, endPitch3, 1f, 0.8f, num);
                        chordDelay = 0.3f * chordDelayMul;
                        break;
                    }
                case EnumTalkType.Goodbye:
                    {
                        float num7 = 1.25f - 0.6f * (float)totalLettersTalked / (float)totalLettersToTalk;
                        PlaySound(num7, num7 * 0.9f, 0.7f, 0.6f, num);
                        chordDelay = 0.25f * chordDelayMul;
                        break;
                    }
                case EnumTalkType.Death:
                    num = 2.3f;
                    PlaySound(0.75f, 0.3f, 1f, 0.2f, num);
                    break;
                case EnumTalkType.Thrust:
                    num = 0.12f;
                    PlaySound(0.5f, 0.8f, 0.4f, 1f, num);
                    break;
                case EnumTalkType.Shrug:
                    num = 0.6f;
                    PlaySound(0.9f, 1.5f, 0.8f, 0.8f, num);
                    break;
                case EnumTalkType.Meet:
                    {
                        float num6 = 0.75f + 0.5f * (float)Rand.NextDouble() + (float)totalLettersTalked / (float)totalLettersToTalk / 3f;
                        PlaySound(num6, num6 * 1.5f, 0.75f, 0.75f, num);
                        if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
                        {
                            chordDelay = 0.15f * chordDelayMul;
                            currentLetterInWord = 0;
                        }

                        break;
                    }
                case EnumTalkType.Complain:
                    {
                        float num2 = 0.75f + 0.5f * (float)Rand.NextDouble();
                        float num3 = num2 + 0.15f;
                        num = 0.05f;
                        PlaySound(num2, num3, num2, num3, num);
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
                        float startPitch = 0.75f + 0.25f * (float)Rand.NextDouble();
                        float endPitch2 = 0.75f + 0.25f * (float)Rand.NextDouble();
                        PlaySound(startPitch, endPitch2, 0.75f, 0.75f, num);
                        if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
                        {
                            chordDelay = 0.35f * chordDelayMul;
                            currentLetterInWord = 0;
                        }

                        break;
                    }
                case EnumTalkType.Laugh:
                    {
                        float num8 = (float)Rand.NextDouble() * 0.1f;
                        float num9 = (float)Math.Pow(Math.Min(1f, 1f / pitchModifier), 2.0);
                        num = 0.1f;
                        float num10 = num8 + 1.5f - (float)currentLetterInWord / (20f / num9);
                        float endPitch = num10 - 0.05f;
                        PlaySound(num10, endPitch, 1f, 0.8f, num);
                        chordDelay = 0.2f * chordDelayMul * num9;
                        break;
                    }
                case EnumTalkType.Hurt:
                    {
                        float num4 = 0.75f + 0.5f * (float)Rand.NextDouble() + (1f - (float)totalLettersTalked / (float)totalLettersToTalk);
                        num /= 4f;
                        float num5 = 0.5f + (1f - (float)totalLettersTalked / (float)totalLettersToTalk);
                        PlaySound(num4, num4 - 0.2f, num5, num5, num);
                        if (currentLetterInWord > 1 && capi.World.Rand.NextDouble() < 0.35)
                        {
                            chordDelay = 0.25f * chordDelayMul;
                            currentLetterInWord = 0;
                        }

                        break;
                    }
                case EnumTalkType.Hurt2:
                    {
                        float startpitch = 0.75f + 0.4f * (float)Rand.NextDouble() + (1f - (float)totalLettersTalked / (float)totalLettersToTalk);
                        PlaySound(startpitch, 0.5f + (1f - (float)totalLettersTalked / (float)totalLettersToTalk) / 1.25f, num);
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
                chordDelay += Math.Min(soundLength, num) * chordDelayMul;
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
        AssetLocation soundLocation = GetSoundLocation(startPitch, out pitchOffset);
        SoundParams param = new SoundParams
        {
            Location = soundLocation,
            DisposeOnFinish = true,
            Pitch = (IsMultiSoundVoice ? pitchOffset : startPitch),
            Volume = startvolume,
            Position = entity.Pos.XYZ.ToVec3f().Add(0f, (float)entity.LocalEyePos.Y, 0f),
            ShouldLoop = false,
            Range = 8f
        };
        ILoadedSound loadedSound = capi.World.LoadSound(param);
        slidingPitchSounds.Add(new SlidingPitchSound
        {
            TalkType = talkType,
            startPitch = (IsMultiSoundVoice ? (1f + pitchOffset) : startPitch),
            endPitch = (IsMultiSoundVoice ? (1f + (endPitch - startPitch) + pitchOffset) : endPitch),
            sound = loadedSound,
            startMs = capi.World.ElapsedMilliseconds,
            length = length,
            StartVolumne = startvolume,
            EndVolumne = endvolumne
        });
        loadedSound.Start();
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
#if false // Decompilation log
'182' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
