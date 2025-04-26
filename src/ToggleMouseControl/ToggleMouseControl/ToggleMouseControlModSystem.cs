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

public class ToggleMouseControlModSystem : ModSystem
{
    static public ICoreClientAPI ClientApi;
    static public bool DialogsWantMouseControlPrev;
    static public int DialogsOpenCountPrev;
    static public Config Config;

    static private Harmony harmony;
    static private bool mouseControlToggledOn;

    private bool triggerOnUpAlsoOriginal = false;
    private bool mouseControlKeyIsPressed = false;

    public override double ExecuteOrder()
    {
        return 1.0;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
        // Initial static state defaults
        DialogsWantMouseControlPrev = false;
        DialogsOpenCountPrev = 0;
        mouseControlToggledOn = false;
        SystemPlayerControlMembers.IsValid = false;
        // Configuration
        {
            Config = Fetch<Config>();
            Dump(Config);
        }
        // Enable mouse toggle
        {
            triggerOnUpAlsoOriginal =
                ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso;
            ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso = true;
            ClientApi.Input.HotKeys["togglemousecontrol"].Handler +=
                OnToggleMouseControlHotkey;
        }
        // Apply harmony patches
        {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchCategory(Mod.Info.ModID);
            GuiDialogPatcher.PatchAllImplementations(harmony);
        }
    }

    public override void Dispose()
    {
        // Store settings
        {
            Dump(Config);
        }
        // Unpatch if possible
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            harmony = null;
        }
        // Restore hotkey
        {
            ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso =
                triggerOnUpAlsoOriginal;
            ClientApi.Input.HotKeys["togglemousecontrol"].Handler -=
                OnToggleMouseControlHotkey;
        }
        base.Dispose();
    }

    private bool OnToggleMouseControlHotkey(KeyCombination keyComb)
    {
        bool isPressed = ClientApi.Input.KeyboardKeyState[keyComb.KeyCode];
        if ((mouseControlKeyIsPressed == false) && (isPressed == true))
        {
            mouseControlKeyIsPressed = true;
            ToggleMouseControl();
        }
        if ((mouseControlKeyIsPressed == true) && (isPressed == false))
        {
            mouseControlKeyIsPressed = false;
        }
        return true;
    }

    public static void ToggleMouseControl()
    {
        mouseControlToggledOn = !mouseControlToggledOn;
    }

    public static bool IsMouseControlToggledOn()
    {
        return mouseControlToggledOn;
    }

    public static void Dump(object data)
    {
        ClientApi.StoreModConfig(
            data,
            System.IO.Path.Combine(
                $"{nameof(ToggleMouseControl)}",
                $"{data.GetType().Name}.json"));
    }

    public static dynamic Fetch<T>() where T : new()
    {
        string path = System.IO.Path.Combine(
            $"{nameof(ToggleMouseControl)}",
            $"{typeof(T).Name}.json");
        try
        {
            // Use reflection to call the generic method with the runtime type
            var method = ClientApi.GetType().GetMethod(
                "LoadModConfig", new[] { typeof(string) });
            var genericMethod = method.MakeGenericMethod(typeof(T));
            return genericMethod.Invoke(ClientApi, new object[] { path });
        }
        catch
        {
            return new T();
        }
    }
}

public class Config
{
    public bool GuiAutoToggleMouseControl;

