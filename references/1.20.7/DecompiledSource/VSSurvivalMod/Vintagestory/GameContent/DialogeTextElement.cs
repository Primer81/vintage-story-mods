namespace Vintagestory.GameContent;

public class DialogeTextElement
{
	public string Value;

	public string JumpTo;

	public ConditionElement[] Conditions;

	public int Id;

	public ConditionElement Condition
	{
		set
		{
			Conditions = new ConditionElement[1] { value };
		}
	}
}
