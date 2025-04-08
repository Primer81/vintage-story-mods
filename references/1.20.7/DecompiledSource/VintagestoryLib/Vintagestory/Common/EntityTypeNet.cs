using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public static class EntityTypeNet
{
	public static Packet_EntityType EntityPropertiesToPacket(EntityProperties properties)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return EntityPropertiesToPacket(properties, ms);
	}

	public static Packet_EntityType EntityPropertiesToPacket(EntityProperties properties, FastMemoryStream ms)
	{
		Packet_EntityType packet = new Packet_EntityType
		{
			Class = properties.Class,
			Habitat = (int)properties.Habitat,
			Code = properties.Code.ToShortString(),
			Drops = getDropsPacket(properties.Drops, ms),
			Color = properties.Color,
			Shape = ((properties.Client?.Shape != null) ? CollectibleNet.ToPacket(properties.Client.Shape) : null),
			Renderer = properties.Client?.RendererName,
			GlowLevel = ((properties.Client != null) ? properties.Client.GlowLevel : 0),
			PitchStep = (properties.Client.PitchStep ? 1 : 0),
			Attributes = properties.Attributes?.ToString(),
			CollisionBoxLength = CollectibleNet.SerializePlayerPos(properties.CollisionBoxSize.X),
			CollisionBoxHeight = CollectibleNet.SerializePlayerPos(properties.CollisionBoxSize.Y),
			DeadCollisionBoxLength = CollectibleNet.SerializePlayerPos(properties.DeadCollisionBoxSize.X),
			DeadCollisionBoxHeight = CollectibleNet.SerializePlayerPos(properties.DeadCollisionBoxSize.Y),
			SelectionBoxLength = ((properties.SelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.SelectionBoxSize.X)),
			SelectionBoxHeight = ((properties.SelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.SelectionBoxSize.Y)),
			DeadSelectionBoxLength = ((properties.DeadSelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.DeadSelectionBoxSize.X)),
			DeadSelectionBoxHeight = ((properties.DeadSelectionBoxSize == null) ? (-1) : CollectibleNet.SerializeFloatPrecise(properties.DeadSelectionBoxSize.Y)),
			IdleSoundChance = CollectibleNet.SerializeFloatPrecise(100f * properties.IdleSoundChance),
			IdleSoundRange = CollectibleNet.SerializeFloatPrecise(properties.IdleSoundRange),
			Size = CollectibleNet.SerializeFloatPrecise((properties.Client == null) ? 1f : properties.Client.Size),
			SizeGrowthFactor = CollectibleNet.SerializeFloatPrecise((properties.Client == null) ? 0f : properties.Client.SizeGrowthFactor),
			EyeHeight = CollectibleNet.SerializeFloatPrecise((float)properties.EyeHeight),
			SwimmingEyeHeight = CollectibleNet.SerializeFloatPrecise((float)properties.SwimmingEyeHeight),
			Weight = CollectibleNet.SerializeFloatPrecise(properties.Weight),
			CanClimb = (properties.CanClimb ? 1 : 0),
			CanClimbAnywhere = (properties.CanClimbAnywhere ? 1 : 0),
			FallDamage = (properties.FallDamage ? 1 : 0),
			FallDamageMultiplier = CollectibleNet.SerializeFloatPrecise(properties.FallDamageMultiplier),
			RotateModelOnClimb = (properties.RotateModelOnClimb ? 1 : 0),
			ClimbTouchDistance = CollectibleNet.SerializeFloatVeryPrecise(properties.ClimbTouchDistance),
			KnockbackResistance = CollectibleNet.SerializeFloatPrecise(properties.KnockbackResistance)
		};
		packet.SetVariant(CollectibleNet.ToPacket(properties.Variant));
		if (properties.Client?.Textures != null)
		{
			packet.SetTextureCodes(properties.Client.Textures.Keys.ToArray());
			packet.SetCompositeTextures(CollectibleNet.ToPackets(properties.Client.Textures.Values.ToArray()));
		}
		if (properties.Client?.Animations != null)
		{
			ms.Reset();
			BinaryWriter writer = new BinaryWriter(ms);
			AnimationMetaData[] Animations = properties.Client.Animations;
			writer.Write(Animations.Length);
			for (int k = 0; k < Animations.Length; k++)
			{
				Animations[k].ToBytes(writer);
			}
			packet.SetAnimationMetaData(ms.ToArray());
		}
		if (properties.Client?.BehaviorsAsJsonObj != null)
		{
			JsonObject[] BehaviorsAsJsonObj = properties.Client.BehaviorsAsJsonObj;
			Packet_Behavior[] behaviors = new Packet_Behavior[BehaviorsAsJsonObj.Length];
			for (int j = 0; j < behaviors.Length; j++)
			{
				behaviors[j] = new Packet_Behavior
				{
					Attributes = BehaviorsAsJsonObj[j].ToString()
				};
			}
			packet.SetBehaviors(behaviors);
		}
		if (properties.Sounds != null)
		{
			packet.SetSoundKeys(properties.Sounds.Keys.ToArray());
			AssetLocation[] locations = properties.Sounds.Values.ToArray();
			string[] names = new string[properties.Sounds.Count];
			for (int i = 0; i < names.Length; i++)
			{
				names[i] = locations[i].ToString();
			}
			packet.SetSoundNames(names);
		}
		return packet;
	}

	public static EntityProperties FromPacket(Packet_EntityType packet, IWorldAccessor worldForResolve)
	{
		JsonObject[] behaviors = new JsonObject[packet.BehaviorsCount];
		for (int l = 0; l < behaviors.Length; l++)
		{
			behaviors[l] = new JsonObject(JToken.Parse(packet.Behaviors[l].Attributes));
		}
		Dictionary<string, AssetLocation> sounds = new Dictionary<string, AssetLocation>();
		if (packet.SoundKeys != null)
		{
			for (int k = 0; k < packet.SoundKeysCount; k++)
			{
				sounds[packet.SoundKeys[k]] = new AssetLocation(packet.SoundNames[k]);
			}
		}
		AssetLocation code = new AssetLocation(packet.Code);
		EntityProperties et = new EntityProperties
		{
			Class = packet.Class,
			Variant = CollectibleNet.FromPacket(packet.Variant, packet.VariantCount),
			Code = code,
			Color = packet.Color,
			Habitat = (EnumHabitat)packet.Habitat,
			DropsPacket = packet.Drops,
			Client = new EntityClientProperties(behaviors, null)
			{
				GlowLevel = packet.GlowLevel,
				PitchStep = (packet.PitchStep > 0),
				RendererName = packet.Renderer,
				Shape = ((packet.Shape != null) ? CollectibleNet.FromPacket(packet.Shape) : null),
				Size = CollectibleNet.DeserializeFloatPrecise(packet.Size),
				SizeGrowthFactor = CollectibleNet.DeserializeFloatPrecise(packet.SizeGrowthFactor)
			},
			CollisionBoxSize = new Vec2f((float)CollectibleNet.DeserializePlayerPos(packet.CollisionBoxLength), (float)CollectibleNet.DeserializePlayerPos(packet.CollisionBoxHeight)),
			DeadCollisionBoxSize = new Vec2f((float)CollectibleNet.DeserializePlayerPos(packet.DeadCollisionBoxLength), (float)CollectibleNet.DeserializePlayerPos(packet.DeadCollisionBoxHeight)),
			SelectionBoxSize = new Vec2f(CollectibleNet.DeserializeFloatPrecise(packet.SelectionBoxLength), CollectibleNet.DeserializeFloatPrecise(packet.SelectionBoxHeight)),
			DeadSelectionBoxSize = new Vec2f(CollectibleNet.DeserializeFloatPrecise(packet.DeadSelectionBoxLength), CollectibleNet.DeserializeFloatPrecise(packet.DeadSelectionBoxHeight)),
			Attributes = ((packet.Attributes == null) ? null : new JsonObject(JToken.Parse(packet.Attributes))),
			Sounds = sounds,
			IdleSoundChance = CollectibleNet.DeserializeFloatPrecise(packet.IdleSoundChance) / 100f,
			IdleSoundRange = CollectibleNet.DeserializeFloatPrecise(packet.IdleSoundRange),
			EyeHeight = CollectibleNet.DeserializeFloatPrecise(packet.EyeHeight),
			SwimmingEyeHeight = CollectibleNet.DeserializeFloatPrecise(packet.SwimmingEyeHeight),
			Weight = CollectibleNet.DeserializeFloatPrecise(packet.Weight),
			CanClimb = (packet.CanClimb > 0),
			CanClimbAnywhere = (packet.CanClimbAnywhere > 0),
			FallDamage = (packet.FallDamage > 0),
			FallDamageMultiplier = CollectibleNet.DeserializeFloatPrecise(packet.FallDamageMultiplier),
			RotateModelOnClimb = (packet.RotateModelOnClimb > 0),
			ClimbTouchDistance = CollectibleNet.DeserializeFloatVeryPrecise(packet.ClimbTouchDistance),
			KnockbackResistance = CollectibleNet.DeserializeFloatPrecise(packet.KnockbackResistance)
		};
		if (et.SelectionBoxSize.X < 0f)
		{
			et.SelectionBoxSize = null;
		}
		if (et.DeadSelectionBoxSize.X < 0f)
		{
			et.DeadSelectionBoxSize = null;
		}
		if (packet.AnimationMetaData != null)
		{
			using MemoryStream ms = new MemoryStream(packet.AnimationMetaData);
			BinaryReader reader = new BinaryReader(ms);
			int animationsCount = reader.ReadInt32();
			et.Client.Animations = new AnimationMetaData[animationsCount];
			for (int j = 0; j < animationsCount; j++)
			{
				et.Client.Animations[j] = AnimationMetaData.FromBytes(reader, "1.20.7");
			}
		}
		et.Client.Init(et.Code, worldForResolve);
		et.Client.Textures = new FastSmallDictionary<string, CompositeTexture>(packet.TextureCodesCount);
		for (int i = 0; i < packet.TextureCodesCount; i++)
		{
			et.Client.Textures.Add(packet.TextureCodes[i], CollectibleNet.FromPacket(packet.CompositeTextures[i]));
		}
		CompositeTexture[] alternates = et.Client.FirstTexture?.Alternates;
		et.Client.TexturesAlternatesCount = ((alternates != null) ? alternates.Length : 0);
		return et;
	}

	private static byte[] getDropsPacket(BlockDropItemStack[] drops, FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter writer = new BinaryWriter(ms);
		if (drops == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(drops.Length);
			for (int i = 0; i < drops.Length; i++)
			{
				drops[i].ToBytes(writer);
			}
		}
		return ms.ToArray();
	}
}
