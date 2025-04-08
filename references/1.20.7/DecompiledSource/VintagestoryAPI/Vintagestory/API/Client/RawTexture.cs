namespace Vintagestory.API.Client;

public class RawTexture
{
	public EnumTextureFilter MinFilter = EnumTextureFilter.Linear;

	public EnumTextureFilter MagFilter = EnumTextureFilter.Linear;

	public EnumTextureWrap WrapS = EnumTextureWrap.ClampToEdge;

	public EnumTextureWrap WrapT = EnumTextureWrap.ClampToEdge;

	public EnumTextureInternalFormat PixelInternalFormat = EnumTextureInternalFormat.Rgba8;

	public EnumTexturePixelFormat PixelFormat = EnumTexturePixelFormat.Rgba;

	public int Width;

	public int Height;

	public int TextureId;
}
