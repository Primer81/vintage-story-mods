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

/// <summary>
/// Animation Meta Data is a json type that controls how an animation should be played.
/// </summary>
/// <example>
/// <code language="json">
///             "animations": [
///             	{
///             		"code": "hurt",
///             		"animation": "hurt",
///             		"animationSpeed": 2.2,
///             		"weight": 10,
///             		"blendMode": "AddAverage"
///             	},
///             	{
///             		"code": "die",
///             		"animation": "death",
///             		"animationSpeed": 1.25,
///             		"weight": 10,
///             		"blendMode": "Average",
///             		"triggeredBy": { "onControls": [ "dead" ] }
///             	},
///             	{
///             		"code": "idle",
///             		"animation": "idle",
///             		"blendMode": "AddAverage",
///             		"easeOutSpeed": 4,
///             		"triggeredBy": { "defaultAnim": true }
///             	},
///             	{
///             		"code": "walk",
///             		"animation": "walk",
///             		"weight": 5
///             	}
///             ]
/// </code>
/// </example>
[DocumentAsJson]
public class AnimationMetaData
{
	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// Unique identifier to be able to reference this AnimationMetaData instance
	/// </summary>
	[JsonProperty]
	public string Code;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// Custom attributes that can be used for the animation.
	/// Valid vanilla attributes are:<br />
	/// - damageAtFrame (float)<br />
	/// - soundAtFrame (float)<br />
	/// - authorative (bool)<br />
	/// </summary>
	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The animations code identifier that we want to play
	/// </summary>
	[JsonProperty]
	public string Animation;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The weight of this animation. When using multiple animations at a time, this controls the significance of each animation.
	/// The method for determining final animation values depends on this and <see cref="F:Vintagestory.API.Common.AnimationMetaData.BlendMode" />.
	/// </summary>
	[JsonProperty]
	public float Weight = 1f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A way of specifying <see cref="F:Vintagestory.API.Common.AnimationMetaData.Weight" /> for each element.
	/// Also see <see cref="F:Vintagestory.API.Common.AnimationMetaData.ElementBlendMode" /> to control blend modes per element..
	/// </summary>
	[JsonProperty]
	public Dictionary<string, float> ElementWeight = new Dictionary<string, float>();

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The speed this animation should play at.
	/// </summary>
	[JsonProperty]
	public float AnimationSpeed = 1f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Should this animation speed be multiplied by the movement speed of the entity?
	/// </summary>
	[JsonProperty]
	public bool MulWithWalkSpeed;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// This property can be used in cases where a animation with high weight is played alongside another animation with low element weight.
	/// In these cases, the easeIn become unaturally fast. Setting a value of 0.8f or similar here addresses this issue.<br />
	/// - 0f = uncapped weight<br />
	/// - 0.5f = weight cannot exceed 2<br />
	/// - 1f = weight cannot exceed 1
	/// </summary>
	[JsonProperty]
	public float WeightCapFactor;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>10</jsondefault>-->
	/// A multiplier applied to the weight value to "ease in" the animation. Choose a high value for looping animations or it will be glitchy
	/// </summary>
	[JsonProperty]
	public float EaseInSpeed = 10f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>10</jsondefault>-->
	/// A multiplier applied to the weight value to "ease out" the animation. Choose a high value for looping animations or it will be glitchy
	/// </summary>
	[JsonProperty]
	public float EaseOutSpeed = 10f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// Controls when this animation should be played.
	/// </summary>
	[JsonProperty]
	public AnimationTrigger TriggeredBy;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Add</jsondefault>-->
	/// The animation blend mode. Controls how this animation will react with other concurrent animations.
	/// Also see <see cref="F:Vintagestory.API.Common.AnimationMetaData.ElementBlendMode" /> to control blend mode per element.
	/// </summary>
	[JsonProperty]
	public EnumAnimationBlendMode BlendMode;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A way of specifying <see cref="F:Vintagestory.API.Common.AnimationMetaData.BlendMode" /> per element.
	/// </summary>
	[JsonProperty]
	public Dictionary<string, EnumAnimationBlendMode> ElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Should this animation stop default animations from playing?
	/// </summary>
	[JsonProperty]
	public bool SupressDefaultAnimation;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>99</jsondefault>-->
	/// A value that determines whether to change the first-person eye position for the camera.
	/// Higher values will keep eye position static.
	/// </summary>
	[JsonProperty]
	public float HoldEyePosAfterEasein = 99f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// If true, the server does not sync this animation.
	/// </summary>
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
		EnumEntityActivity[] OnControls = TriggeredBy?.OnControls;
		if (OnControls != null)
		{
			for (int i = 0; i < OnControls.Length; i++)
			{
				withActivitiesMerged |= (int)OnControls[i];
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
		int mask = int.MaxValue;
		return (uint)(GameMath.Crc32(animcode.ToLowerInvariant()) & mask);
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
		if (obj is AnimationMetaData other && other.Animation == Animation && other.AnimationSpeed == AnimationSpeed)
		{
			return other.BlendMode == BlendMode;
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
		foreach (KeyValuePair<string, float> val2 in ElementWeight)
		{
			writer.Write(val2.Key);
			writer.Write(val2.Value);
		}
		writer.Write(AnimationSpeed);
		writer.Write(EaseInSpeed);
		writer.Write(EaseOutSpeed);
		writer.Write(TriggeredBy != null);
		if (TriggeredBy != null)
		{
			writer.Write(TriggeredBy.MatchExact);
			EnumEntityActivity[] OnControls = TriggeredBy.OnControls;
			if (OnControls != null)
			{
				writer.Write(OnControls.Length);
				for (int i = 0; i < OnControls.Length; i++)
				{
					writer.Write((int)OnControls[i]);
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
		foreach (KeyValuePair<string, EnumAnimationBlendMode> val in ElementBlendMode)
		{
			writer.Write(val.Key);
			writer.Write((int)val.Value);
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
		AnimationMetaData animdata = new AnimationMetaData();
		animdata.Code = reader.ReadString().DeDuplicate();
		animdata.Animation = reader.ReadString();
		animdata.Weight = reader.ReadSingle();
		int weightCount = reader.ReadInt32();
		for (int k = 0; k < weightCount; k++)
		{
			animdata.ElementWeight[reader.ReadString().DeDuplicate()] = reader.ReadSingle();
		}
		animdata.AnimationSpeed = reader.ReadSingle();
		animdata.EaseInSpeed = reader.ReadSingle();
		animdata.EaseOutSpeed = reader.ReadSingle();
		if (reader.ReadBoolean())
		{
			animdata.TriggeredBy = new AnimationTrigger();
			animdata.TriggeredBy.MatchExact = reader.ReadBoolean();
			weightCount = reader.ReadInt32();
			animdata.TriggeredBy.OnControls = new EnumEntityActivity[weightCount];
			for (int j = 0; j < weightCount; j++)
			{
				animdata.TriggeredBy.OnControls[j] = (EnumEntityActivity)reader.ReadInt32();
			}
			animdata.TriggeredBy.DefaultAnim = reader.ReadBoolean();
		}
		animdata.BlendMode = (EnumAnimationBlendMode)reader.ReadInt32();
		weightCount = reader.ReadInt32();
		for (int i = 0; i < weightCount; i++)
		{
			animdata.ElementBlendMode[reader.ReadString().DeDuplicate()] = (EnumAnimationBlendMode)reader.ReadInt32();
		}
		animdata.MulWithWalkSpeed = reader.ReadBoolean();
		if (GameVersion.IsAtLeastVersion(version, "1.12.5-dev.1"))
		{
			animdata.StartFrameOnce = reader.ReadSingle();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.13.0-dev.3"))
		{
			animdata.HoldEyePosAfterEasein = reader.ReadSingle();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.17.0-dev.18"))
		{
			animdata.ClientSide = reader.ReadBoolean();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.19.0-dev.20"))
		{
			string attributes = reader.ReadString();
			if (attributes != "")
			{
				animdata.Attributes = new JsonObject(JToken.Parse(attributes));
			}
			else
			{
				animdata.Attributes = new JsonObject(JToken.Parse("{}"));
			}
		}
		if (GameVersion.IsAtLeastVersion(version, "1.19.0-rc.6"))
		{
			animdata.WeightCapFactor = reader.ReadSingle();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.20.0-dev.13") && reader.ReadBoolean())
		{
			animdata.AnimationSound = new AnimationSound
			{
				Location = AssetLocation.Create(reader.ReadString()),
				Range = reader.ReadSingle(),
				Frame = reader.ReadInt32(),
				RandomizePitch = reader.ReadBoolean()
			};
		}
		animdata.Init();
		return animdata;
	}

	internal void DeDuplicate()
	{
		Code = Code.DeDuplicate();
		Dictionary<string, float> newElementWeight = new Dictionary<string, float>(ElementWeight.Count);
		foreach (KeyValuePair<string, float> entry2 in ElementWeight)
		{
			newElementWeight[entry2.Key.DeDuplicate()] = entry2.Value;
		}
		ElementWeight = newElementWeight;
		Dictionary<string, EnumAnimationBlendMode> newElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>(ElementBlendMode.Count);
		foreach (KeyValuePair<string, EnumAnimationBlendMode> entry in ElementBlendMode)
		{
			newElementBlendMode[entry.Key.DeDuplicate()] = entry.Value;
		}
		ElementBlendMode = newElementBlendMode;
	}
}
