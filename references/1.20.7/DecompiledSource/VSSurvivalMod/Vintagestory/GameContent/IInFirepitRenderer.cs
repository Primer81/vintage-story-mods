using System;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public interface IInFirepitRenderer : IRenderer, IDisposable
{
	void OnUpdate(float temperature);

	void OnCookingComplete();
}
