using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class PlaySongAction : EntityActionBase
{
	[JsonProperty]
	public float durationSeconds;

	[JsonProperty]
	public string soundLocation;

	[JsonProperty]
	public float pitch = 1f;

	[JsonProperty]
	public float range = 32f;

	[JsonProperty]
	public float volume = 1f;

	[JsonProperty]
	public AnimationMetaData animMeta;

	private float secondsPassed;

	private bool stop;

	private float accum;

	public override string Type => "playsong";

	public PlaySongAction(EntityActivitySystem vas, float durationSeconds, string soundLocation, float pitch, float range, float volume, AnimationMetaData animMeta)
	{
		base.vas = vas;
		this.durationSeconds = durationSeconds;
		this.soundLocation = soundLocation;
		this.pitch = pitch;
		this.range = range;
		this.volume = volume;
		this.animMeta = animMeta;
	}

	public PlaySongAction()
	{
	}

	public override void OnTick(float dt)
	{
		accum += dt;
		if (accum > 3f)
		{
			sendSongPacket();
		}
		secondsPassed += dt;
	}

	public override bool IsFinished()
	{
		if (!stop)
		{
			return secondsPassed > durationSeconds;
		}
		return true;
	}

	public override void Start(EntityActivity act)
	{
		secondsPassed = 0f;
		stop = false;
		vas.Entity.AnimManager.StartAnimation(animMeta.Init());
		sendSongPacket();
	}

	private void sendSongPacket()
	{
		(vas.Entity.Api as ICoreServerAPI).Network.BroadcastEntityPacket(vas.Entity.EntityId, 201, SerializerUtil.Serialize(new SongPacket
		{
			SecondsPassed = secondsPassed,
			SoundLocation = soundLocation
		}));
	}

	public override void OnHurt(DamageSource dmgSource, float damage)
	{
		stop = true;
		Cancel();
	}

	public override void Cancel()
	{
		(vas.Entity.Api as ICoreServerAPI).Network.BroadcastEntityPacket(vas.Entity.EntityId, 202);
		Finish();
	}

	public override void Finish()
	{
		vas.Entity.AnimManager.StopAnimation(animMeta.Code);
		(vas.Entity.Api as ICoreServerAPI).Network.BroadcastEntityPacket(vas.Entity.EntityId, 202);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Sound Location", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "soundlocation").AddStaticText("Duration in Seconds", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "durationSec")
			.AddStaticText("Range", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "range")
			.AddStaticText("Animation Code", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "animation")
			.AddStaticText("Animation Speed", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "speed");
		singleComposer.GetTextInput("soundlocation").SetValue(soundLocation);
		singleComposer.GetNumberInput("durationSec").SetValue(durationSeconds);
		singleComposer.GetNumberInput("range").SetValue(range);
		singleComposer.GetTextInput("animation").SetValue(animMeta?.Animation ?? "");
		singleComposer.GetNumberInput("speed").SetValue(animMeta?.AnimationSpeed ?? 1f);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		animMeta = new AnimationMetaData
		{
			Animation = singleComposer.GetTextInput("animation").GetText(),
			AnimationSpeed = singleComposer.GetNumberInput("speed").GetText().ToFloat(1f)
		};
		soundLocation = singleComposer.GetTextInput("soundlocation").GetText();
		durationSeconds = singleComposer.GetNumberInput("durationSec").GetValue();
		range = singleComposer.GetNumberInput("range").GetValue();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new PlaySongAction(vas, durationSeconds, soundLocation, pitch, range, volume, animMeta);
	}

	public override string ToString()
	{
		return $"Play song {soundLocation} for {durationSeconds} seconds, with anim {animMeta?.Code}";
	}
}
