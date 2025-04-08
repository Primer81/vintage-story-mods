using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemHighlightBlocks : ClientSystem
{
	private Dictionary<int, BlockHighlight> highlightsByslotId = new Dictionary<int, BlockHighlight>();

	public override string Name => "hibl";

	public SystemHighlightBlocks(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[52] = HandlePacket;
		game.eventManager.RegisterRenderer(OnRenderFrame3DTransparent, EnumRenderStage.OIT, Name, 0.89);
		game.eventManager.OnHighlightBlocks += EventManager_OnHighlightBlocks;
	}

	private void EventManager_OnHighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
	{
		BlockHighlight orCreateHighlight = getOrCreateHighlight(slotId);
		orCreateHighlight.mode = mode;
		orCreateHighlight.shape = shape;
		orCreateHighlight.Scale = scale;
		orCreateHighlight.TesselateModel(game, blocks.ToArray(), colors?.ToArray());
	}

	private void HandlePacket(Packet_Server packet)
	{
		BlockHighlight highlight = getOrCreateHighlight(packet.HighlightBlocks.Slotid);
		if (packet.HighlightBlocks.Blocks.Length == 0)
		{
			highlight.Dispose(game);
			highlightsByslotId.Remove(packet.HighlightBlocks.Slotid);
			return;
		}
		highlight.mode = (EnumHighlightBlocksMode)packet.HighlightBlocks.Mode;
		highlight.shape = (EnumHighlightShape)packet.HighlightBlocks.Shape;
		highlight.Scale = CollectibleNet.DeserializeFloatVeryPrecise(packet.HighlightBlocks.Scale);
		BlockPos[] positions = BlockTypeNet.UnpackBlockPositions(packet.HighlightBlocks.Blocks);
		int count = packet.HighlightBlocks.ColorsCount;
		int[] colors = new int[count];
		if (count > 0)
		{
			Array.Copy(packet.HighlightBlocks.Colors, colors, count);
		}
		highlight.TesselateModel(game, positions, colors);
	}

	public void OnRenderFrame3DTransparent(float deltaTime)
	{
		if (highlightsByslotId.Count == 0)
		{
			return;
		}
		ShaderProgramBlockhighlights prog = ShaderPrograms.Blockhighlights;
		prog.Use();
		Vec3d playerPos = game.EntityPlayer.CameraPos;
		foreach (var (_, highlight) in highlightsByslotId)
		{
			if (highlight.modelRef == null)
			{
				continue;
			}
			if (highlight.mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || highlight.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
			{
				if (game.BlockSelection == null || game.BlockSelection.Position == null)
				{
					continue;
				}
				highlight.origin.X = game.BlockSelection.Position.X + game.BlockSelection.Face.Normali.X;
				highlight.origin.Y = game.BlockSelection.Position.Y + game.BlockSelection.Face.Normali.Y;
				highlight.origin.Z = game.BlockSelection.Position.Z + game.BlockSelection.Face.Normali.Z;
			}
			if (highlight.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
			{
				highlight.origin.X += highlight.attachmentPoints[game.BlockSelection.Face.Index].X;
				highlight.origin.Y += highlight.attachmentPoints[game.BlockSelection.Face.Index].Y;
				highlight.origin.Z += highlight.attachmentPoints[game.BlockSelection.Face.Index].Z;
			}
			game.GlPushMatrix();
			game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
			if (highlight.mode == EnumHighlightBlocksMode.CenteredToBlockSelectionIndex || highlight.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex)
			{
				if (game.BlockSelection == null || game.BlockSelection.Position == null)
				{
					game.GlPopMatrix();
					continue;
				}
				Cuboidf[] boxes = game.GetBlockIntersectionBoxes(game.BlockSelection.Position);
				if (boxes == null || boxes.Length == 0 || game.BlockSelection.SelectionBoxIndex >= boxes.Length)
				{
					game.GlPopMatrix();
					continue;
				}
				BlockPos pos = game.BlockSelection.Position;
				float scale = highlight.Scale;
				Vec3d hitPos = game.BlockSelection.HitPosition;
				int faceIndex = game.BlockSelection.Face.Index;
				double posx;
				double posy;
				double posz;
				if (highlight.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex && highlight.shape != EnumHighlightShape.Cube)
				{
					posx = (float)pos.X + (float)(int)(hitPos.X * 16.0) / 16f + (float)highlight.attachmentPoints[faceIndex].X * scale;
					posy = (float)pos.Y + (float)(int)(hitPos.Y * 16.0) / 16f + (float)highlight.attachmentPoints[faceIndex].Y * scale;
					posz = (float)pos.Z + (float)(int)(hitPos.Z * 16.0) / 16f + (float)highlight.attachmentPoints[faceIndex].Z * scale;
				}
				else
				{
					posx = (float)pos.X + (float)(int)(hitPos.X * 16.0) / 16f;
					posy = (float)pos.Y + (float)(int)(hitPos.Y * 16.0) / 16f;
					posz = (float)pos.Z + (float)(int)(hitPos.Z * 16.0) / 16f;
				}
				if (highlight.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex && highlight.shape == EnumHighlightShape.Cube)
				{
					if (highlight.attachmentPoints[faceIndex].X < 0)
					{
						posx -= Math.Ceiling((float)highlight.Size.X / 2f) * (double)scale;
					}
					else if (highlight.attachmentPoints[faceIndex].X > 0)
					{
						posx += (double)((float)highlight.attachmentPoints[faceIndex].X * scale);
					}
					if (highlight.attachmentPoints[faceIndex].Y < 0)
					{
						posy -= Math.Ceiling((float)highlight.Size.Y / 2f) * (double)scale;
					}
					else if (highlight.attachmentPoints[faceIndex].Y > 0)
					{
						posy += (double)((float)highlight.attachmentPoints[faceIndex].Y * scale);
					}
					if (highlight.attachmentPoints[faceIndex].Z < 0)
					{
						posz -= Math.Ceiling((float)highlight.Size.Z / 2f) * (double)scale;
					}
					else if (highlight.attachmentPoints[faceIndex].Z > 0)
					{
						posz += (double)((float)highlight.attachmentPoints[faceIndex].Z * scale);
					}
				}
				game.GlTranslate((float)(posx - playerPos.X), (float)(posy - playerPos.Y), (float)(posz - playerPos.Z));
				game.GlScale(scale, scale, scale);
			}
			else
			{
				game.GlTranslate((float)((double)highlight.origin.X - playerPos.X), (float)((double)highlight.origin.Y - playerPos.Y), (float)((double)highlight.origin.Z - playerPos.Z));
			}
			prog.ProjectionMatrix = game.CurrentProjectionMatrix;
			prog.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(highlight.modelRef);
			game.GlPopMatrix();
		}
		prog.Stop();
	}

	public override void Dispose(ClientMain game)
	{
		foreach (KeyValuePair<int, BlockHighlight> item in highlightsByslotId)
		{
			item.Value.Dispose(game);
		}
		highlightsByslotId.Clear();
	}

	private BlockHighlight getOrCreateHighlight(int slotId)
	{
		if (!highlightsByslotId.TryGetValue(slotId, out var highlight))
		{
			return highlightsByslotId[slotId] = new BlockHighlight();
		}
		return highlight;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
