using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class AudioMetaData : AudioData
{
	public byte[] Pcm;

	public int Channels;

	public int Rate;

	public int BitsPerSample = 16;

	public IAsset Asset;

	public bool AutoUnload;

	private List<MainThreadAction> waitingToPlay;

	public AudioMetaData(IAsset asset)
	{
		if (asset == null)
		{
			throw new ArgumentNullException("Asset cannot be null");
		}
		Asset = asset;
	}

	private void DoLoad()
	{
		if (!Asset.IsLoaded())
		{
			Asset.Origin.LoadAsset(Asset);
		}
		AudioMetaData meta = (AudioMetaData)ScreenManager.Platform.CreateAudioData(Asset);
		Pcm = meta.Pcm;
		Channels = meta.Channels;
		Rate = meta.Rate;
		BitsPerSample = meta.BitsPerSample;
		Asset.Data = null;
		Loaded = 2;
		lock (this)
		{
			if (waitingToPlay == null)
			{
				return;
			}
			foreach (MainThreadAction item in waitingToPlay)
			{
				item.Enqueue();
			}
			waitingToPlay = null;
		}
	}

	public override bool Load()
	{
		if (AsyncHelper.CanProceedOnThisThread(ref Loaded))
		{
			DoLoad();
			return true;
		}
		if (Loaded >= 2)
		{
			return true;
		}
		return false;
	}

	public override int Load_Async(MainThreadAction onCompleted)
	{
		if (AsyncHelper.CanProceedOnThisThread(ref Loaded))
		{
			TyronThreadPool.QueueTask(delegate
			{
				DoLoad();
				onCompleted.Enqueue();
			});
			return -1;
		}
		if (Loaded == 1)
		{
			AddOnLoaded(onCompleted);
			return -1;
		}
		return onCompleted.Invoke();
	}

	public void AddOnLoaded(MainThreadAction onCompleted)
	{
		lock (this)
		{
			if (Loaded == 3)
			{
				onCompleted.Enqueue();
				return;
			}
			if (waitingToPlay == null)
			{
				waitingToPlay = new List<MainThreadAction>();
			}
			waitingToPlay.Add(onCompleted);
		}
	}

	public void Unload()
	{
		Pcm = null;
		Channels = 0;
		Rate = 0;
		BitsPerSample = 0;
		Asset.Data = null;
		Loaded = 0;
	}
}
