namespace Vintagestory.Server.Network;

public delegate void OnReceivedMessageDelegate(byte[] data, TcpNetConnection tcpConnection);
