using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public static class AnimationCache
{
	/// <summary>
	/// Clears the animation cache.
	/// </summary>
	/// <param name="api"></param>
	public static void ClearCache(ICoreAPI api)
	{
		api.ObjectCache["animCache"] = null;
	}

	/// <summary>
	/// Clears the animation cache.
	/// </summary>
	/// <param name="api"></param>
	/// <param name="entity"></param>
	public static void ClearCache(ICoreAPI api, Entity entity)
	{
		Dictionary<string, AnimCacheEntry> orCreate = ObjectCacheUtil.GetOrCreate(api, "animCache", () => new Dictionary<string, AnimCacheEntry>());
		string dictKey = string.Concat(entity.Code, "-", entity.Properties.Client.ShapeForEntity.Base.ToString());
		orCreate.Remove(dictKey);
	}

	public static IAnimationManager LoadAnimatorCached(this IAnimationManager manager, ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
	{
		return InitManager(api, manager, entity, entityShape, copyOverAnims, requirePosesOnServer, requireJointsForElements);
	}

	/// <summary>
	/// Initializes the cache to the Animation Manager then spits it back out.
	/// </summary>
	/// <param name="api"></param>
	/// <param name="manager"></param>
	/// <param name="entity"></param>
	/// <param name="entityShape"></param>
	/// <param name="copyOverAnims"></param>
	/// <param name="requireJointsForElements"></param>
	/// <returns></returns>
	[Obsolete("Use manager.LoadAnimator() or manager.LoadAnimatorCached() instead")]
	public static IAnimationManager InitManager(ICoreAPI api, IAnimationManager manager, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
	{
		if (entityShape == null)
		{
			return new NoAnimationManager();
		}
		string dictKey = string.Concat(entity.Code, "-", entity.Properties.Client.ShapeForEntity.Base.ToString());
		Dictionary<string, AnimCacheEntry> animCache = ObjectCacheUtil.GetOrCreate(api, "animCache", () => new Dictionary<string, AnimCacheEntry>());
		IAnimator animator = null;
		if (animCache.TryGetValue(dictKey, out var cacheObj))
		{
			manager.Init(entity.Api, entity);
			animator = (manager.Animator = ((api.Side == EnumAppSide.Client) ? ClientAnimator.CreateForEntity(entity, cacheObj.RootPoses, cacheObj.Animations, cacheObj.RootElems, entityShape.JointsById) : ClientAnimator.CreateForEntity(entity, cacheObj.RootPoses, cacheObj.Animations, cacheObj.RootElems, entityShape.JointsById)));
			manager.CopyOverAnimStates(copyOverAnims, animator);
		}
		else
		{
			animator = manager.LoadAnimator(api, entity, entityShape, copyOverAnims, requirePosesOnServer, requireJointsForElements);
			animCache[dictKey] = new AnimCacheEntry
			{
				Animations = entityShape.Animations,
				RootElems = (animator as AnimatorBase).RootElements,
				RootPoses = (animator as AnimatorBase).RootPoses
			};
		}
		return manager;
	}
}
