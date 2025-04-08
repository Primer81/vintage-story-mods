using System;
using System.IO;

namespace Vintagestory.Common;

public class GameOrigin : PathOrigin
{
	public GameOrigin(string assetsPath)
		: this(assetsPath, null)
	{
	}

	public GameOrigin(string assetsPath, string pathForReservedCharsCheck)
		: base("game", assetsPath, pathForReservedCharsCheck)
	{
		domain = "game";
		ReadOnlySpan<char> readOnlySpan = Path.Combine(Path.GetFullPath(assetsPath), "game");
		char reference = Path.DirectorySeparatorChar;
		fullPath = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
	}
}
