namespace Vintagestory.API.Client;

/// <summary>
/// Holds arbitrary int data for meshes to be used in the shader
/// </summary>
public class CustomMeshDataPartInt : CustomMeshDataPart<int>
{
	public DataConversion Conversion = DataConversion.Integer;

	/// <summary>
	/// Empty constructor.
	/// </summary>
	public CustomMeshDataPartInt()
	{
	}

	/// <summary>
	/// Size initialization constructor.
	/// </summary>
	/// <param name="size"></param>
	public CustomMeshDataPartInt(int size)
		: base(size)
	{
	}

	/// <summary>
	/// Creates a clone of this collection of data parts.
	/// </summary>
	/// <returns>A clone of this collection of data parts.</returns>
	public CustomMeshDataPartInt Clone()
	{
		CustomMeshDataPartInt customMeshDataPartInt = new CustomMeshDataPartInt();
		customMeshDataPartInt.SetFrom(this);
		return customMeshDataPartInt;
	}

	public CustomMeshDataPartInt EmptyClone()
	{
		return EmptyClone(new CustomMeshDataPartInt()) as CustomMeshDataPartInt;
	}
}
