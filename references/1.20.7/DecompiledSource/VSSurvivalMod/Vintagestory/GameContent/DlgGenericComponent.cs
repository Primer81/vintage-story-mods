namespace Vintagestory.GameContent;

public class DlgGenericComponent : DialogueComponent
{
	public override string Execute()
	{
		setVars();
		if (JumpTo == null)
		{
			return "next";
		}
		return JumpTo;
	}
}
