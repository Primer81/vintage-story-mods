using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public static class ObjectCacheUtil
{
	public static T TryGet<T>(ICoreAPI api, string key)
	{
		if (api.ObjectCache.TryGetValue(key, out var obj))
		{
			return (T)obj;
		}
		return default(T);
	}

	public static T GetOrCreate<T>(ICoreAPI api, string key, CreateCachableObjectDelegate<T> onRequireCreate)
	{
		if (!api.ObjectCache.TryGetValue(key, out var obj) || obj == null)
		{
			T typedObj = onRequireCreate();
			api.ObjectCache[key] = typedObj;
			return typedObj;
		}
		return (T)obj;
	}

	public static bool Delete(ICoreAPI api, string key)
	{
		if (key == null)
		{
			return false;
		}
		return api.ObjectCache.Remove(key);
	}
}
