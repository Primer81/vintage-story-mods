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

[HarmonyPatchCategory("togglemousecontrol")]
[HarmonyPatch(typeof(GuiCompositeSettings))]
[HarmonyPatch("OnAccessibilityOptions")]
public static class GuiCompositeSettings_OnAccessibilityOptions
{
    private static MethodInfo arrayEmptyMethod;
    private static MethodInfo langGetMethod;
    private static MethodInfo whiteSmallishTextMethod;
    private static MethodInfo whiteSmallTextMethod;
    private static MethodInfo flatCopyMethod;
    private static MethodInfo belowCopyMethod;
    private static MethodInfo withFixedWidthMethod;
    private static MethodInfo withFixedHeightMethod;
    private static MethodInfo addStaticTextMethod;
    private static MethodInfo addHoverTextMethod;
    private static MethodInfo addSwitchMethod;
    private static MethodInfo loadComposerMethod;
    private static MethodInfo onAutoMouseToggleChangedMethod;

    private static MethodInfo getSwitchMethod;
    private static FieldInfo configField;
    private static FieldInfo guiAutoToggleMouseControlField;
    private static FieldInfo composerField;
    private static FieldInfo switchOnField;

    public static void onAutoMouseToggleChanged(bool on)
    {
        ToggleMouseControlModSystem.Config.GuiAutoToggleMouseControl = on;
        ToggleMouseControlModSystem.Dump(ToggleMouseControlModSystem.Config);
        if (ToggleMouseControlModSystem.IsMouseControlToggledOn())
        {
            ToggleMouseControlModSystem.ToggleMouseControl();
        }
    }

    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        bool success = true;
        var codes = new List<CodeInstruction>(instructions);
        int idxToAddGuiSetting = -1;
        int idxToInitGuiSetting = -1;
        if (success)
        {
            success = LoadDependentReflectionData();
        }
        // Find the instruction index at which to insert
        if (success)
        {
            idxToAddGuiSetting =
                FindAutoToggleMouseSettingInstructionIndex(codes);
            success = idxToAddGuiSetting > 0;
        }
        if (success)
        {
            idxToInitGuiSetting =
                FindSettingInitializationInstructionIndex(codes);
            success = idxToInitGuiSetting > 0;
        }
        // If we found the sprint toggle, insert our new toggle before it
        if (success)
        {
            AddAutoToggleMouseSetting(codes, idxToAddGuiSetting);
            InitAutoToggleMouseSetting(codes, idxToInitGuiSetting);
        }
        // Return the modified instructions
        foreach (var code in codes)
        {
            yield return code;
        }
    }

    public static bool LoadDependentReflectionData()
    {
        bool success = true;
        arrayEmptyMethod = AccessTools.Method(
            typeof(Array), "Empty").MakeGenericMethod(typeof(object));
        success = success && arrayEmptyMethod != null;
        langGetMethod = AccessTools.Method(
            typeof(Lang), "Get", new[] {
                typeof(string), typeof(object[])
            });
        success = success && langGetMethod != null;
        whiteSmallishTextMethod = AccessTools.Method(
            typeof(CairoFont), "WhiteSmallishText");
        success = success && whiteSmallishTextMethod != null;
        whiteSmallTextMethod = AccessTools.Method(
            typeof(CairoFont), "WhiteSmallText");
        success = success && whiteSmallTextMethod != null;
        flatCopyMethod = AccessTools.Method(
            typeof(ElementBounds), "FlatCopy");
        success = success && flatCopyMethod != null;
        belowCopyMethod = AccessTools.Method(
            typeof(ElementBounds), "BelowCopy", new[] {
                typeof(double), typeof(double),
                typeof(double), typeof(double)
            });
        success = success && belowCopyMethod != null;
        withFixedWidthMethod = AccessTools.Method(
            typeof(ElementBounds), "WithFixedWidth", new[] {
                typeof(double)
            });
        success = success && withFixedWidthMethod != null;
        withFixedHeightMethod = AccessTools.Method(
            typeof(ElementBounds), "WithFixedHeight", new[] {
                typeof(double)
            });
        success = success && withFixedHeightMethod != null;
        addStaticTextMethod = AccessTools.Method(
            typeof(Vintagestory.API.Client.GuiComposerHelpers), "AddStaticText",
            new[] {
                typeof(GuiComposer), typeof(string), typeof(CairoFont),
                typeof(ElementBounds), typeof(string)
            });
        success = success && addStaticTextMethod != null;
        addHoverTextMethod = AccessTools.Method(
            typeof(Vintagestory.API.Client.GuiComposerHelpers), "AddHoverText",
            new[] {
                typeof(GuiComposer), typeof(string), typeof(CairoFont), typeof(int),
                typeof(ElementBounds), typeof(string)
            });
        success = success && addHoverTextMethod != null;
        addSwitchMethod = AccessTools.Method(
            typeof(Vintagestory.API.Client.GuiComposerHelpers), "AddSwitch",
            new[] {
                typeof(GuiComposer), typeof(Action<bool>),
                typeof(ElementBounds), typeof(string),
                typeof(double), typeof(double)
            });
        success = success && addSwitchMethod != null;
        loadComposerMethod = AccessTools.Method(
            typeof(IGuiCompositeHandler), "LoadComposer",
            new[] { typeof(GuiComposer) });
        success = success && loadComposerMethod != null;
        onAutoMouseToggleChangedMethod = AccessTools.Method(
            typeof(GuiCompositeSettings_OnAccessibilityOptions),
            "onAutoMouseToggleChanged");
        success = success && onAutoMouseToggleChangedMethod != null;
        getSwitchMethod = AccessTools.Method(
            typeof(Vintagestory.API.Client.GuiComposerHelpers), "GetSwitch",
            new[] { typeof(GuiComposer), typeof(string) });
        success = success && getSwitchMethod != null;
        configField = AccessTools.Field(
            typeof(ToggleMouseControlModSystem), "Config");
        success = success && configField != null;
        guiAutoToggleMouseControlField = AccessTools.Field(
            typeof(Config), "GuiAutoToggleMouseControl");
        success = success && guiAutoToggleMouseControlField != null;
        composerField = AccessTools.Field(
            typeof(GuiCompositeSettings), "composer");
        success = success && composerField != null;
        switchOnField = AccessTools.Field(typeof(GuiElementSwitch), "On");
        success = success && switchOnField != null;
        return success;
    }

    public static int FindAutoToggleMouseSettingInstructionIndex(List<CodeInstruction> codes)
    {
        int instructionHookIndex = -1;
        const string operandHookName = "setting-name-bobblehead";

        // Find code instruction index at which to insert the static
        // text, hover text, and switch callback code instructions
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].operand is string operand && operand.Equals(operandHookName))
            {
                instructionHookIndex = i;
                break;
            }
        }
        return instructionHookIndex;
    }

    public static int FindSettingInitializationInstructionIndex(List<CodeInstruction> codes)
    {
        int instructionHookIndex = -1;

        // Search for the LoadComposer method call from the end of the method
        // This is typically the last significant operation in the method
        for (int i = codes.Count - 1; i >= 0; --i)
        {
            if (codes[i].opcode == OpCodes.Callvirt &&
                codes[i].operand is MethodInfo method &&
                method == loadComposerMethod)
            {
                // We found the method call, now we need to find where the arguments start
                // For a method call like handler.LoadComposer(composer), we need:
                // 1. The 'handler' instance (this.handler or just handler)
                // 2. The 'composer' argument

                // Start from the method call and work backwards to find where the argument loading begins
                int j = i - 1;

                // Skip past any conversion or preparation instructions
                while (j >= 0 && !IsArgumentLoadInstruction(codes[j]))
                {
                    j--;
                }

                // Now j points to the last argument load instruction
                // We need to find the first argument load instruction
                while (j >= 0 && IsArgumentLoadInstruction(codes[j]))
                {
                    j--;
                }

                // j+1 is now the index of the first instruction that loads arguments
                instructionHookIndex = j + 1;
                break;
            }
        }

        return instructionHookIndex;
    }

    public static void AddAutoToggleMouseSetting(
        List<CodeInstruction> codes, int instructionHookIndex)
    {
        const string operandSettingNameAutoToggleMouse =
            "togglemousecontrol:setting-name-autotogglemouse";
        const string operandSettingHoverAutoToggleMouse =
            "togglemousecontrol:setting-hover-autotogglemouse";

        // Insert our own static text for the new switch
        var newInstructions = new List<CodeInstruction>
        {
            // Load the string for our toggle label
            new CodeInstruction(OpCodes.Ldstr, operandSettingNameAutoToggleMouse),

            // Call Lang.Get(string, object[])
            new CodeInstruction(OpCodes.Call, arrayEmptyMethod),
            new CodeInstruction(OpCodes.Call, langGetMethod),

            // Get the font
            new CodeInstruction(OpCodes.Call, whiteSmallishTextMethod),

            // Load leftText variable
            new CodeInstruction(OpCodes.Ldloc_0),

            // Create a copy of the bounds below the previous element
            new CodeInstruction(OpCodes.Ldc_R8, 0.0),
            new CodeInstruction(OpCodes.Ldc_R8, 2.0),
            new CodeInstruction(OpCodes.Ldc_R8, 0.0),
            new CodeInstruction(OpCodes.Ldc_R8, 0.0),
            new CodeInstruction(OpCodes.Callvirt, belowCopyMethod),

            // Duplicate the bounds for assignment to leftText
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Stloc_0),

            // Last parameter (null)
            new CodeInstruction(OpCodes.Ldnull),

            // Call AddStaticText - this is an extension method so
            // it's a regular call, not callvirt
            new CodeInstruction(OpCodes.Call, addStaticTextMethod)
        };

        // Insert our own hover text for the new switch
        newInstructions.AddRange(new List<CodeInstruction>
        {
            // Load the string for our toggle label
            new CodeInstruction(OpCodes.Ldstr, operandSettingHoverAutoToggleMouse),

            // Call Lang.Get(string, object[])
            new CodeInstruction(OpCodes.Call, arrayEmptyMethod),
            new CodeInstruction(OpCodes.Call, langGetMethod),

            // Get the font
            new CodeInstruction(OpCodes.Call, whiteSmallTextMethod),

            // Load constant integer
            new CodeInstruction(OpCodes.Ldc_I4, 250),

            // Load leftText variable
            new CodeInstruction(OpCodes.Ldloc_0),

            // Create a flat copy of the leftText variable
            new CodeInstruction(OpCodes.Callvirt, flatCopyMethod),

            // Set fixed height
            new CodeInstruction(OpCodes.Ldc_R8, 25.0),
            new CodeInstruction(OpCodes.Callvirt, withFixedHeightMethod),

            // Last parameter (null)
            new CodeInstruction(OpCodes.Ldnull),

            // Call AddHoverText - this is an extension method so
            // it's a regular call, not callvirt
            new CodeInstruction(OpCodes.Call, addHoverTextMethod)
        });

        // Insert our own switch with our own callback
        newInstructions.AddRange(new List<CodeInstruction>
        {
            // Create delegate for our toggle change event
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Ldftn, onAutoMouseToggleChangedMethod),
            new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(
                typeof(Action<bool>), new[] { typeof(object), typeof(IntPtr) })),

            // Load rightSlider variable and create a flat copy
            new CodeInstruction(OpCodes.Ldloc_1), // Assuming rightSlider is stored in local variable 1

            // Create a copy of the bounds below the previous element
            new CodeInstruction(OpCodes.Ldc_R8, 0.0),
            new CodeInstruction(OpCodes.Ldc_R8, 20.0),
            new CodeInstruction(OpCodes.Ldc_R8, 0.0),
            new CodeInstruction(OpCodes.Ldc_R8, 0.0),
            new CodeInstruction(OpCodes.Callvirt, belowCopyMethod),

            // Duplicate the bounds for assignment to rightSlider
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Stloc_1),

            // Load the key for the switch
            new CodeInstruction(OpCodes.Ldstr, "autoMouseToggleSwitch"),

            // Load the actual default values from the source code
            new CodeInstruction(OpCodes.Ldc_R8, 30.0), // Size default is 30.0
            new CodeInstruction(OpCodes.Ldc_R8, 4.0),  // Padding default is 4.0

            // Call AddSwitch
            new CodeInstruction(OpCodes.Call, addSwitchMethod)
        });

        // Insert our new instructions
        codes.InsertRange(instructionHookIndex, newInstructions);
    }

    public static void InitAutoToggleMouseSetting(
        List<CodeInstruction> codes, int instructionHookIndex)
    {
        // Insert code to initialize the switch
        var newInstructions = new List<CodeInstruction>
        {
            // TODO: Insert code instructions to reproduce the following line of code:
            // composer.GetSwitch("autoMouseToggleSwitch").On =
            //     ToggleMouseControlModSystem.Config.GuiAutoToggleMouseControl;
            // Load the composer field from 'this'
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, composerField),

            // Call GetSwitch("autoMouseToggleSwitch")
            // Since GetSwitch is an extension method, we need to pass the composer as the first argument
            new CodeInstruction(OpCodes.Ldstr, "autoMouseToggleSwitch"),
            new CodeInstruction(OpCodes.Call, getSwitchMethod),

            // Get ToggleMouseControlModSystem.Config (static field)
            new CodeInstruction(OpCodes.Ldsfld, configField),

            // Get Config.GuiAutoToggleMouseControl (instance field)
            new CodeInstruction(OpCodes.Ldfld, guiAutoToggleMouseControlField),

            // Set the On field of the switch
            new CodeInstruction(OpCodes.Stfld, switchOnField)
        };

        // Insert our new instructions
        codes.InsertRange(instructionHookIndex, newInstructions);
    }

    // Helper method to identify instructions that load method arguments
    private static bool IsArgumentLoadInstruction(CodeInstruction instruction)
    {
        // Common opcodes used to load arguments
        return instruction.opcode == OpCodes.Ldloc_0 ||
            instruction.opcode == OpCodes.Ldloc_1 ||
            instruction.opcode == OpCodes.Ldloc_2 ||
            instruction.opcode == OpCodes.Ldloc_3 ||
            instruction.opcode == OpCodes.Ldloc_S ||
            instruction.opcode == OpCodes.Ldloc ||
            instruction.opcode == OpCodes.Ldarg_0 ||
            instruction.opcode == OpCodes.Ldarg_1 ||
            instruction.opcode == OpCodes.Ldarg_2 ||
            instruction.opcode == OpCodes.Ldarg_3 ||
            instruction.opcode == OpCodes.Ldarg_S ||
            instruction.opcode == OpCodes.Ldarg ||
            instruction.opcode == OpCodes.Ldfld;
    }
}