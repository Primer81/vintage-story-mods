namespace Vintagestory.Client.NoObf;

public delegate void OnSrvActionComplete<T>(EnumAuthServerResponse reqStatus, T response) where T : ServerCtrlResponse;
