using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntitySign : BlockEntity, IRotatable
{
	public string text = "";

	private BlockEntitySignRenderer signRenderer;

	private int color;

	private int tempColor;

	private ItemStack tempStack;

	private float angleRad;

	private float fontSize = 20f;

	public Cuboidf[] colSelBox;

	private BlockSign blockSign;

	private GuiDialogBlockEntityTextInput editDialog;

	private MeshData mesh;

	public bool Translateable { get; set; }

	public virtual float MeshAngleRad
	{
		get
		{
			return angleRad;
		}
		set
		{
			bool changed = angleRad != value;
			angleRad = value;
			if (base.Block?.CollisionBoxes != null)
			{
				colSelBox = new Cuboidf[1] { base.Block.CollisionBoxes[0].RotatedCopy(0f, value * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5)) };
			}
			if (signRenderer != null && base.Block?.Variant["attachment"] != "wall")
			{
				signRenderer.SetFreestanding(angleRad);
			}
			if (changed)
			{
				MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		blockSign = base.Block as BlockSign;
		if (api is ICoreClientAPI)
		{
			signRenderer = new BlockEntitySignRenderer(Pos, (ICoreClientAPI)api, blockSign?.signConfig.CopyWithFontSize(fontSize));
			signRenderer.fontSize = fontSize;
			signRenderer.translateable = Translateable;
			if (text.Length > 0)
			{
				signRenderer.SetNewText(text, color);
			}
			if (base.Block.Variant["attachment"] != "wall")
			{
				signRenderer.SetFreestanding(angleRad);
			}
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		signRenderer?.Dispose();
		signRenderer = null;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		color = tree.GetInt("color");
		if (color == 0)
		{
			color = ColorUtil.BlackArgb;
		}
		text = tree.GetString("text", "");
		if (!tree.HasAttribute("meshAngle"))
		{
			MeshAngleRad = base.Block.Shape.rotateY * ((float)Math.PI / 180f);
		}
		else
		{
			MeshAngleRad = tree.GetFloat("meshAngle");
		}
		fontSize = tree.GetFloat("fontSize", (blockSign?.signConfig?.FontSize).GetValueOrDefault(20f));
		Translateable = tree.GetBool("translateable");
		if (signRenderer != null)
		{
			signRenderer.fontSize = fontSize;
			signRenderer.translateable = Translateable;
			signRenderer.SetNewText(text, color);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("color", color);
		tree.SetString("text", text);
		tree.SetFloat("meshAngle", MeshAngleRad);
		tree.SetFloat("fontSize", fontSize);
		tree.SetBool("translateable", Translateable);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (!Api.World.Claims.TryAccess(player, Pos, EnumBlockAccessFlags.BuildOrBreak))
		{
			player.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return;
		}
		if (packetid == 1002)
		{
			EditSignPacket packet = SerializerUtil.Deserialize<EditSignPacket>(data);
			text = packet.Text;
			fontSize = packet.FontSize;
			color = tempColor;
			MarkDirty(redrawOnClient: true);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
			if (Api.World.Rand.NextDouble() < 0.85)
			{
				player.InventoryManager.TryGiveItemstack(tempStack);
			}
		}
		if (packetid == 1003 && tempStack != null)
		{
			player.InventoryManager.TryGiveItemstack(tempStack);
			tempStack = null;
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			if (editDialog != null && editDialog.IsOpened())
			{
				return;
			}
			editDialog = new GuiDialogBlockEntityTextInput(Lang.Get("Edit Sign text"), Pos, text, Api as ICoreClientAPI, blockSign?.signConfig.CopyWithFontSize(fontSize));
			editDialog.OnTextChanged = DidChangeTextClientSide;
			editDialog.OnCloseCancel = delegate
			{
				signRenderer.SetNewText(text, color);
				(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1003);
			};
			editDialog.TryOpen();
		}
		if (packetid == 1000)
		{
			EditSignPacket packet = SerializerUtil.Deserialize<EditSignPacket>(data);
			if (signRenderer != null)
			{
				signRenderer.fontSize = packet.FontSize;
				signRenderer.SetNewText(packet.Text, color);
			}
		}
	}

	private void DidChangeTextClientSide(string text)
	{
		if (editDialog != null)
		{
			fontSize = editDialog.FontSize;
			signRenderer.fontSize = fontSize;
			signRenderer?.SetNewText(text, tempColor);
		}
	}

	public void OnRightClick(IPlayer byPlayer)
	{
		if (byPlayer == null || !(byPlayer.Entity?.Controls?.ShiftKey).GetValueOrDefault())
		{
			return;
		}
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (hotbarSlot != null && (hotbarSlot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists).GetValueOrDefault())
		{
			JsonObject jsonObject = hotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
			int r = jsonObject["red"].AsInt();
			int g = jsonObject["green"].AsInt();
			int b = jsonObject["blue"].AsInt();
			tempColor = ColorUtil.ToRgba(255, r, g, b);
			tempStack = hotbarSlot.TakeOut(1);
			hotbarSlot.MarkDirty();
			if (Api is ICoreServerAPI sapi)
			{
				sapi.Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 1001);
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		signRenderer?.Dispose();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (base.Block.Variant["attachment"] != "ground")
		{
			return base.OnTesselation(mesher, tessThreadTesselator);
		}
		ensureMeshExists();
		mesher.AddMeshData(mesh);
		return true;
	}

	private void ensureMeshExists()
	{
		mesh = ObjectCacheUtil.GetOrCreate(Api, "signmesh" + base.Block.Code.ToString() + "/" + base.Block.Shape.Base?.ToString() + "/" + MeshAngleRad, delegate
		{
			ICoreClientAPI obj = Api as ICoreClientAPI;
			Shape cachedShape = obj.TesselatorManager.GetCachedShape(base.Block.Shape.Base);
			obj.Tesselator.TesselateShape(base.Block, cachedShape, out mesh, new Vec3f(0f, MeshAngleRad * (180f / (float)Math.PI), 0f));
			return mesh;
		});
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngleRad = tree.GetFloat("meshAngle");
		MeshAngleRad -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngleRad);
	}
}
