using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class GeneratedStructure
{
	/// <summary>
	/// Block position of the structure
	/// </summary>
	public Cuboidi Location;

	/// <summary>
	/// Code as defined in the WorldGenStructure object
	/// </summary>
	public string Code;

	/// <summary>
	/// Group as defined in the WorldGenStructure object
	/// </summary>
	public string Group;

	/// <summary>
	/// If this flag is set, trees and shrubs will not generate inside the structure's bounding box 
	/// </summary>
	public bool SuppressTreesAndShrubs;

	/// <summary>
	/// If this flag is set, rivulets will not generate inside the structure's bounding box 
	/// </summary>
	public bool SuppressRivulets;
}
