using System.Runtime.InteropServices;

public static class ConsoleWindowUtil
{
	private static class NativeFunctions
	{
		public enum StdHandle
		{
			STD_INPUT_HANDLE = -10,
			STD_OUTPUT_HANDLE = -11,
			STD_ERROR_HANDLE = -12
		}

		public enum ConsoleMode : uint
		{
			ENABLE_ECHO_INPUT = 4u,
			ENABLE_EXTENDED_FLAGS = 128u,
			ENABLE_INSERT_MODE = 32u,
			ENABLE_LINE_INPUT = 2u,
			ENABLE_MOUSE_INPUT = 16u,
			ENABLE_PROCESSED_INPUT = 1u,
			ENABLE_QUICK_EDIT_MODE = 64u,
			ENABLE_WINDOW_INPUT = 8u,
			ENABLE_VIRTUAL_TERMINAL_INPUT = 512u,
			ENABLE_PROCESSED_OUTPUT = 1u,
			ENABLE_WRAP_AT_EOL_OUTPUT = 2u,
			ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4u,
			DISABLE_NEWLINE_AUTO_RETURN = 8u,
			ENABLE_LVB_GRID_WORLDWIDE = 16u
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern nint GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
	}

	public static void QuickEditMode(bool Enable)
	{
		nint stdHandle = NativeFunctions.GetStdHandle(-10);
		NativeFunctions.GetConsoleMode(stdHandle, out var consoleMode);
		consoleMode = ((!Enable) ? (consoleMode & 0xFFFFFFBFu) : (consoleMode | 0x40u));
		consoleMode |= 0x80u;
		NativeFunctions.SetConsoleMode(stdHandle, consoleMode);
	}
}
