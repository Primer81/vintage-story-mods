using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public class PerceptionEffects
{
	private ICoreClientAPI capi;

	private int nextPerceptionEffectId = 1;

	private Dictionary<string, PerceptionEffect> registeredPerceptionEffects = new Dictionary<string, PerceptionEffect>();

	private Dictionary<string, PerceptionEffect> activePerceptionEffects = new Dictionary<string, PerceptionEffect>();

	public ICollection<string> RegisteredEffects => registeredPerceptionEffects.Keys;

	public PerceptionEffects(ICoreClientAPI capi)
	{
		this.capi = capi;
		RegisterPerceptionEffect(new DamagedPerceptionEffect(capi), "damaged");
		RegisterPerceptionEffect(new FreezingPerceptionEffect(capi), "freezing");
		RegisterPerceptionEffect(new DrunkPerceptionEffect(capi), "drunk");
	}

	public void RegisterPerceptionEffect(PerceptionEffect effect, string code)
	{
		effect.PerceptionEffectId = nextPerceptionEffectId++;
		effect.Code = code;
		registeredPerceptionEffects[code] = effect;
	}

	public void TriggerEffect(string code, float intensity, bool? on = null)
	{
		if ((!on.HasValue) ? (!activePerceptionEffects.ContainsKey(code)) : on.Value)
		{
			activePerceptionEffects[code] = registeredPerceptionEffects[code];
			activePerceptionEffects[code].NowActive(intensity);
		}
		else if (activePerceptionEffects.ContainsKey(code))
		{
			activePerceptionEffects[code].NowDisabled();
			activePerceptionEffects.Remove(code);
			capi.Render.ShaderUniforms.PerceptionEffectId = 0;
		}
	}

	public void OnBeforeGameRender(float dt)
	{
		foreach (KeyValuePair<string, PerceptionEffect> activePerceptionEffect in activePerceptionEffects)
		{
			activePerceptionEffect.Value.OnBeforeGameRender(dt);
		}
	}

	public void OnOwnPlayerDataReceived(EntityPlayer eplr)
	{
		TriggerEffect("damaged", 1f, true);
		TriggerEffect("freezing", 1f, true);
		TriggerEffect("drunk", 0f, true);
		foreach (KeyValuePair<string, PerceptionEffect> activePerceptionEffect in activePerceptionEffects)
		{
			activePerceptionEffect.Value.OnOwnPlayerDataReceived(eplr);
		}
	}

	public void ApplyToFpHand(Matrixf modelMat)
	{
		foreach (KeyValuePair<string, PerceptionEffect> activePerceptionEffect in activePerceptionEffects)
		{
			activePerceptionEffect.Value.ApplyToFpHand(modelMat);
		}
	}

	public void ApplyToTpPlayer(EntityPlayer entityPlr, float[] modelMatrix, float? playerIntensity = null)
	{
		foreach (KeyValuePair<string, PerceptionEffect> activePerceptionEffect in activePerceptionEffects)
		{
			activePerceptionEffect.Value.ApplyToTpPlayer(entityPlr, modelMatrix, playerIntensity);
		}
	}
}
