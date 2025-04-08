namespace Vintagestory.API.Client;

/// <summary>
/// Holds arbitrary short data for meshes to be used in the shader
/// </summary>
public class CustomMeshDataPartShort : CustomMeshDataPart<short>
{
	public DataConversion Conversion = DataConversion.NormalizedFloat;

	/// <summary>
	/// Empty constructor.
	/// </summary>
	public CustomMeshDataPartShort()
	{
	}

	/// <summary>
	/// Size initialization constructor.
	/// </summary>
	/// <param name="size"></param>
	public CustomMeshDataPartShort(int size)
		: base(size)
	{
	}

	/// <summary>
	/// Creates a clone of this collection of data parts.
	/// </summary>
	/// <returns>A clone of this collection of data parts.</returns>
	public CustomMeshDataPartShort Clone()
	{
		CustomMeshDataPartShort customMeshDataPartShort = new CustomMeshDataPartShort();
		customMeshDataPartShort.SetFrom(this);
		return customMeshDataPartShort;
	}

	public CustomMeshDataPartShort EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartShort()) as CustomMeshDataPartShort;
	}
}
