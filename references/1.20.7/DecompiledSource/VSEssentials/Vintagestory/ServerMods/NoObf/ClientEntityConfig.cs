using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public class ClientEntityConfig
{
	[JsonProperty]
	public string Renderer;

	[JsonProperty]
	protected CompositeTexture Texture;

	[JsonProperty]
	public int GlowLevel;

	[JsonProperty]
	public CompositeShape Shape;

	[JsonProperty(ItemConverterType = typeof(JsonAttributesConverter))]
	public JsonObject[] Behaviors;

	[JsonProperty]
	public float Size = 1f;

	[JsonProperty]
	public float SizeGrowthFactor;

	[JsonProperty]
	public AnimationMetaData[] Animations;

	[JsonProperty]
	public bool PitchStep = true;

	public Dictionary<string, AnimationMetaData> AnimationsByMetaCode = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty]
	public Dictionary<string, CompositeTexture> Textures { get; set; } = new Dictionary<string, CompositeTexture>();


	public CompositeTexture FirstTexture
	{
		get
		{
			if (Textures != null && Textures.Count != 0)
			{
				return Textures.First().Value;
			}
			return null;
		}
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Texture != null)
		{
			Textures["all"] = Texture;
		}
		Init();
	}

	public void Init()
	{
		if (Animations == null)
		{
			return;
		}
		for (int i = 0; i < Animations.Length; i++)
		{
			AnimationMetaData animMeta = Animations[i];
			if (animMeta.Animation != null)
			{
				AnimationsByMetaCode[animMeta.Code] = animMeta;
			}
		}
	}
}
