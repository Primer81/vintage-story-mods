using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public interface IMeshCreator
{
	float texSizeU { get; }

	float texSizeV { get; }

	MeshData CreateMesh();

	void RegisterMesh(MeshRef mesh);
}
