namespace Vintagestory.API.Common;

public static class ItemClassMethods
{
	public static string Name(this EnumItemClass s1)
	{
		return s1 switch
		{
			EnumItemClass.Block => "block", 
			EnumItemClass.Item => "item", 
			_ => null, 
		};
	}
}
