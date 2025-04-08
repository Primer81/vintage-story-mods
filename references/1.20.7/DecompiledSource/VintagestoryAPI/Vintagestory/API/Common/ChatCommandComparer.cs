using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class ChatCommandComparer : IEqualityComparer<IChatCommand>
{
	private static ChatCommandComparer _instance;

	public static ChatCommandComparer Comparer
	{
		get
		{
			if (_instance == null)
			{
				_instance = new ChatCommandComparer();
			}
			return _instance;
		}
	}

	public bool Equals(IChatCommand x, IChatCommand y)
	{
		if (y != null && x != null)
		{
			return x.GetHashCode() == y.GetHashCode();
		}
		return false;
	}

	public int GetHashCode(IChatCommand obj)
	{
		return obj.GetHashCode();
	}
}