    public Config()
    {
        GuiAutoToggleMouseControl = false;
    }
}

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
    private static MethodInfo onAutoMouseToggleChangedMethod;

    public static void onAutoMouseToggleChanged(bool on)
    {
        ToggleMouseControlModSystem.Config.GuiAutoToggleMouseControl = on;
    }

    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        bool success = true;
        var codes = new List<CodeInstruction>(instructions);
        int instructionHookIndex = -1;
        // Load methods needed for transpiling
        if (success)
        {
            success = LoadMethods();
        }
        // Find the instruction index at which to insert
        if (success)
        {
            instructionHookIndex = FindAutoToggleMouseSettingInstructionIndex(codes);
            success = instructionHookIndex > 0;
        }
        // If we found the sprint toggle, insert our new toggle before it
        if (success)
        {
            AddAutoToggleMouseSetting(codes, instructionHookIndex);
        }
        // Return the modified instructions
        foreach (var code in codes)
        {
            yield return code;
        }
    }

    public static bool LoadMethods()
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
        onAutoMouseToggleChangedMethod = AccessTools.Method(
            typeof(GuiCompositeSettings_OnAccessibilityOptions),
            "onAutoMouseToggleChanged");
        success = success && onAutoMouseToggleChangedMethod != null;
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

    public static void AddAutoToggleMouseSetting(
        List<CodeInstruction> codes, int instructionHookIndex)
    {
        const string operandSettingNameAutoToggleMouse =
            "togglemousecontrol:setting-name-autotogglemouse";
        const string operandSettingHoverAutoToggleMouse =
            "togglemousecontrol:setting-hover-autotogglemouse";

        // Insert our own static text for the new switch
        var autoToggleMouseInstructions = new List<CodeInstruction>
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
        autoToggleMouseInstructions.AddRange(new List<CodeInstruction>
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
        autoToggleMouseInstructions.AddRange(new List<CodeInstruction>
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
        codes.InsertRange(instructionHookIndex, autoToggleMouseInstructions);
    }
}

[HarmonyPatchCategory("togglemousecontrol")]
internal static class Patches
{
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(SystemPlayerControl), "OnGameTick")]
    public static bool Before_SystemPlayerControl_OnGameTick(
        SystemPlayerControl __instance,
        ClientMain ___game,
        int ___forwardKey,
        int ___backwardKey,
        int ___leftKey,
        int ___rightKey,
        int ___jumpKey,
        int ___sneakKey,
        int ___sprintKey,
        int ___ctrlKey,
        int ___shiftKey,
        bool ___nowFloorSitting,
        EntityControls ___prevControls,
        float dt)
    {
        bool runOriginal = true;
        SystemPlayerControlMembers.game = ___game;
        SystemPlayerControlMembers.forwardKey = ___forwardKey;
        SystemPlayerControlMembers.backwardKey = ___backwardKey;
        SystemPlayerControlMembers.leftKey = ___leftKey;
        SystemPlayerControlMembers.rightKey = ___rightKey;
        SystemPlayerControlMembers.jumpKey = ___jumpKey;
        SystemPlayerControlMembers.sneakKey = ___sneakKey;
        SystemPlayerControlMembers.sprintKey = ___sprintKey;
        SystemPlayerControlMembers.ctrlKey = ___ctrlKey;
        SystemPlayerControlMembers.shiftKey = ___shiftKey;
        SystemPlayerControlMembers.nowFloorSitting = ___nowFloorSitting;
        SystemPlayerControlMembers.prevControls = ___prevControls;
        SystemPlayerControlMembers.IsValid = true;
        return runOriginal;
    }

    private static bool IsInstanceOfPlayerControls(EntityControls instance)
    {
        bool isInstanceOfPlayerControls = false;
        if (SystemPlayerControlMembers.IsValid == true)
        {
            ClientMain game = SystemPlayerControlMembers.game;
            // This logic for retrieving the player controls is based on
            // the same code used in SystemPlayerControls.OnGameTick(...)
            // for retriving the player controls. It should match.
            EntityControls controls =
                (game.EntityPlayer.MountedOn == null)
                ? game.EntityPlayer.Controls
                : game.EntityPlayer.MountedOn.Controls;
            if (ReferenceEquals(instance, controls))
            {
                isInstanceOfPlayerControls = true;
            }
        }
        return isInstanceOfPlayerControls;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityControls), "set_Sprint")]
    public static bool Before_EntityControls_set_Sprint(
        EntityControls __instance, ref bool value)
    {
        bool runOriginal = true;
        if (IsInstanceOfPlayerControls(__instance))
        {
            // This logic for setting the value of Sprint is based on the same
            // logic for setting Sprint in SystemPlayerControl.OnGameTick(...).
            // They should match EXCEPT this code should not depend on the
            // locally calculated `allMovementCaptured` variable.
            bool wasSprint = __instance.Sprint;
            value = SystemPlayerControlMembers.game.KeyboardState[
                        SystemPlayerControlMembers.sprintKey] ||
                    (wasSprint &&
                    __instance.TriesToMove &&
                    ClientSettings.ToggleSprint);
        }
        return runOriginal;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityControls), "set_Sneak")]
    public static bool Before_EntityControls_set_Sneak(
        EntityControls __instance, ref bool value)
    {
        bool runOriginal = true;
        if (IsInstanceOfPlayerControls(__instance))
        {
            // This logic for setting the value of Sneak is based on the same
            // logic for setting Sneak in SystemPlayerControl.OnGameTick(...).
            // They should match EXCEPT this code should not depend on the
            // locally calculated `allMovementCaptured` variable.
            value = SystemPlayerControlMembers.game.KeyboardState[
                SystemPlayerControlMembers.sneakKey];
        }
        return runOriginal;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityControls), "set_Jump")]
    public static bool Before_EntityControls_set_Jump(
        EntityControls __instance, ref bool value)
    {
        bool runOriginal = true;
        if (IsInstanceOfPlayerControls(__instance))
        {
            // This logic for setting the value of Jump is based on the same
            // logic for setting Jump in SystemPlayerControl.OnGameTick(...).
            // They should match EXCEPT this code should not depend on the
            // locally calculated `allMovementCaptured` variable.
            FieldInfo worlddataFieldInfo = typeof(ClientPlayer).GetField("worlddata",
                BindingFlags.NonPublic | BindingFlags.Instance);
            ClientWorldPlayerData worlddata = (ClientWorldPlayerData)worlddataFieldInfo.GetValue(
                SystemPlayerControlMembers.game.player);

            value = SystemPlayerControlMembers.game.KeyboardState[
                SystemPlayerControlMembers.jumpKey] &&
                (SystemPlayerControlMembers.game.EntityPlayer.PrevFrameCanStandUp ||
                    worlddata.NoClip);
        }
        return runOriginal;
    }

    // Stop filter text input from taking focus on handbook open
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiOpened")]
    public static void After_GuiDialogHandbook_OnGuiOpened(
        GuiComposer ___overviewGui)
    {
        ___overviewGui.UnfocusOwnElements();
    }

    // Stops GUIs with scrollbars from intercepting the mouse wheel
    // events when the mouse is grabbed
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiElementScrollbar), "OnMouseWheel")]
    public static bool Before_GuiElementScrollbar_OnMouseWheel(
        GuiElementScrollbar __instance,
        ICoreClientAPI api, MouseWheelEventArgs args)
    {
        bool runOriginal = !ToggleMouseControlModSystem.ClientApi.Input.MouseGrabbed;
        return runOriginal;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientMain), "UpdateFreeMouse")]
    public static bool Before_ClientMain_UpdateFreeMouse(
        ClientMain __instance,
        ref ICoreClientAPI ___api,
        ref bool ___mouseWorldInteractAnyway,
        ref bool ___exitToDisconnectScreen,
        ref bool ___exitToMainMenu)
    {
        bool dialogsWantMouseControl =
            ___api.Gui.OpenedGuis
                .Where((GuiDialog gui) => gui.DialogType == EnumDialogType.Dialog)
                .Any((GuiDialog dlg) => dlg.PrefersUngrabbedMouse) ||
            (__instance.DialogsOpened != 0);
        int dialogsOpenCount = __instance.DialogsOpened;
        bool forceDisableMouseGrab =
            ___api.Gui.OpenedGuis.Any((GuiDialog gui) => gui.DisableMouseGrab) ||
            ___api.IsGamePaused ||
            (___api.World.Player.Entity.Alive == false);
        bool canGrabMouse =
            ScreenManager.Platform.IsFocused &&
            !___exitToDisconnectScreen &&
            !___exitToMainMenu &&
            __instance.BlocksReceivedAndLoaded &&
            !forceDisableMouseGrab;
        if (ToggleMouseControlModSystem.Config.GuiAutoToggleMouseControl == true)
        {
            if ((dialogsWantMouseControl != ToggleMouseControlModSystem.DialogsWantMouseControlPrev) ||
                (dialogsOpenCount > ToggleMouseControlModSystem.DialogsOpenCountPrev))
            {
                if (ToggleMouseControlModSystem.IsMouseControlToggledOn() != dialogsWantMouseControl)
                {
                    ToggleMouseControlModSystem.ToggleMouseControl();
                }
            }
        }
        if (canGrabMouse)
        {
            __instance.MouseGrabbed = !ToggleMouseControlModSystem.IsMouseControlToggledOn();
        }
        else
        {
            __instance.MouseGrabbed = false;
        }
        ___mouseWorldInteractAnyway = !__instance.MouseGrabbed;
        ToggleMouseControlModSystem.DialogsWantMouseControlPrev = dialogsWantMouseControl;
        ToggleMouseControlModSystem.DialogsOpenCountPrev = dialogsOpenCount;
        return false;
    }

    public static void PatchAllImplementationsOfMethodWithPrefixAndPostfix(
        Harmony harmony,
        Type methodParentClass,
        string method,
        Type thisClassType,
        string prefix,
        string postfix)
    {
        // Get the parent method
        MethodInfo parentMethod = AccessTools.Method(methodParentClass, method);
        if (parentMethod == null)
        {
            ToggleMouseControlModSystem.ClientApi.Logger.Error(
                $"Failed to find parent method {methodParentClass.Name}.{method}");
            return;
        }

        // Patch the parent method
        harmony.Patch(
            parentMethod,
            prefix: new HarmonyMethod(AccessTools.Method(thisClassType, prefix)),
            postfix: new HarmonyMethod(AccessTools.Method(thisClassType, postfix))
        );

        // Find all types in all loaded assemblies
        List<Type> derivedTypes = new List<Type>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    // Skip generic type definitions (we'll handle constructed generic types)
                    if (type.IsGenericTypeDefinition)
                    {
                        continue;
                    }

                    // Check if this type derives from GuiElement
                    if (type != methodParentClass && methodParentClass.IsAssignableFrom(type))
                    {
                        derivedTypes.Add(type);
                    }
                }
            }
            catch (Exception ex)
            {
                ToggleMouseControlModSystem.ClientApi.Logger.Error(
                    $"Error scanning assembly {assembly.FullName}: {ex.Message}");
            }
        }

        // Patch all derived implementations that override the method
        foreach (Type derivedType in derivedTypes)
        {
            try
            {
                if (derivedType.IsGenericType)
                {
                    PatchDerivedGenericTypeWithPrefixAndPostfix(
                        harmony, derivedType, parentMethod, method,
                        thisClassType, prefix, postfix);
                }
                else
                {
                    PatchDerivedTypeWithPrefixAndPostfix(
                        harmony, derivedType, parentMethod, method,
                        thisClassType, prefix, postfix);
                }
            }
            catch (Exception ex)
            {
                ToggleMouseControlModSystem.ClientApi.Logger.Error(
                    $"Error patching type {derivedType.FullName}: {ex.Message}");
            }
        }
    }

    private static void PatchDerivedTypeWithPrefixAndPostfix(
        Harmony harmony,
        Type derivedType, MethodInfo parentMethod,
        string method,
        Type thisClassType,
        string prefix,
        string postfix)
    {
        // Look for the method in this specific type (not inherited)
        MethodInfo overriddenMethod = derivedType.GetMethod(method,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        if (overriddenMethod != null)
        {
            Console.WriteLine($"Patching overridden method in {derivedType.Name}");
            harmony.Patch(
                overriddenMethod,
                prefix: new HarmonyMethod(AccessTools.Method(thisClassType, prefix)),
                postfix: new HarmonyMethod(AccessTools.Method(thisClassType, postfix))
            );
        }
    }

    private static void PatchDerivedGenericTypeWithPrefixAndPostfix(
        Harmony harmony,
        Type derivedType, MethodInfo parentMethod,
        string method,
        Type thisClassType,
        string prefix,
        string postfix)
    {
        // Look for the method in this specific type (not inherited)
        MethodInfo overriddenMethod = null;

        // Try to get the method directly first
        overriddenMethod = derivedType.GetMethod(method,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // If not found, try to get it through reflection with parameter matching
        if (overriddenMethod == null && parentMethod.GetParameters().Length > 0)
        {
            // Get parameter types from the parent method
            Type[] paramTypes = parentMethod.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();

            // Try to find the method with matching parameters
            overriddenMethod = derivedType.GetMethod(
                method,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null,
                paramTypes,
                null
            );
        }

        if (overriddenMethod != null)
        {
            ToggleMouseControlModSystem.ClientApi.Logger.Debug(
                $"Patching generic overridden method in {derivedType.Name}");
            harmony.Patch(
                overriddenMethod,
                prefix: new HarmonyMethod(AccessTools.Method(thisClassType, prefix)),
                postfix: new HarmonyMethod(AccessTools.Method(thisClassType, postfix))
            );
        }
    }
}

public static class GuiDialogPatcher
{
    public static bool ShouldRunOriginalGuiDialogInputHandlers(GuiDialog __instance)
    {
        bool runOriginal = true;
        if (ToggleMouseControlModSystem.ClientApi.Input.MouseGrabbed == true)
        {
            if (__instance != null)
            {
                // Use reflection to safely check the property
                try
                {
                    var prefersUngrabbedMouse = AccessTools.Property(
                        typeof(GuiDialog), "PrefersUngrabbedMouse")?
                        .GetValue(__instance);
                    runOriginal =
                        (prefersUngrabbedMouse == null) ||
                        ((bool)prefersUngrabbedMouse == false);
                }
                catch
                {
                    // Property access failed, default to true
                    ToggleMouseControlModSystem.ClientApi?.Logger?.Debug(
                        "Failed to access PrefersUngrabbedMouse property");
                }
            }
        }
        return runOriginal;
    }

    public static void PatchAllImplementations(Harmony harmony)
    {
        // Patch OnMouseWheel
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseWheel),
            nameof(Before_GuiDialog_OnMouseWheel),
            nameof(After_GuiDialog_OnMouseWheel));
        // Patch OnMouseMove
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseMove),
            nameof(Before_GuiDialog_OnMouseMove),
            nameof(After_GuiDialog_OnMouseMove));
        // Patch OnMouseDown/OnMouseUp
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseDown),
            nameof(Before_GuiDialog_OnMouseDown),
            nameof(After_GuiDialog_OnMouseDown));
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseUp),
            nameof(Before_GuiDialog_OnMouseUp),
            nameof(After_GuiDialog_OnMouseUp));
        // Patch OnKeyDown/OnKeyUp
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnKeyDown),
            nameof(Before_GuiDialog_OnKeyDown),
            nameof(After_GuiDialog_OnKeyDown));
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnKeyUp),
            nameof(Before_GuiDialog_OnKeyUp),
            nameof(After_GuiDialog_OnKeyUp));
    }

    public static void PatchAllImplementationsOfMethodWithPrefixAndPostfix(
        Harmony harmony,
        Type methodParentClass,
        string method,
        string prefix,
        string postfix)
    {
        // This class's type!
        Type thisClassType = MethodBase.GetCurrentMethod().DeclaringType;
        Patches.PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            methodParentClass,
            method,
            thisClassType,
            prefix,
            postfix);
    }

    public static bool Before_GuiDialog_OnMouseWheel(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnMouseWheel(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnMouseMove(
        GuiDialog __instance,
        ref MouseEvent args,
        GuiDialog.DlgComposers ___Composers)
    {
        bool runOriginal = ShouldRunOriginalGuiDialogInputHandlers(__instance);
        if (runOriginal == false)
        {
            runOriginal = true;
            {
                int guiDialogOriginX = 0;
                int guiDialogOriginY = 0;
                GuiComposer[] composers = ___Composers.ToArray();
                for (int i = 0; i < composers.Length; i++)
                {
                    guiDialogOriginX = Math.Min(
                        guiDialogOriginX, (int)composers[i].Bounds.absFixedX);
                    guiDialogOriginY = Math.Min(
                        guiDialogOriginY, (int)composers[i].Bounds.absFixedY);
                }
                args = new MouseEvent(
                    (int)(guiDialogOriginX),
                    (int)(guiDialogOriginY),
                    0,
                    0,
                    EnumMouseButton.None,
                    0
                );
            }
        }
        return runOriginal;
    }
    public static void After_GuiDialog_OnMouseMove(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnMouseDown(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnMouseDown(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnMouseUp(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnMouseUp(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnKeyDown(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnKeyDown(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnKeyUp(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnKeyUp(GuiDialog __instance)
    {
    }
}