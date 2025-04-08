namespace Vintagestory.Client.NoObf;

public class GameServerStatus : ServerCtrlResponse
{
	public string ConnectionString;

	public string Identifier;

	public string Version;

	public string Password;

	public float ActiveserverDays;

	public int QuantitySavegames;

	public EnumDownloadSavesStatus DownloadState;

	public string Downloadsavefilename;

	public string[] Regions;
}
