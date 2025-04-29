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

public class Config
{
    public bool GuiAutoToggleMouseControl;
    public Dictionary<string, GuiConfig> GuiConfigs;

    public Config()
    {
        GuiAutoToggleMouseControl = true;
        GuiConfigs = new Dictionary<string, GuiConfig>();
    }
}

public class GuiConfig
{
    public bool AutoToggleMouse;
    public bool TreatAsImmersive;

    public GuiConfig()
    {
        AutoToggleMouse = true;
        TreatAsImmersive = false;
    }

    public GuiConfig(GuiDialog dlg)
    {
        AutoToggleMouse = true;
        TreatAsImmersive = !dlg.PrefersUngrabbedMouse;
    }
}