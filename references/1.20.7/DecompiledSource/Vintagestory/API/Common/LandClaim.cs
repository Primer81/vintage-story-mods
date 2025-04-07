#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[ProtoContract]
public class LandClaim
{
    [ProtoMember(1)]
    public List<Cuboidi> Areas = new List<Cuboidi>();

    [ProtoMember(2)]
    public int ProtectionLevel;

    [ProtoMember(3)]
    public long OwnedByEntityId;

    [ProtoMember(4)]
    public string OwnedByPlayerUid;

    [ProtoMember(5)]
    public uint OwnedByPlayerGroupUid;

    [ProtoMember(6)]
    public string LastKnownOwnerName;

    [ProtoMember(7)]
    public string Description;

    //
    // Summary:
    //     Other groups allowed to use this land
    [ProtoMember(8)]
    public Dictionary<int, EnumBlockAccessFlags> PermittedPlayerGroupIds = new Dictionary<int, EnumBlockAccessFlags>();

    //
    // Summary:
    //     Other players allowed to use this land
    [ProtoMember(9)]
    public Dictionary<string, EnumBlockAccessFlags> PermittedPlayerUids = new Dictionary<string, EnumBlockAccessFlags>();

    //
    // Summary:
    //     Other players allowed to use this land, name of the player at the time the privilege
    //     was granted
    [ProtoMember(10)]
    public Dictionary<string, string> PermittedPlayerLastKnownPlayerName = new Dictionary<string, string>();

    [ProtoMember(11)]
    public bool AllowUseEveryone;

    public BlockPos Center
    {
        get
        {
            if (Areas.Count == 0)
            {
                return new BlockPos(0, 0, 0);
            }

            Vec3d vec3d = new Vec3d();
            int num = 0;
            foreach (Cuboidi area in Areas)
            {
                _ = area.Center;
                num += area.SizeXYZ;
            }

            foreach (Cuboidi area2 in Areas)
            {
                Vec3i center = area2.Center;
                Vec3d vec3d2 = new Vec3d(center.X, center.Y, center.Z);
                vec3d += vec3d2 * ((double)area2.SizeXYZ / (double)num);
            }

            return new BlockPos((int)(vec3d.X / (double)Areas.Count), (int)(vec3d.Y / (double)Areas.Count), (int)(vec3d.Z / (double)Areas.Count));
        }
    }

    public int SizeXZ
    {
        get
        {
            int num = 0;
            foreach (Cuboidi area in Areas)
            {
                _ = area.Center;
                num += area.SizeXZ;
            }

            return num;
        }
    }

    public int SizeXYZ
    {
        get
        {
            int num = 0;
            foreach (Cuboidi area in Areas)
            {
                _ = area.Center;
                num += area.SizeXYZ;
            }

            return num;
        }
    }

    public static LandClaim CreateClaim(IPlayer player, int protectionLevel = 1)
    {
        return new LandClaim
        {
            OwnedByPlayerUid = player.PlayerUID,
            ProtectionLevel = protectionLevel,
            LastKnownOwnerName = Lang.Get("Player " + player.PlayerName)
        };
    }

    public static LandClaim CreateClaim(EntityAgent entity, int protectionLevel = 1)
    {
        string name = entity.GetName();
        return new LandClaim
        {
            OwnedByEntityId = entity.EntityId,
            ProtectionLevel = protectionLevel,
            LastKnownOwnerName = Lang.Get("item-creature-" + entity.Code) + ((name == null) ? "" : (" " + name))
        };
    }

    public static LandClaim CreateClaim(string ownerName, int protectionLevel = 1)
    {
        return new LandClaim
        {
            ProtectionLevel = protectionLevel,
            LastKnownOwnerName = ownerName
        };
    }

    public EnumPlayerAccessResult TestPlayerAccess(IPlayer player, EnumBlockAccessFlags claimFlag)
    {
        if (player.PlayerUID.Equals(OwnedByPlayerUid))
        {
            return EnumPlayerAccessResult.OkOwner;
        }

        if (OwnedByPlayerGroupUid != 0 && player.Groups.Any((PlayerGroupMembership ms) => ms.GroupUid == OwnedByPlayerGroupUid))
        {
            return EnumPlayerAccessResult.OkGroup;
        }

        if (player.Role.PrivilegeLevel > ProtectionLevel && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            return EnumPlayerAccessResult.OkPrivilege;
        }

        if (PermittedPlayerUids.TryGetValue(player.PlayerUID, out var value) && (value & claimFlag) > EnumBlockAccessFlags.None)
        {
            return EnumPlayerAccessResult.OkGrantedPlayer;
        }

        PlayerGroupMembership[] groups = player.Groups;
        foreach (PlayerGroupMembership playerGroupMembership in groups)
        {
            if (PermittedPlayerGroupIds.TryGetValue(playerGroupMembership.GroupUid, out value) && (value & claimFlag) > EnumBlockAccessFlags.None)
            {
                return EnumPlayerAccessResult.OkGrantedGroup;
            }
        }

        return EnumPlayerAccessResult.Denied;
    }

    public bool PositionInside(Vec3d position)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            if (Areas[i].Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    public bool PositionInside(BlockPos position)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            if (Areas[i].Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    public EnumClaimError AddArea(Cuboidi cuboidi)
    {
        if (Areas.Count == 0)
        {
            Areas.Add(cuboidi);
            return EnumClaimError.NoError;
        }

        for (int i = 0; i < Areas.Count; i++)
        {
            if (Areas[i].Intersects(cuboidi))
            {
                return EnumClaimError.Overlapping;
            }
        }

        for (int j = 0; j < Areas.Count; j++)
        {
            if (Areas[j].IsAdjacent(cuboidi))
            {
                Areas.Add(cuboidi);
                return EnumClaimError.NoError;
            }
        }

        return EnumClaimError.NotAdjacent;
    }

    public bool Intersects(Cuboidi cuboidi)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            if (Areas[i].Intersects(cuboidi))
            {
                return true;
            }
        }

        return false;
    }

    //
    // Summary:
    //     Ignores y-values
    //
    // Parameters:
    //   rec:
    public bool Intersects2d(HorRectanglei rec)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            if (Areas[i].Intersects(rec))
            {
                return true;
            }
        }

        return false;
    }

    public LandClaim Clone()
    {
        List<Cuboidi> list = new List<Cuboidi>();
        for (int i = 0; i < Areas.Count; i++)
        {
            list.Add(Areas[i].Clone());
        }

        return new LandClaim
        {
            Areas = list,
            Description = Description,
            LastKnownOwnerName = LastKnownOwnerName,
            OwnedByEntityId = OwnedByEntityId,
            OwnedByPlayerGroupUid = OwnedByPlayerGroupUid,
            OwnedByPlayerUid = OwnedByPlayerUid,
            PermittedPlayerGroupIds = new Dictionary<int, EnumBlockAccessFlags>(PermittedPlayerGroupIds),
            PermittedPlayerUids = new Dictionary<string, EnumBlockAccessFlags>(PermittedPlayerUids),
            ProtectionLevel = ProtectionLevel
        };
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
