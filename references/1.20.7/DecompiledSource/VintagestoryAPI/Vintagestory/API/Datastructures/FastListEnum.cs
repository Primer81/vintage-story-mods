using System.Collections;

namespace Vintagestory.API.Datastructures;

public class FastListEnum<TElem> : IEnumerator
{
	private int pos = -1;

	private FastList<TElem> list;

	public object Current => list[pos];

	public FastListEnum(FastList<TElem> list)
	{
		this.list = list;
	}

	public bool MoveNext()
	{
		pos++;
		if (pos >= list.Count)
		{
			return false;
		}
		return true;
	}

	public void Reset()
	{
		pos = -1;
	}
}
