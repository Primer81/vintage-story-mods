using System;
using System.Runtime.InteropServices;

namespace Vintagestory;

public class WindowsConsole
{
	private enum StandardHandle : uint
	{
		Input = 4294967286u,
		Output = 4294967285u,
		Error = 4294967284u
	}

	private enum FileType : uint
	{
		Unknown,
		Disk,
		Char,
		Pipe
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AttachConsole(int dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint GetStdHandle(StandardHandle nStdHandle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetStdHandle(StandardHandle nStdHandle, nint handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern FileType GetFileType(nint handle);

	private static bool IsRedirected(nint handle)
	{
		FileType fileType = GetFileType(handle);
		if (fileType != FileType.Disk)
		{
			return fileType == FileType.Pipe;
		}
		return true;
	}

	public static void Attach()
	{
		if (IsRedirected(GetStdHandle(StandardHandle.Output)))
		{
			_ = Console.Out;
		}
		bool num = IsRedirected(GetStdHandle(StandardHandle.Error));
		if (num)
		{
			_ = Console.Error;
		}
		AttachConsole(-1);
		if (!num)
		{
			SetStdHandle(StandardHandle.Error, GetStdHandle(StandardHandle.Output));
		}
	}
}
