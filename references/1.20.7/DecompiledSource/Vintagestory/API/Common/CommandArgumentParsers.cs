#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class CommandArgumentParsers
{
    private ICoreAPI api;

    public CommandArgumentParsers(ICoreAPI api)
    {
        this.api = api;
    }

    public UnparsedArg Unparsed(string argname, params string[] validRange)
    {
        return new UnparsedArg(argname, validRange);
    }

    public DirectionArgParser<Vec3i> IntDirection(string argName)
    {
        return new DirectionArgParser<Vec3i>(argName, isMandatoryArg: true);
    }

    public EntitiesArgParser Entities(string argName)
    {
        return new EntitiesArgParser(argName, api, isMandatoryArg: true);
    }

    //
    // Summary:
    //     Defaults to caller entity
    //
    // Parameters:
    //   argName:
    public EntitiesArgParser OptionalEntities(string argName)
    {
        return new EntitiesArgParser(argName, api, isMandatoryArg: false);
    }

    public EntityTypeArgParser EntityType(string argName)
    {
        return new EntityTypeArgParser(argName, api, isMandatoryArg: true);
    }

    public IntArgParser IntRange(string argName, int min, int max)
    {
        return new IntArgParser(argName, min, max, 0, isMandatoryArg: true);
    }

    public IntArgParser OptionalIntRange(string argName, int min, int max, int defaultValue = 0)
    {
        return new IntArgParser(argName, min, max, defaultValue, isMandatoryArg: false);
    }

    public IntArgParser OptionalInt(string argName, int defaultValue = 0)
    {
        return new IntArgParser(argName, defaultValue, isMandatoryArg: false);
    }

    public IntArgParser Int(string argName)
    {
        return new IntArgParser(argName, 0, isMandatoryArg: true);
    }

    public LongArgParser OptionalLong(string argName, int defaultValue = 0)
    {
        return new LongArgParser(argName, defaultValue, isMandatoryArg: false);
    }

    public LongArgParser Long(string argName)
    {
        return new LongArgParser(argName, 0L, isMandatoryArg: true);
    }

    public BoolArgParser Bool(string argName, string trueAlias = "on")
    {
        return new BoolArgParser(argName, trueAlias, isMandatoryArg: true);
    }

    public BoolArgParser OptionalBool(string argName, string trueAlias = "on")
    {
        return new BoolArgParser(argName, trueAlias, isMandatoryArg: false);
    }

    public DoubleArgParser OptionalDouble(string argName, double defaultvalue = 0.0)
    {
        return new DoubleArgParser(argName, defaultvalue, isMandatoryArg: false);
    }

    public FloatArgParser Float(string argName)
    {
        return new FloatArgParser(argName, 0f, isMandatoryArg: true);
    }

    public FloatArgParser OptionalFloat(string argName, float defaultvalue = 0f)
    {
        return new FloatArgParser(argName, defaultvalue, isMandatoryArg: false);
    }

    public DoubleArgParser Double(string argName)
    {
        return new DoubleArgParser(argName, 0.0, isMandatoryArg: true);
    }

    public DoubleArgParser DoubleRange(string argName, double min, double max)
    {
        return new DoubleArgParser(argName, min, max, isMandatoryArg: true);
    }

    //
    // Summary:
    //     A currently online player
    //
    // Parameters:
    //   argName:
    public OnlinePlayerArgParser OnlinePlayer(string argName)
    {
        return new OnlinePlayerArgParser(argName, api, isMandatoryArg: true);
    }

    //
    // Summary:
    //     All selected players
    //
    // Parameters:
    //   argName:
    public PlayersArgParser PlayerUids(string argName)
    {
        return new PlayersArgParser(argName, api, isMandatoryArg: true);
    }

    //
    // Summary:
    //     All selected players
    //
    // Parameters:
    //   argName:
    public PlayersArgParser OptionalPlayerUids(string argName)
    {
        return new PlayersArgParser(argName, api, isMandatoryArg: false);
    }

    //
    // Summary:
    //     Parses IPlayerRole, only works on Serverside since it needs the Serverconfig
    //
    //
    // Parameters:
    //   argName:
    public PlayerRoleArgParser PlayerRole(string argName)
    {
        return new PlayerRoleArgParser(argName, api, isMandatoryArg: true);
    }

    //
    // Summary:
    //     Parses IPlayerRole, only works on Serverside since it needs the Serverconfig
    //
    //
    // Parameters:
    //   argName:
    public PlayerRoleArgParser OptionalPlayerRole(string argName)
    {
        return new PlayerRoleArgParser(argName, api, isMandatoryArg: false);
    }

    public PrivilegeArgParser Privilege(string privilege)
    {
        return new PrivilegeArgParser(privilege, api, isMandatoryArg: true);
    }

    public PrivilegeArgParser OptionalPrivilege(string privilege)
    {
        return new PrivilegeArgParser(privilege, api, isMandatoryArg: false);
    }

    public WordArgParser Word(string argName)
    {
        return new WordArgParser(argName, isMandatoryArg: true);
    }

    public WordArgParser OptionalWord(string argName)
    {
        return new WordArgParser(argName, isMandatoryArg: false);
    }

    public WordRangeArgParser OptionalWordRange(string argName, params string[] words)
    {
        return new WordRangeArgParser(argName, isMandatoryArg: false, words);
    }

    public WordArgParser Word(string argName, string[] wordSuggestions)
    {
        return new WordArgParser(argName, isMandatoryArg: true, wordSuggestions);
    }

    //
    // Summary:
    //     Parses a string which is either a color name or a hex value as a System.Drawing.Color
    //
    //
    // Parameters:
    //   argName:
    public ColorArgParser Color(string argName)
    {
        return new ColorArgParser(argName, isMandatoryArg: true);
    }

    //
    // Summary:
    //     Parses a string which is either a color name or a hex value as a System.Drawing.Color
    //
    //
    // Parameters:
    //   argName:
    public ColorArgParser OptionalColor(string argName)
    {
        return new ColorArgParser(argName, isMandatoryArg: false);
    }

    //
    // Summary:
    //     All remaining arguments together
    //
    // Parameters:
    //   argName:
    public StringArgParser All(string argName)
    {
        return new StringArgParser(argName, isMandatoryArg: true);
    }

    //
    // Summary:
    //     All remaining arguments together
    //
    // Parameters:
    //   argName:
    public StringArgParser OptionalAll(string argName)
    {
        return new StringArgParser(argName, isMandatoryArg: false);
    }

    public WordRangeArgParser WordRange(string argName, params string[] words)
    {
        return new WordRangeArgParser(argName, isMandatoryArg: true, words);
    }

    public WorldPositionArgParser WorldPosition(string argName)
    {
        return new WorldPositionArgParser(argName, api, isMandatoryArg: true);
    }

    public WorldPosition2DArgParser WorldPosition2D(string argName)
    {
        return new WorldPosition2DArgParser(argName, api, isMandatoryArg: true);
    }

    public Vec3iArgParser Vec3i(string argName)
    {
        return new Vec3iArgParser(argName, api, isMandatoryArg: true);
    }

    public Vec3iArgParser OptionalVec3i(string argName)
    {
        return new Vec3iArgParser(argName, api, isMandatoryArg: true);
    }

    public CollectibleArgParser Item(string argName)
    {
        return new CollectibleArgParser(argName, api, EnumItemClass.Item, isMandatoryArg: true);
    }

    public CollectibleArgParser Block(string argName)
    {
        return new CollectibleArgParser(argName, api, EnumItemClass.Block, isMandatoryArg: true);
    }

    //
    // Summary:
    //     Defaults to caller position
    //
    // Parameters:
    //   argName:
    public WorldPositionArgParser OptionalWorldPosition(string argName)
    {
        return new WorldPositionArgParser(argName, api, isMandatoryArg: false);
    }

    //
    // Summary:
    //     Currently only supports time spans (i.e. now + time)
    //
    // Parameters:
    //   argName:
    public DatetimeArgParser DateTime(string argName)
    {
        return new DatetimeArgParser(argName, isMandatoryArg: true);
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
