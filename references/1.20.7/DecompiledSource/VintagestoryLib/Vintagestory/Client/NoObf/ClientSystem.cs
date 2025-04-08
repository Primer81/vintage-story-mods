using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public abstract class ClientSystem
{
	internal ClientMain game;

	internal long threadMillisecondsSinceStart;

	public abstract string Name { get; }

	public ClientSystem(ClientMain game)
	{
		this.game = game;
	}

	public virtual void OnNewFrameReadOnlyMainThread(float deltaTime)
	{
	}

	public virtual void OnKeyDown(KeyEvent args)
	{
	}

	public virtual void OnKeyPress(KeyEvent args)
	{
	}

	public virtual void OnMouseUp(MouseEvent args)
	{
	}

	public virtual void OnKeyUp(KeyEvent args)
	{
	}

	public virtual void OnMouseDown(MouseEvent args)
	{
	}

	public virtual void OnMouseMove(MouseEvent args)
	{
	}

	public virtual void OnMouseWheel(MouseWheelEventArgs args)
	{
	}

	public virtual void OnTouchStart(TouchEventArgs e)
	{
	}

	public virtual void OnTouchMove(TouchEventArgs e)
	{
	}

	public virtual void OnTouchEnd(TouchEventArgs e)
	{
	}

	public virtual void OnOwnPlayerDataReceived()
	{
	}

	public virtual bool OnMouseEnterSlot(ItemSlot slot)
	{
		return false;
	}

	public virtual bool OnMouseLeaveSlot(ItemSlot itemSlot)
	{
		return false;
	}

	public virtual int SeperateThreadTickIntervalMs()
	{
		return 20;
	}

	public virtual void OnSeperateThreadGameTick(float dt)
	{
	}

	public virtual bool OnMouseClickSlot(ItemSlot itemSlot)
	{
		return false;
	}

	public virtual void OnBlockTexturesLoaded()
	{
	}

	public virtual void OnServerIdentificationReceived()
	{
	}

	public virtual void Dispose(ClientMain game)
	{
	}

	public virtual bool CaptureAllInputs()
	{
		return false;
	}

	public virtual bool CaptureRawMouse()
	{
		return false;
	}

	public abstract EnumClientSystemType GetSystemType();

	internal virtual void OnBlockSet(BlockPos pos)
	{
	}

	internal virtual void OnLevelFinalize()
	{
	}
}
