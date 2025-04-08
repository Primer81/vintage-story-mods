namespace Vintagestory.API.Common;

public interface IMountableListener
{
	void DidUnnmount(EntityAgent entityAgent);

	void DidMount(EntityAgent entityAgent);
}
