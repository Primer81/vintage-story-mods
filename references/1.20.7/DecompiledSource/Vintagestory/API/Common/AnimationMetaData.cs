#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     Animation Meta Data is a json type that controls how an animation should be played.
[DocumentAsJson]
public class AnimationMetaData
{
    //
    // Summary:
    //     Unique identifier to be able to reference this AnimationMetaData instance
    [JsonProperty]
    public string Code;

    //
    // Summary:
    //     Custom attributes that can be used for the animation. Valid vanilla attributes
    //     are:
    //     - damageAtFrame (float)
    //     - soundAtFrame (float)
    //     - authorative (bool)
    [JsonProperty]
    [JsonConverter(typeof(JsonAttributesConverter))]
    public JsonObject Attributes;

    //
    // Summary:
    //     The animations code identifier that we want to play
    [JsonProperty]
    public string Animation;

    //
    // Summary:
    //     The weight of this animation. When using multiple animations at a time, this
    //     controls the significance of each animation. The method for determining final
    //     animation values depends on this and Vintagestory.API.Common.AnimationMetaData.BlendMode.
    [JsonProperty]
    public float Weight = 1f;

    //
    // Summary:
    //     A way of specifying Vintagestory.API.Common.AnimationMetaData.Weight for each
    //     element. Also see Vintagestory.API.Common.AnimationMetaData.ElementBlendMode
    //     to control blend modes per element..
    [JsonProperty]
    public Dictionary<string, float> ElementWeight = new Dictionary<string, float>();

    //
    // Summary:
    //     The speed this animation should play at.
    [JsonProperty]
    public float AnimationSpeed = 1f;

    //
    // Summary:
    //     Should this animation speed be multiplied by the movement speed of the entity?
    [JsonProperty]
    public bool MulWithWalkSpeed;

    //
    // Summary:
    //     This property can be used in cases where a animation with high weight is played
    //     alongside another animation with low element weight. In these cases, the easeIn
    //     become unaturally fast. Setting a value of 0.8f or similar here addresses this
    //     issue.
    //     - 0f = uncapped weight
    //     - 0.5f = weight cannot exceed 2
    //     - 1f = weight cannot exceed 1
    [JsonProperty]
    public float WeightCapFactor;

    //
    // Summary:
    //     A multiplier applied to the weight value to "ease in" the animation. Choose a
    //     high value for looping animations or it will be glitchy
    [JsonProperty]
    public float EaseInSpeed = 10f;

    //
    // Summary:
    //     A multiplier applied to the weight value to "ease out" the animation. Choose
    //     a high value for looping animations or it will be glitchy
    [JsonProperty]
    public float EaseOutSpeed = 10f;

    //
    // Summary:
    //     Controls when this animation should be played.
    [JsonProperty]
    public AnimationTrigger TriggeredBy;

    //
    // Summary:
    //     The animation blend mode. Controls how this animation will react with other concurrent
    //     animations. Also see Vintagestory.API.Common.AnimationMetaData.ElementBlendMode
    //     to control blend mode per element.
    [JsonProperty]
    public EnumAnimationBlendMode BlendMode;

    //
    // Summary:
    //     A way of specifying Vintagestory.API.Common.AnimationMetaData.BlendMode per element.
    [JsonProperty]
    public Dictionary<string, EnumAnimationBlendMode> ElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>(StringComparer.OrdinalIgnoreCase);

    //
    // Summary:
    //     Should this animation stop default animations from playing?
    [JsonProperty]
    public bool SupressDefaultAnimation;

    //
    // Summary:
    //     A value that determines whether to change the first-person eye position for the
    //     camera. Higher values will keep eye position static.
    [JsonProperty]
    public float HoldEyePosAfterEasein = 99f;

    //
    // Summary:
    //     If true, the server does not sync this animation.
    [JsonProperty]
    public bool ClientSide;

    [JsonProperty]
    public bool WithFpVariant;

    [JsonProperty]
    public AnimationSound AnimationSound;

    public AnimationMetaData FpVariant;

    public float StartFrameOnce;

    private int withActivitiesMerged;

    public uint CodeCrc32;

    public bool WasStartedFromTrigger;

    public float GetCurrentAnimationSpeed(float walkspeed)
    {
        return AnimationSpeed * (MulWithWalkSpeed ? walkspeed : 1f) * GlobalConstants.OverallSpeedMultiplier;
    }

