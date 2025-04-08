using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Vintagestory.API.Common;

public class FrameProfilerUtil
{
	public bool Enabled;

	public bool PrintSlowTicks;

	public int PrintSlowTicksThreshold = 40;

	public ProfileEntryRange PrevRootEntry;

	public string summary;

	public string OutputPrefix = "";

	public static ConcurrentQueue<string> offThreadProfiles = new ConcurrentQueue<string>();

	public static bool PrintSlowTicks_Offthreads;

	public static int PrintSlowTicksThreshold_Offthreads = 40;

	private Stopwatch stopwatch = new Stopwatch();

	private ProfileEntryRange rootEntry;

	private ProfileEntryRange currentEntry;

	private string beginText;

	private Action<string> onLogoutputHandler;

	public FrameProfilerUtil(Action<string> onLogoutputHandler)
	{
		this.onLogoutputHandler = onLogoutputHandler;
		stopwatch.Start();
	}

	/// <summary>
	/// Used to create a FrameProfilerUtil on threads other than the main thread
	/// </summary>
	/// <param name="outputPrefix"></param>
	public FrameProfilerUtil(string outputPrefix)
		: this(delegate(string text)
		{
			offThreadProfiles.Enqueue(text);
		})
	{
		OutputPrefix = outputPrefix;
	}

	/// <summary>
	/// Called by the game engine for each render frame or server tick
	/// </summary>
	public void Begin(string beginText = null, params object[] args)
	{
		if (Enabled || PrintSlowTicks)
		{
			this.beginText = ((beginText == null) ? null : string.Format(beginText, args));
			currentEntry = null;
			rootEntry = Enter("all");
		}
	}

	public ProfileEntryRange Enter(string code)
	{
		if (!Enabled && !PrintSlowTicks)
		{
			return null;
		}
		long elapsedTicks = stopwatch.ElapsedTicks;
		if (currentEntry == null)
		{
			ProfileEntryRange obj = new ProfileEntryRange
			{
				Code = code,
				Start = elapsedTicks,
				LastMark = elapsedTicks,
				CallCount = 0
			};
			ProfileEntryRange result = obj;
			currentEntry = obj;
			return result;
		}
		if (currentEntry.ChildRanges == null)
		{
			currentEntry.ChildRanges = new Dictionary<string, ProfileEntryRange>();
		}
		if (!currentEntry.ChildRanges.TryGetValue(code, out var entry))
		{
			Dictionary<string, ProfileEntryRange> childRanges = currentEntry.ChildRanges;
			ProfileEntryRange obj2 = new ProfileEntryRange
			{
				Code = code,
				Start = elapsedTicks,
				LastMark = elapsedTicks,
				CallCount = 0
			};
			entry = obj2;
			childRanges[code] = obj2;
			entry.ParentRange = currentEntry;
		}
		else
		{
			entry.Start = elapsedTicks;
			entry.LastMark = elapsedTicks;
		}
		currentEntry = entry;
		entry.CallCount++;
		return entry;
	}

	/// <summary>
	/// Same as <see cref="M:Vintagestory.API.Common.FrameProfilerUtil.Mark(System.String)" /> when <see cref="M:Vintagestory.API.Common.FrameProfilerUtil.Enter(System.String)" /> was called before.
	/// </summary>
	public void Leave()
	{
		if (Enabled || PrintSlowTicks)
		{
			long elapsedTicks = stopwatch.ElapsedTicks;
			currentEntry.ElapsedTicks += elapsedTicks - currentEntry.Start;
			currentEntry.LastMark = elapsedTicks;
			currentEntry = currentEntry.ParentRange;
			for (ProfileEntryRange parent = currentEntry; parent != null; parent = parent.ParentRange)
			{
				parent.LastMark = elapsedTicks;
			}
		}
	}

	/// <summary>
	/// Use this method to add a frame profiling marker, will set or add the time ellapsed since the previous mark to the frame profiling reults.
	/// </summary>
	/// <param name="code"></param>
	public void Mark(string code)
	{
		if (!Enabled && !PrintSlowTicks)
		{
			return;
		}
		if (code == null)
		{
			throw new ArgumentNullException("marker name may not be null!");
		}
		try
		{
			ProfileEntryRange entry = currentEntry;
			if (entry != null)
			{
				Dictionary<string, ProfileEntry> marks = entry.Marks;
				if (marks == null)
				{
					marks = (entry.Marks = new Dictionary<string, ProfileEntry>());
				}
				if (!marks.TryGetValue(code, out var ms))
				{
					ms = (marks[code] = new ProfileEntry());
				}
				long ticks = stopwatch.ElapsedTicks;
				ms.ElapsedTicks += (int)(ticks - entry.LastMark);
				ms.CallCount++;
				entry.LastMark = ticks;
			}
		}
		catch (Exception)
		{
		}
	}

	/// <summary>
	/// Called by the game engine at the end of the render frame or server tick
	/// </summary>
	public void End()
	{
		if (!Enabled && !PrintSlowTicks)
		{
			return;
		}
		Mark("end");
		Leave();
		PrevRootEntry = rootEntry;
		double ms = (double)rootEntry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0;
		if (PrintSlowTicks && ms > (double)PrintSlowTicksThreshold)
		{
			StringBuilder strib = new StringBuilder();
			if (beginText != null)
			{
				strib.Append(beginText).Append(' ');
			}
			strib.AppendLine($"{OutputPrefix}A tick took {ms:0.##} ms");
			slowTicksToString(rootEntry, strib);
			summary = "Stopwatched total= " + ms + "ms";
			onLogoutputHandler(strib.ToString());
		}
	}

	public void OffThreadEnd()
	{
		End();
		Enabled = (PrintSlowTicks = PrintSlowTicks_Offthreads);
		PrintSlowTicksThreshold = PrintSlowTicksThreshold_Offthreads;
	}

	private void slowTicksToString(ProfileEntryRange entry, StringBuilder strib, double thresholdMs = 0.35, string indent = "")
	{
		try
		{
			double timeMS = (double)entry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0;
			if (timeMS < thresholdMs)
			{
				return;
			}
			if (entry.CallCount > 1)
			{
				strib.AppendLine(indent + $"{timeMS:0.00}ms, {entry.CallCount:####} calls, avg {timeMS * 1000.0 / (double)Math.Max(entry.CallCount, 1):0.00} us/call: {entry.Code:0.00}");
			}
			else
			{
				strib.AppendLine(indent + $"{timeMS:0.00}ms, {entry.CallCount:####} call : {entry.Code}");
			}
			List<ProfileEntryRange> profiles = new List<ProfileEntryRange>();
			if (entry.Marks != null)
			{
				profiles.AddRange(entry.Marks.Select((KeyValuePair<string, ProfileEntry> e) => new ProfileEntryRange
				{
					ElapsedTicks = e.Value.ElapsedTicks,
					Code = e.Key,
					CallCount = e.Value.CallCount
				}));
			}
			if (entry.ChildRanges != null)
			{
				profiles.AddRange(entry.ChildRanges.Values);
			}
			IOrderedEnumerable<ProfileEntryRange> orderedEnumerable = profiles.OrderByDescending((ProfileEntryRange prof) => prof.ElapsedTicks);
			int i = 0;
			foreach (ProfileEntryRange prof2 in orderedEnumerable)
			{
				if (i++ > 8)
				{
					break;
				}
				slowTicksToString(prof2, strib, thresholdMs, indent + "  ");
			}
		}
		catch (Exception)
		{
		}
	}
}
