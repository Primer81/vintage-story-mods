using System.Collections.Generic;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class DummyNetwork
{
	internal Queue<object> ServerReceiveBuffer;

	internal Queue<object> ClientReceiveBuffer;

	internal MonitorObject ServerReceiveBufferLock;

	internal MonitorObject ClientReceiveBufferLock;

	public DummyNetwork()
	{
		Clear();
	}

	public void Start()
	{
		ServerReceiveBufferLock = new MonitorObject();
		ClientReceiveBufferLock = new MonitorObject();
	}

	public void Clear()
	{
		ServerReceiveBuffer = new Queue<object>();
		ClientReceiveBuffer = new Queue<object>();
	}
}