    public AnimationMetaData Init()
    {
        withActivitiesMerged = 0;
        EnumEntityActivity[] array = TriggeredBy?.OnControls;
        if (array != null)
        {
            for (int i = 0; i < array.Length; i++)
            {
                withActivitiesMerged |= (int)array[i];
            }
        }

        CodeCrc32 = GetCrc32(Code);
        if (WithFpVariant)
        {
            FpVariant = Clone();
            FpVariant.WithFpVariant = false;
            FpVariant.Animation += "-fp";
            FpVariant.Code += "-fp";
            FpVariant.Init();
        }

        if (AnimationSound != null)
        {
            AnimationSound.Location.WithPathPrefixOnce("sounds/");
        }

        return this;
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        Animation = Animation?.ToLowerInvariant() ?? "";
        if (Code == null)
        {
            Code = Animation;
        }

        CodeCrc32 = GetCrc32(Code);
    }

    public static uint GetCrc32(string animcode)
    {
        int num = int.MaxValue;
        return (uint)(GameMath.Crc32(animcode.ToLowerInvariant()) & num);
    }

    public bool Matches(int currentActivities)
    {
        AnimationTrigger triggeredBy = TriggeredBy;
        if (triggeredBy == null || !triggeredBy.MatchExact)
        {
            return (currentActivities & withActivitiesMerged) > 0;
        }

        return currentActivities == withActivitiesMerged;
    }

    public AnimationMetaData Clone()
    {
        return new AnimationMetaData
        {
            Code = Code,
            Animation = Animation,
            AnimationSound = AnimationSound?.Clone(),
            Weight = Weight,
            Attributes = Attributes?.Clone(),
            ClientSide = ClientSide,
            ElementWeight = new Dictionary<string, float>(ElementWeight),
            AnimationSpeed = AnimationSpeed,
            MulWithWalkSpeed = MulWithWalkSpeed,
            EaseInSpeed = EaseInSpeed,
            EaseOutSpeed = EaseOutSpeed,
            TriggeredBy = TriggeredBy?.Clone(),
            BlendMode = BlendMode,
            ElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>(ElementBlendMode),
            withActivitiesMerged = withActivitiesMerged,
            CodeCrc32 = CodeCrc32,
            WasStartedFromTrigger = WasStartedFromTrigger,
            HoldEyePosAfterEasein = HoldEyePosAfterEasein,
            StartFrameOnce = StartFrameOnce,
            SupressDefaultAnimation = SupressDefaultAnimation,
            WeightCapFactor = WeightCapFactor
        };
    }

