using System;
using System.Collections.Generic;
using Cairo;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class InventoryItemRenderer : IRenderer, IDisposable
{
	private ClientMain game;

	private Dictionary<string, LoadedTexture> StackSizeTextures = new Dictionary<string, LoadedTexture>();

	private MeshRef quadModelRef;

	private CairoFont stackSizeFont;

	private Matrixf modelMat = new Matrixf();

	private Queue<AtlasRenderTask> queue = new Queue<AtlasRenderTask>();

	private int[] clearPixels;

	public double RenderOrder => 9.0;

	public int RenderRange => 24;

	public InventoryItemRenderer(ClientMain game)
	{
		this.game = game;
		MeshData modeldata = QuadMeshUtil.GetQuad();
		modeldata.Uv = new float[8] { 1f, 1f, 0f, 1f, 0f, 0f, 1f, 0f };
		modeldata.Rgba = new byte[16];
		modeldata.Rgba.Fill(byte.MaxValue);
		quadModelRef = game.Platform.UploadMesh(modeldata);
		stackSizeFont = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.SmallishFontSize);
		stackSizeFont.FontWeight = FontWeight.Bold;
		stackSizeFont.Color = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		stackSizeFont.StrokeColor = new double[4] { 0.0, 0.0, 0.0, 1.0 };
		stackSizeFont.StrokeWidth = (double)ClientSettings.GUIScale + 0.25;
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			stackSizeFont.StrokeWidth = (double)ClientSettings.GUIScale + 0.25;
			foreach (KeyValuePair<string, LoadedTexture> stackSizeTexture in StackSizeTextures)
			{
				stackSizeTexture.Value?.Dispose();
			}
			StackSizeTextures.Clear();
		});
		game.eventManager.RegisterRenderer(this, EnumRenderStage.Ortho, "renderstacktoatlas");
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		while (queue.Count > 0)
		{
			AtlasRenderTask task = queue.Dequeue();
			RenderItemStackToAtlas(task.Stack, task.Atlas, task.Size, task.OnComplete, task.Color, task.SepiaLevel, task.Scale);
		}
	}

	public bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			game.EnqueueMainThreadTask(delegate
			{
				queue.Enqueue(new AtlasRenderTask
				{
					Stack = stack,
					Atlas = atlas,
					Size = size,
					Color = color,
					SepiaLevel = sepiaLevel,
					OnComplete = onComplete,
					Scale = scale
				});
			}, "enqueueRenderTask");
			return false;
		}
		if (game.currentRenderStage != EnumRenderStage.Ortho)
		{
			queue.Enqueue(new AtlasRenderTask
			{
				Stack = stack,
				Atlas = atlas,
				Size = size,
				OnComplete = onComplete
			});
			return false;
		}
		if (!atlas.AllocateTextureSpace(size, size, out var subid, out var texPos))
		{
			throw new Exception($"Was not able to allocate texture space of size {size}x{size} for item stack '{stack.GetName()}', maybe atlas is full?");
		}
		FramebufferAttrsAttachment[] attachments = new FramebufferAttrsAttachment[2]
		{
			new FramebufferAttrsAttachment
			{
				AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
				Texture = new RawTexture
				{
					Width = atlas.Size.Width,
					Height = atlas.Size.Height,
					TextureId = texPos.atlasTextureId,
					PixelFormat = EnumTexturePixelFormat.Rgba,
					PixelInternalFormat = EnumTextureInternalFormat.Rgba8
				}
			},
			new FramebufferAttrsAttachment
			{
				AttachmentType = EnumFramebufferAttachment.DepthAttachment,
				Texture = new RawTexture
				{
					Width = atlas.Size.Width,
					Height = atlas.Size.Height,
					PixelFormat = EnumTexturePixelFormat.DepthComponent,
					PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
				}
			}
		};
		FrameBufferRef fb = game.Platform.CreateFramebuffer(new FramebufferAttrs("atlasRenderer", atlas.Size.Width, atlas.Size.Height)
		{
			Attachments = attachments
		});
		game.Platform.LoadFrameBuffer(fb);
		game.Platform.GlEnableDepthTest();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.Platform.ClearFrameBuffer(fb, null, clearDepthBuffer: true, clearColorBuffers: false);
		game.OrthoMode(atlas.Size.Width, atlas.Size.Height, inverseY: true);
		float x = texPos.x1 * (float)atlas.Size.Width + (float)size / 2f;
		float y = texPos.y1 * (float)atlas.Size.Height + (float)size / 2f;
		game.guiShaderProg.SepiaLevel = sepiaLevel;
		if (clearPixels == null || clearPixels.Length < size * size)
		{
			clearPixels = new int[size * size];
		}
		game.Platform.BindTexture2d(texPos.atlasTextureId);
		GL.TexSubImage2D(TextureTarget.Texture2D, 0, (int)(texPos.x1 * (float)atlas.Size.Width), (int)(texPos.y1 * (float)atlas.Size.Height), size, size, PixelFormat.Bgra, PixelType.UnsignedByte, clearPixels);
		game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(stack), x, y, 500.0, (float)(size / 2) * scale, color, shading: true, origRotate: false, showStackSize: false);
		game.PerspectiveMode();
		game.guiShaderProg.SepiaLevel = 0f;
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.Default);
		fb.ColorTextureIds = new int[0];
		game.Platform.DisposeFrameBuffer(fb);
		onComplete(subid);
		return true;
	}

	public void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
	{
		game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
		game.guiShaderProg.ExtraGlow = 0;
		game.guiShaderProg.ApplyColor = 1;
		game.guiShaderProg.Tex2d2D = game.EntityAtlasManager.AtlasTextures[0].TextureId;
		game.guiShaderProg.AlphaTest = 0.1f;
		game.guiShaderProg.NoTexture = 0f;
		game.guiShaderProg.OverlayOpacity = 0f;
		game.guiShaderProg.NormalShaded = 1;
		entity.Properties.Client.Renderer.RenderToGui(dt, posX, posY, posZ, yawDelta, size);
		game.guiShaderProg.NormalShaded = 0;
	}

	public void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool origRotate = false, bool showStackSize = true)
	{
		RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, 0f, shading, origRotate, showStackSize);
	}

	public void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool origRotate = false, bool showStackSize = true)
	{
		ItemStack itemstack = inSlot.Itemstack;
		ItemRenderInfo renderInfo = GetItemStackRenderInfo(game, inSlot, EnumItemRenderTarget.Gui, dt);
		if (renderInfo.ModelRef == null)
		{
			return;
		}
		itemstack.Collectible.InGuiIdle(game, itemstack);
		ModelTransform transform = renderInfo.Transform;
		if (transform == null)
		{
			return;
		}
		bool upsidedown = itemstack.Class == EnumItemClass.Block;
		bool rotate = origRotate && renderInfo.Transform.Rotate;
		modelMat.Identity();
		modelMat.Translate((int)posX - ((itemstack.Class == EnumItemClass.Item) ? 3 : 0), (int)posY - ((itemstack.Class == EnumItemClass.Item) ? 1 : 0), (float)posZ);
		modelMat.Translate((double)transform.Origin.X + GuiElement.scaled(transform.Translation.X), (double)transform.Origin.Y + GuiElement.scaled(transform.Translation.Y), (double)(transform.Origin.Z * size) + GuiElement.scaled(transform.Translation.Z));
		modelMat.Scale(size * transform.ScaleXYZ.X, size * transform.ScaleXYZ.Y, size * transform.ScaleXYZ.Z);
		modelMat.RotateXDeg(transform.Rotation.X + (upsidedown ? 180f : 0f));
		modelMat.RotateYDeg(transform.Rotation.Y - (float)((!upsidedown) ? 1 : (-1)) * (rotate ? ((float)game.Platform.EllapsedMs / 50f) : 0f));
		modelMat.RotateZDeg(transform.Rotation.Z);
		modelMat.Translate(0f - transform.Origin.X, 0f - transform.Origin.Y, 0f - transform.Origin.Z);
		int num = (int)itemstack.Collectible.GetTemperature(game, itemstack);
		float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
		float[] drawcolor = ColorUtil.ToRGBAFloats(color);
		int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
		bool tempGlowMode = itemstack.Attributes.HasAttribute("temperature");
		game.guiShaderProg.NormalShaded = (renderInfo.NormalShaded ? 1 : 0);
		game.guiShaderProg.RgbaIn = new Vec4f(drawcolor[0], drawcolor[1], drawcolor[2], drawcolor[3]);
		game.guiShaderProg.ExtraGlow = extraGlow;
		game.guiShaderProg.TempGlowMode = (tempGlowMode ? 1 : 0);
		game.guiShaderProg.RgbaGlowIn = (tempGlowMode ? new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f) : new Vec4f(1f, 1f, 1f, (float)extraGlow / 255f));
		game.guiShaderProg.ApplyColor = (renderInfo.ApplyColor ? 1 : 0);
		game.guiShaderProg.AlphaTest = renderInfo.AlphaTest;
		game.guiShaderProg.OverlayOpacity = renderInfo.OverlayOpacity;
		if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0f)
		{
			game.guiShaderProg.Tex2dOverlay2D = renderInfo.OverlayTexture.TextureId;
			game.guiShaderProg.OverlayTextureSize = new Vec2f(renderInfo.OverlayTexture.Width, renderInfo.OverlayTexture.Height);
			game.guiShaderProg.BaseTextureSize = new Vec2f(renderInfo.TextureSize.Width, renderInfo.TextureSize.Height);
			TextureAtlasPosition texPos = GetTextureAtlasPosition(game, itemstack);
			game.guiShaderProg.BaseUvOrigin = new Vec2f(texPos.x1, texPos.y1);
		}
		game.guiShaderProg.ModelMatrix = modelMat.Values;
		game.guiShaderProg.ProjectionMatrix = game.CurrentProjectionMatrix;
		game.guiShaderProg.ModelViewMatrix = modelMat.ReverseMul(game.CurrentModelViewMatrix).Values;
		game.guiShaderProg.ApplyModelMat = 1;
		if (game.api.eventapi.itemStackRenderersByTarget[(int)itemstack.Collectible.ItemClass][0].TryGetValue(itemstack.Collectible.Id, out var renderer))
		{
			renderer(inSlot, renderInfo, modelMat, posX, posY, posZ, size, color, origRotate, showStackSize);
			game.guiShaderProg.ApplyModelMat = 0;
			game.guiShaderProg.NormalShaded = 0;
			game.guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
			game.guiShaderProg.AlphaTest = 0f;
			return;
		}
		game.guiShaderProg.DamageEffect = renderInfo.DamageEffect;
		game.api.renderapi.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d");
		game.guiShaderProg.ApplyModelMat = 0;
		game.guiShaderProg.NormalShaded = 0;
		game.guiShaderProg.TempGlowMode = 0;
		game.guiShaderProg.DamageEffect = 0f;
		LoadedTexture stackSizeTexture = null;
		if (itemstack.StackSize != 1 && showStackSize)
		{
			float mul2 = size / (float)GuiElement.scaled(25.600000381469727);
			string key = itemstack.StackSize + "-" + (int)(mul2 * 100f);
			if (!StackSizeTextures.TryGetValue(key, out stackSizeTexture))
			{
				stackSizeTexture = (StackSizeTextures[key] = GenStackSizeTexture(itemstack.StackSize, mul2));
			}
		}
		if (stackSizeTexture != null)
		{
			float mul = size / (float)GuiElement.scaled(25.600000381469727);
			game.Platform.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
			game.Render2DLoadedTexture(stackSizeTexture, (int)(posX + (double)size + 1.0 - (double)stackSizeTexture.Width), (int)(posY + (double)mul * GuiElement.scaled(3.0) - GuiElement.scaled(4.0)), (int)posZ + 100);
			game.Platform.GlToggleBlend(on: true);
		}
		game.guiShaderProg.AlphaTest = 0f;
		game.guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
	}

	private LoadedTexture GenStackSizeTexture(int stackSize, float fontSizeMultiplier = 1f)
	{
		CairoFont font = stackSizeFont.Clone();
		font.UnscaledFontsize *= fontSizeMultiplier;
		return game.api.guiapi.TextTexture.GenTextTexture(stackSize.ToString() ?? "", font);
	}

	public static ItemRenderInfo GetItemStackRenderInfo(ClientMain game, ItemSlot inSlot, EnumItemRenderTarget target, float dt)
	{
		ItemStack itemstack = inSlot.Itemstack;
		if (itemstack == null || itemstack.Collectible.Code == null)
		{
			return new ItemRenderInfo();
		}
		ItemRenderInfo renderInfo = new ItemRenderInfo();
		renderInfo.dt = dt;
		switch (target)
		{
		case EnumItemRenderTarget.Ground:
			renderInfo.Transform = itemstack.Collectible.GroundTransform;
			break;
		case EnumItemRenderTarget.Gui:
			renderInfo.Transform = itemstack.Collectible.GuiTransform;
			break;
		case EnumItemRenderTarget.HandTp:
			renderInfo.Transform = itemstack.Collectible.TpHandTransform;
			break;
		case EnumItemRenderTarget.HandTpOff:
			renderInfo.Transform = itemstack.Collectible.TpOffHandTransform ?? itemstack.Collectible.TpHandTransform;
			break;
		}
		if (itemstack.Collectible?.Code == null)
		{
			renderInfo.ModelRef = ((itemstack.Block == null) ? game.TesselatorManager.unknownItemModelRef : game.TesselatorManager.unknownBlockModelRef);
		}
		else if (itemstack.Class == EnumItemClass.Block)
		{
			renderInfo.ModelRef = game.TesselatorManager.blockModelRefsInventory[itemstack.Id];
		}
		else
		{
			int variant = (itemstack.TempAttributes.HasAttribute("renderVariant") ? itemstack.TempAttributes.GetInt("renderVariant") : itemstack.Attributes.GetInt("renderVariant"));
			if (variant != 0 && (variant < 0 || game.TesselatorManager.altItemModelRefsInventory[itemstack.Id] == null || game.TesselatorManager.altItemModelRefsInventory[itemstack.Id].Length < variant - 1))
			{
				game.Logger.Warning("Itemstack {0} has an invalid renderVariant {1}. No such model variant exists. Will reset to 0", itemstack.GetName(), variant);
				itemstack.TempAttributes.SetInt("renderVariant", 0);
				variant = 0;
			}
			if (variant == 0)
			{
				renderInfo.ModelRef = game.TesselatorManager.itemModelRefsInventory[itemstack.Id];
			}
			else
			{
				renderInfo.ModelRef = game.TesselatorManager.altItemModelRefsInventory[itemstack.Id][variant - 1];
			}
		}
		ItemRenderInfo itemRenderInfo = renderInfo;
		int normalShaded;
		if (itemstack.Class != 0)
		{
			CompositeShape shape = itemstack.Item.Shape;
			normalShaded = ((shape != null && !shape.VoxelizeTexture) ? 1 : 0);
		}
		else
		{
			CompositeShape shape2 = itemstack.Block.Shape;
			normalShaded = ((shape2 != null && !shape2.VoxelizeTexture) ? 1 : 0);
		}
		itemRenderInfo.NormalShaded = (byte)normalShaded != 0;
		renderInfo.TextureSize.Width = ((itemstack.Class == EnumItemClass.Block) ? game.BlockAtlasManager.Size.Width : game.ItemAtlasManager.Size.Width);
		renderInfo.TextureSize.Height = ((itemstack.Class == EnumItemClass.Block) ? game.BlockAtlasManager.Size.Height : game.ItemAtlasManager.Size.Height);
		renderInfo.HalfTransparent = itemstack.Block != null && (itemstack.Block.RenderPass == EnumChunkRenderPass.Meta || itemstack.Block.RenderPass == EnumChunkRenderPass.Transparent);
		renderInfo.AlphaTest = itemstack.Collectible.RenderAlphaTest;
		renderInfo.CullFaces = itemstack.Block != null && (itemstack.Block.RenderPass == EnumChunkRenderPass.Opaque || itemstack.Block.RenderPass == EnumChunkRenderPass.TopSoil);
		renderInfo.ApplyColor = renderInfo.NormalShaded;
		TransitionState state = itemstack.Collectible.UpdateAndGetTransitionState(game, inSlot, EnumTransitionType.Perish);
		if (state != null && state.TransitionLevel > 0f)
		{
			renderInfo.SetRotOverlay(game.api, state.TransitionLevel);
		}
		renderInfo.InSlot = inSlot;
		itemstack.Collectible.OnBeforeRender(game.api, itemstack, target, ref renderInfo);
		return renderInfo;
	}

	public static TextureAtlasPosition GetTextureAtlasPosition(ClientMain game, IItemStack itemstack)
	{
		int tileSide = BlockFacing.UP.Index;
		if (itemstack.Collectible.Code == null)
		{
			return game.BlockAtlasManager.UnknownTexturePos;
		}
		if (itemstack.Class == EnumItemClass.Block)
		{
			int textureSubId = game.FastBlockTextureSubidsByBlockAndFace[itemstack.Id][tileSide];
			return game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
		}
		if (itemstack.Item.FirstTexture == null)
		{
			return game.BlockAtlasManager.UnknownTexturePos;
		}
		int textureSubId2 = itemstack.Item.FirstTexture.Baked.TextureSubId;
		return game.ItemAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId2];
	}

	public int GetCurrentBlockOrItemTextureId(int side)
	{
		ItemSlot slot = game.player.inventoryMgr.ActiveHotbarSlot;
		if (slot != null && slot.Itemstack.Class == EnumItemClass.Block)
		{
			return game.FastBlockTextureSubidsByBlockAndFace[slot.Itemstack.Id][side];
		}
		return 0;
	}

	public int GetBlockOrItemTextureId(BlockFacing facing, IItemStack itemstack)
	{
		if (itemstack.Class != 0)
		{
			return 0;
		}
		return game.FastBlockTextureSubidsByBlockAndFace[itemstack.Id][facing.Index];
	}

	public void Dispose()
	{
		quadModelRef?.Dispose();
		foreach (KeyValuePair<string, LoadedTexture> stackSizeTexture in StackSizeTextures)
		{
			stackSizeTexture.Value?.Dispose();
		}
	}
}
