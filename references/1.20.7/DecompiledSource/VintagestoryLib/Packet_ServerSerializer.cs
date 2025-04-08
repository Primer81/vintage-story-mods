public class Packet_ServerSerializer
{
	private const int field = 8;

	public static Packet_Server DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Server instance = new Packet_Server();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Server DeserializeBuffer(byte[] buffer, int length, Packet_Server instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Server Deserialize(CitoMemoryStream stream, Packet_Server instance)
	{
		instance.InitializeValues();
		int keyInt;
		while (true)
		{
			keyInt = stream.ReadByte();
			if (((uint)keyInt & 0x80u) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if (((uint)keyInt & 0x4000u) != 0)
				{
					break;
				}
			}
			switch (keyInt)
			{
			case 0:
				return null;
			case 720:
				instance.Id = ProtocolParser.ReadUInt32(stream);
				break;
			case 618:
				if (instance.Token == null)
				{
					instance.Token = Packet_LoginTokenAnswerSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_LoginTokenAnswerSerializer.DeserializeLengthDelimited(stream, instance.Token);
				}
				break;
			case 10:
				if (instance.Identification == null)
				{
					instance.Identification = Packet_ServerIdentificationSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerIdentificationSerializer.DeserializeLengthDelimited(stream, instance.Identification);
				}
				break;
			case 18:
				if (instance.LevelInitialize == null)
				{
					instance.LevelInitialize = Packet_ServerLevelInitializeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerLevelInitializeSerializer.DeserializeLengthDelimited(stream, instance.LevelInitialize);
				}
				break;
			case 26:
				if (instance.LevelDataChunk == null)
				{
					instance.LevelDataChunk = Packet_ServerLevelProgressSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerLevelProgressSerializer.DeserializeLengthDelimited(stream, instance.LevelDataChunk);
				}
				break;
			case 34:
				if (instance.LevelFinalize == null)
				{
					instance.LevelFinalize = Packet_ServerLevelFinalizeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerLevelFinalizeSerializer.DeserializeLengthDelimited(stream, instance.LevelFinalize);
				}
				break;
			case 42:
				if (instance.SetBlock == null)
				{
					instance.SetBlock = Packet_ServerSetBlockSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlockSerializer.DeserializeLengthDelimited(stream, instance.SetBlock);
				}
				break;
			case 58:
				if (instance.Chatline == null)
				{
					instance.Chatline = Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ChatLineSerializer.DeserializeLengthDelimited(stream, instance.Chatline);
				}
				break;
			case 66:
				if (instance.DisconnectPlayer == null)
				{
					instance.DisconnectPlayer = Packet_ServerDisconnectPlayerSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerDisconnectPlayerSerializer.DeserializeLengthDelimited(stream, instance.DisconnectPlayer);
				}
				break;
			case 74:
				if (instance.Chunks == null)
				{
					instance.Chunks = Packet_ServerChunksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerChunksSerializer.DeserializeLengthDelimited(stream, instance.Chunks);
				}
				break;
			case 82:
				if (instance.UnloadChunk == null)
				{
					instance.UnloadChunk = Packet_UnloadServerChunkSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UnloadServerChunkSerializer.DeserializeLengthDelimited(stream, instance.UnloadChunk);
				}
				break;
			case 90:
				if (instance.Calendar == null)
				{
					instance.Calendar = Packet_ServerCalendarSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerCalendarSerializer.DeserializeLengthDelimited(stream, instance.Calendar);
				}
				break;
			case 122:
				if (instance.MapChunk == null)
				{
					instance.MapChunk = Packet_ServerMapChunkSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerMapChunkSerializer.DeserializeLengthDelimited(stream, instance.MapChunk);
				}
				break;
			case 130:
				if (instance.Ping == null)
				{
					instance.Ping = Packet_ServerPingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerPingSerializer.DeserializeLengthDelimited(stream, instance.Ping);
				}
				break;
			case 138:
				if (instance.PlayerPing == null)
				{
					instance.PlayerPing = Packet_ServerPlayerPingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerPlayerPingSerializer.DeserializeLengthDelimited(stream, instance.PlayerPing);
				}
				break;
			case 146:
				if (instance.Sound == null)
				{
					instance.Sound = Packet_ServerSoundSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSoundSerializer.DeserializeLengthDelimited(stream, instance.Sound);
				}
				break;
			case 154:
				if (instance.Assets == null)
				{
					instance.Assets = Packet_ServerAssetsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerAssetsSerializer.DeserializeLengthDelimited(stream, instance.Assets);
				}
				break;
			case 170:
				if (instance.WorldMetaData == null)
				{
					instance.WorldMetaData = Packet_WorldMetaDataSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_WorldMetaDataSerializer.DeserializeLengthDelimited(stream, instance.WorldMetaData);
				}
				break;
			case 226:
				if (instance.QueryAnswer == null)
				{
					instance.QueryAnswer = Packet_ServerQueryAnswerSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerQueryAnswerSerializer.DeserializeLengthDelimited(stream, instance.QueryAnswer);
				}
				break;
			case 234:
				if (instance.Redirect == null)
				{
					instance.Redirect = Packet_ServerRedirectSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerRedirectSerializer.DeserializeLengthDelimited(stream, instance.Redirect);
				}
				break;
			case 242:
				if (instance.InventoryContents == null)
				{
					instance.InventoryContents = Packet_InventoryContentsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InventoryContentsSerializer.DeserializeLengthDelimited(stream, instance.InventoryContents);
				}
				break;
			case 250:
				if (instance.InventoryUpdate == null)
				{
					instance.InventoryUpdate = Packet_InventoryUpdateSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InventoryUpdateSerializer.DeserializeLengthDelimited(stream, instance.InventoryUpdate);
				}
				break;
			case 258:
				if (instance.InventoryDoubleUpdate == null)
				{
					instance.InventoryDoubleUpdate = Packet_InventoryDoubleUpdateSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InventoryDoubleUpdateSerializer.DeserializeLengthDelimited(stream, instance.InventoryDoubleUpdate);
				}
				break;
			case 274:
				if (instance.Entity == null)
				{
					instance.Entity = Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntitySerializer.DeserializeLengthDelimited(stream, instance.Entity);
				}
				break;
			case 282:
				if (instance.EntitySpawn == null)
				{
					instance.EntitySpawn = Packet_EntitySpawnSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntitySpawnSerializer.DeserializeLengthDelimited(stream, instance.EntitySpawn);
				}
				break;
			case 290:
				if (instance.EntityDespawn == null)
				{
					instance.EntityDespawn = Packet_EntityDespawnSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityDespawnSerializer.DeserializeLengthDelimited(stream, instance.EntityDespawn);
				}
				break;
			case 306:
				if (instance.EntityAttributes == null)
				{
					instance.EntityAttributes = Packet_EntityAttributesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityAttributesSerializer.DeserializeLengthDelimited(stream, instance.EntityAttributes);
				}
				break;
			case 314:
				if (instance.EntityAttributeUpdate == null)
				{
					instance.EntityAttributeUpdate = Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimited(stream, instance.EntityAttributeUpdate);
				}
				break;
			case 538:
				if (instance.EntityPacket == null)
				{
					instance.EntityPacket = Packet_EntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPacketSerializer.DeserializeLengthDelimited(stream, instance.EntityPacket);
				}
				break;
			case 322:
				if (instance.Entities == null)
				{
					instance.Entities = Packet_EntitiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntitiesSerializer.DeserializeLengthDelimited(stream, instance.Entities);
				}
				break;
			case 330:
				if (instance.PlayerData == null)
				{
					instance.PlayerData = Packet_PlayerDataSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerDataSerializer.DeserializeLengthDelimited(stream, instance.PlayerData);
				}
				break;
			case 338:
				if (instance.MapRegion == null)
				{
					instance.MapRegion = Packet_MapRegionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_MapRegionSerializer.DeserializeLengthDelimited(stream, instance.MapRegion);
				}
				break;
			case 354:
				if (instance.BlockEntityMessage == null)
				{
					instance.BlockEntityMessage = Packet_BlockEntityMessageSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockEntityMessageSerializer.DeserializeLengthDelimited(stream, instance.BlockEntityMessage);
				}
				break;
			case 362:
				if (instance.PlayerDeath == null)
				{
					instance.PlayerDeath = Packet_PlayerDeathSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerDeathSerializer.DeserializeLengthDelimited(stream, instance.PlayerDeath);
				}
				break;
			case 370:
				if (instance.ModeChange == null)
				{
					instance.ModeChange = Packet_PlayerModeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerModeSerializer.DeserializeLengthDelimited(stream, instance.ModeChange);
				}
				break;
			case 378:
				if (instance.SetBlocks == null)
				{
					instance.SetBlocks = Packet_ServerSetBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance.SetBlocks);
				}
				break;
			case 386:
				if (instance.BlockEntities == null)
				{
					instance.BlockEntities = Packet_BlockEntitiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockEntitiesSerializer.DeserializeLengthDelimited(stream, instance.BlockEntities);
				}
				break;
			case 394:
				if (instance.PlayerGroups == null)
				{
					instance.PlayerGroups = Packet_PlayerGroupsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerGroupsSerializer.DeserializeLengthDelimited(stream, instance.PlayerGroups);
				}
				break;
			case 402:
				if (instance.PlayerGroup == null)
				{
					instance.PlayerGroup = Packet_PlayerGroupSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerGroupSerializer.DeserializeLengthDelimited(stream, instance.PlayerGroup);
				}
				break;
			case 410:
				if (instance.EntityPosition == null)
				{
					instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
				}
				break;
			case 418:
				if (instance.HighlightBlocks == null)
				{
					instance.HighlightBlocks = Packet_HighlightBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_HighlightBlocksSerializer.DeserializeLengthDelimited(stream, instance.HighlightBlocks);
				}
				break;
			case 426:
				if (instance.SelectedHotbarSlot == null)
				{
					instance.SelectedHotbarSlot = Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimited(stream, instance.SelectedHotbarSlot);
				}
				break;
			case 442:
				if (instance.CustomPacket == null)
				{
					instance.CustomPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.CustomPacket);
				}
				break;
			case 450:
				if (instance.NetworkChannels == null)
				{
					instance.NetworkChannels = Packet_NetworkChannelsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NetworkChannelsSerializer.DeserializeLengthDelimited(stream, instance.NetworkChannels);
				}
				break;
			case 458:
				if (instance.GotoGroup == null)
				{
					instance.GotoGroup = Packet_GotoGroupSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_GotoGroupSerializer.DeserializeLengthDelimited(stream, instance.GotoGroup);
				}
				break;
			case 466:
				if (instance.ExchangeBlock == null)
				{
					instance.ExchangeBlock = Packet_ServerExchangeBlockSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerExchangeBlockSerializer.DeserializeLengthDelimited(stream, instance.ExchangeBlock);
				}
				break;
			case 474:
				if (instance.BulkEntityAttributes == null)
				{
					instance.BulkEntityAttributes = Packet_BulkEntityAttributesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BulkEntityAttributesSerializer.DeserializeLengthDelimited(stream, instance.BulkEntityAttributes);
				}
				break;
			case 482:
				if (instance.SpawnParticles == null)
				{
					instance.SpawnParticles = Packet_SpawnParticlesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_SpawnParticlesSerializer.DeserializeLengthDelimited(stream, instance.SpawnParticles);
				}
				break;
			case 490:
				if (instance.BulkEntityDebugAttributes == null)
				{
					instance.BulkEntityDebugAttributes = Packet_BulkEntityDebugAttributesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BulkEntityDebugAttributesSerializer.DeserializeLengthDelimited(stream, instance.BulkEntityDebugAttributes);
				}
				break;
			case 498:
				if (instance.SetBlocksNoRelight == null)
				{
					instance.SetBlocksNoRelight = Packet_ServerSetBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance.SetBlocksNoRelight);
				}
				break;
			case 514:
				if (instance.BlockDamage == null)
				{
					instance.BlockDamage = Packet_BlockDamageSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockDamageSerializer.DeserializeLengthDelimited(stream, instance.BlockDamage);
				}
				break;
			case 522:
				if (instance.Ambient == null)
				{
					instance.Ambient = Packet_AmbientSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_AmbientSerializer.DeserializeLengthDelimited(stream, instance.Ambient);
				}
				break;
			case 530:
				if (instance.NotifySlot == null)
				{
					instance.NotifySlot = Packet_NotifySlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NotifySlotSerializer.DeserializeLengthDelimited(stream, instance.NotifySlot);
				}
				break;
			case 546:
				if (instance.IngameError == null)
				{
					instance.IngameError = Packet_IngameErrorSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IngameErrorSerializer.DeserializeLengthDelimited(stream, instance.IngameError);
				}
				break;
			case 554:
				if (instance.IngameDiscovery == null)
				{
					instance.IngameDiscovery = Packet_IngameDiscoverySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IngameDiscoverySerializer.DeserializeLengthDelimited(stream, instance.IngameDiscovery);
				}
				break;
			case 562:
				if (instance.SetBlocksMinimal == null)
				{
					instance.SetBlocksMinimal = Packet_ServerSetBlocksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance.SetBlocksMinimal);
				}
				break;
			case 570:
				if (instance.SetDecors == null)
				{
					instance.SetDecors = Packet_ServerSetDecorsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerSetDecorsSerializer.DeserializeLengthDelimited(stream, instance.SetDecors);
				}
				break;
			case 578:
				if (instance.RemoveBlockLight == null)
				{
					instance.RemoveBlockLight = Packet_RemoveBlockLightSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_RemoveBlockLightSerializer.DeserializeLengthDelimited(stream, instance.RemoveBlockLight);
				}
				break;
			case 586:
				if (instance.ServerReady == null)
				{
					instance.ServerReady = Packet_ServerReadySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ServerReadySerializer.DeserializeLengthDelimited(stream, instance.ServerReady);
				}
				break;
			case 594:
				if (instance.UnloadMapRegion == null)
				{
					instance.UnloadMapRegion = Packet_UnloadMapRegionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UnloadMapRegionSerializer.DeserializeLengthDelimited(stream, instance.UnloadMapRegion);
				}
				break;
			case 602:
				if (instance.LandClaims == null)
				{
					instance.LandClaims = Packet_LandClaimsSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_LandClaimsSerializer.DeserializeLengthDelimited(stream, instance.LandClaims);
				}
				break;
			case 610:
				if (instance.Roles == null)
				{
					instance.Roles = Packet_RolesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_RolesSerializer.DeserializeLengthDelimited(stream, instance.Roles);
				}
				break;
			case 626:
				if (instance.UdpPacket == null)
				{
					instance.UdpPacket = Packet_UdpPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UdpPacketSerializer.DeserializeLengthDelimited(stream, instance.UdpPacket);
				}
				break;
			case 634:
				if (instance.QueuePacket == null)
				{
					instance.QueuePacket = Packet_QueuePacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_QueuePacketSerializer.DeserializeLengthDelimited(stream, instance.QueuePacket);
				}
				break;
			default:
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				break;
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
	}

	public static Packet_Server DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Server instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Server result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Server instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Id != 1)
		{
			stream.WriteKey(90, 0);
			ProtocolParser.WriteUInt32(stream, instance.Id);
		}
		if (instance.Token != null)
		{
			stream.WriteKey(77, 2);
			CitoMemoryStream ms59 = new CitoMemoryStream(subBuffer);
			Packet_LoginTokenAnswerSerializer.Serialize(ms59, instance.Token);
			int len63 = ms59.Position();
			ProtocolParser.WriteUInt32_(stream, len63);
			stream.Write(ms59.GetBuffer(), 0, len63);
		}
		if (instance.Identification != null)
		{
			stream.WriteByte(10);
			CitoMemoryStream ms1 = new CitoMemoryStream(subBuffer);
			Packet_ServerIdentificationSerializer.Serialize(ms1, instance.Identification);
			int len62 = ms1.Position();
			ProtocolParser.WriteUInt32_(stream, len62);
			stream.Write(ms1.GetBuffer(), 0, len62);
		}
		if (instance.LevelInitialize != null)
		{
			stream.WriteByte(18);
			CitoMemoryStream ms9 = new CitoMemoryStream(subBuffer);
			Packet_ServerLevelInitializeSerializer.Serialize(ms9, instance.LevelInitialize);
			int len61 = ms9.Position();
			ProtocolParser.WriteUInt32_(stream, len61);
			stream.Write(ms9.GetBuffer(), 0, len61);
		}
		if (instance.LevelDataChunk != null)
		{
			stream.WriteByte(26);
			CitoMemoryStream ms13 = new CitoMemoryStream(subBuffer);
			Packet_ServerLevelProgressSerializer.Serialize(ms13, instance.LevelDataChunk);
			int len60 = ms13.Position();
			ProtocolParser.WriteUInt32_(stream, len60);
			stream.Write(ms13.GetBuffer(), 0, len60);
		}
		if (instance.LevelFinalize != null)
		{
			stream.WriteByte(34);
			CitoMemoryStream ms22 = new CitoMemoryStream(subBuffer);
			Packet_ServerLevelFinalizeSerializer.Serialize(ms22, instance.LevelFinalize);
			int len59 = ms22.Position();
			ProtocolParser.WriteUInt32_(stream, len59);
			stream.Write(ms22.GetBuffer(), 0, len59);
		}
		if (instance.SetBlock != null)
		{
			stream.WriteByte(42);
			CitoMemoryStream ms32 = new CitoMemoryStream(subBuffer);
			Packet_ServerSetBlockSerializer.Serialize(ms32, instance.SetBlock);
			int len58 = ms32.Position();
			ProtocolParser.WriteUInt32_(stream, len58);
			stream.Write(ms32.GetBuffer(), 0, len58);
		}
		if (instance.Chatline != null)
		{
			stream.WriteByte(58);
			CitoMemoryStream ms51 = new CitoMemoryStream(subBuffer);
			Packet_ChatLineSerializer.Serialize(ms51, instance.Chatline);
			int len57 = ms51.Position();
			ProtocolParser.WriteUInt32_(stream, len57);
			stream.Write(ms51.GetBuffer(), 0, len57);
		}
		if (instance.DisconnectPlayer != null)
		{
			stream.WriteByte(66);
			CitoMemoryStream ms62 = new CitoMemoryStream(subBuffer);
			Packet_ServerDisconnectPlayerSerializer.Serialize(ms62, instance.DisconnectPlayer);
			int len56 = ms62.Position();
			ProtocolParser.WriteUInt32_(stream, len56);
			stream.Write(ms62.GetBuffer(), 0, len56);
		}
		if (instance.Chunks != null)
		{
			stream.WriteByte(74);
			CitoMemoryStream ms63 = new CitoMemoryStream(subBuffer);
			Packet_ServerChunksSerializer.Serialize(ms63, instance.Chunks);
			int len55 = ms63.Position();
			ProtocolParser.WriteUInt32_(stream, len55);
			stream.Write(ms63.GetBuffer(), 0, len55);
		}
		if (instance.UnloadChunk != null)
		{
			stream.WriteByte(82);
			CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
			Packet_UnloadServerChunkSerializer.Serialize(ms2, instance.UnloadChunk);
			int len54 = ms2.Position();
			ProtocolParser.WriteUInt32_(stream, len54);
			stream.Write(ms2.GetBuffer(), 0, len54);
		}
		if (instance.Calendar != null)
		{
			stream.WriteByte(90);
			CitoMemoryStream ms3 = new CitoMemoryStream(subBuffer);
			Packet_ServerCalendarSerializer.Serialize(ms3, instance.Calendar);
			int len53 = ms3.Position();
			ProtocolParser.WriteUInt32_(stream, len53);
			stream.Write(ms3.GetBuffer(), 0, len53);
		}
		if (instance.MapChunk != null)
		{
			stream.WriteByte(122);
			CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
			Packet_ServerMapChunkSerializer.Serialize(ms4, instance.MapChunk);
			int len52 = ms4.Position();
			ProtocolParser.WriteUInt32_(stream, len52);
			stream.Write(ms4.GetBuffer(), 0, len52);
		}
		if (instance.Ping != null)
		{
			stream.WriteKey(16, 2);
			CitoMemoryStream ms5 = new CitoMemoryStream(subBuffer);
			Packet_ServerPingSerializer.Serialize(ms5, instance.Ping);
			int len51 = ms5.Position();
			ProtocolParser.WriteUInt32_(stream, len51);
			stream.Write(ms5.GetBuffer(), 0, len51);
		}
		if (instance.PlayerPing != null)
		{
			stream.WriteKey(17, 2);
			CitoMemoryStream ms6 = new CitoMemoryStream(subBuffer);
			Packet_ServerPlayerPingSerializer.Serialize(ms6, instance.PlayerPing);
			int len50 = ms6.Position();
			ProtocolParser.WriteUInt32_(stream, len50);
			stream.Write(ms6.GetBuffer(), 0, len50);
		}
		if (instance.Sound != null)
		{
			stream.WriteKey(18, 2);
			CitoMemoryStream ms7 = new CitoMemoryStream(subBuffer);
			Packet_ServerSoundSerializer.Serialize(ms7, instance.Sound);
			int len49 = ms7.Position();
			ProtocolParser.WriteUInt32_(stream, len49);
			stream.Write(ms7.GetBuffer(), 0, len49);
		}
		if (instance.Assets != null)
		{
			stream.WriteKey(19, 2);
			CitoMemoryStream ms8 = new CitoMemoryStream(subBuffer);
			Packet_ServerAssetsSerializer.Serialize(ms8, instance.Assets);
			int len48 = ms8.Position();
			ProtocolParser.WriteUInt32_(stream, len48);
			stream.Write(ms8.GetBuffer(), 0, len48);
		}
		if (instance.WorldMetaData != null)
		{
			stream.WriteKey(21, 2);
			CitoMemoryStream ms10 = new CitoMemoryStream(subBuffer);
			Packet_WorldMetaDataSerializer.Serialize(ms10, instance.WorldMetaData);
			int len47 = ms10.Position();
			ProtocolParser.WriteUInt32_(stream, len47);
			stream.Write(ms10.GetBuffer(), 0, len47);
		}
		if (instance.QueryAnswer != null)
		{
			stream.WriteKey(28, 2);
			CitoMemoryStream ms11 = new CitoMemoryStream(subBuffer);
			Packet_ServerQueryAnswerSerializer.Serialize(ms11, instance.QueryAnswer);
			int len46 = ms11.Position();
			ProtocolParser.WriteUInt32_(stream, len46);
			stream.Write(ms11.GetBuffer(), 0, len46);
		}
		if (instance.Redirect != null)
		{
			stream.WriteKey(29, 2);
			CitoMemoryStream ms12 = new CitoMemoryStream(subBuffer);
			Packet_ServerRedirectSerializer.Serialize(ms12, instance.Redirect);
			int len45 = ms12.Position();
			ProtocolParser.WriteUInt32_(stream, len45);
			stream.Write(ms12.GetBuffer(), 0, len45);
		}
		if (instance.InventoryContents != null)
		{
			stream.WriteKey(30, 2);
			CitoMemoryStream ms14 = new CitoMemoryStream(subBuffer);
			Packet_InventoryContentsSerializer.Serialize(ms14, instance.InventoryContents);
			int len44 = ms14.Position();
			ProtocolParser.WriteUInt32_(stream, len44);
			stream.Write(ms14.GetBuffer(), 0, len44);
		}
		if (instance.InventoryUpdate != null)
		{
			stream.WriteKey(31, 2);
			CitoMemoryStream ms15 = new CitoMemoryStream(subBuffer);
			Packet_InventoryUpdateSerializer.Serialize(ms15, instance.InventoryUpdate);
			int len43 = ms15.Position();
			ProtocolParser.WriteUInt32_(stream, len43);
			stream.Write(ms15.GetBuffer(), 0, len43);
		}
		if (instance.InventoryDoubleUpdate != null)
		{
			stream.WriteKey(32, 2);
			CitoMemoryStream ms16 = new CitoMemoryStream(subBuffer);
			Packet_InventoryDoubleUpdateSerializer.Serialize(ms16, instance.InventoryDoubleUpdate);
			int len42 = ms16.Position();
			ProtocolParser.WriteUInt32_(stream, len42);
			stream.Write(ms16.GetBuffer(), 0, len42);
		}
		if (instance.Entity != null)
		{
			stream.WriteKey(34, 2);
			CitoMemoryStream ms17 = new CitoMemoryStream(subBuffer);
			Packet_EntitySerializer.Serialize(ms17, instance.Entity);
			int len41 = ms17.Position();
			ProtocolParser.WriteUInt32_(stream, len41);
			stream.Write(ms17.GetBuffer(), 0, len41);
		}
		if (instance.EntitySpawn != null)
		{
			stream.WriteKey(35, 2);
			CitoMemoryStream ms18 = new CitoMemoryStream(subBuffer);
			Packet_EntitySpawnSerializer.Serialize(ms18, instance.EntitySpawn);
			int len40 = ms18.Position();
			ProtocolParser.WriteUInt32_(stream, len40);
			stream.Write(ms18.GetBuffer(), 0, len40);
		}
		if (instance.EntityDespawn != null)
		{
			stream.WriteKey(36, 2);
			CitoMemoryStream ms19 = new CitoMemoryStream(subBuffer);
			Packet_EntityDespawnSerializer.Serialize(ms19, instance.EntityDespawn);
			int len39 = ms19.Position();
			ProtocolParser.WriteUInt32_(stream, len39);
			stream.Write(ms19.GetBuffer(), 0, len39);
		}
		if (instance.EntityAttributes != null)
		{
			stream.WriteKey(38, 2);
			CitoMemoryStream ms20 = new CitoMemoryStream(subBuffer);
			Packet_EntityAttributesSerializer.Serialize(ms20, instance.EntityAttributes);
			int len38 = ms20.Position();
			ProtocolParser.WriteUInt32_(stream, len38);
			stream.Write(ms20.GetBuffer(), 0, len38);
		}
		if (instance.EntityAttributeUpdate != null)
		{
			stream.WriteKey(39, 2);
			CitoMemoryStream ms21 = new CitoMemoryStream(subBuffer);
			Packet_EntityAttributeUpdateSerializer.Serialize(ms21, instance.EntityAttributeUpdate);
			int len37 = ms21.Position();
			ProtocolParser.WriteUInt32_(stream, len37);
			stream.Write(ms21.GetBuffer(), 0, len37);
		}
		if (instance.EntityPacket != null)
		{
			stream.WriteKey(67, 2);
			CitoMemoryStream ms48 = new CitoMemoryStream(subBuffer);
			Packet_EntityPacketSerializer.Serialize(ms48, instance.EntityPacket);
			int len36 = ms48.Position();
			ProtocolParser.WriteUInt32_(stream, len36);
			stream.Write(ms48.GetBuffer(), 0, len36);
		}
		if (instance.Entities != null)
		{
			stream.WriteKey(40, 2);
			CitoMemoryStream ms23 = new CitoMemoryStream(subBuffer);
			Packet_EntitiesSerializer.Serialize(ms23, instance.Entities);
			int len35 = ms23.Position();
			ProtocolParser.WriteUInt32_(stream, len35);
			stream.Write(ms23.GetBuffer(), 0, len35);
		}
		if (instance.PlayerData != null)
		{
			stream.WriteKey(41, 2);
			CitoMemoryStream ms24 = new CitoMemoryStream(subBuffer);
			Packet_PlayerDataSerializer.Serialize(ms24, instance.PlayerData);
			int len34 = ms24.Position();
			ProtocolParser.WriteUInt32_(stream, len34);
			stream.Write(ms24.GetBuffer(), 0, len34);
		}
		if (instance.MapRegion != null)
		{
			stream.WriteKey(42, 2);
			CitoMemoryStream ms25 = new CitoMemoryStream(subBuffer);
			Packet_MapRegionSerializer.Serialize(ms25, instance.MapRegion);
			int len33 = ms25.Position();
			ProtocolParser.WriteUInt32_(stream, len33);
			stream.Write(ms25.GetBuffer(), 0, len33);
		}
		if (instance.BlockEntityMessage != null)
		{
			stream.WriteKey(44, 2);
			CitoMemoryStream ms26 = new CitoMemoryStream(subBuffer);
			Packet_BlockEntityMessageSerializer.Serialize(ms26, instance.BlockEntityMessage);
			int len32 = ms26.Position();
			ProtocolParser.WriteUInt32_(stream, len32);
			stream.Write(ms26.GetBuffer(), 0, len32);
		}
		if (instance.PlayerDeath != null)
		{
			stream.WriteKey(45, 2);
			CitoMemoryStream ms27 = new CitoMemoryStream(subBuffer);
			Packet_PlayerDeathSerializer.Serialize(ms27, instance.PlayerDeath);
			int len31 = ms27.Position();
			ProtocolParser.WriteUInt32_(stream, len31);
			stream.Write(ms27.GetBuffer(), 0, len31);
		}
		if (instance.ModeChange != null)
		{
			stream.WriteKey(46, 2);
			CitoMemoryStream ms28 = new CitoMemoryStream(subBuffer);
			Packet_PlayerModeSerializer.Serialize(ms28, instance.ModeChange);
			int len30 = ms28.Position();
			ProtocolParser.WriteUInt32_(stream, len30);
			stream.Write(ms28.GetBuffer(), 0, len30);
		}
		if (instance.SetBlocks != null)
		{
			stream.WriteKey(47, 2);
			CitoMemoryStream ms29 = new CitoMemoryStream(subBuffer);
			Packet_ServerSetBlocksSerializer.Serialize(ms29, instance.SetBlocks);
			int len29 = ms29.Position();
			ProtocolParser.WriteUInt32_(stream, len29);
			stream.Write(ms29.GetBuffer(), 0, len29);
		}
		if (instance.BlockEntities != null)
		{
			stream.WriteKey(48, 2);
			CitoMemoryStream ms30 = new CitoMemoryStream(subBuffer);
			Packet_BlockEntitiesSerializer.Serialize(ms30, instance.BlockEntities);
			int len28 = ms30.Position();
			ProtocolParser.WriteUInt32_(stream, len28);
			stream.Write(ms30.GetBuffer(), 0, len28);
		}
		if (instance.PlayerGroups != null)
		{
			stream.WriteKey(49, 2);
			CitoMemoryStream ms31 = new CitoMemoryStream(subBuffer);
			Packet_PlayerGroupsSerializer.Serialize(ms31, instance.PlayerGroups);
			int len27 = ms31.Position();
			ProtocolParser.WriteUInt32_(stream, len27);
			stream.Write(ms31.GetBuffer(), 0, len27);
		}
		if (instance.PlayerGroup != null)
		{
			stream.WriteKey(50, 2);
			CitoMemoryStream ms33 = new CitoMemoryStream(subBuffer);
			Packet_PlayerGroupSerializer.Serialize(ms33, instance.PlayerGroup);
			int len26 = ms33.Position();
			ProtocolParser.WriteUInt32_(stream, len26);
			stream.Write(ms33.GetBuffer(), 0, len26);
		}
		if (instance.EntityPosition != null)
		{
			stream.WriteKey(51, 2);
			CitoMemoryStream ms34 = new CitoMemoryStream(subBuffer);
			Packet_EntityPositionSerializer.Serialize(ms34, instance.EntityPosition);
			int len25 = ms34.Position();
			ProtocolParser.WriteUInt32_(stream, len25);
			stream.Write(ms34.GetBuffer(), 0, len25);
		}
		if (instance.HighlightBlocks != null)
		{
			stream.WriteKey(52, 2);
			CitoMemoryStream ms35 = new CitoMemoryStream(subBuffer);
			Packet_HighlightBlocksSerializer.Serialize(ms35, instance.HighlightBlocks);
			int len24 = ms35.Position();
			ProtocolParser.WriteUInt32_(stream, len24);
			stream.Write(ms35.GetBuffer(), 0, len24);
		}
		if (instance.SelectedHotbarSlot != null)
		{
			stream.WriteKey(53, 2);
			CitoMemoryStream ms36 = new CitoMemoryStream(subBuffer);
			Packet_SelectedHotbarSlotSerializer.Serialize(ms36, instance.SelectedHotbarSlot);
			int len23 = ms36.Position();
			ProtocolParser.WriteUInt32_(stream, len23);
			stream.Write(ms36.GetBuffer(), 0, len23);
		}
		if (instance.CustomPacket != null)
		{
			stream.WriteKey(55, 2);
			CitoMemoryStream ms37 = new CitoMemoryStream(subBuffer);
			Packet_CustomPacketSerializer.Serialize(ms37, instance.CustomPacket);
			int len22 = ms37.Position();
			ProtocolParser.WriteUInt32_(stream, len22);
			stream.Write(ms37.GetBuffer(), 0, len22);
		}
		if (instance.NetworkChannels != null)
		{
			stream.WriteKey(56, 2);
			CitoMemoryStream ms38 = new CitoMemoryStream(subBuffer);
			Packet_NetworkChannelsSerializer.Serialize(ms38, instance.NetworkChannels);
			int len21 = ms38.Position();
			ProtocolParser.WriteUInt32_(stream, len21);
			stream.Write(ms38.GetBuffer(), 0, len21);
		}
		if (instance.GotoGroup != null)
		{
			stream.WriteKey(57, 2);
			CitoMemoryStream ms39 = new CitoMemoryStream(subBuffer);
			Packet_GotoGroupSerializer.Serialize(ms39, instance.GotoGroup);
			int len20 = ms39.Position();
			ProtocolParser.WriteUInt32_(stream, len20);
			stream.Write(ms39.GetBuffer(), 0, len20);
		}
		if (instance.ExchangeBlock != null)
		{
			stream.WriteKey(58, 2);
			CitoMemoryStream ms40 = new CitoMemoryStream(subBuffer);
			Packet_ServerExchangeBlockSerializer.Serialize(ms40, instance.ExchangeBlock);
			int len19 = ms40.Position();
			ProtocolParser.WriteUInt32_(stream, len19);
			stream.Write(ms40.GetBuffer(), 0, len19);
		}
		if (instance.BulkEntityAttributes != null)
		{
			stream.WriteKey(59, 2);
			CitoMemoryStream ms41 = new CitoMemoryStream(subBuffer);
			Packet_BulkEntityAttributesSerializer.Serialize(ms41, instance.BulkEntityAttributes);
			int len18 = ms41.Position();
			ProtocolParser.WriteUInt32_(stream, len18);
			stream.Write(ms41.GetBuffer(), 0, len18);
		}
		if (instance.SpawnParticles != null)
		{
			stream.WriteKey(60, 2);
			CitoMemoryStream ms42 = new CitoMemoryStream(subBuffer);
			Packet_SpawnParticlesSerializer.Serialize(ms42, instance.SpawnParticles);
			int len17 = ms42.Position();
			ProtocolParser.WriteUInt32_(stream, len17);
			stream.Write(ms42.GetBuffer(), 0, len17);
		}
		if (instance.BulkEntityDebugAttributes != null)
		{
			stream.WriteKey(61, 2);
			CitoMemoryStream ms43 = new CitoMemoryStream(subBuffer);
			Packet_BulkEntityDebugAttributesSerializer.Serialize(ms43, instance.BulkEntityDebugAttributes);
			int len16 = ms43.Position();
			ProtocolParser.WriteUInt32_(stream, len16);
			stream.Write(ms43.GetBuffer(), 0, len16);
		}
		if (instance.SetBlocksNoRelight != null)
		{
			stream.WriteKey(62, 2);
			CitoMemoryStream ms44 = new CitoMemoryStream(subBuffer);
			Packet_ServerSetBlocksSerializer.Serialize(ms44, instance.SetBlocksNoRelight);
			int len15 = ms44.Position();
			ProtocolParser.WriteUInt32_(stream, len15);
			stream.Write(ms44.GetBuffer(), 0, len15);
		}
		if (instance.BlockDamage != null)
		{
			stream.WriteKey(64, 2);
			CitoMemoryStream ms45 = new CitoMemoryStream(subBuffer);
			Packet_BlockDamageSerializer.Serialize(ms45, instance.BlockDamage);
			int len14 = ms45.Position();
			ProtocolParser.WriteUInt32_(stream, len14);
			stream.Write(ms45.GetBuffer(), 0, len14);
		}
		if (instance.Ambient != null)
		{
			stream.WriteKey(65, 2);
			CitoMemoryStream ms46 = new CitoMemoryStream(subBuffer);
			Packet_AmbientSerializer.Serialize(ms46, instance.Ambient);
			int len13 = ms46.Position();
			ProtocolParser.WriteUInt32_(stream, len13);
			stream.Write(ms46.GetBuffer(), 0, len13);
		}
		if (instance.NotifySlot != null)
		{
			stream.WriteKey(66, 2);
			CitoMemoryStream ms47 = new CitoMemoryStream(subBuffer);
			Packet_NotifySlotSerializer.Serialize(ms47, instance.NotifySlot);
			int len12 = ms47.Position();
			ProtocolParser.WriteUInt32_(stream, len12);
			stream.Write(ms47.GetBuffer(), 0, len12);
		}
		if (instance.IngameError != null)
		{
			stream.WriteKey(68, 2);
			CitoMemoryStream ms49 = new CitoMemoryStream(subBuffer);
			Packet_IngameErrorSerializer.Serialize(ms49, instance.IngameError);
			int len11 = ms49.Position();
			ProtocolParser.WriteUInt32_(stream, len11);
			stream.Write(ms49.GetBuffer(), 0, len11);
		}
		if (instance.IngameDiscovery != null)
		{
			stream.WriteKey(69, 2);
			CitoMemoryStream ms50 = new CitoMemoryStream(subBuffer);
			Packet_IngameDiscoverySerializer.Serialize(ms50, instance.IngameDiscovery);
			int len10 = ms50.Position();
			ProtocolParser.WriteUInt32_(stream, len10);
			stream.Write(ms50.GetBuffer(), 0, len10);
		}
		if (instance.SetBlocksMinimal != null)
		{
			stream.WriteKey(70, 2);
			CitoMemoryStream ms52 = new CitoMemoryStream(subBuffer);
			Packet_ServerSetBlocksSerializer.Serialize(ms52, instance.SetBlocksMinimal);
			int len9 = ms52.Position();
			ProtocolParser.WriteUInt32_(stream, len9);
			stream.Write(ms52.GetBuffer(), 0, len9);
		}
		if (instance.SetDecors != null)
		{
			stream.WriteKey(71, 2);
			CitoMemoryStream ms53 = new CitoMemoryStream(subBuffer);
			Packet_ServerSetDecorsSerializer.Serialize(ms53, instance.SetDecors);
			int len8 = ms53.Position();
			ProtocolParser.WriteUInt32_(stream, len8);
			stream.Write(ms53.GetBuffer(), 0, len8);
		}
		if (instance.RemoveBlockLight != null)
		{
			stream.WriteKey(72, 2);
			CitoMemoryStream ms54 = new CitoMemoryStream(subBuffer);
			Packet_RemoveBlockLightSerializer.Serialize(ms54, instance.RemoveBlockLight);
			int len7 = ms54.Position();
			ProtocolParser.WriteUInt32_(stream, len7);
			stream.Write(ms54.GetBuffer(), 0, len7);
		}
		if (instance.ServerReady != null)
		{
			stream.WriteKey(73, 2);
			CitoMemoryStream ms55 = new CitoMemoryStream(subBuffer);
			Packet_ServerReadySerializer.Serialize(ms55, instance.ServerReady);
			int len6 = ms55.Position();
			ProtocolParser.WriteUInt32_(stream, len6);
			stream.Write(ms55.GetBuffer(), 0, len6);
		}
		if (instance.UnloadMapRegion != null)
		{
			stream.WriteKey(74, 2);
			CitoMemoryStream ms56 = new CitoMemoryStream(subBuffer);
			Packet_UnloadMapRegionSerializer.Serialize(ms56, instance.UnloadMapRegion);
			int len5 = ms56.Position();
			ProtocolParser.WriteUInt32_(stream, len5);
			stream.Write(ms56.GetBuffer(), 0, len5);
		}
		if (instance.LandClaims != null)
		{
			stream.WriteKey(75, 2);
			CitoMemoryStream ms57 = new CitoMemoryStream(subBuffer);
			Packet_LandClaimsSerializer.Serialize(ms57, instance.LandClaims);
			int len4 = ms57.Position();
			ProtocolParser.WriteUInt32_(stream, len4);
			stream.Write(ms57.GetBuffer(), 0, len4);
		}
		if (instance.Roles != null)
		{
			stream.WriteKey(76, 2);
			CitoMemoryStream ms58 = new CitoMemoryStream(subBuffer);
			Packet_RolesSerializer.Serialize(ms58, instance.Roles);
			int len3 = ms58.Position();
			ProtocolParser.WriteUInt32_(stream, len3);
			stream.Write(ms58.GetBuffer(), 0, len3);
		}
		if (instance.UdpPacket != null)
		{
			stream.WriteKey(78, 2);
			CitoMemoryStream ms60 = new CitoMemoryStream(subBuffer);
			Packet_UdpPacketSerializer.Serialize(ms60, instance.UdpPacket);
			int len2 = ms60.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms60.GetBuffer(), 0, len2);
		}
		if (instance.QueuePacket != null)
		{
			stream.WriteKey(79, 2);
			CitoMemoryStream ms61 = new CitoMemoryStream(subBuffer);
			Packet_QueuePacketSerializer.Serialize(ms61, instance.QueuePacket);
			int len = ms61.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms61.GetBuffer(), 0, len);
		}
	}

	public static byte[] SerializeToBytes(Packet_Server instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Server instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
