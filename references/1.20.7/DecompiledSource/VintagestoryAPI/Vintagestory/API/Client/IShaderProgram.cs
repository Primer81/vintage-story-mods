using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IShaderProgram : IDisposable
{
	/// <summary>
	/// When loading from file this is the asset domain to load from
	/// </summary>
	string AssetDomain { get; set; }

	/// <summary>
	/// A uniqe shader pass number assigned to each shader program
	/// </summary>
	int PassId { get; }

	/// <summary>
	/// The name it was registered with. If you want to load this shader from a file, make sure the use the filename here
	/// </summary>
	string PassName { get; }

	/// <summary>
	/// If true, it well configure the textures to clamp to the edge (CLAMP_TO_EDGE). Requires the textureid to be defined using SetTextureIds
	/// </summary>
	bool ClampTexturesToEdge { get; set; }

	/// <summary>
	/// The vertex shader of this shader program
	/// </summary>
	IShader VertexShader { get; set; }

	/// <summary>
	/// The fragment shader of this shader program
	/// </summary>
	IShader FragmentShader { get; set; }

	/// <summary>
	/// The geometry shader of this shader program
	/// </summary>
	IShader GeometryShader { get; set; }

	/// <summary>
	/// True if this shader has been disposed
	/// </summary>
	bool Disposed { get; }

	bool LoadError { get; }

	OrderedDictionary<string, UBORef> UBOs { get; }

	void Use();

	void Stop();

	bool Compile();

	void Uniform(string uniformName, float value);

	void Uniform(string uniformName, int value);

	void Uniform(string uniformName, Vec2f value);

	void Uniform(string uniformName, Vec3f value);

	void Uniform(string uniformName, Vec4f value);

	void Uniforms4(string uniformName, int count, float[] values);

	void UniformMatrix(string uniformName, float[] matrix);

	void BindTexture2D(string samplerName, int textureId, int textureNumber);

	void BindTextureCube(string samplerName, int textureId, int textureNumber);

	void UniformMatrices(string uniformName, int count, float[] matrix);

	void UniformMatrices4x3(string uniformName, int count, float[] matrix);

	bool HasUniform(string uniformName);
}
