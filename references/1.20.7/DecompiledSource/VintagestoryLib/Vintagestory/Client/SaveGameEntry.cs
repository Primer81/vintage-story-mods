using System;

namespace Vintagestory.Client;

internal class SaveGameEntry
{
	public DateTime Modificationdate;

	public string Filename;

	public SaveGame Savegame;

	public int DatabaseVersion;

	public bool IsReadOnly;
}
