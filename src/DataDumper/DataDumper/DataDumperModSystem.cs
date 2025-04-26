using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.Gui;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Vintagestory.Client.NoObf;
using Vintagestory.Client;
using System;
using System.Linq;

namespace DataDumper;

public class DataDumperModSystem : ModSystem
{
    static ICoreClientAPI clientApi;
    static private Harmony harmony;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        clientApi.Event.RegisterGameTickListener(OnGameTickDump, 10000);
        // Apply harmony patches
        {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchCategory(Mod.Info.ModID);
        }
    }

    public static void Dump(object data, string suffix = "")
    {
        clientApi.StoreModConfig(
            data,
            Path.Combine(
                $"{nameof(DataDumper)}",
                $"{data.GetType().Name}{suffix}.json"));
    }

    private static void OnGameTickDump(float dt)
    {
        Dump(new HotKeysDump(clientApi.Input.HotKeys));
        Dump(new LoadedGuisDump(clientApi.Gui.LoadedGuis));
        Dump(new KeyCodesDump());
    }
}

class HotKeyDump
{
    public string Name;
    public string Code;
    public string DefaultMapping;
    public bool TriggerOnUpAlso;
    public string KeyCombinationType;

    public HotKeyDump(HotKey hotKey)
    {
        Name = hotKey.Name;
        Code = hotKey.Code;
        DefaultMapping = hotKey.DefaultMapping.ToString();
        TriggerOnUpAlso = hotKey.TriggerOnUpAlso;
        KeyCombinationType = hotKey.KeyCombinationType.ToString();
    }
}

class HotKeysDump
{
    public List<HotKeyDump> HotKeys = new List<HotKeyDump>();

    public HotKeysDump(OrderedDictionary<string, HotKey> hotKeys)
    {
        foreach (var hotKey in  hotKeys)
        {
            HotKeys.Add(new HotKeyDump(hotKey.Value));
        }
    }
}

class GuiDialogDump
{
    public string DebugName;
    public string DialogType;
    public bool DisableMouseGrab;
    public double DrawOrder;
    public bool Focusable;
    public double InputOrder;
    public bool PrefersUngrabbedMouse;
    public string ToggleKeyCombinationCode;
    public bool UnregisterOnClose;
    public float ZSize;

    public GuiDialogDump(GuiDialog guiDialog)
    {
        DebugName = guiDialog.DebugName;
        DialogType = guiDialog.DialogType.ToString();
        DisableMouseGrab = guiDialog.DisableMouseGrab;
        DrawOrder = guiDialog.DrawOrder;
        Focusable = guiDialog.Focusable;
        InputOrder = guiDialog.InputOrder;
        PrefersUngrabbedMouse = guiDialog.PrefersUngrabbedMouse;
        ToggleKeyCombinationCode = guiDialog.ToggleKeyCombinationCode;
        UnregisterOnClose = guiDialog.UnregisterOnClose;
        ZSize = guiDialog.ZSize;
    }
}

class LoadedGuisDump
{
    public List<GuiDialogDump> LoadedGuis;

    public LoadedGuisDump(List<GuiDialog> loadedGuis)
    {
        LoadedGuis = new List<GuiDialogDump>();
        foreach (var loadedGui in loadedGuis)
        {
            LoadedGuis.Add(new GuiDialogDump(loadedGui));
        }
    }
}

class GuiComposerDump
{
    public bool Composed;
    public string DialogName;
    public bool Enabled;
    public string MouseOverCursor;
    public bool Tabbable;
    public ElementBounds Bounds;
    public ElementBounds CurParentBounds;
    public int CurrentElementKey;
    public string CurrentTabIndexElement;
    public string FirstTabbableElement;
    public string LastAddedElement;
    public ElementBounds LastAddedElementBounds;
    public int MaxTabIndex;

    public GuiComposerDump(GuiComposer guiComposer)
    {
        Composed = guiComposer.Composed;
        DialogName = guiComposer.DialogName;
        Enabled = guiComposer.Enabled;
        MouseOverCursor = guiComposer.MouseOverCursor;
        Tabbable = guiComposer.Tabbable;
        Bounds = guiComposer.Bounds;
        CurParentBounds = guiComposer.CurParentBounds;
        CurrentElementKey = guiComposer.CurrentElementKey;
        CurrentTabIndexElement = guiComposer.CurrentTabIndexElement.ToString();
        FirstTabbableElement = guiComposer.FirstTabbableElement.ToString();
        LastAddedElement = guiComposer.LastAddedElement.ToString();
        LastAddedElementBounds = guiComposer.LastAddedElementBounds;
        MaxTabIndex = guiComposer.MaxTabIndex;
    }
}

class ComposersDump
{
    Dictionary<string, GuiComposerDump> Composers =
        new Dictionary<string, GuiComposerDump>();

    public ComposersDump(Dictionary<string, GuiComposer> composers)
    {
        Composers = new Dictionary<string, GuiComposerDump>();
        foreach (var composer in composers)
        {
            Composers.Add(composer.Key, new GuiComposerDump(composer.Value));
        }
    }
}

class KeyCodesDump
{
    public Dictionary<string, int> KeyCodes;

    public KeyCodesDump()
    {
        KeyCodes = new Dictionary<string, int>();
        foreach (GlKeys key in Enum.GetValues(typeof(GlKeys)))
        {
            if (!KeyCodes.ContainsKey(key.ToString()))
            {
                KeyCodes.Add(key.ToString(), (int)key);
            }
        }
    }
}

public class OpCodeDump
{
    public string? Name;
    public int Size;
    public short Value;

    public OpCodeDump(System.Reflection.Emit.OpCode opCode)
    {
        Name = opCode.Name;
        Size = opCode.Size;
        Value = opCode.Value;
    }
}

public class CodeInstructionDump
{
    public OpCodeDump OpCode;
    public string? Operand;
    public List<string> Labels;

    public CodeInstructionDump(CodeInstruction instruction)
    {
        OpCode = new OpCodeDump(instruction.opcode);
        Operand = instruction.operand?.ToString();
        Labels = new List<string>();
        foreach (System.Reflection.Emit.Label label in instruction.labels)
        {
            Labels.Add(label.ToString());
        }
    }
}

public class CodeInstructionsDump
{
    public List<CodeInstructionDump> Instructions;

    public CodeInstructionsDump(List<CodeInstruction> instructions)
    {
        Instructions = new List<CodeInstructionDump>();
        foreach (CodeInstruction instruction in instructions)
        {
            Instructions.Add(new CodeInstructionDump(instruction));
        }
    }
}

[HarmonyPatchCategory("datadumper")]
[HarmonyPatch(typeof(GuiCompositeSettings))]
[HarmonyPatch("OnAccessibilityOptions")]
public static class GuiCompositeSettings_OnAccessibilityOptions
{
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instructionsList = instructions.ToList();
        var codeInstructionsDump = new CodeInstructionsDump(instructionsList);
        DataDumperModSystem.Dump(
            codeInstructionsDump,
            "_" + nameof(GuiCompositeSettings_OnAccessibilityOptions));
        return instructionsList;
    }
}