using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityTrader : EntityTradingHumanoid, ITalkUtil
{
	public static OrderedDictionary<string, TraderPersonality> Personalities = new OrderedDictionary<string, TraderPersonality>
	{
		{
			"formal",
			new TraderPersonality(1.5f, 1f, 0.9f)
		},
		{
			"balanced",
			new TraderPersonality(1.8000001f, 0.9f, 1.1f)
		},
		{
			"lazy",
			new TraderPersonality(2.475f, 0.7f, 0.9f)
		},
		{
			"rowdy",
			new TraderPersonality(1.125f, 1f, 1.8f)
		}
	};

	public EntityTalkUtil talkUtil;

	private EntityBehaviorConversable ConversableBh => GetBehavior<EntityBehaviorConversable>();

	public string Personality
	{
		get
		{
			return WatchedAttributes.GetString("personality", "formal");
		}
		set
		{
			WatchedAttributes.SetString("personality", value);
			talkUtil?.SetModifiers(Personalities[value].ChordDelayMul, Personalities[value].PitchModifier, Personalities[value].VolumneModifier);
		}
	}

	public override EntityTalkUtil TalkUtil => talkUtil;

	public EntityTrader()
	{
		AnimManager = new PersonalizedAnimationManager();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		if (api.Side == EnumAppSide.Client)
		{
			talkUtil = new EntityTalkUtil(api as ICoreClientAPI, this, isMultiSoundVoice: false);
		}
		(AnimManager as PersonalizedAnimationManager).Personality = Personality;
		Personality = Personality;
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		if (World.Api.Side == EnumAppSide.Server)
		{
			Personality = Personalities.GetKeyAtIndex(World.Rand.Next(Personalities.Count));
			(AnimManager as PersonalizedAnimationManager).Personality = Personality;
		}
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
			talkUtil.Talk(EnumTalkType.Hurt);
		}
		if (packetid == 198)
		{
			talkUtil.Talk(EnumTalkType.Death);
		}
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (Alive && AnimManager.ActiveAnimationsByAnimCode.Count == 0)
		{
			AnimManager.StartAnimation(new AnimationMetaData
			{
				Code = "idle",
				Animation = "idle",
				EaseOutSpeed = 10000f,
				EaseInSpeed = 10000f
			});
		}
		if (World.Side == EnumAppSide.Client)
		{
			talkUtil.OnGameTick(dt);
		}
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		(AnimManager as PersonalizedAnimationManager).Personality = Personality;
	}

	public override void Revive()
	{
		base.Revive();
		if (Attributes.HasAttribute("spawnX"))
		{
			ServerPos.X = Attributes.GetDouble("spawnX");
			ServerPos.Y = Attributes.GetDouble("spawnY");
			ServerPos.Z = Attributes.GetDouble("spawnZ");
		}
	}

	public override void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
	{
		if (type == "hurt" && World.Side == EnumAppSide.Server)
		{
			(World.Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 199);
		}
		else if (type == "death" && World.Side == EnumAppSide.Server)
		{
			(World.Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 198);
		}
		else
		{
			base.PlayEntitySound(type, dualCallByPlayer, randomizePitch, range);
		}
	}
}
