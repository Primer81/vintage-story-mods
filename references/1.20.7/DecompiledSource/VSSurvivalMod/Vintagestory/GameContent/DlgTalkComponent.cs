using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class DlgTalkComponent : DialogueComponent
{
	public DialogeTextElement[] Text;

	private HashSet<int> usedAnswers;

	private bool IsPlayer => Owner == "player";

	public override string Execute()
	{
		setVars();
		RichTextComponentBase[] comps = genText(!IsPlayer);
		if (comps.Length != 0)
		{
			dialog?.EmitDialogue(comps);
		}
		if (IsPlayer)
		{
			return null;
		}
		if (JumpTo == null)
		{
			return "next";
		}
		return JumpTo;
	}

	protected RichTextComponentBase[] genText(bool selectRandom)
	{
		List<RichTextComponentBase> comps = new List<RichTextComponentBase>();
		ICoreAPI api = controller.NPCEntity.Api;
		if (api.Side != EnumAppSide.Client)
		{
			return comps.ToArray();
		}
		CairoFont font = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
		if (IsPlayer)
		{
			comps.Add(new RichTextComponent(api as ICoreClientAPI, "\r\n", font));
		}
		int answerNumber = 1;
		List<DialogeTextElement> elems = new List<DialogeTextElement>();
		for (int j = 0; j < Text.Length; j++)
		{
			if (!selectRandom || conditionsMet(Text[j].Conditions))
			{
				elems.Add(Text[j]);
			}
		}
		int rnd = api.World.Rand.Next(elems.Count);
		for (int i = 0; i < elems.Count; i++)
		{
			if ((!selectRandom && !conditionsMet(Text[i].Conditions)) || (selectRandom && i != rnd))
			{
				continue;
			}
			string text = Lang.Get(elems[i].Value).Replace("{characterclass}", Lang.Get("characterclass-" + controller.PlayerEntity.WatchedAttributes.GetString("characterClass"))).Replace("{playername}", controller.PlayerEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName)
				.Replace("{npcname}", controller.NPCEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);
			if (IsPlayer)
			{
				int id = elems[i].Id;
				LinkTextComponent lcomp = new LinkTextComponent(api as ICoreClientAPI, answerNumber + ". " + text, font, delegate
				{
					SelectAnswerById(id);
				});
				comps.Add(lcomp);
				comps.Add(new RichTextComponent(api as ICoreClientAPI, "\r\n", font));
				CairoFont font2 = lcomp.Font;
				HashSet<int> hashSet = usedAnswers;
				font2.WithColor((hashSet != null && hashSet.Contains(id)) ? GuiStyle.ColorParchment : GuiStyle.ColorTime1).WithOrientation(EnumTextOrientation.Right);
				answerNumber++;
			}
			else
			{
				comps.AddRange(VtmlUtil.Richtextify(api as ICoreClientAPI, text + "\r\n", font));
			}
		}
		return comps.ToArray();
	}

	private bool conditionsMet(ConditionElement[] conds)
	{
		if (conds == null)
		{
			return true;
		}
		foreach (ConditionElement cond in conds)
		{
			if (!isConditionMet(cond))
			{
				return false;
			}
		}
		return true;
	}

	private bool isConditionMet(ConditionElement cond)
	{
		if (IsConditionMet(cond.Variable, cond.IsValue, cond.Invert))
		{
			return true;
		}
		return false;
	}

	public void SelectAnswerById(int id)
	{
		ICoreAPI api = controller.NPCEntity.Api;
		DialogeTextElement answer = Text.FirstOrDefault((DialogeTextElement elem) => elem.Id == id);
		if (answer == null)
		{
			api.Logger.Warning($"Got invalid answer index: {id} for {controller.NPCEntity.Code}");
			return;
		}
		if (IsPlayer)
		{
			if (usedAnswers == null)
			{
				usedAnswers = new HashSet<int>();
			}
			usedAnswers.Add(id);
		}
		if (api is ICoreClientAPI capi)
		{
			capi.Network.SendEntityPacket(controller.NPCEntity.EntityId, EntityBehaviorConversable.SelectAnswerPacketId, SerializerUtil.Serialize(id));
		}
		dialog?.ClearDialogue();
		jumpTo(answer.JumpTo);
	}

	private void jumpTo(string code)
	{
		controller.JumpTo(code);
	}

	public override void Init(ref int uniqueIdCounter)
	{
		DialogeTextElement[] text = Text;
		for (int i = 0; i < text.Length; i++)
		{
			text[i].Id = uniqueIdCounter++;
		}
	}
}
