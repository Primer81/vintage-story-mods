using System.Collections.Generic;

namespace Vintagestory.API.Net;

public class GameBuild
{
	public string filename;

	public string filesize;

	public string md5;

	public Dictionary<string, string> urls;

	public bool latest;
}
