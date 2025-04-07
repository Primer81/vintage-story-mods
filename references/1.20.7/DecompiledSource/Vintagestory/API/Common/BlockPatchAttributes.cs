#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class BlockPatchAttributes
{
    //
    // Summary:
    //     List of asset codes for the base (coralblock) types of a coral reef blockpatch
    public string[]? CoralBase;

    //
    // Summary:
    //     List of asset codes for the coral structure types of a coral reef blockpatch
    public string[]? CoralStructure;

    //
    // Summary:
    //     List of asset codes for the coral shelved types of a coral reef blockpatch These
    //     need to have HorizontalOrientable behaviour and only specify one side in here
    //     "coralshelf-north"
    public string[]? CoralShelve;

    //
    // Summary:
    //     List of asset codes for the coral types of a coral reef blockpatch coral-brain,
    //     coral-fan ...
    public string[]? Coral;

    //
    // Summary:
    //     Defines the minimum 2D size of the coral reef
    public int? CoralMinSize;

    //
    // Summary:
    //     Defines the random size between 0 - X that will be added additionally to the
    //     reef
    public int? CoralRandomSize;

    //
    // Summary:
    //     Chance for a shelf block to spawn on a under water cliff. The chance is rolled
    //     for each height The patch will try to spawn them until it reaches minWaterDepth
    public float? CoralVerticalGrowChance;

    //
    // Summary:
    //     Chance that any Plant will spawn
    public float? CoralPlantsChance;

    //
    // Summary:
    //     Specifiy which plants should spawn for this blockpatch and their heigh and how
    //     often a specific plant should be choosen
    public Dictionary<string, CoralPlantConfig>? CoralPlants;

    //
    // Summary:
    //     Chance that a shelf will spawn instead of a structure on top of a coralblock
    public float? CoralShelveChance;

    //
    // Summary:
    //     Chance that coral generatin will replace all other block patches in its area
    public float? CoralReplaceOtherPatches;

    //
    // Summary:
    //     If no shelf was spanwed this chance controls how likely a structure will spawn
    //     instead of a coral. If a structure is spawned then a coral will spawn on top
    //     If no shelve nor structure was spawned then also a coral will be spawned
    public float? CoralStructureChance;

    //
    // Summary:
    //     How thick the base coral full block layer should be for this patch (goes down
    //     into the ground, helpfull for cliffs) 1 -> replace the gravel with coral 2 ->
    //     go 1 block below gravel and also replace and so on
    public int CoralBaseHeight;

    //
    // Summary:
    //     Chance that a BlockCrowfoot will spawn a flower when it reaches the water surface
    public float? FlowerChance;

    //
    // Summary:
    //     Heigh distribution for BlockSeaweed and BlockCrowfoot types
    public NatFloat? Height;

    [JsonIgnore]
    public Block[]? CoralBaseBlock;

    [JsonIgnore]
    public Block[]? CoralStructureBlock;

    [JsonIgnore]
    public Block[][]? CoralShelveBlock;

    [JsonIgnore]
    public Block[]? CoralBlock;

    public void Init(ICoreServerAPI sapi, int i)
    {
        List<Block> list = new List<Block>();
        if (CoralBase != null)
        {
            string[] coralBase = CoralBase;
            foreach (string text in coralBase)
            {
                Block[] array = sapi.World.SearchBlocks(new AssetLocation(text));
                if (array != null)
                {
                    list.AddRange(array);
                    continue;
                }

                sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralBaseBlocks block with code {1}. Will ignore.", i, text);
            }

            CoralBaseBlock = list.ToArray();
            list.Clear();
        }

        if (CoralStructure != null)
        {
            string[] coralBase = CoralStructure;
            foreach (string text2 in coralBase)
            {
                Block[] array2 = sapi.World.SearchBlocks(new AssetLocation(text2));
                if (array2 != null)
                {
                    list.AddRange(array2);
                    continue;
                }

                sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralBaseBlocks block with code {1}. Will ignore.", i, text2);
            }

            CoralStructureBlock = list.ToArray();
            list.Clear();
        }

        if (CoralShelve != null)
        {
            List<Block[]> list2 = new List<Block[]>();
            string[] coralBase = CoralShelve;
            foreach (string text3 in coralBase)
            {
                Block[] array3 = sapi.World.SearchBlocks(new AssetLocation(text3));
                if (array3 != null)
                {
                    List<Block[]> list3 = new List<Block[]>();
                    Block[] array4 = array3;
                    foreach (Block block in array4)
                    {
                        string codeWithoutParts = block.CodeWithoutParts(1);
                        if (!list2.Any((Block[] c) => c[0].Code.Path.Equals(codeWithoutParts + "-north")))
                        {
                            Block[] item = new Block[4]
                            {
                                sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-north")),
                                sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-east")),
                                sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-south")),
                                sapi.World.BlockAccessor.GetBlock(new AssetLocation(codeWithoutParts + "-west"))
                            };
                            list3.Add(item);
                        }
                    }

                    list2.AddRange(list3);
                }
                else
                {
                    sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralBaseBlocks block with code {1}. Will ignore.", i, text3);
                }
            }

            CoralShelveBlock = list2.ToArray();
        }

        if (Coral != null)
        {
            string[] coralBase = Coral;
            foreach (string text4 in coralBase)
            {
                Block[] array5 = sapi.World.SearchBlocks(new AssetLocation(text4));
                if (array5 != null)
                {
                    list.AddRange(array5);
                    continue;
                }

                sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralBaseBlocks block with code {1}. Will ignore.", i, text4);
            }

            CoralBlock = list.ToArray();
            list.Clear();
        }

        if (CoralPlants == null)
        {
            return;
        }

        foreach (KeyValuePair<string, CoralPlantConfig> coralPlant in CoralPlants)
        {
            coralPlant.Deconstruct(out var key, out var value);
            string text5 = key;
            CoralPlantConfig coralPlantConfig = value;
            Block[] array6 = sapi.World.SearchBlocks(new AssetLocation(text5));
            if (array6 != null)
            {
                coralPlantConfig.Block = array6;
                continue;
            }

            sapi.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve CoralPlants block with code {1}. Will ignore.", i, text5);
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
