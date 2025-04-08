using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class TutorialStepPressHotkeys : TutorialStepBase
{
	private List<string> hotkeysToPress = new List<string>();

	private HashSet<string> hotkeysPressed = new HashSet<string>();

	private ICoreClientAPI capi;

	private HashSet<EnumEntityAction> activeActions = new HashSet<EnumEntityAction>();

	private EnumEntityAction deferredActionTrigger = EnumEntityAction.None;

	private EnumEntityAction deferredActionPreReq = EnumEntityAction.None;

	public static Dictionary<EnumEntityAction, string> actionToHotkeyMapping = new Dictionary<EnumEntityAction, string>
	{
		{
			EnumEntityAction.Forward,
			"walkforward"
		},
		{
			EnumEntityAction.Backward,
			"walkbackward"
		},
		{
			EnumEntityAction.Left,
			"walkleft"
		},
		{
			EnumEntityAction.Right,
			"walkright"
		},
		{
			EnumEntityAction.Sneak,
			"sneak"
		},
		{
			EnumEntityAction.Sprint,
			"sprint"
		},
		{
			EnumEntityAction.Jump,
			"jump"
		},
		{
			EnumEntityAction.FloorSit,
			"sitdown"
		},
		{
			EnumEntityAction.CtrlKey,
			"ctrl"
		},
		{
			EnumEntityAction.ShiftKey,
			"shift"
		}
	};

	public override bool Complete => hotkeysPressed.Count == hotkeysToPress.Count;

	public TutorialStepPressHotkeys(ICoreClientAPI capi, string text, params string[] hotkeys)
	{
		this.capi = capi;
		base.text = text;
		hotkeysToPress.AddRange(hotkeys);
	}

	public override RichTextComponentBase[] GetText(CairoFont font)
	{
		_ = capi.Input.HotKeys;
		List<string> hotkeyvtml = new List<string>();
		foreach (string hkcode in hotkeysToPress)
		{
			if (hotkeysPressed.Contains(hkcode))
			{
				hotkeyvtml.Add("<font color=\"#99ff99\"><hk>" + hkcode + "</hk></font>");
			}
			else
			{
				hotkeyvtml.Add("<hk>" + hkcode + "</hk>");
			}
		}
		object[] obj = new object[2]
		{
			index + 1,
			null
		};
		string key = text;
		object[] args = hotkeyvtml.ToArray();
		obj[1] = Lang.Get(key, args);
		string vtmlCode = Lang.Get("tutorialstep-numbered", obj);
		return VtmlUtil.Richtextify(capi, vtmlCode, font);
	}

	public override void Restart()
	{
		hotkeysPressed.Clear();
		deferredActionTrigger = EnumEntityAction.None;
		deferredActionPreReq = EnumEntityAction.None;
	}

	public override void Skip()
	{
		foreach (string hk in hotkeysToPress)
		{
			hotkeysPressed.Add(hk);
		}
	}

	public override bool OnHotkeyPressed(string hotkeycode, KeyCombination keyComb)
	{
		if (hotkeysToPress.Contains(hotkeycode) && !hotkeysPressed.Contains(hotkeycode))
		{
			hotkeysPressed.Add(hotkeycode);
			return true;
		}
		return false;
	}

	public override bool OnAction(EnumEntityAction action, bool on)
	{
		if (on)
		{
			activeActions.Add(action);
		}
		else
		{
			activeActions.Remove(action);
		}
		EnumEntityAction preCondition = ((action != EnumEntityAction.Sprint) ? EnumEntityAction.None : EnumEntityAction.Forward);
		if (on && actionToHotkeyMapping.TryGetValue(action, out var keycode))
		{
			if (preCondition != EnumEntityAction.None && !activeActions.Contains(preCondition))
			{
				deferredActionTrigger = preCondition;
				deferredActionPreReq = action;
				return false;
			}
			if (action == deferredActionTrigger && activeActions.Contains(deferredActionPreReq) && actionToHotkeyMapping.TryGetValue(deferredActionPreReq, out var keycode2))
			{
				deferredActionTrigger = EnumEntityAction.None;
				deferredActionPreReq = EnumEntityAction.None;
				bool preReqResult = OnHotkeyPressed(keycode2, null);
				return OnHotkeyPressed(keycode, null) || preReqResult;
			}
			return OnHotkeyPressed(keycode, null);
		}
		if (action == deferredActionPreReq && !on)
		{
			deferredActionTrigger = EnumEntityAction.None;
			deferredActionPreReq = EnumEntityAction.None;
		}
		return false;
	}

	public override void ToJson(JsonObject job)
	{
		if (hotkeysPressed.Count > 0)
		{
			JToken token = (job.Token[code] = new JObject());
			JToken token2 = new JsonObject(token).Token;
			object[] content = hotkeysPressed.ToArray();
			token2["pressed"] = new JArray(content);
		}
	}

	public override void FromJson(JsonObject job)
	{
		if (!(job[code]["pressed"].Token is JArray arr))
		{
			return;
		}
		hotkeysPressed.Clear();
		foreach (JToken item in arr)
		{
			string val = (string?)item;
			hotkeysPressed.Add(val);
		}
	}
}
