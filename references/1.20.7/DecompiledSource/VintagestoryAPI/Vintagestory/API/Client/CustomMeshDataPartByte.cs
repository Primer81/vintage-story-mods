namespace Vintagestory.API.Client;

/// <summary>
/// Holds arbitrary byte data for meshes to be used in the shader
/// </summary>
public class CustomMeshDataPartByte : CustomMeshDataPart<byte>
{
	public DataConversion Conversion = DataConversion.NormalizedFloat;

	/// <summary>
	/// Empty Constructor.
	/// </summary>
	public CustomMeshDataPartByte()
	{
	}

	/// <summary>
	/// Size initialization constructor.
	/// </summary>
	/// <param name="size"></param>
	public CustomMeshDataPartByte(int size)
		: base(size)
	{
	}

	/// <summary>
	/// adds values to the bytes part per four bytes.
	/// </summary>
	/// <param name="fourbytes">the integer mask of four separate bytes.</param>
	public unsafe void AddBytes(int fourbytes)
	{
		if (Count + 4 >= base.BufferSize)
		{
			GrowBuffer();
		}
		fixed (byte* bytes = Values)
		{
			int* bytesInt = (int*)bytes;
			bytesInt[Count / 4] = fourbytes;
		}
		Count += 4;
	}

	/// <summary>
	/// Creates a clone of this collection of data parts.
	/// </summary>
	/// <returns>A clone of this collection of data parts.</returns>
	public CustomMeshDataPartByte Clone()
	{
		CustomMeshDataPartByte customMeshDataPartByte = new CustomMeshDataPartByte();
		customMeshDataPartByte.SetFrom(this);
		customMeshDataPartByte.Conversion = Conversion;
		return customMeshDataPartByte;
	}

	public CustomMeshDataPartByte EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartByte()) as CustomMeshDataPartByte;
	}
}
