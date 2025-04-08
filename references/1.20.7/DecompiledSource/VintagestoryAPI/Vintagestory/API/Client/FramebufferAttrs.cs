namespace Vintagestory.API.Client;

public class FramebufferAttrs
{
	public string Name;

	public FramebufferAttrsAttachment[] Attachments;

	public int Width;

	public int Height;

	public FramebufferAttrs(string name, int width, int height)
	{
		Name = name;
		Width = width;
		Height = height;
	}
}
