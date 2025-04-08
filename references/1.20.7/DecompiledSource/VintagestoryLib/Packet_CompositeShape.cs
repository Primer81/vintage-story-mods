public class Packet_CompositeShape
{
	public string Base;

	public int Rotatex;

	public int Rotatey;

	public int Rotatez;

	public Packet_CompositeShape[] Alternates;

	public int AlternatesCount;

	public int AlternatesLength;

	public Packet_CompositeShape[] Overlays;

	public int OverlaysCount;

	public int OverlaysLength;

	public int VoxelizeShape;

	public string[] SelectiveElements;

	public int SelectiveElementsCount;

	public int SelectiveElementsLength;

	public string[] IgnoreElements;

	public int IgnoreElementsCount;

	public int IgnoreElementsLength;

	public int QuantityElements;

	public int QuantityElementsSet;

	public int Format;

	public int Offsetx;

	public int Offsety;

	public int Offsetz;

	public bool InsertBakedTextures;

	public int ScaleAdjust;

	public const int BaseFieldID = 1;

	public const int RotatexFieldID = 2;

	public const int RotateyFieldID = 3;

	public const int RotatezFieldID = 4;

	public const int AlternatesFieldID = 5;

	public const int OverlaysFieldID = 11;

	public const int VoxelizeShapeFieldID = 6;

	public const int SelectiveElementsFieldID = 7;

	public const int IgnoreElementsFieldID = 17;

	public const int QuantityElementsFieldID = 8;

	public const int QuantityElementsSetFieldID = 9;

	public const int FormatFieldID = 10;

	public const int OffsetxFieldID = 12;

	public const int OffsetyFieldID = 13;

	public const int OffsetzFieldID = 14;

	public const int InsertBakedTexturesFieldID = 15;

	public const int ScaleAdjustFieldID = 16;

	public void SetBase(string value)
	{
		Base = value;
	}

	public void SetRotatex(int value)
	{
		Rotatex = value;
	}

	public void SetRotatey(int value)
	{
		Rotatey = value;
	}

	public void SetRotatez(int value)
	{
		Rotatez = value;
	}

	public Packet_CompositeShape[] GetAlternates()
	{
		return Alternates;
	}

	public void SetAlternates(Packet_CompositeShape[] value, int count, int length)
	{
		Alternates = value;
		AlternatesCount = count;
		AlternatesLength = length;
	}

	public void SetAlternates(Packet_CompositeShape[] value)
	{
		Alternates = value;
		AlternatesCount = value.Length;
		AlternatesLength = value.Length;
	}

	public int GetAlternatesCount()
	{
		return AlternatesCount;
	}

	public void AlternatesAdd(Packet_CompositeShape value)
	{
		if (AlternatesCount >= AlternatesLength)
		{
			if ((AlternatesLength *= 2) == 0)
			{
				AlternatesLength = 1;
			}
			Packet_CompositeShape[] newArray = new Packet_CompositeShape[AlternatesLength];
			for (int i = 0; i < AlternatesCount; i++)
			{
				newArray[i] = Alternates[i];
			}
			Alternates = newArray;
		}
		Alternates[AlternatesCount++] = value;
	}

	public Packet_CompositeShape[] GetOverlays()
	{
		return Overlays;
	}

	public void SetOverlays(Packet_CompositeShape[] value, int count, int length)
	{
		Overlays = value;
		OverlaysCount = count;
		OverlaysLength = length;
	}

	public void SetOverlays(Packet_CompositeShape[] value)
	{
		Overlays = value;
		OverlaysCount = value.Length;
		OverlaysLength = value.Length;
	}

	public int GetOverlaysCount()
	{
		return OverlaysCount;
	}

	public void OverlaysAdd(Packet_CompositeShape value)
	{
		if (OverlaysCount >= OverlaysLength)
		{
			if ((OverlaysLength *= 2) == 0)
			{
				OverlaysLength = 1;
			}
			Packet_CompositeShape[] newArray = new Packet_CompositeShape[OverlaysLength];
			for (int i = 0; i < OverlaysCount; i++)
			{
				newArray[i] = Overlays[i];
			}
			Overlays = newArray;
		}
		Overlays[OverlaysCount++] = value;
	}

	public void SetVoxelizeShape(int value)
	{
		VoxelizeShape = value;
	}

	public string[] GetSelectiveElements()
	{
		return SelectiveElements;
	}

	public void SetSelectiveElements(string[] value, int count, int length)
	{
		SelectiveElements = value;
		SelectiveElementsCount = count;
		SelectiveElementsLength = length;
	}

	public void SetSelectiveElements(string[] value)
	{
		SelectiveElements = value;
		SelectiveElementsCount = value.Length;
		SelectiveElementsLength = value.Length;
	}

	public int GetSelectiveElementsCount()
	{
		return SelectiveElementsCount;
	}

	public void SelectiveElementsAdd(string value)
	{
		if (SelectiveElementsCount >= SelectiveElementsLength)
		{
			if ((SelectiveElementsLength *= 2) == 0)
			{
				SelectiveElementsLength = 1;
			}
			string[] newArray = new string[SelectiveElementsLength];
			for (int i = 0; i < SelectiveElementsCount; i++)
			{
				newArray[i] = SelectiveElements[i];
			}
			SelectiveElements = newArray;
		}
		SelectiveElements[SelectiveElementsCount++] = value;
	}

	public string[] GetIgnoreElements()
	{
		return IgnoreElements;
	}

	public void SetIgnoreElements(string[] value, int count, int length)
	{
		IgnoreElements = value;
		IgnoreElementsCount = count;
		IgnoreElementsLength = length;
	}

	public void SetIgnoreElements(string[] value)
	{
		IgnoreElements = value;
		IgnoreElementsCount = value.Length;
		IgnoreElementsLength = value.Length;
	}

	public int GetIgnoreElementsCount()
	{
		return IgnoreElementsCount;
	}

	public void IgnoreElementsAdd(string value)
	{
		if (IgnoreElementsCount >= IgnoreElementsLength)
		{
			if ((IgnoreElementsLength *= 2) == 0)
			{
				IgnoreElementsLength = 1;
			}
			string[] newArray = new string[IgnoreElementsLength];
			for (int i = 0; i < IgnoreElementsCount; i++)
			{
				newArray[i] = IgnoreElements[i];
			}
			IgnoreElements = newArray;
		}
		IgnoreElements[IgnoreElementsCount++] = value;
	}

	public void SetQuantityElements(int value)
	{
		QuantityElements = value;
	}

	public void SetQuantityElementsSet(int value)
	{
		QuantityElementsSet = value;
	}

	public void SetFormat(int value)
	{
		Format = value;
	}

	public void SetOffsetx(int value)
	{
		Offsetx = value;
	}

	public void SetOffsety(int value)
	{
		Offsety = value;
	}

	public void SetOffsetz(int value)
	{
		Offsetz = value;
	}

	public void SetInsertBakedTextures(bool value)
	{
		InsertBakedTextures = value;
	}

	public void SetScaleAdjust(int value)
	{
		ScaleAdjust = value;
	}

	internal void InitializeValues()
	{
	}
}
