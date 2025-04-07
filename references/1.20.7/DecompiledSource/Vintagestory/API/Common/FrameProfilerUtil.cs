#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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

    //
    // Summary:
    //     Used to create a FrameProfilerUtil on threads other than the main thread
    //
    // Parameters:
    //   outputPrefix:
    public FrameProfilerUtil(string outputPrefix)
        : this(delegate (string text)
        {
            offThreadProfiles.Enqueue(text);
        })
    {
        OutputPrefix = outputPrefix;
    }

    //
    // Summary:
    //     Called by the game engine for each render frame or server tick
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

        if (!currentEntry.ChildRanges.TryGetValue(code, out var value))
        {
            Dictionary<string, ProfileEntryRange> childRanges = currentEntry.ChildRanges;
            ProfileEntryRange obj2 = new ProfileEntryRange
            {
                Code = code,
                Start = elapsedTicks,
                LastMark = elapsedTicks,
                CallCount = 0
            };
            value = obj2;
            childRanges[code] = obj2;
            value.ParentRange = currentEntry;
        }
        else
        {
            value.Start = elapsedTicks;
            value.LastMark = elapsedTicks;
        }

        currentEntry = value;
        value.CallCount++;
        return value;
    }

    //
    // Summary:
    //     Same as Vintagestory.API.Common.FrameProfilerUtil.Mark(System.String) when Vintagestory.API.Common.FrameProfilerUtil.Enter(System.String)
    //     was called before.
    public void Leave()
    {
        if (Enabled || PrintSlowTicks)
        {
            long elapsedTicks = stopwatch.ElapsedTicks;
            currentEntry.ElapsedTicks += elapsedTicks - currentEntry.Start;
            currentEntry.LastMark = elapsedTicks;
            currentEntry = currentEntry.ParentRange;
            for (ProfileEntryRange parentRange = currentEntry; parentRange != null; parentRange = parentRange.ParentRange)
            {
                parentRange.LastMark = elapsedTicks;
            }
        }
    }

    //
    // Summary:
    //     Use this method to add a frame profiling marker, will set or add the time ellapsed
    //     since the previous mark to the frame profiling reults.
    //
    // Parameters:
    //   code:
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
            ProfileEntryRange profileEntryRange = currentEntry;
            if (profileEntryRange != null)
            {
                Dictionary<string, ProfileEntry> dictionary = profileEntryRange.Marks;
                if (dictionary == null)
                {
                    dictionary = (profileEntryRange.Marks = new Dictionary<string, ProfileEntry>());
                }

                if (!dictionary.TryGetValue(code, out var value))
                {
                    value = (dictionary[code] = new ProfileEntry());
                }

                long elapsedTicks = stopwatch.ElapsedTicks;
                value.ElapsedTicks += (int)(elapsedTicks - profileEntryRange.LastMark);
                value.CallCount++;
                profileEntryRange.LastMark = elapsedTicks;
            }
        }
        catch (Exception)
        {
        }
    }

    //
    // Summary:
    //     Called by the game engine at the end of the render frame or server tick
    public void End()
    {
        if (!Enabled && !PrintSlowTicks)
        {
            return;
        }

        Mark("end");
        Leave();
        PrevRootEntry = rootEntry;
        double num = (double)rootEntry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0;
        if (PrintSlowTicks && num > (double)PrintSlowTicksThreshold)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (beginText != null)
            {
                stringBuilder.Append(beginText).Append(' ');
            }

            stringBuilder.AppendLine($"{OutputPrefix}A tick took {num:0.##} ms");
            slowTicksToString(rootEntry, stringBuilder);
            summary = "Stopwatched total= " + num + "ms";
            onLogoutputHandler(stringBuilder.ToString());
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
            double num = (double)entry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0;
            if (num < thresholdMs)
            {
                return;
            }

            if (entry.CallCount > 1)
            {
                strib.AppendLine(indent + $"{num:0.00}ms, {entry.CallCount:####} calls, avg {num * 1000.0 / (double)Math.Max(entry.CallCount, 1):0.00} us/call: {entry.Code:0.00}");
            }
            else
            {
                strib.AppendLine(indent + $"{num:0.00}ms, {entry.CallCount:####} call : {entry.Code}");
            }

            List<ProfileEntryRange> list = new List<ProfileEntryRange>();
            if (entry.Marks != null)
            {
                list.AddRange(entry.Marks.Select((KeyValuePair<string, ProfileEntry> e) => new ProfileEntryRange
                {
                    ElapsedTicks = e.Value.ElapsedTicks,
                    Code = e.Key,
                    CallCount = e.Value.CallCount
                }));
            }

            if (entry.ChildRanges != null)
            {
                list.AddRange(entry.ChildRanges.Values);
            }

            IOrderedEnumerable<ProfileEntryRange> orderedEnumerable = list.OrderByDescending((ProfileEntryRange prof) => prof.ElapsedTicks);
            int num2 = 0;
            foreach (ProfileEntryRange item in orderedEnumerable)
            {
                if (num2++ > 8)
                {
                    break;
                }

                slowTicksToString(item, strib, thresholdMs, indent + "  ");
            }
        }
        catch (Exception)
        {
        }
    }
}
#if false // Decompilation log
'182' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
