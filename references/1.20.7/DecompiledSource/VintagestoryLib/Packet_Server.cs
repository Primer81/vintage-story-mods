public class Packet_Server : IPacket
{
	public int Id;

	public Packet_LoginTokenAnswer Token;

	public Packet_ServerIdentification Identification;

	public Packet_ServerLevelInitialize LevelInitialize;

	public Packet_ServerLevelProgress LevelDataChunk;

	public Packet_ServerLevelFinalize LevelFinalize;

	public Packet_ServerSetBlock SetBlock;

	public Packet_ChatLine Chatline;

	public Packet_ServerDisconnectPlayer DisconnectPlayer;

	public Packet_ServerChunks Chunks;

	public Packet_UnloadServerChunk UnloadChunk;

	public Packet_ServerCalendar Calendar;

	public Packet_ServerMapChunk MapChunk;

	public Packet_ServerPing Ping;

	public Packet_ServerPlayerPing PlayerPing;

	public Packet_ServerSound Sound;

	public Packet_ServerAssets Assets;

	public Packet_WorldMetaData WorldMetaData;

	public Packet_ServerQueryAnswer QueryAnswer;

	public Packet_ServerRedirect Redirect;

	public Packet_InventoryContents InventoryContents;

	public Packet_InventoryUpdate InventoryUpdate;

	public Packet_InventoryDoubleUpdate InventoryDoubleUpdate;

	public Packet_Entity Entity;

	public Packet_EntitySpawn EntitySpawn;

	public Packet_EntityDespawn EntityDespawn;

	public Packet_EntityAttributes EntityAttributes;

	public Packet_EntityAttributeUpdate EntityAttributeUpdate;

	public Packet_EntityPacket EntityPacket;

	public Packet_Entities Entities;

	public Packet_PlayerData PlayerData;

	public Packet_MapRegion MapRegion;

	public Packet_BlockEntityMessage BlockEntityMessage;

	public Packet_PlayerDeath PlayerDeath;

	public Packet_PlayerMode ModeChange;

	public Packet_ServerSetBlocks SetBlocks;

	public Packet_BlockEntities BlockEntities;

	public Packet_PlayerGroups PlayerGroups;

	public Packet_PlayerGroup PlayerGroup;

	public Packet_EntityPosition EntityPosition;

	public Packet_HighlightBlocks HighlightBlocks;

	public Packet_SelectedHotbarSlot SelectedHotbarSlot;

	public Packet_CustomPacket CustomPacket;

	public Packet_NetworkChannels NetworkChannels;

	public Packet_GotoGroup GotoGroup;

	public Packet_ServerExchangeBlock ExchangeBlock;

	public Packet_BulkEntityAttributes BulkEntityAttributes;

	public Packet_SpawnParticles SpawnParticles;

	public Packet_BulkEntityDebugAttributes BulkEntityDebugAttributes;

	public Packet_ServerSetBlocks SetBlocksNoRelight;

	public Packet_BlockDamage BlockDamage;

	public Packet_Ambient Ambient;

	public Packet_NotifySlot NotifySlot;

	public Packet_IngameError IngameError;

	public Packet_IngameDiscovery IngameDiscovery;

	public Packet_ServerSetBlocks SetBlocksMinimal;

	public Packet_ServerSetDecors SetDecors;

	public Packet_RemoveBlockLight RemoveBlockLight;

	public Packet_ServerReady ServerReady;

	public Packet_UnloadMapRegion UnloadMapRegion;

	public Packet_LandClaims LandClaims;

	public Packet_Roles Roles;

	public Packet_UdpPacket UdpPacket;

	public Packet_QueuePacket QueuePacket;

	public const int IdFieldID = 90;

	public const int TokenFieldID = 77;

	public const int IdentificationFieldID = 1;

	public const int LevelInitializeFieldID = 2;

	public const int LevelDataChunkFieldID = 3;

	public const int LevelFinalizeFieldID = 4;

	public const int SetBlockFieldID = 5;

	public const int ChatlineFieldID = 7;

	public const int DisconnectPlayerFieldID = 8;

	public const int ChunksFieldID = 9;

	public const int UnloadChunkFieldID = 10;

	public const int CalendarFieldID = 11;

	public const int MapChunkFieldID = 15;

	public const int PingFieldID = 16;

	public const int PlayerPingFieldID = 17;

	public const int SoundFieldID = 18;

	public const int AssetsFieldID = 19;

	public const int WorldMetaDataFieldID = 21;

	public const int QueryAnswerFieldID = 28;

	public const int RedirectFieldID = 29;

	public const int InventoryContentsFieldID = 30;

	public const int InventoryUpdateFieldID = 31;

	public const int InventoryDoubleUpdateFieldID = 32;

	public const int EntityFieldID = 34;

	public const int EntitySpawnFieldID = 35;

	public const int EntityDespawnFieldID = 36;

	public const int EntityAttributesFieldID = 38;

	public const int EntityAttributeUpdateFieldID = 39;

	public const int EntityPacketFieldID = 67;

	public const int EntitiesFieldID = 40;

	public const int PlayerDataFieldID = 41;

	public const int MapRegionFieldID = 42;

	public const int BlockEntityMessageFieldID = 44;

	public const int PlayerDeathFieldID = 45;

	public const int ModeChangeFieldID = 46;

	public const int SetBlocksFieldID = 47;

	public const int BlockEntitiesFieldID = 48;

	public const int PlayerGroupsFieldID = 49;

	public const int PlayerGroupFieldID = 50;

	public const int EntityPositionFieldID = 51;

	public const int HighlightBlocksFieldID = 52;

	public const int SelectedHotbarSlotFieldID = 53;

	public const int CustomPacketFieldID = 55;

	public const int NetworkChannelsFieldID = 56;

	public const int GotoGroupFieldID = 57;

	public const int ExchangeBlockFieldID = 58;

	public const int BulkEntityAttributesFieldID = 59;

	public const int SpawnParticlesFieldID = 60;

	public const int BulkEntityDebugAttributesFieldID = 61;

	public const int SetBlocksNoRelightFieldID = 62;

	public const int BlockDamageFieldID = 64;

	public const int AmbientFieldID = 65;

	public const int NotifySlotFieldID = 66;

	public const int IngameErrorFieldID = 68;

	public const int IngameDiscoveryFieldID = 69;

	public const int SetBlocksMinimalFieldID = 70;

	public const int SetDecorsFieldID = 71;

	public const int RemoveBlockLightFieldID = 72;

	public const int ServerReadyFieldID = 73;

	public const int UnloadMapRegionFieldID = 74;

	public const int LandClaimsFieldID = 75;

	public const int RolesFieldID = 76;

	public const int UdpPacketFieldID = 78;

	public const int QueuePacketFieldID = 79;

	public void SerializeTo(CitoStream stream)
	{
		Packet_ServerSerializer.Serialize(stream, this);
	}

	public void SetId(int value)
	{
		Id = value;
	}

	public void SetToken(Packet_LoginTokenAnswer value)
	{
		Token = value;
	}

	public void SetIdentification(Packet_ServerIdentification value)
	{
		Identification = value;
	}

	public void SetLevelInitialize(Packet_ServerLevelInitialize value)
	{
		LevelInitialize = value;
	}

	public void SetLevelDataChunk(Packet_ServerLevelProgress value)
	{
		LevelDataChunk = value;
	}

	public void SetLevelFinalize(Packet_ServerLevelFinalize value)
	{
		LevelFinalize = value;
	}

	public void SetSetBlock(Packet_ServerSetBlock value)
	{
		SetBlock = value;
	}

	public void SetChatline(Packet_ChatLine value)
	{
		Chatline = value;
	}

	public void SetDisconnectPlayer(Packet_ServerDisconnectPlayer value)
	{
		DisconnectPlayer = value;
	}

	public void SetChunks(Packet_ServerChunks value)
	{
		Chunks = value;
	}

	public void SetUnloadChunk(Packet_UnloadServerChunk value)
	{
		UnloadChunk = value;
	}

	public void SetCalendar(Packet_ServerCalendar value)
	{
		Calendar = value;
	}

	public void SetMapChunk(Packet_ServerMapChunk value)
	{
		MapChunk = value;
	}

	public void SetPing(Packet_ServerPing value)
	{
		Ping = value;
	}

	public void SetPlayerPing(Packet_ServerPlayerPing value)
	{
		PlayerPing = value;
	}

	public void SetSound(Packet_ServerSound value)
	{
		Sound = value;
	}

	public void SetAssets(Packet_ServerAssets value)
	{
		Assets = value;
	}

	public void SetWorldMetaData(Packet_WorldMetaData value)
	{
		WorldMetaData = value;
	}

	public void SetQueryAnswer(Packet_ServerQueryAnswer value)
	{
		QueryAnswer = value;
	}

	public void SetRedirect(Packet_ServerRedirect value)
	{
		Redirect = value;
	}

	public void SetInventoryContents(Packet_InventoryContents value)
	{
		InventoryContents = value;
	}

	public void SetInventoryUpdate(Packet_InventoryUpdate value)
	{
		InventoryUpdate = value;
	}

	public void SetInventoryDoubleUpdate(Packet_InventoryDoubleUpdate value)
	{
		InventoryDoubleUpdate = value;
	}

	public void SetEntity(Packet_Entity value)
	{
		Entity = value;
	}

	public void SetEntitySpawn(Packet_EntitySpawn value)
	{
		EntitySpawn = value;
	}

	public void SetEntityDespawn(Packet_EntityDespawn value)
	{
		EntityDespawn = value;
	}

	public void SetEntityAttributes(Packet_EntityAttributes value)
	{
		EntityAttributes = value;
	}

	public void SetEntityAttributeUpdate(Packet_EntityAttributeUpdate value)
	{
		EntityAttributeUpdate = value;
	}

	public void SetEntityPacket(Packet_EntityPacket value)
	{
		EntityPacket = value;
	}

	public void SetEntities(Packet_Entities value)
	{
		Entities = value;
	}

	public void SetPlayerData(Packet_PlayerData value)
	{
		PlayerData = value;
	}

	public void SetMapRegion(Packet_MapRegion value)
	{
		MapRegion = value;
	}

	public void SetBlockEntityMessage(Packet_BlockEntityMessage value)
	{
		BlockEntityMessage = value;
	}

	public void SetPlayerDeath(Packet_PlayerDeath value)
	{
		PlayerDeath = value;
	}

	public void SetModeChange(Packet_PlayerMode value)
	{
		ModeChange = value;
	}

	public void SetSetBlocks(Packet_ServerSetBlocks value)
	{
		SetBlocks = value;
	}

	public void SetBlockEntities(Packet_BlockEntities value)
	{
		BlockEntities = value;
	}

	public void SetPlayerGroups(Packet_PlayerGroups value)
	{
		PlayerGroups = value;
	}

	public void SetPlayerGroup(Packet_PlayerGroup value)
	{
		PlayerGroup = value;
	}

	public void SetEntityPosition(Packet_EntityPosition value)
	{
		EntityPosition = value;
	}

	public void SetHighlightBlocks(Packet_HighlightBlocks value)
	{
		HighlightBlocks = value;
	}

	public void SetSelectedHotbarSlot(Packet_SelectedHotbarSlot value)
	{
		SelectedHotbarSlot = value;
	}

	public void SetCustomPacket(Packet_CustomPacket value)
	{
		CustomPacket = value;
	}

	public void SetNetworkChannels(Packet_NetworkChannels value)
	{
		NetworkChannels = value;
	}

	public void SetGotoGroup(Packet_GotoGroup value)
	{
		GotoGroup = value;
	}

	public void SetExchangeBlock(Packet_ServerExchangeBlock value)
	{
		ExchangeBlock = value;
	}

	public void SetBulkEntityAttributes(Packet_BulkEntityAttributes value)
	{
		BulkEntityAttributes = value;
	}

	public void SetSpawnParticles(Packet_SpawnParticles value)
	{
		SpawnParticles = value;
	}

	public void SetBulkEntityDebugAttributes(Packet_BulkEntityDebugAttributes value)
	{
		BulkEntityDebugAttributes = value;
	}

	public void SetSetBlocksNoRelight(Packet_ServerSetBlocks value)
	{
		SetBlocksNoRelight = value;
	}

	public void SetBlockDamage(Packet_BlockDamage value)
	{
		BlockDamage = value;
	}

	public void SetAmbient(Packet_Ambient value)
	{
		Ambient = value;
	}

	public void SetNotifySlot(Packet_NotifySlot value)
	{
		NotifySlot = value;
	}

	public void SetIngameError(Packet_IngameError value)
	{
		IngameError = value;
	}

	public void SetIngameDiscovery(Packet_IngameDiscovery value)
	{
		IngameDiscovery = value;
	}

	public void SetSetBlocksMinimal(Packet_ServerSetBlocks value)
	{
		SetBlocksMinimal = value;
	}

	public void SetSetDecors(Packet_ServerSetDecors value)
	{
		SetDecors = value;
	}

	public void SetRemoveBlockLight(Packet_RemoveBlockLight value)
	{
		RemoveBlockLight = value;
	}

	public void SetServerReady(Packet_ServerReady value)
	{
		ServerReady = value;
	}

	public void SetUnloadMapRegion(Packet_UnloadMapRegion value)
	{
		UnloadMapRegion = value;
	}

	public void SetLandClaims(Packet_LandClaims value)
	{
		LandClaims = value;
	}

	public void SetRoles(Packet_Roles value)
	{
		Roles = value;
	}

	public void SetUdpPacket(Packet_UdpPacket value)
	{
		UdpPacket = value;
	}

	public void SetQueuePacket(Packet_QueuePacket value)
	{
		QueuePacket = value;
	}

	internal void InitializeValues()
	{
		Id = 1;
	}
}
