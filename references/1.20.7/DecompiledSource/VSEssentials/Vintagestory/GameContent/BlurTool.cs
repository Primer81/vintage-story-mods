namespace Vintagestory.GameContent;

public class BlurTool
{
	public static void Blur(byte[] data, int sizeX, int sizeZ, int range)
	{
		BoxBlurHorizontal(data, range, 0, 0, sizeX, sizeZ);
		BoxBlurVertical(data, range, 0, 0, sizeX, sizeZ);
	}

	public unsafe static void BoxBlurHorizontal(byte[] map, int range, int xStart, int yStart, int xEnd, int yEnd)
	{
		fixed (byte* pixels = map)
		{
			int w = xEnd - xStart;
			int halfRange = range / 2;
			int index = yStart * w;
			byte[] newColors = new byte[w];
			for (int y = yStart; y < yEnd; y++)
			{
				int hits = 0;
				int r = 0;
				for (int x2 = xStart - halfRange; x2 < xEnd; x2++)
				{
					int oldPixel = x2 - halfRange - 1;
					if (oldPixel >= xStart)
					{
						byte col2 = pixels[index + oldPixel];
						if (col2 != 0)
						{
							r -= col2;
						}
						hits--;
					}
					int newPixel = x2 + halfRange;
					if (newPixel < xEnd)
					{
						byte col = pixels[index + newPixel];
						if (col != 0)
						{
							r += col;
						}
						hits++;
					}
					if (x2 >= xStart)
					{
						byte color = (byte)(r / hits);
						newColors[x2] = color;
					}
				}
				for (int x = xStart; x < xEnd; x++)
				{
					pixels[index + x] = newColors[x];
				}
				index += w;
			}
		}
	}

	public unsafe static void BoxBlurVertical(byte[] map, int range, int xStart, int yStart, int xEnd, int yEnd)
	{
		fixed (byte* pixels = map)
		{
			int w = xEnd - xStart;
			int num = yEnd - yStart;
			int halfRange = range / 2;
			byte[] newColors = new byte[num];
			int oldPixelOffset = -(halfRange + 1) * w;
			int newPixelOffset = halfRange * w;
			for (int x = xStart; x < xEnd; x++)
			{
				int hits = 0;
				int r = 0;
				int index = yStart * w - halfRange * w + x;
				for (int y2 = yStart - halfRange; y2 < yEnd; y2++)
				{
					if (y2 - halfRange - 1 >= yStart)
					{
						byte col2 = pixels[index + oldPixelOffset];
						if (col2 != 0)
						{
							r -= col2;
						}
						hits--;
					}
					if (y2 + halfRange < yEnd)
					{
						byte col = pixels[index + newPixelOffset];
						if (col != 0)
						{
							r += col;
						}
						hits++;
					}
					if (y2 >= yStart)
					{
						byte color = (byte)(r / hits);
						newColors[y2] = color;
					}
					index += w;
				}
				for (int y = yStart; y < yEnd; y++)
				{
					pixels[y * w + x] = newColors[y];
				}
			}
		}
	}
}
