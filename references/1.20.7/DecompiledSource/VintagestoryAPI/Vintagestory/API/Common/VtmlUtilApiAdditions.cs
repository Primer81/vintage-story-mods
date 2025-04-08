namespace Vintagestory.API.Common;

public static class VtmlUtilApiAdditions
{
	public static void RegisterVtmlTagConverter(this ICoreAPI api, string tagName, Tag2RichTextDelegate converterHandler)
	{
		VtmlUtil.TagConverters[tagName] = converterHandler;
	}
}
