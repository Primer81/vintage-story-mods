using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class ShapeTesselator : ITesselatorAPI
{
	public OrderedDictionary<AssetLocation, UnloadableShape> shapes;

	public OrderedDictionary<AssetLocation, IAsset> objs;

	public OrderedDictionary<AssetLocation, GltfType> gltfs;

	private ClientMain game;

	private Vec3f noRotation = new Vec3f();

	private Vec3f constantCenter = new Vec3f(0.5f, 0.5f, 0.5f);

	private Vec3f constantCenterXZ = new Vec3f(0.5f, 0f, 0.5f);

	private Vec3f rotationVec = new Vec3f();

	private Vec3f offsetVec = new Vec3f();

	private Vec3f xyzVec = new Vec3f();

	private Vec3f centerVec = new Vec3f();

	public MeshData unknownItemModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private ObjTesselator objTesselator = new ObjTesselator();

	private GltfTesselator gltfTesselator = new GltfTesselator();

	private TesselationMetaData meta = new TesselationMetaData();

	private MeshData elementMeshData = new MeshData(24, 36).WithColorMaps().WithRenderpasses();

	private StackMatrix4 stackMatrix = new StackMatrix4(64);

	private int[] flags = new int[4];

	private static int[] noFlags = new int[4];

	public ShapeTesselator(ClientMain game, OrderedDictionary<AssetLocation, UnloadableShape> shapes, OrderedDictionary<AssetLocation, IAsset> objs, OrderedDictionary<AssetLocation, GltfType> gltfs)
	{
		this.shapes = shapes;
		this.objs = objs;
		this.game = game;
		this.gltfs = gltfs;
	}

	public void TesselateShape(string type, AssetLocation sourceName, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[] selectiveElements = null)
	{
		if (!quantityElements.HasValue && compositeShape.QuantityElements > 0)
		{
			quantityElements = compositeShape.QuantityElements;
		}
		if (selectiveElements == null)
		{
			selectiveElements = compositeShape.SelectiveElements;
		}
		meta.UsesColorMap = false;
		meta.TypeForLogging = type + " " + sourceName;
		meta.TexSource = texSource;
		meta.GeneralGlowLevel = generalGlowLevel;
		meta.QuantityElements = quantityElements;
		meta.WithJointIds = false;
		meta.SelectiveElements = selectiveElements;
		meta.IgnoreElements = compositeShape.IgnoreElements;
		meta.ClimateColorMapId = climateColorMapIndex;
		meta.SeasonColorMapId = seasonColorMapIndex;
		switch (compositeShape.Format)
		{
		case EnumShapeFormat.Obj:
			objTesselator.Load(objs[compositeShape.Base], out modeldata, texSource["obj"], meta, -1);
			ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
			return;
		case EnumShapeFormat.GltfEmbedded:
		{
			TextureAtlasPosition texPos = ((texSource["gltf"] == game.api.BlockTextureAtlas[new AssetLocation("unknown")]) ? null : texSource["gltf"]);
			gltfTesselator.Load(gltfs[compositeShape.Base], out modeldata, texPos, generalGlowLevel, climateColorMapIndex, seasonColorMapIndex, -1, out var bakedTextures);
			if (compositeShape.InsertBakedTextures)
			{
				gltfs[compositeShape.Base].BaseTextures = new TextureAtlasPosition[bakedTextures.Length];
				gltfs[compositeShape.Base].PBRTextures = new TextureAtlasPosition[bakedTextures.Length];
				gltfs[compositeShape.Base].NormalTextures = new TextureAtlasPosition[bakedTextures.Length];
				for (int j = 0; j < bakedTextures.Length; j++)
				{
					byte[][] bytes = bakedTextures[j];
					if (bytes[0] != null)
					{
						if (!game.api.BlockTextureAtlas.InsertTexture(bytes[0], out var _, out var position3))
						{
							game.Logger.Debug("Failed adding baked in gltf base texture to atlas from: {0}, texture probably too large.", compositeShape.Base);
							gltfs[compositeShape.Base].BaseTextures[j] = game.api.BlockTextureAtlas[new AssetLocation("unknown")];
						}
						else
						{
							gltfs[compositeShape.Base].BaseTextures[j] = position3;
							if (texPos == null)
							{
								modeldata.SetTexPos(position3);
							}
						}
					}
					if (bytes[1] != null)
					{
						if (!game.api.BlockTextureAtlas.InsertTexture(bytes[1], out var _, out var position2))
						{
							game.Logger.Debug("Failed adding baked in gltf pbr texture to atlas from: {0}, texture probably too large.", compositeShape.Base);
						}
						else
						{
							gltfs[compositeShape.Base].PBRTextures[j] = position2;
						}
					}
					if (bytes[2] != null)
					{
						if (!game.api.BlockTextureAtlas.InsertTexture(bytes[2], out var _, out var position))
						{
							game.Logger.Debug("Failed adding baked in gltf normal texture to atlas from: {0}, texture probably too large.", compositeShape.Base);
						}
						else
						{
							gltfs[compositeShape.Base].NormalTextures[j] = position;
						}
					}
				}
			}
			ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
			return;
		}
		}
		if (!shapes.TryGetValue(compositeShape.Base, out var shape))
		{
			if (shapes.Count < 2)
			{
				throw new Exception("Something went wrong in the startup process, no " + type + " shapes have been loaded at all. Please try disabling all mods apart from Essentials, Survival, Creative. If that solves the issue, check which mod is causing this. If that does not solve the issue, please report.");
			}
			game.Logger.Error("Could not find shape {0} for {1} {2}", compositeShape.Base, type, sourceName);
			if (compositeShape.Base is AssetLocationAndSource als)
			{
				game.Logger.Notification(als.Source.ToString());
			}
			throw new FileNotFoundException(string.Concat("Could not find shape file: ", compositeShape.Base, " in ", type, "type ", sourceName, ".  Possibly a broken mod (", sourceName.Domain, ") or different versions of that mod between server and client?"));
		}
		if (!shape.Loaded)
		{
			shape.Load(game, new AssetLocationAndSource(compositeShape.Base));
		}
		rotationVec.Set(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ);
		offsetVec.Set(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
		TesselateShape(shape, out modeldata, rotationVec, offsetVec, compositeShape.Scale, meta);
		if (compositeShape.Overlays != null)
		{
			for (int i = 0; i < compositeShape.Overlays.Length; i++)
			{
				CompositeShape ovCompShape = compositeShape.Overlays[i];
				meta.QuantityElements = quantityElements;
				rotationVec.Set(ovCompShape.rotateX, ovCompShape.rotateY, ovCompShape.rotateZ);
				offsetVec.Set(ovCompShape.offsetX, ovCompShape.offsetY, ovCompShape.offsetZ);
				TesselateShape(shapes[ovCompShape.Base], out var ovModelData, rotationVec, offsetVec, compositeShape.Scale, meta);
				modeldata.AddMeshData(ovModelData);
			}
		}
	}

	public void TesselateShape(CollectibleObject collObj, Shape shape, out MeshData modeldata, Vec3f rotation = null, int? quantityElements = null, string[] selectiveElements = null)
	{
		if (collObj.ItemClass == EnumItemClass.Item)
		{
			TextureSource texSource = new TextureSource(game, game.ItemAtlasManager.Size, collObj as Item);
			TesselateShape("item shape", shape, out modeldata, texSource, rotation, 0, 0, 0, quantityElements, selectiveElements);
		}
		else
		{
			TextureSource texSource2 = new TextureSource(game, game.BlockAtlasManager.Size, collObj as Block);
			TesselateShape("block shape", shape, out modeldata, texSource2, rotation, 0, 0, 0, quantityElements, selectiveElements);
		}
	}

	public void TesselateShape(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f wholeMeshRotation = null, int generalGlowLevel = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, int? quantityElements = null, string[] selectiveElements = null)
	{
		meta.TypeForLogging = typeForLogging;
		meta.TexSource = texSource;
		meta.GeneralGlowLevel = generalGlowLevel;
		meta.GeneralWindMode = 0;
		meta.ClimateColorMapId = climateColorMapId;
		meta.SeasonColorMapId = seasonColorMapId;
		meta.QuantityElements = quantityElements;
		meta.SelectiveElements = selectiveElements;
		meta.WithJointIds = false;
		TesselateShape(shapeBase, out modeldata, wholeMeshRotation, null, 1f, meta);
	}

	public void TesselateShapeWithJointIds(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f rotation, int? quantityElements, string[] selectiveElements)
	{
		meta.TypeForLogging = typeForLogging;
		meta.TexSource = texSource;
		meta.GeneralGlowLevel = 0;
		meta.ClimateColorMapId = 0;
		meta.SeasonColorMapId = 0;
		meta.QuantityElements = quantityElements;
		meta.SelectiveElements = selectiveElements;
		meta.WithJointIds = true;
		TesselateShape(shapeBase, out modeldata, rotation, null, 1f, meta);
	}

	public void TesselateShape(TesselationMetaData meta, Shape shapeBase, out MeshData modeldata)
	{
		this.meta.TypeForLogging = meta.TypeForLogging;
		this.meta.TexSource = meta.TexSource;
		this.meta.GeneralGlowLevel = meta.GeneralGlowLevel;
		this.meta.ClimateColorMapId = meta.ClimateColorMapId;
		this.meta.SeasonColorMapId = meta.SeasonColorMapId;
		this.meta.QuantityElements = meta.QuantityElements;
		this.meta.SelectiveElements = meta.SelectiveElements;
		this.meta.IgnoreElements = meta.IgnoreElements;
		this.meta.WithJointIds = meta.WithJointIds;
		this.meta.WithDamageEffect = meta.WithDamageEffect;
		TesselateShape(shapeBase, out modeldata, meta.Rotation, null, 1f, meta);
	}

	public void TesselateShape(Shape shapeBase, out MeshData modeldata, Vec3f wholeMeshRotation, Vec3f wholeMeshOffset, float wholeMeshScale, TesselationMetaData meta)
	{
		if (wholeMeshRotation == null)
		{
			wholeMeshRotation = noRotation;
		}
		modeldata = new MeshData(24, 36).WithColorMaps().WithRenderpasses();
		if (meta.WithJointIds)
		{
			modeldata.CustomInts = new CustomMeshDataPartInt();
			modeldata.CustomInts.InterleaveSizes = new int[1] { 1 };
			modeldata.CustomInts.InterleaveOffsets = new int[1];
			modeldata.CustomInts.InterleaveStride = 0;
			elementMeshData.CustomInts = new CustomMeshDataPartInt();
		}
		else
		{
			elementMeshData.CustomInts = null;
		}
		if (meta.WithDamageEffect)
		{
			modeldata.CustomFloats = new CustomMeshDataPartFloat();
			modeldata.CustomFloats.InterleaveSizes = new int[1] { 1 };
			modeldata.CustomFloats.InterleaveOffsets = new int[1];
			modeldata.CustomFloats.InterleaveStride = 0;
			elementMeshData.CustomFloats = new CustomMeshDataPartFloat();
		}
		stackMatrix.Clear();
		stackMatrix.PushIdentity();
		Dictionary<string, int[]> texturesSizes = shapeBase.TextureSizes;
		meta.TexturesSizes = texturesSizes;
		meta.defaultTextureSize = new int[2] { shapeBase.TextureWidth, shapeBase.TextureHeight };
		TesselateShapeElements(modeldata, shapeBase.Elements, meta);
		if (wholeMeshScale != 1f)
		{
			modeldata.Scale(constantCenterXZ, wholeMeshScale, wholeMeshScale, wholeMeshScale);
		}
		if (wholeMeshRotation.X != 0f || wholeMeshRotation.Y != 0f || wholeMeshRotation.Z != 0f)
		{
			modeldata.Rotate(constantCenter, wholeMeshRotation.X * ((float)Math.PI / 180f), wholeMeshRotation.Y * ((float)Math.PI / 180f), wholeMeshRotation.Z * ((float)Math.PI / 180f));
		}
		if (wholeMeshOffset != null && !wholeMeshOffset.IsZero)
		{
			modeldata.Translate(wholeMeshOffset);
		}
	}

	private void TesselateShapeElements(MeshData meshdata, ShapeElement[] elements, TesselationMetaData meta)
	{
		int i = 0;
		string[] childIgnoreElements = null;
		foreach (ShapeElement element in elements)
		{
			if (meta.QuantityElements.HasValue && meta.QuantityElements-- <= 0)
			{
				break;
			}
			if (!SelectiveMatch(element.Name, meta.SelectiveElements, out var childSelectiveElements) || (meta.IgnoreElements != null && SelectiveMatch(element.Name, meta.IgnoreElements, out childIgnoreElements)))
			{
				continue;
			}
			if (element.From == null || element.From.Length != 3)
			{
				ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ": shape element " + i + " has illegal from coordinates (not set or not length 3). Ignoring element.");
				break;
			}
			if (element.To == null || element.To.Length != 3)
			{
				ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ": shape element " + i + " has illegal to coordinates (not set or not length 3). Ignoring element.");
				break;
			}
			stackMatrix.Push();
			double rotationOrigin0;
			double rotationOrigin1;
			double rotationOrigin2;
			if (element.RotationOrigin == null)
			{
				rotationOrigin0 = 0.0;
				rotationOrigin1 = 0.0;
				rotationOrigin2 = 0.0;
			}
			else
			{
				rotationOrigin0 = element.RotationOrigin[0];
				rotationOrigin1 = element.RotationOrigin[1];
				rotationOrigin2 = element.RotationOrigin[2];
				stackMatrix.Translate(rotationOrigin0 / 16.0, rotationOrigin1 / 16.0, rotationOrigin2 / 16.0);
			}
			if (element.RotationX != 0.0)
			{
				stackMatrix.Rotate(element.RotationX * (Math.PI / 180.0), 1.0, 0.0, 0.0);
			}
			if (element.RotationY != 0.0)
			{
				stackMatrix.Rotate(element.RotationY * (Math.PI / 180.0), 0.0, 1.0, 0.0);
			}
			if (element.RotationZ != 0.0)
			{
				stackMatrix.Rotate(element.RotationZ * (Math.PI / 180.0), 0.0, 0.0, 1.0);
			}
			if (element.ScaleX != 1.0 || element.ScaleY != 1.0 || element.ScaleZ != 1.0)
			{
				stackMatrix.Scale(element.ScaleX, element.ScaleY, element.ScaleZ);
			}
			stackMatrix.Translate((element.From[0] - rotationOrigin0) / 16.0, (element.From[1] - rotationOrigin1) / 16.0, (element.From[2] - rotationOrigin2) / 16.0);
			if (element.HasFaces())
			{
				elementMeshData.Clear();
				TesselateShapeElement(i, elementMeshData, element, meta);
				elementMeshData.MatrixTransform(stackMatrix.Top);
				meshdata.AddMeshData(elementMeshData);
			}
			i++;
			if (element.Children != null)
			{
				TesselationMetaData cmeta = meta;
				if (childSelectiveElements != null || childIgnoreElements != null)
				{
					cmeta = meta.Clone();
					cmeta.SelectiveElements = childSelectiveElements;
					cmeta.IgnoreElements = childIgnoreElements;
				}
				TesselateShapeElements(meshdata, element.Children, cmeta);
			}
			stackMatrix.Pop();
		}
	}

	private void TesselateShapeElement(int indexForLogging, MeshData meshdata, ShapeElement element, TesselationMetaData meta)
	{
		Size2i atlasSize = meta.TexSource.AtlasSize;
		xyzVec.Set((float)(element.To[0] - element.From[0]) / 16f, (float)(element.To[1] - element.From[1]) / 16f, (float)(element.To[2] - element.From[2]) / 16f);
		Vec3f sizeXyz = xyzVec;
		if (sizeXyz.IsZero)
		{
			return;
		}
		centerVec.Set(sizeXyz.X / 2f, sizeXyz.Y / 2f, sizeXyz.Z / 2f);
		Vec3f relativeCenterXyz = centerVec;
		byte climateColorMapId = 0;
		byte seasonColorMapId = 0;
		short renderPass = element.RenderPass;
		if (element.DisableRandomDrawOffset)
		{
			renderPass += 1024;
		}
		bool firstRenderedFace = true;
		for (int f = 0; f < 6; f++)
		{
			ShapeElementFace face = element.FacesResolved[f];
			if (face == null)
			{
				continue;
			}
			BlockFacing facing = BlockFacing.ALLFACES[f];
			if (firstRenderedFace)
			{
				firstRenderedFace = false;
				climateColorMapId = ((element.ClimateColorMap == null || element.ClimateColorMap.Length == 0) ? meta.ClimateColorMapId : ((byte)(game.ColorMaps.IndexOfKey(element.ClimateColorMap) + 1)));
				seasonColorMapId = (byte)((element.SeasonColorMap == null || element.SeasonColorMap.Length == 0) ? meta.SeasonColorMapId : (game.ColorMaps.TryGetValue(element.SeasonColorMap, out var scm) ? ((byte)(scm.RectIndex + 1)) : 0));
				meta.UsesColorMap |= climateColorMapId + seasonColorMapId > 0;
			}
			float uvCoords0;
			float uvCoords1;
			float uvCoords2;
			float uvCoords3;
			if (face.Uv == null)
			{
				uvCoords0 = 0f;
				uvCoords1 = 0f;
				uvCoords2 = 0f;
				uvCoords3 = 0f;
				if (facing.Axis == EnumAxis.Y)
				{
					uvCoords2 = sizeXyz.X * 16f;
					uvCoords3 = sizeXyz.Z * 16f;
				}
				else if (facing.Axis == EnumAxis.X)
				{
					uvCoords2 = sizeXyz.Z * 16f;
					uvCoords3 = sizeXyz.Y * 16f;
				}
				else if (facing.Axis == EnumAxis.Z)
				{
					uvCoords2 = sizeXyz.X * 16f;
					uvCoords3 = sizeXyz.Y * 16f;
				}
			}
			else
			{
				if (face.Uv.Length != 4)
				{
					ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ", shape element " + indexForLogging + ": Facing '" + facing.Code + "' doesn't have exactly 4 uv values. Ignoring face.");
					continue;
				}
				uvCoords0 = face.Uv[0];
				uvCoords1 = face.Uv[1];
				uvCoords2 = face.Uv[2];
				uvCoords3 = face.Uv[3];
			}
			string texturecode = face.Texture;
			TextureAtlasPosition texPos = meta.TexSource[texturecode];
			if (texPos == null)
			{
				throw new ArgumentNullException("Unable to find a texture for texture code '" + texturecode + "' in " + meta.TypeForLogging + ". Giving up. Sorry.");
			}
			if (!meta.TexturesSizes.TryGetValue(texturecode, out var textureSize))
			{
				textureSize = meta.defaultTextureSize;
			}
			float ratiox = (texPos.x2 - texPos.x1) * (float)atlasSize.Width / (float)textureSize[0];
			float ratioy = (texPos.y2 - texPos.y1) * (float)atlasSize.Height / (float)textureSize[1];
			uvCoords0 *= ratiox;
			uvCoords1 *= ratioy;
			uvCoords2 *= ratiox;
			uvCoords3 *= ratioy;
			if (uvCoords1 == uvCoords3)
			{
				uvCoords3 += 1f / 32f;
			}
			if (uvCoords0 == uvCoords2)
			{
				uvCoords2 += 1f / 32f;
			}
			int rot = (int)(face.Rotation / 90f);
			Vec2f originUv = new Vec2f(texPos.x1 + uvCoords0 / (float)atlasSize.Width, texPos.y1 + uvCoords3 / (float)atlasSize.Height);
			Vec2f sizeUv = new Vec2f((uvCoords2 - uvCoords0) / (float)atlasSize.Width, (uvCoords1 - uvCoords3) / (float)atlasSize.Height);
			sizeUv.X -= Math.Max(0f, originUv.X + sizeUv.X - texPos.x2);
			sizeUv.Y -= Math.Max(0f, originUv.Y + sizeUv.Y - texPos.y2);
			ModelCubeUtilExt.EnumShadeMode shade = ModelCubeUtilExt.EnumShadeMode.On;
			int baseFlags = ((element.ZOffset & 7) << 8) | (meta.GeneralGlowLevel + face.Glow);
			if (face.ReflectiveMode != 0)
			{
				baseFlags |= 0x800;
				sbyte k = (sbyte)Math.Max(0, (int)(face.ReflectiveMode - 1));
				face.WindData = new sbyte[4] { k, k, k, k };
			}
			if (element.Shade)
			{
				baseFlags |= BlockFacing.AllVertexFlagsNormals[facing.Index];
			}
			else if (element.GradientShade)
			{
				shade = ModelCubeUtilExt.EnumShadeMode.Gradient;
			}
			else
			{
				shade = ModelCubeUtilExt.EnumShadeMode.Off;
				baseFlags |= BlockFacing.UP.NormalPackedFlags;
			}
			flags[0] = (flags[1] = (flags[2] = (flags[3] = baseFlags)));
			if (face.WindMode == null)
			{
				int wind = meta.GeneralWindMode << 25;
				if (wind != 0)
				{
					flags[0] |= wind;
					flags[1] |= wind;
					flags[2] |= wind;
					flags[3] |= wind;
					meshdata.HasAnyWindModeSet = true;
				}
			}
			else
			{
				for (int j = 0; j < flags.Length; j++)
				{
					int windMode = face.WindMode[j];
					if (windMode > 0)
					{
						VertexFlags.SetWindMode(ref flags[j], windMode);
						meshdata.HasAnyWindModeSet = true;
					}
				}
			}
			if (face.WindData != null)
			{
				for (int i = 0; i < flags.Length; i++)
				{
					int windData = face.WindData[i];
					if (windData > 0)
					{
						VertexFlags.SetWindData(ref flags[i], windData);
					}
				}
			}
			ModelCubeUtilExt.AddFace(meshdata, facing, relativeCenterXyz, sizeXyz, originUv, sizeUv, texPos.atlasTextureId, element.Color, shade, flags, 1f, rot % 4, climateColorMapId, seasonColorMapId, renderPass);
			if (meta.WithJointIds)
			{
				meshdata.CustomInts.Add(element.JointId, element.JointId, element.JointId, element.JointId);
			}
			if (meta.WithDamageEffect)
			{
				meshdata.CustomFloats.Add(element.DamageEffect, element.DamageEffect, element.DamageEffect, element.DamageEffect);
			}
		}
	}

	public void TesselateBlock(Block block, out MeshData meshdata)
	{
		TextureSource texSource = new TextureSource(game, game.BlockAtlasManager.Size, block);
		TesselateBlock(block, out meshdata, texSource);
	}

	public void TesselateBlock(Block block, out MeshData modeldata, TextureSource textureSource, int? quantityElements = null, string[] selectiveElements = null)
	{
		TesselateBlock(block, block.Shape, out modeldata, textureSource, quantityElements, selectiveElements);
	}

	public void TesselateBlock(Block block, CompositeShape compositeShape, out MeshData modeldata, TextureSource texSource, int? quantityElements = null, string[] selectiveElements = null)
	{
		byte climateColorMapId = (byte)((block.ClimateColorMapResolved != null) ? ((byte)(block.ClimateColorMapResolved.RectIndex + 1)) : 0);
		byte seasonColorMapId = (byte)((block.SeasonColorMapResolved != null) ? ((byte)(block.SeasonColorMapResolved.RectIndex + 1)) : 0);
		meta.GeneralWindMode = (int)block.VertexFlags.WindMode;
		TesselateShape("block", block.Code, compositeShape, out modeldata, texSource, block.VertexFlags.GlowLevel, climateColorMapId, seasonColorMapId, quantityElements, selectiveElements);
		if (compositeShape.Format == EnumShapeFormat.VintageStory)
		{
			block.ShapeUsesColormap |= meta.UsesColorMap || block.ClimateColorMap != null || block.SeasonColorMap != null;
		}
	}

	public void TesselateItem(Item item, out MeshData modeldata, ITexPositionSource texSource)
	{
		meta.GeneralWindMode = 0;
		if (item.Shape == null || item.Shape.VoxelizeTexture)
		{
			CompositeTexture texture = item.FirstTexture;
			if (item.Shape?.Base != null)
			{
				texture = item.Textures[item.Shape.Base.Path.ToString()];
			}
			BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(game, texture.Baked.BakedName.ToString());
			TextureAtlasPosition pos = texSource[texture.Baked.BakedName.ToString()];
			modeldata = VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, pos);
		}
		else
		{
			TesselateShape("item", item.Code, item.Shape, out modeldata, texSource, 0, 0, 0);
		}
	}

	public void TesselateItem(Item item, out MeshData modeldata)
	{
		TesselateItem(item, item.Shape, out modeldata);
	}

	public void TesselateItem(Item item, CompositeShape forShape, out MeshData modeldata)
	{
		meta.GeneralWindMode = 0;
		if (item == null || item.Code == null)
		{
			modeldata = unknownItemModelData;
		}
		else if (forShape == null || forShape.VoxelizeTexture)
		{
			CompositeTexture texture = item.FirstTexture;
			if (forShape?.Base != null && !item.Textures.TryGetValue(forShape.Base.ToShortString(), out texture))
			{
				ScreenManager.Platform.Logger.Warning("Item {0} has no shape defined and has no texture definition. Will use unknown texture.", item.Code);
			}
			if (texture != null)
			{
				int textureSubId = texture.Baked.TextureSubId;
				TextureAtlasPosition pos = game.ItemAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
				BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(texture.Baked.BakedName, "Item code ", item.Code));
				modeldata = VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, pos);
			}
			else
			{
				modeldata = unknownItemModelData;
			}
		}
		else
		{
			TextureSource texSource = new TextureSource(game, game.BlockAtlasManager.Size, item);
			TesselateShape("item", item.Code, forShape, out modeldata, texSource, 0, 0, 0);
		}
	}

	public static MeshData VoxelizeTextureStatic(int[] texturePixels, int width, int height, TextureAtlasPosition pos, Vec3f rotation = null)
	{
		MeshData modeldata = new MeshData(20, 20);
		if (rotation == null)
		{
			rotation = new Vec3f();
		}
		if (pos == null)
		{
			pos = new TextureAtlasPosition();
		}
		float scale = 1.5f;
		float uvWidth = pos.x2 - pos.x1;
		float uvHeight = pos.y2 - pos.y1;
		Vec3f centerXyz = new Vec3f(0f, 0f, 0.5f);
		Vec3f sizeXyz = new Vec3f(scale / (float)width, scale / (float)height, scale / 24f);
		Vec2f originUv = new Vec2f(0f, 0f);
		Vec2f sizeUv = new Vec2f(uvWidth / (float)width, uvHeight / (float)height);
		int[] faceColors = new int[6];
		ModelCubeUtilExt.EnumShadeMode shade = ModelCubeUtilExt.EnumShadeMode.On;
		for (int i = 0; i < 6; i++)
		{
			float brightness = BlockFacing.ALLFACES[i].GetFaceBrightness(rotation.X, rotation.Y, rotation.Z, CubeMeshUtil.DefaultBlockSideShadingsByFacing);
			faceColors[i] = ColorUtil.ColorMultiply3(-1, brightness);
		}
		int textureId = pos.atlasTextureId;
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				centerXyz.X = scale * (float)x / (float)width - (scale - 1f) / 4f;
				centerXyz.Y = scale * (float)y / (float)height - (scale - 1f) / 4f;
				originUv.X = pos.x1 + uvWidth * (float)x / (float)width;
				originUv.Y = pos.y1 + uvHeight * (float)y / (float)height;
				if (((texturePixels[y * width + x] >> 24) & 0xFF) > 5)
				{
					bool num = x > 0 && ((texturePixels[y * width + x - 1] >> 24) & 0xFF) > 5;
					bool downOpaque = y > 0 && ((texturePixels[(y - 1) * width + x] >> 24) & 0xFF) > 5;
					bool num2 = x < width - 1 && ((texturePixels[y * width + x + 1] >> 24) & 0xFF) > 5;
					bool topOpaque = y < height - 1 && ((texturePixels[(y + 1) * width + x] >> 24) & 0xFF) > 5;
					if (!num2)
					{
						ModelCubeUtilExt.AddFace(modeldata, BlockFacing.EAST, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.EAST.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					if (!num)
					{
						ModelCubeUtilExt.AddFace(modeldata, BlockFacing.WEST, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.WEST.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					if (!topOpaque)
					{
						ModelCubeUtilExt.AddFace(modeldata, BlockFacing.UP, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.DOWN.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					if (!downOpaque)
					{
						ModelCubeUtilExt.AddFace(modeldata, BlockFacing.DOWN, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.UP.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					}
					ModelCubeUtilExt.AddFace(modeldata, BlockFacing.NORTH, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.SOUTH.Index], shade, noFlags, 1f, 0, 0, 0, -1);
					ModelCubeUtilExt.AddFace(modeldata, BlockFacing.SOUTH, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.NORTH.Index], shade, noFlags, 1f, 0, 0, 0, -1);
				}
			}
		}
		return modeldata;
	}

	public MeshData VoxelizeTexture(CompositeTexture texture, Size2i atlasSize, TextureAtlasPosition atlasPos)
	{
		BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(texture.Baked.BakedName));
		return VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, atlasPos);
	}

	public MeshData VoxelizeTexture(int[] texturePixels, int width, int height, Size2i atlasSize, TextureAtlasPosition atlasPos)
	{
		return VoxelizeTextureStatic(texturePixels, width, height, atlasPos);
	}

	public int AltTexturesCount(Block block)
	{
		int cnt = 0;
		foreach (CompositeTexture value in block.Textures.Values)
		{
			BakedCompositeTexture[] variants = value.Baked?.BakedVariants;
			if (variants != null && variants.Length > cnt)
			{
				cnt = variants.Length;
			}
		}
		return cnt;
	}

	public int TileTexturesCount(Block block)
	{
		int cnt = 0;
		foreach (CompositeTexture value in block.Textures.Values)
		{
			BakedCompositeTexture[] tiles = value.Baked?.BakedTiles;
			if (tiles != null && tiles.Length > cnt)
			{
				cnt = tiles.Length;
			}
		}
		return cnt;
	}

	public ITexPositionSource GetTexSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false)
	{
		return GetTextureSource(block, altTextureNumber, returnNullWhenMissing);
	}

	public ITexPositionSource GetTextureSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, game.BlockAtlasManager.Size, block, altTextureNumber)
		{
			returnNullWhenMissing = returnNullWhenMissing
		};
	}

	public ITexPositionSource GetTextureSource(Item item, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, game.ItemAtlasManager.Size, item)
		{
			returnNullWhenMissing = returnNullWhenMissing
		};
	}

	public ITexPositionSource GetTextureSource(Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, game.EntityAtlasManager.Size, entity, extraTextures, altTextureNumber)
		{
			returnNullWhenMissing = returnNullWhenMissing
		};
	}

	public void ApplyCompositeShapeModifiers(ref MeshData modeldata, CompositeShape compositeShape)
	{
		if (compositeShape.Scale != 1f)
		{
			modeldata.Scale(constantCenterXZ, compositeShape.Scale, compositeShape.Scale, compositeShape.Scale);
		}
		if (compositeShape.rotateX != 0f || compositeShape.rotateY != 0f || compositeShape.rotateZ != 0f)
		{
			modeldata.Rotate(constantCenter, compositeShape.rotateX * ((float)Math.PI / 180f), compositeShape.rotateY * ((float)Math.PI / 180f), compositeShape.rotateZ * ((float)Math.PI / 180f));
		}
		if (compositeShape.offsetX != 0f || compositeShape.offsetY != 0f || compositeShape.offsetZ != 0f)
		{
			modeldata.Translate(new Vec3f(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ));
		}
	}

	private bool SelectiveMatch(string needle, string[] haystackElements, out string[] childHaystackElements)
	{
		childHaystackElements = null;
		if (haystackElements == null)
		{
			return true;
		}
		for (int i = 0; i < haystackElements.Length; i++)
		{
			string haystack = haystackElements[i];
			if (haystack.Length == 0)
			{
				continue;
			}
			if (haystack == needle)
			{
				childHaystackElements = new string[0];
				return true;
			}
			if (haystack == "*" || haystack.EqualsFast(needle + "/*") || (haystack[haystack.Length - 1] == '*' && needle.StartsWithFast(haystack.Substring(0, haystack.Length - 1))))
			{
				childHaystackElements = new string[1] { "*" };
				return true;
			}
			if (haystack.IndexOf('/') != needle.Length || !haystack.StartsWithFast(needle))
			{
				continue;
			}
			int childSelectionsCount = 0;
			for (int k = i; k < haystackElements.Length; k++)
			{
				if (haystackElements[k].IndexOf('/') == needle.Length && haystackElements[k].StartsWithFast(needle))
				{
					childSelectionsCount++;
				}
			}
			childHaystackElements = new string[childSelectionsCount];
			if (childSelectionsCount > 0)
			{
				int cSEIndex = 0;
				for (int j = i; j < haystackElements.Length; j++)
				{
					haystack = haystackElements[j];
					int slashIndex = haystack.IndexOf('/');
					if (slashIndex == needle.Length && haystack.StartsWithFast(needle))
					{
						childHaystackElements[cSEIndex++] = haystack.Substring(slashIndex + 1);
					}
				}
			}
			return true;
		}
		return false;
	}
}
