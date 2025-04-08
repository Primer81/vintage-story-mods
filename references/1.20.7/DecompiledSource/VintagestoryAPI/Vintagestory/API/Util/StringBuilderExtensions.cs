using System.Text;

namespace Vintagestory.API.Util;

public static class StringBuilderExtensions
{
	public static void AppendLineOnce(this StringBuilder sb)
	{
		if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
		{
			sb.AppendLine();
		}
	}
}
