#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class EntitiesArgParser : ArgumentParserBase
{
    private Entity[] entities;

    private ICoreAPI api;

    public EntitiesArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
        : base(argName, isMandatoryArg)
    {
        this.api = api;
    }

    public override string GetSyntaxExplanation(string indent)
    {
        return indent + GetSyntax() + " is either a player name, or else one of the following selection codes:\n" + indent + "  s[] for self\n" + indent + "  l[] for the entity currently looked at\n" + indent + "  p[] for all players\n" + indent + "  e[] for all entities.\n" + indent + "  Inside the square brackets, one or more filters can be added, to be more selective.  Filters include name, type, class, alive, range.  For example, <code>e[type=gazelle,range=3,alive=true]</code>.  The filters minx/miny/minz/maxx/maxy/maxz can also be used to specify a volume to search, coordinates are relative to the command caller's position.\n" + indent + "  This argument may be omitted if the remainder of the command makes sense, in which case it will be interpreted as self.";
    }

    public override object GetValue()
    {
        return entities;
    }

    public override void SetValue(object data)
    {
        entities = (Entity[])data;
    }

    public override void PreProcess(TextCommandCallingArgs args)
    {
        entities = null;
        base.PreProcess(args);
        if (base.IsMissing)
        {
            entities = new Entity[1] { args.Caller.Entity };
        }
    }

    public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
    {
        string maybeplayername = args.RawArgs.PeekWord();
        IPlayer player = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName.Equals(maybeplayername, StringComparison.InvariantCultureIgnoreCase));
        if (player != null)
        {
            args.RawArgs.PopWord();
            entities = new Entity[1] { player.Entity };
            return EnumParseResult.Good;
        }

        string text = maybeplayername;
        char c = ((text != null && text.Length > 0) ? maybeplayername[0] : ' ');
        if (c != 'p' && c != 'e' && c != 'l' && c != 's')
        {
            lastErrorMessage = Lang.Get("Not a player name and not a selector p, e, l or s: {0}'", maybeplayername);
            entities = new Entity[1] { args.Caller.Entity };
            return EnumParseResult.DependsOnSubsequent;
        }

        c = args.RawArgs.PopChar().GetValueOrDefault(' ');
        Dictionary<string, string> dictionary;
        if (args.RawArgs.PeekChar() == '[')
        {
            string parseErrorMsg;
            string strargs = args.RawArgs.PopCodeBlock('[', ']', out parseErrorMsg);
            if (parseErrorMsg != null)
            {
                lastErrorMessage = parseErrorMsg;
                return EnumParseResult.Bad;
            }

            dictionary = parseSubArgs(strargs);
        }
        else
        {
            if (args.RawArgs.PeekChar() != ' ')
            {
                lastErrorMessage = "Invalid selector, needs to be p,e,l,s followed by [";
                return EnumParseResult.Bad;
            }

            args.RawArgs.PopWord();
            dictionary = new Dictionary<string, string>();
        }

        Vec3d sourcePos = args.Caller.Pos;
        Entity entity = args.Caller.Entity;
        float? range = null;
        if (dictionary.TryGetValue("range", out var value))
        {
            range = value.ToFloat();
            dictionary.Remove("range");
        }

        AssetLocation type = null;
        if (dictionary.TryGetValue("type", out var value2))
        {
            type = new AssetLocation(value2);
            dictionary.Remove("type");
        }

        string classstr = null;
        if (dictionary.TryGetValue("class", out classstr))
        {
            classstr = classstr.ToLowerInvariant();
            dictionary.Remove("class");
        }

        string name = null;
        if (dictionary.TryGetValue("name", out name))
        {
            dictionary.Remove("name");
        }

        bool? alive = null;
        if (dictionary.TryGetValue("alive", out var value3))
        {
            alive = value3.ToBool();
            dictionary.Remove("alive");
        }

        long? id = null;
        if (dictionary.TryGetValue("id", out var value4))
        {
            id = value4.ToLong(0L);
            dictionary.Remove("id");
        }

        Cuboidi box = null;
        if (sourcePos != null)
        {
            bool flag = false;
            string[] array = new string[6] { "minx", "miny", "minz", "maxx", "maxy", "maxz" };
            int[] array2 = new int[6];
            for (int i = 0; i < array.Length; i++)
            {
                if (dictionary.TryGetValue(array[i], out var value5))
                {
                    array2[i] = value5.ToInt() + i / 3;
                    dictionary.Remove(array[i]);
                    flag = true;
                }
            }

            if (flag)
            {
                BlockPos asBlockPos = sourcePos.AsBlockPos;
                box = new Cuboidi(array2).Translate(asBlockPos.X, asBlockPos.Y, asBlockPos.Z);
            }
        }

        if (dictionary.Count > 0)
        {
            lastErrorMessage = "Unknown selector '" + string.Join(", ", dictionary.Keys) + "'";
            return EnumParseResult.Bad;
        }

        List<Entity> list = new List<Entity>();
        if (range.HasValue && sourcePos == null)
        {
            lastErrorMessage = "Can't use range argument without source pos";
            return EnumParseResult.Bad;
        }

        switch (c)
        {
            case 'p':
                {
                    IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
                    foreach (IPlayer player2 in allOnlinePlayers)
                    {
                        if (entityMatches(player2.Entity, sourcePos, type, classstr, range, box, name, alive, id))
                        {
                            list.Add(player2.Entity);
                        }
                    }

                    entities = list.ToArray();
                    return EnumParseResult.Good;
                }
            case 'e':
                if (!range.HasValue)
                {
                    ICollection<Entity> collection = ((api.Side != EnumAppSide.Server) ? (api as ICoreClientAPI).World.LoadedEntities.Values : (api as ICoreServerAPI).World.LoadedEntities.Values);
                    foreach (Entity item in collection)
                    {
                        if (entityMatches(item, sourcePos, type, classstr, range, box, name, alive, id))
                        {
                            list.Add(item);
                        }
                    }

                    entities = list.ToArray();
                }
                else
                {
                    float num = range.Value;
                    entities = api.World.GetEntitiesAround(sourcePos, num, num, (Entity e) => entityMatches(e, sourcePos, type, classstr, range, box, name, alive, id));
                }

                return EnumParseResult.Good;
            case 'l':
                {
                    if (!(entity is EntityPlayer entityPlayer))
                    {
                        lastErrorMessage = "Can't use 'l' without source player";
                        return EnumParseResult.Bad;
                    }

                    if (entityPlayer.Player.CurrentEntitySelection == null)
                    {
                        lastErrorMessage = "Not looking at an entity";
                        return EnumParseResult.Bad;
                    }

                    Entity entity2 = entityPlayer.Player.CurrentEntitySelection.Entity;
                    if (entityMatches(entity2, sourcePos, type, classstr, range, box, name, alive, id))
                    {
                        entities = new Entity[1] { entity2 };
                    }
                    else
                    {
                        entities = new Entity[0];
                    }

                    return EnumParseResult.Good;
                }
            case 's':
                if (entityMatches(entity, sourcePos, type, classstr, range, box, name, alive, id))
                {
                    entities = new Entity[1] { entity };
                }
                else
                {
                    entities = new Entity[0];
                }

                return EnumParseResult.Good;
            default:
                lastErrorMessage = "Wrong selector, needs to be a player name or p,e,l or s";
                return EnumParseResult.Bad;
        }
    }

    private bool entityMatches(Entity e, Vec3d sourcePos, AssetLocation type, string classstr, float? range, Cuboidi box, string name, bool? alive, long? id)
    {
        if (id.HasValue && e.EntityId != id)
        {
            return false;
        }

        if (range.HasValue && e.SidedPos.DistanceTo(sourcePos) > (double?)range)
        {
            return false;
        }

        if (box != null && !box.ContainsOrTouches(e.SidedPos))
        {
            return false;
        }

        if (classstr != null && classstr != e.Class.ToLowerInvariant())
        {
            return false;
        }

        if (type != null && !WildcardUtil.Match(type, e.Code))
        {
            return false;
        }

        if (alive.HasValue && e.Alive != alive)
        {
            return false;
        }

        if (name != null && !WildcardUtil.Match(name, e.GetName()))
        {
            return false;
        }

        return true;
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
