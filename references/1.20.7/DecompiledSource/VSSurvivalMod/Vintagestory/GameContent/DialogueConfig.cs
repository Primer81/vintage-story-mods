namespace Vintagestory.GameContent;

public class DialogueConfig
{
	public DialogueComponent[] components;

	private int uniqueIdCounter;

	public void Init()
	{
		DialogueComponent[] array = components;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init(ref uniqueIdCounter);
		}
	}
}
