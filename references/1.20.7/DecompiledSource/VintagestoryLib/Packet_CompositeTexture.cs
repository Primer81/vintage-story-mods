public class Packet_CompositeTexture
{
	public string Base;

	public Packet_BlendedOverlayTexture[] Overlays;

	public int OverlaysCount;

	public int OverlaysLength;

	public Packet_CompositeTexture[] Alternates;

	public int AlternatesCount;

	public int AlternatesLength;

	public int Rotation;

	public int Alpha;

	public Packet_CompositeTexture[] Tiles;

	public int TilesCount;

	public int TilesLength;

	public int TilesWidth;

	public const int BaseFieldID = 1;

	public const int OverlaysFieldID = 2;

	public const int AlternatesFieldID = 3;

	public const int RotationFieldID = 4;

	public const int AlphaFieldID = 5;

	public const int TilesFieldID = 6;

	public const int TilesWidthFieldID = 7;

	public void SetBase(string value)
	{
		Base = value;
	}

	public Packet_BlendedOverlayTexture[] GetOverlays()
	{
		return Overlays;
	}

	public void SetOverlays(Packet_BlendedOverlayTexture[] value, int count, int length)
	{
		Overlays = value;
		OverlaysCount = count;
		OverlaysLength = length;
	}

	public void SetOverlays(Packet_BlendedOverlayTexture[] value)
	{
		Overlays = value;
		OverlaysCount = value.Length;
		OverlaysLength = value.Length;
	}

	public int GetOverlaysCount()
	{
		return OverlaysCount;
	}

	public void OverlaysAdd(Packet_BlendedOverlayTexture value)
	{
		if (OverlaysCount >= OverlaysLength)
		{
			if ((OverlaysLength *= 2) == 0)
			{
				OverlaysLength = 1;
			}
			Packet_BlendedOverlayTexture[] newArray = new Packet_BlendedOverlayTexture[OverlaysLength];
			for (int i = 0; i < OverlaysCount; i++)
			{
				newArray[i] = Overlays[i];
			}
			Overlays = newArray;
		}
		Overlays[OverlaysCount++] = value;
	}

	public Packet_CompositeTexture[] GetAlternates()
	{
		return Alternates;
	}

	public void SetAlternates(Packet_CompositeTexture[] value, int count, int length)
	{
		Alternates = value;
		AlternatesCount = count;
		AlternatesLength = length;
	}

	public void SetAlternates(Packet_CompositeTexture[] value)
	{
		Alternates = value;
		AlternatesCount = value.Length;
		AlternatesLength = value.Length;
	}

	public int GetAlternatesCount()
	{
		return AlternatesCount;
	}

	public void AlternatesAdd(Packet_CompositeTexture value)
	{
		if (AlternatesCount >= AlternatesLength)
		{
			if ((AlternatesLength *= 2) == 0)
			{
				AlternatesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[AlternatesLength];
			for (int i = 0; i < AlternatesCount; i++)
			{
				newArray[i] = Alternates[i];
			}
			Alternates = newArray;
		}
		Alternates[AlternatesCount++] = value;
	}

	public void SetRotation(int value)
	{
		Rotation = value;
	}

	public void SetAlpha(int value)
	{
		Alpha = value;
	}

	public Packet_CompositeTexture[] GetTiles()
	{
		return Tiles;
	}

	public void SetTiles(Packet_CompositeTexture[] value, int count, int length)
	{
		Tiles = value;
		TilesCount = count;
		TilesLength = length;
	}

	public void SetTiles(Packet_CompositeTexture[] value)
	{
		Tiles = value;
		TilesCount = value.Length;
		TilesLength = value.Length;
	}

	public int GetTilesCount()
	{
		return TilesCount;
	}

	public void TilesAdd(Packet_CompositeTexture value)
	{
		if (TilesCount >= TilesLength)
		{
			if ((TilesLength *= 2) == 0)
			{
				TilesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[TilesLength];
			for (int i = 0; i < TilesCount; i++)
			{
				newArray[i] = Tiles[i];
			}
			Tiles = newArray;
		}
		Tiles[TilesCount++] = value;
	}

	public void SetTilesWidth(int value)
	{
		TilesWidth = value;
	}

	internal void InitializeValues()
	{
	}
}