    public override bool Equals(object obj)
    {
        if (obj is AnimationMetaData animationMetaData && animationMetaData.Animation == Animation && animationMetaData.AnimationSpeed == AnimationSpeed)
        {
            return animationMetaData.BlendMode == BlendMode;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Animation.GetHashCode() ^ AnimationSpeed.GetHashCode() ^ BlendMode.GetHashCode();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(Code);
        writer.Write(Animation);
        writer.Write(Weight);
        writer.Write(ElementWeight.Count);
        foreach (KeyValuePair<string, float> item in ElementWeight)
        {
            writer.Write(item.Key);
            writer.Write(item.Value);
        }

        writer.Write(AnimationSpeed);
        writer.Write(EaseInSpeed);
        writer.Write(EaseOutSpeed);
        writer.Write(TriggeredBy != null);
        if (TriggeredBy != null)
        {
            writer.Write(TriggeredBy.MatchExact);
            EnumEntityActivity[] onControls = TriggeredBy.OnControls;
            if (onControls != null)
            {
                writer.Write(onControls.Length);
                for (int i = 0; i < onControls.Length; i++)
                {
                    writer.Write((int)onControls[i]);
                }
            }
            else
            {
                writer.Write(0);
            }

            writer.Write(TriggeredBy.DefaultAnim);
        }

        writer.Write((int)BlendMode);
        writer.Write(ElementBlendMode.Count);
        foreach (KeyValuePair<string, EnumAnimationBlendMode> item2 in ElementBlendMode)
        {
            writer.Write(item2.Key);
            writer.Write((int)item2.Value);
        }

        writer.Write(MulWithWalkSpeed);
        writer.Write(StartFrameOnce);
        writer.Write(HoldEyePosAfterEasein);
        writer.Write(ClientSide);
        writer.Write(Attributes?.ToString() ?? "");
        writer.Write(WeightCapFactor);
        writer.Write(AnimationSound != null);
        if (AnimationSound != null)
        {
            writer.Write(AnimationSound.Location.ToShortString());
            writer.Write(AnimationSound.Range);
            writer.Write(AnimationSound.Frame);
            writer.Write(AnimationSound.RandomizePitch);
        }
    }

    public static AnimationMetaData FromBytes(BinaryReader reader, string version)
    {
        AnimationMetaData animationMetaData = new AnimationMetaData();
        animationMetaData.Code = reader.ReadString().DeDuplicate();
        animationMetaData.Animation = reader.ReadString();
        animationMetaData.Weight = reader.ReadSingle();
        int num = reader.ReadInt32();
        for (int i = 0; i < num; i++)
        {
            animationMetaData.ElementWeight[reader.ReadString().DeDuplicate()] = reader.ReadSingle();
        }

        animationMetaData.AnimationSpeed = reader.ReadSingle();
        animationMetaData.EaseInSpeed = reader.ReadSingle();
        animationMetaData.EaseOutSpeed = reader.ReadSingle();
        if (reader.ReadBoolean())
        {
            animationMetaData.TriggeredBy = new AnimationTrigger();
            animationMetaData.TriggeredBy.MatchExact = reader.ReadBoolean();
            num = reader.ReadInt32();
            animationMetaData.TriggeredBy.OnControls = new EnumEntityActivity[num];
            for (int j = 0; j < num; j++)
            {
                animationMetaData.TriggeredBy.OnControls[j] = (EnumEntityActivity)reader.ReadInt32();
            }

            animationMetaData.TriggeredBy.DefaultAnim = reader.ReadBoolean();
        }

        animationMetaData.BlendMode = (EnumAnimationBlendMode)reader.ReadInt32();
        num = reader.ReadInt32();
        for (int k = 0; k < num; k++)
        {
            animationMetaData.ElementBlendMode[reader.ReadString().DeDuplicate()] = (EnumAnimationBlendMode)reader.ReadInt32();
        }

        animationMetaData.MulWithWalkSpeed = reader.ReadBoolean();
        if (GameVersion.IsAtLeastVersion(version, "1.12.5-dev.1"))
        {
            animationMetaData.StartFrameOnce = reader.ReadSingle();
        }

        if (GameVersion.IsAtLeastVersion(version, "1.13.0-dev.3"))
        {
            animationMetaData.HoldEyePosAfterEasein = reader.ReadSingle();
        }

        if (GameVersion.IsAtLeastVersion(version, "1.17.0-dev.18"))
        {
            animationMetaData.ClientSide = reader.ReadBoolean();
        }

        if (GameVersion.IsAtLeastVersion(version, "1.19.0-dev.20"))
        {
            string text = reader.ReadString();
            if (text != "")
            {
                animationMetaData.Attributes = new JsonObject(JToken.Parse(text));
            }
            else
            {
                animationMetaData.Attributes = new JsonObject(JToken.Parse("{}"));
            }
        }

        if (GameVersion.IsAtLeastVersion(version, "1.19.0-rc.6"))
        {
            animationMetaData.WeightCapFactor = reader.ReadSingle();
        }

        if (GameVersion.IsAtLeastVersion(version, "1.20.0-dev.13") && reader.ReadBoolean())
        {
            animationMetaData.AnimationSound = new AnimationSound
            {
                Location = AssetLocation.Create(reader.ReadString()),
                Range = reader.ReadSingle(),
                Frame = reader.ReadInt32(),
                RandomizePitch = reader.ReadBoolean()
            };
        }

        animationMetaData.Init();
        return animationMetaData;
    }

    internal void DeDuplicate()
    {
        Code = Code.DeDuplicate();
        Dictionary<string, float> dictionary = new Dictionary<string, float>(ElementWeight.Count);
        foreach (KeyValuePair<string, float> item in ElementWeight)
        {
            dictionary[item.Key.DeDuplicate()] = item.Value;
        }

        ElementWeight = dictionary;
        Dictionary<string, EnumAnimationBlendMode> dictionary2 = new Dictionary<string, EnumAnimationBlendMode>(ElementBlendMode.Count);
        foreach (KeyValuePair<string, EnumAnimationBlendMode> item2 in ElementBlendMode)
        {
            dictionary2[item2.Key.DeDuplicate()] = item2.Value;
        }

        ElementBlendMode = dictionary2;
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
