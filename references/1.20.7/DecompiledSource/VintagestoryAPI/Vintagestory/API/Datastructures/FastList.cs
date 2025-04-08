using System.Collections;

namespace Vintagestory.API.Datastructures;

/// <summary>
/// Does not clear elements in Clear(), but only sets the Count to 0
/// </summary>
/// <typeparam name="TElem"></typeparam>
public class FastList<TElem> : IEnumerable
{
	private TElem[] elements = new TElem[0];

	private int count;

	public int Count => count;

	public TElem this[int index]
	{
		get
		{
			return elements[index];
		}
		set
		{
			elements[index] = value;
		}
	}

	public void Add(TElem elem)
	{
		if (count >= elements.Length)
		{
			TElem[] newelements = new TElem[count + 50];
			for (int i = 0; i < elements.Length; i++)
			{
				newelements[i] = elements[i];
			}
			elements = newelements;
		}
		elements[count] = elem;
		count++;
	}

	public void Clear()
	{
		count = 0;
	}

	public void RemoveAt(int index)
	{
		count--;
		if (index != count - 1)
		{
			for (int i = index; i < count; i++)
			{
				elements[i] = elements[i + 1];
			}
		}
	}

	public IEnumerator GetEnumerator()
	{
		return new FastListEnum<TElem>(this);
	}

	public bool Contains(TElem needle)
	{
		for (int i = 0; i < elements.Length; i++)
		{
			if (i >= Count)
			{
				return false;
			}
			ref TElem reference = ref needle;
			TElem val = default(TElem);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			if (reference.Equals(elements[i]))
			{
				return true;
			}
		}
		return false;
	}
}
