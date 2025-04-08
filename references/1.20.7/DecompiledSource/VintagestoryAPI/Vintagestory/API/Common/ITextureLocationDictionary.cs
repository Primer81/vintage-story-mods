using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public interface ITextureLocationDictionary
{
	int this[AssetLocationAndSource textureLoc] { get; }

	bool AddTextureLocation(AssetLocationAndSource textureLoc);

	int GetOrAddTextureLocation(AssetLocationAndSource textureLoc);

	bool ContainsKey(AssetLocation loc);

	void SetTextureLocation(AssetLocationAndSource assetLocationAndSource);

	void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc);
}
