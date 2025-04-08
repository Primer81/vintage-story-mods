using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

[ProtoContract]
public class ServerWorldPlayerData : IWorldPlayerData
{
	[ProtoIgnore]
	internal int currentClientId;

	[ProtoIgnore]
	internal bool connected;

	[ProtoMember(1)]
	internal string PlayerUID;

	[ProtoMember(2)]
	private Dictionary<string, byte[]> inventoriesSerialized;

	[ProtoMember(3)]
	private byte[] EntityPlayerSerialized;

	[ProtoMember(4)]
	public EnumGameMode GameMode;

	[ProtoMember(5)]
	public float MoveSpeedMultiplier;

	[ProtoMember(11)]
	public float PickingRange;

	[ProtoMember(6)]
	public bool FreeMove;

	[ProtoMember(7)]
	public bool NoClip;

	[ProtoMember(8)]
	public int Viewdistance;

	[ProtoMember(9)]
	private int selectedHotbarslot;

	[ProtoMember(10)]
	private EnumFreeMovAxisLock freeMovePlaneLock;

	[ProtoMember(12)]
	private bool areaSelectionMode;

	[ProtoMember(13)]
	private bool didSelectSkin;

	[ProtoMember(14)]
	private PlayerSpawnPos spawnPosition;

	[ProtoMember(15)]
	public Dictionary<string, byte[]> ModData;

	[ProtoMember(16)]
	public float PreviousPickingRange = 100f;

	[ProtoMember(17)]
	public int Deaths;

	[ProtoMember(18)]
	public bool RenderMetaBlocks;

	private EntityPlayer Entityplayer;

	internal OrderedDictionary<string, InventoryBase> inventories = new OrderedDictionary<string, InventoryBase>();

	string IWorldPlayerData.PlayerUID => PlayerUID;

	public EntityPlayer EntityPlayer
	{
		get
		{
			return Entityplayer;
		}
		set
		{
			Entityplayer = value;
		}
	}

	public PlayerSpawnPos SpawnPosition
	{
		get
		{
			return spawnPosition;
		}
		set
		{
			spawnPosition = value;
		}
	}

	public EntityControls EntityControls => Entityplayer.Controls;

	public int LastApprovedViewDistance
	{
		get
		{
			return Viewdistance;
		}
		set
		{
			LastApprovedViewDistance = value;
		}
	}

	EnumGameMode IWorldPlayerData.CurrentGameMode
	{
		get
		{
			if (!connected)
			{
				return EnumGameMode.Spectator;
			}
			return GameMode;
		}
		set
		{
			GameMode = value;
			EntityPlayer?.UpdatePartitioning();
		}
	}

	bool IWorldPlayerData.FreeMove
	{
		get
		{
			return FreeMove;
		}
		set
		{
			FreeMove = value;
		}
	}

	bool IWorldPlayerData.NoClip
	{
		get
		{
			return NoClip;
		}
		set
		{
			NoClip = value;
			EntityPlayer?.UpdatePartitioning();
		}
	}

	public bool AreaSelectionMode
	{
		get
		{
			return areaSelectionMode;
		}
		set
		{
			areaSelectionMode = value;
		}
	}

	public EnumFreeMovAxisLock FreeMovePlaneLock
	{
		get
		{
			return freeMovePlaneLock;
		}
		set
		{
			freeMovePlaneLock = value;
		}
	}

	float IWorldPlayerData.MoveSpeedMultiplier
	{
		get
		{
			return MoveSpeedMultiplier;
		}
		set
		{
			MoveSpeedMultiplier = value;
		}
	}

	float IWorldPlayerData.PickingRange
	{
		get
		{
			return PickingRange;
		}
		set
		{
			PickingRange = value;
		}
	}

	public int CurrentClientId => currentClientId;

	public bool Connected => connected;

	public bool DidSelectSkin
	{
		get
		{
			return didSelectSkin;
		}
		set
		{
			didSelectSkin = value;
		}
	}

	public int SelectedHotbarSlot
	{
		get
		{
			return selectedHotbarslot;
		}
		set
		{
			selectedHotbarslot = value;
		}
	}

	public int DesiredViewDistance { get; set; }

	int IWorldPlayerData.Deaths => Deaths;

