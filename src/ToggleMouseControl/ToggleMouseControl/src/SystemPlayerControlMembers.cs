using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using Vintagestory.GameContent;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using System;
using System.Linq;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.Common;

namespace ToggleMouseControl;

static class SystemPlayerControlMembers
{
    public static bool IsValid;

    public static ClientMain game;
    public static int forwardKey;
    public static int backwardKey;
    public static int leftKey;
    public static int rightKey;
    public static int jumpKey;
    public static int sneakKey;
    public static int sprintKey;
    public static int ctrlKey;
    public static int shiftKey;
    public static bool nowFloorSitting;
    public static EntityControls prevControls;
}