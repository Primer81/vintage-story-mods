using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientWorldPlayerData : IWorldPlayerData
{
	private EnumGameMode gameMode;

	private float moveSpeedMultiplier;

	private float pickingRange;

	private bool freeMove;

	private bool noClip;

	private int deaths;

	private bool areaSelectionMode;

	private EnumFreeMovAxisLock freeMovePlaneLock;

	public int ClientId;

	public string PlayerUID;

	public string PlayerName;

	private int prevViewDistance;

	private EntityPlayer entityplayer;

	public EnumGameMode CurrentGameMode
	{
		get
		{
			return gameMode;
		}
		set
		{
			gameMode = value;
		}
	}

	public bool RenderMetablocks { get; set; }

	public float MoveSpeedMultiplier
	{
		get
		{
			return moveSpeedMultiplier;
		}
		set
		{
			moveSpeedMultiplier = value;
		}
	}

	public float PickingRange
	{
		get
		{
			return pickingRange;
		}
		set
		{
			pickingRange = value;
		}
	}

	public bool FreeMove
	{
		get
		{
			return freeMove;
		}
		set
		{
			freeMove = value;
		}
	}

	public bool NoClip
	{
		get
		{
			return noClip;
		}
		set
		{
			noClip = value;
		}
	}

	public int Deaths
	{
		get
		{
			return deaths;
		}
		set
		{
			deaths = value;
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

	string IWorldPlayerData.PlayerUID => PlayerUID;

	public EntityPlayer EntityPlayer
	{
		get
		{
			return entityplayer;
		}
		set
		{
			entityplayer = value;
		}
	}

	public EntityControls EntityControls => entityplayer.Controls;

	public int LastApprovedViewDistance
	{
		get
		{
			return prevViewDistance;
		}
		set
		{
			prevViewDistance = value;
		}
	}

	public int CurrentClientId => ClientId;

	public bool Connected => true;

	public bool DidSelectSkin
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public int DesiredViewDistance { get; set; }

	private ClientWorldPlayerData()
	{
		DesiredViewDistance = ClientSettings.ViewDistance;
		RenderMetablocks = ClientSettings.RenderMetaBlocks;
	}

	public void RequestNewViewDistance(ClientMain game)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, freeMovePlaneLock, RenderMetablocks);
	}

	public void RequestMode(ClientMain game)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, freeMovePlaneLock, RenderMetablocks);
	}

	public void SetMode(ClientMain game, float moveSpeedMultiplier)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, freeMovePlaneLock, RenderMetablocks);
	}

	public void RequestMode(ClientMain game, bool noClip, bool freeMove)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, freeMovePlaneLock, RenderMetablocks);
	}

	public void RequestMode(ClientMain game, EnumFreeMovAxisLock FreeMovePlaneLock)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, FreeMovePlaneLock, RenderMetablocks);
	}

	public void RequestModeNoClip(ClientMain game, bool noClip)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, freeMovePlaneLock, RenderMetablocks);
	}

	public void RequestModeFreeMove(ClientMain game, bool freeMove)
	{
		RequestMode(game, moveSpeedMultiplier, pickingRange, gameMode, freeMove, noClip, freeMovePlaneLock, RenderMetablocks);
	}

	public void RequestMode(ClientMain game, float moveSpeed, float pickRange, EnumGameMode gameMode, bool freeMove, bool noClip, EnumFreeMovAxisLock freeMovePlaneLock, bool renderMetaBlocks)
	{
		DesiredViewDistance = ClientSettings.ViewDistance;
		Packet_Client packet = new Packet_Client
		{
			Id = 20,
			RequestModeChange = new Packet_PlayerMode
			{
				PlayerUID = PlayerUID,
				GameMode = (int)gameMode,
				FreeMove = (freeMove ? 1 : 0),
				NoClip = (noClip ? 1 : 0),
				MoveSpeed = CollectibleNet.SerializeFloat(moveSpeed),
				PickingRange = CollectibleNet.SerializeFloat(pickRange),
				ViewDistance = ClientSettings.ViewDistance,
				FreeMovePlaneLock = (int)freeMovePlaneLock,
				RenderMetaBlocks = (renderMetaBlocks ? 1 : 0)
			}
		};
		game.SendPacketClient(packet);
	}

	public static ClientWorldPlayerData CreateNew()
	{
		return new ClientWorldPlayerData();
	}

	public ClientWorldPlayerData Clone()
	{
		return new ClientWorldPlayerData
		{
			ClientId = ClientId,
			EntityPlayer = entityplayer,
			freeMove = freeMove,
			freeMovePlaneLock = freeMovePlaneLock,
			gameMode = gameMode,
			moveSpeedMultiplier = moveSpeedMultiplier,
			noClip = noClip,
			pickingRange = pickingRange,
			PlayerUID = PlayerUID,
			areaSelectionMode = areaSelectionMode
		};
	}

	public void UpdateFromPacket(ClientMain game, Packet_PlayerData packet)
	{
		gameMode = (EnumGameMode)packet.GameMode;
		moveSpeedMultiplier = CollectibleNet.DeserializeFloat(packet.MoveSpeed);
		pickingRange = CollectibleNet.DeserializeFloat(packet.PickingRange);
		areaSelectionMode = packet.AreaSelectionMode > 0;
		freeMovePlaneLock = (EnumFreeMovAxisLock)packet.FreeMovePlaneLock;
		freeMove = packet.FreeMove > 0;
		noClip = packet.NoClip > 0;
		deaths = packet.Deaths;
		PlayerUID = packet.PlayerUID;
		PlayerName = packet.PlayerName;
		ClientId = packet.ClientId;
		Entity entity = null;
		game.LoadedEntities.TryGetValue(packet.EntityId, out entity);
		if (entity != null)
		{
			EntityPlayer = (EntityPlayer)entity;
			EntityPlayer.UpdatePartitioning();
		}
	}

	public void UpdateFromPacket(ClientMain game, Packet_PlayerMode mode)
	{
		moveSpeedMultiplier = CollectibleNet.DeserializeFloat(mode.MoveSpeed);
		pickingRange = CollectibleNet.DeserializeFloat(mode.PickingRange);
		gameMode = (EnumGameMode)mode.GameMode;
		freeMove = mode.FreeMove > 0;
		noClip = mode.NoClip > 0;
		freeMovePlaneLock = (EnumFreeMovAxisLock)mode.FreeMovePlaneLock;
		if (ClientId == game.player.ClientId && mode.ViewDistance != ClientSettings.ViewDistance)
		{
			LastApprovedViewDistance = mode.ViewDistance;
			ClientSettings.ViewDistance = mode.ViewDistance;
		}
		game.player.Entity.UpdatePartitioning();
	}

	public void SetModdata(string key, byte[] data)
	{
		throw new NotImplementedException();
	}

	public void RemoveModdata(string key)
	{
		throw new NotImplementedException();
	}

	public byte[] GetModdata(string key)
	{
		throw new NotImplementedException();
	}

	public void SetModData<T>(string key, T data)
	{
		throw new NotImplementedException();
	}

	public T GetModData<T>(string key, T defaultValue = default(T))
	{
		throw new NotImplementedException();
	}
}
