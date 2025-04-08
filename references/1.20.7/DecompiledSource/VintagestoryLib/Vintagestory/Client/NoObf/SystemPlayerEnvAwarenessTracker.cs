using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemPlayerEnvAwarenessTracker : ClientSystem
{
	private TrackedPlayerProperties currentProperties = new TrackedPlayerProperties();

	public override string Name => "pltr";

	private TrackedPlayerProperties latestProperties => game.playerProperties;

	public SystemPlayerEnvAwarenessTracker(ClientMain game)
		: base(game)
	{
		game.RegisterGameTickListener(OnGameTick, 20);
		game.RegisterGameTickListener(OnGameTick1s, 1000);
	}

	private void OnGameTick1s(float dt)
	{
		GlobalConstants.CurrentDistanceToRainfallClient = game.blockAccessor.GetDistanceToRainFall(game.EntityPlayer.Pos.AsBlockPos, 12, 4);
	}

	public override void OnOwnPlayerDataReceived()
	{
		base.OnOwnPlayerDataReceived();
		BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
		currentProperties.PlayerChunkPos.X = pos.X / game.WorldMap.ClientChunkSize;
		currentProperties.PlayerChunkPos.Y = pos.InternalY / game.WorldMap.ClientChunkSize;
		currentProperties.PlayerChunkPos.Z = pos.Z / game.WorldMap.ClientChunkSize;
	}

	public void OnGameTick(float dt)
	{
		latestProperties.EyesInWaterColorShift = game.GetEyesInWaterColorShift();
		latestProperties.EyesInWaterDepth = game.EyesInWaterDepth();
		latestProperties.EyesInLavaColorShift = game.GetEyesInLavaColorShift();
		latestProperties.EyesInLavaDepth = game.EyesInLavaDepth();
		latestProperties.DayLight = game.GameWorldCalendar.DayLightStrength;
		latestProperties.MoonLight = game.GameWorldCalendar.MoonLightStrength;
		BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
		latestProperties.PlayerChunkPos.X = pos.X / game.WorldMap.ClientChunkSize;
		latestProperties.PlayerChunkPos.Y = pos.InternalY / game.WorldMap.ClientChunkSize;
		latestProperties.PlayerChunkPos.Z = pos.Z / game.WorldMap.ClientChunkSize;
		latestProperties.PlayerPosDiv8.X = pos.X / 8;
		latestProperties.PlayerPosDiv8.Y = pos.InternalY / 8;
		latestProperties.PlayerPosDiv8.Z = pos.Z / 8;
		latestProperties.FallSpeed = game.EntityPlayer.Pos.Motion.Length();
		latestProperties.DistanceToSpawnPoint = (int)game.EntityPlayer.Pos.DistanceTo(game.player.SpawnPosition.ToVec3d());
		double y = game.EntityPlayer.Pos.Y;
		currentProperties.posY = (latestProperties.posY = (((double)game.SeaLevel < y) ? ((float)(y / (double)game.SeaLevel)) : ((float)((y - (double)game.SeaLevel) / (double)(game.WorldMap.MapSizeY - game.SeaLevel)))));
		currentProperties.sunSlight = (latestProperties.sunSlight = game.WorldMap.RelaxedBlockAccess.GetLightLevel(pos, EnumLightLevelType.OnlySunLight));
		currentProperties.Playstyle = (latestProperties.Playstyle = game.ServerInfo.Playstyle);
		currentProperties.PlayListCode = (latestProperties.PlayListCode = game.ServerInfo.PlayListCode);
		if (Math.Abs(latestProperties.FallSpeed - currentProperties.FallSpeed) > 0.005)
		{
			Trigger(EnumProperty.FallSpeed);
		}
		if (Math.Abs(latestProperties.EyesInWaterDepth - currentProperties.EyesInWaterDepth) > 0.005f || currentProperties.EyesInWaterDepth == 0f)
		{
			Trigger(EnumProperty.EyesInWaterDepth);
		}
		if (latestProperties.EyesInWaterColorShift != currentProperties.EyesInWaterColorShift)
		{
			Trigger(EnumProperty.EyesInWaterColorShift);
		}
		if (Math.Abs(latestProperties.EyesInLavaDepth - currentProperties.EyesInLavaDepth) > 0.005f)
		{
			Trigger(EnumProperty.EyesInLavaDepth);
		}
		if (latestProperties.EyesInLavaColorShift != currentProperties.EyesInLavaColorShift)
		{
			Trigger(EnumProperty.EyesInLavaColorShift);
		}
		if (latestProperties.DayLight != currentProperties.DayLight)
		{
			Trigger(EnumProperty.DayLight);
		}
		if (latestProperties.MoonLight != currentProperties.MoonLight)
		{
			Trigger(EnumProperty.MoonLight);
		}
		if (!latestProperties.PlayerChunkPos.Equals(currentProperties.PlayerChunkPos))
		{
			Trigger(EnumProperty.PlayerChunkPos);
		}
		if (!latestProperties.PlayerPosDiv8.Equals(currentProperties.PlayerPosDiv8))
		{
			Trigger(EnumProperty.PlayerPosDiv8);
		}
		currentProperties.EyesInWaterColorShift = latestProperties.EyesInWaterColorShift;
		currentProperties.EyesInWaterDepth = latestProperties.EyesInWaterDepth;
		currentProperties.EyesInLavaColorShift = latestProperties.EyesInLavaColorShift;
		currentProperties.EyesInLavaDepth = latestProperties.EyesInLavaDepth;
		currentProperties.DayLight = latestProperties.DayLight;
		currentProperties.PlayerChunkPos.Set(latestProperties.PlayerChunkPos);
		currentProperties.PlayerPosDiv8.Set(latestProperties.PlayerPosDiv8);
		currentProperties.FallSpeed = latestProperties.FallSpeed;
	}

	public void Trigger(EnumProperty property)
	{
		List<OnPlayerPropertyChanged> watchers = null;
		game.eventManager?.OnPlayerPropertyChanged.TryGetValue(property, out watchers);
		if (watchers == null)
		{
			return;
		}
		foreach (OnPlayerPropertyChanged item in watchers)
		{
			item(currentProperties, latestProperties);
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