	public static ServerWorldPlayerData CreateNew(string playername, string playerUID)
	{
		return new ServerWorldPlayerData
		{
			Entityplayer = new EntityPlayer(),
			GameMode = EnumGameMode.Survival,
			MoveSpeedMultiplier = 1f,
			PickingRange = GlobalConstants.DefaultPickingRange,
			PlayerUID = playerUID,
			ModData = new Dictionary<string, byte[]>()
		};
	}

	public void Init(ServerMain server)
	{
		if (inventoriesSerialized == null)
		{
			inventoriesSerialized = new Dictionary<string, byte[]>();
		}
		bool clearinv = server.ClearPlayerInvs.Remove(PlayerUID);
		foreach (KeyValuePair<string, byte[]> val in inventoriesSerialized)
		{
			string[] parts = val.Key.Split('-');
			try
			{
				InventoryBase inv = ServerMain.ClassRegistry.CreateInventory(parts[0], val.Key, server.api);
				BinaryReader reader2 = new BinaryReader(new MemoryStream(val.Value));
				TreeAttribute tree = new TreeAttribute();
				tree.FromBytes(reader2);
				inv.FromTreeAttributes(tree);
				inventories.Add(val.Key, inv);
				if (clearinv)
				{
					inv.Clear();
				}
			}
			catch (Exception e2)
			{
				ServerMain.Logger.Error("Could not load a players inventory. Will ignore.");
				ServerMain.Logger.Error(e2);
			}
		}
		if (EntityPlayerSerialized != null && EntityPlayerSerialized.Length != 0)
		{
			using MemoryStream ms = new MemoryStream(EntityPlayerSerialized);
			BinaryReader reader = new BinaryReader(ms);
			string className = reader.ReadString();
			try
			{
				Entityplayer = (EntityPlayer)ServerMain.ClassRegistry.CreateEntity(className);
				Entityplayer.Code = GlobalConstants.EntityPlayerTypeCode;
				Entityplayer.FromBytes(reader, forClient: false);
				Entityplayer.Pos.FromBytes(reader);
				if (server.Config.RepairMode && Entityplayer.Code.Path != "player")
				{
					ServerMain.Logger.Error("Something derped with the player entity, its code is not 'player'. We are in repair mode so will reset this to 'player'. Will also reset their position to spawn for safety");
					Entityplayer.ServerPos.SetFrom(server.DefaultSpawnPosition);
					Entityplayer.Pos.SetFrom(server.DefaultSpawnPosition);
					Entityplayer.Code = AssetLocation.Create("game:player");
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Could not load an entityplayer. Will not read it's stored properties. Exception:");
				ServerMain.Logger.Error(e);
			}
		}
		EntityPlayerSerialized = null;
		if (PickingRange == 0f)
		{
			PickingRange = GlobalConstants.DefaultPickingRange;
		}
	}

	public void BeforeSerialization()
	{
		if (inventories == null)
		{
			return;
		}
		Dictionary<string, byte[]> dicts = new Dictionary<string, byte[]>();
		using FastMemoryStream ms = new FastMemoryStream();
		foreach (KeyValuePair<string, InventoryBase> val in inventories)
		{
			if (val.Value is InventoryBasePlayer)
			{
				ms.Reset();
				BinaryWriter writer2 = new BinaryWriter(ms);
				TreeAttribute tree = new TreeAttribute();
				val.Value.ToTreeAttributes(tree);
				tree.ToBytes(writer2);
				dicts.Add(val.Key, ms.ToArray());
			}
		}
		inventoriesSerialized = dicts;
		if (Entityplayer != null)
		{
			ms.Reset();
			BinaryWriter writer = new BinaryWriter(ms);
			writer.Write(ServerMain.ClassRegistry.entityTypeToClassNameMapping[Entityplayer.GetType()]);
			Entityplayer.ToBytes(writer, forClient: false);
			Entityplayer.Pos.ToBytes(writer);
			EntityPlayerSerialized = ms.ToArray();
		}
	}

	[ProtoAfterDeserialization]
	private void afterDeserialization()
	{
		if (ModData == null)
		{
			ModData = new Dictionary<string, byte[]>();
		}
	}

	public Packet_Server ToPacket(IServerPlayer owningPlayer, bool sendInventory = true, bool sendPrivileges = true)
	{
		FuzzyEntityPos spawnPos = owningPlayer.GetSpawnPosition(consumeSpawnUse: false);
		Packet_Server packet = new Packet_Server
		{
			Id = 41,
			PlayerData = new Packet_PlayerData
			{
				ClientId = owningPlayer.ClientId,
				EntityId = Entityplayer.EntityId,
				GameMode = (int)GameMode,
				MoveSpeed = CollectibleNet.SerializeFloat(MoveSpeedMultiplier),
				FreeMove = (FreeMove ? 1 : 0),
				PickingRange = CollectibleNet.SerializeFloat(PickingRange),
				NoClip = (NoClip ? 1 : 0),
				Deaths = Deaths,
				InventoryContents = null,
				InventoryContentsCount = 0,
				InventoryContentsLength = 0,
				PlayerUID = PlayerUID,
				HotbarSlotId = owningPlayer.InventoryManager.ActiveHotbarSlotNumber,
				Entitlements = string.Join(",", owningPlayer.Entitlements.Select((Entitlement e) => e.Code).ToArray()),
				FreeMovePlaneLock = (int)freeMovePlaneLock,
				AreaSelectionMode = (areaSelectionMode ? 1 : 0),
				Spawnx = spawnPos.XInt,
				Spawny = spawnPos.YInt,
				Spawnz = spawnPos.ZInt
			}
		};
		if (sendPrivileges)
		{
			packet.PlayerData.SetPrivileges(owningPlayer.Privileges);
			packet.PlayerData.RoleCode = owningPlayer.Role.Code;
		}
		if (sendInventory)
		{
			List<Packet_InventoryContents> invpackets = new List<Packet_InventoryContents>();
			foreach (InventoryBase inv in inventories.ValuesOrdered)
			{
				if (inv is InventoryBasePlayer)
				{
					invpackets.Add((inv.InvNetworkUtil as InventoryNetworkUtil).ToPacket(owningPlayer));
				}
			}
			packet.PlayerData.SetInventoryContents(invpackets.ToArray());
		}
		return packet;
	}

	public Packet_Server ToPacketForOtherPlayers(IServerPlayer owningPlayer)
	{
		List<InventoryBasePlayer> visibleInventories = new List<InventoryBasePlayer>();
		foreach (InventoryBase inv2 in inventories.ValuesOrdered)
		{
			if (inv2 is InventoryBasePlayer && (inv2.ClassName == "hotbar" || inv2.ClassName == "character" || inv2.ClassName == "backpack"))
			{
				visibleInventories.Add((InventoryBasePlayer)inv2);
			}
		}
		Packet_InventoryContents[] contents = new Packet_InventoryContents[visibleInventories.Count];
		int i = 0;
		foreach (InventoryBasePlayer inv in visibleInventories)
		{
			contents[i++] = (inv.InvNetworkUtil as InventoryNetworkUtil).ToPacket(owningPlayer);
		}
		return new Packet_Server
		{
			Id = 41,
			PlayerData = new Packet_PlayerData
			{
				ClientId = owningPlayer.ClientId,
				EntityId = Entityplayer.EntityId,
				PlayerUID = PlayerUID,
				PlayerName = owningPlayer.PlayerName,
				GameMode = (int)GameMode,
				MoveSpeed = CollectibleNet.SerializeFloat(MoveSpeedMultiplier),
				PickingRange = CollectibleNet.SerializeFloat(PickingRange),
				FreeMove = (FreeMove ? 1 : 0),
				NoClip = (NoClip ? 1 : 0),
				InventoryContents = contents,
				HotbarSlotId = owningPlayer.InventoryManager.ActiveHotbarSlotNumber,
				Entitlements = string.Join(",", owningPlayer.Entitlements.Select((Entitlement e) => e.Code).ToArray()),
				InventoryContentsCount = contents.Length,
				InventoryContentsLength = contents.Length,
				FreeMovePlaneLock = (int)freeMovePlaneLock,
				AreaSelectionMode = (areaSelectionMode ? 1 : 0)
			}
		};
	}

	public void SetModData<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModData<T>(string key, T defaultValue = default(T))
	{
		byte[] data = GetModdata(key);
		if (data == null)
		{
			return defaultValue;
		}
		return SerializerUtil.Deserialize<T>(data);
	}

	public void SetModdata(string key, byte[] data)
	{
		ModData[key] = data;
	}

	public void RemoveModdata(string key)
	{
		ModData.Remove(key);
	}

	public byte[] GetModdata(string key)
	{
		ModData.TryGetValue(key, out var data);
		return data;
	}
}
