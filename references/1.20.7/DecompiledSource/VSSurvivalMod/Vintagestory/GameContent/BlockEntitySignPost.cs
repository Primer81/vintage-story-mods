using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntitySignPost : BlockEntity
{
	public string[] textByCardinalDirection = new string[8];

	private BlockEntitySignPostRenderer signRenderer;

	private int color;

	private int tempColor;

	private ItemStack tempStack;

	private MeshData signMesh;

	private GuiDialogSignPost dlg;

	public string GetTextForDirection(Cardinal dir)
	{
		return textByCardinalDirection[dir.Index];
	}

	public BlockEntitySignPost()
	{
		for (int i = 0; i < 8; i++)
		{
			textByCardinalDirection[i] = "";
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api is ICoreClientAPI)
		{
			CairoFont font = new CairoFont(20.0, GuiStyle.StandardFontName, new double[4] { 0.0, 0.0, 0.0, 0.8 });
			signRenderer = new BlockEntitySignPostRenderer(Pos, (ICoreClientAPI)api, font);
			if (textByCardinalDirection.Length != 0)
			{
				signRenderer.SetNewText(textByCardinalDirection, color);
			}
			Shape shape = Shape.TryGet(api, AssetLocation.Create("shapes/block/wood/signpost/sign.json"));
			if (shape != null)
			{
				(api as ICoreClientAPI).Tesselator.TesselateShape(base.Block, shape, out signMesh);
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
		for (int i = 0; i < 8; i++)
		{
			textByCardinalDirection[i] = tree.GetString("text" + i, "");
		}
		signRenderer?.SetNewText(textByCardinalDirection, color);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("color", color);
		for (int i = 0; i < 8; i++)
		{
			tree.SetString("text" + i, textByCardinalDirection[i]);
		}
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1002)
		{
			using (MemoryStream ms = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(ms);
				for (int i = 0; i < 8; i++)
				{
					textByCardinalDirection[i] = reader.ReadString();
					if (textByCardinalDirection[i] == null)
					{
						textByCardinalDirection[i] = "";
					}
				}
			}
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
			using MemoryStream input = new MemoryStream(data);
			BinaryReader reader2 = new BinaryReader(input);
			reader2.ReadString();
			string dialogTitle = reader2.ReadString();
			for (int j = 0; j < 8; j++)
			{
				textByCardinalDirection[j] = reader2.ReadString();
				if (textByCardinalDirection[j] == null)
				{
					textByCardinalDirection[j] = "";
				}
			}
			_ = (IClientWorldAccessor)Api.World;
			CairoFont font = new CairoFont(20.0, GuiStyle.StandardFontName, new double[4] { 0.0, 0.0, 0.0, 0.8 });
			if (dlg != null && dlg.IsOpened())
			{
				return;
			}
			dlg = new GuiDialogSignPost(dialogTitle, Pos, textByCardinalDirection, Api as ICoreClientAPI, font);
			dlg.OnTextChanged = DidChangeTextClientSide;
			dlg.OnCloseCancel = delegate
			{
				signRenderer.SetNewText(textByCardinalDirection, color);
				(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1003);
			};
			dlg.OnClosed += delegate
			{
				dlg.Dispose();
				dlg = null;
			};
			dlg.TryOpen();
		}
		if (packetid != 1000)
		{
			return;
		}
		using MemoryStream ms = new MemoryStream(data);
		BinaryReader reader = new BinaryReader(ms);
		for (int i = 0; i < 8; i++)
		{
			textByCardinalDirection[i] = reader.ReadString();
			if (textByCardinalDirection[i] == null)
			{
				textByCardinalDirection[i] = "";
			}
		}
		if (signRenderer != null)
		{
			signRenderer.SetNewText(textByCardinalDirection, color);
		}
	}

	private void DidChangeTextClientSide(string[] textByCardinalDirection)
	{
		signRenderer?.SetNewText(textByCardinalDirection, tempColor);
		this.textByCardinalDirection = textByCardinalDirection;
		MarkDirty(redrawOnClient: true);
	}

	public void OnRightClick(IPlayer byPlayer)
	{
		if (byPlayer == null || !(byPlayer.Entity?.Controls?.ShiftKey).GetValueOrDefault())
		{
			return;
		}
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (hotbarSlot == null || !(hotbarSlot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists).GetValueOrDefault())
		{
			return;
		}
		JsonObject jsonObject = hotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
		int r = jsonObject["red"].AsInt();
		int g = jsonObject["green"].AsInt();
		int b = jsonObject["blue"].AsInt();
		tempColor = ColorUtil.ToRgba(255, r, g, b);
		tempStack = hotbarSlot.TakeOut(1);
		hotbarSlot.MarkDirty();
		if (!(Api.World is IServerWorldAccessor))
		{
			return;
		}
		byte[] data;
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter writer = new BinaryWriter(ms);
			writer.Write("BlockEntityTextInput");
			writer.Write(Lang.Get("Edit Sign Text"));
			for (int i = 0; i < 8; i++)
			{
				writer.Write(textByCardinalDirection[i]);
			}
			data = ms.ToArray();
		}
		((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 1001, data);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		signRenderer?.Dispose();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		for (int i = 0; i < 8; i++)
		{
			if (textByCardinalDirection[i].Length != 0)
			{
				Cardinal obj = Cardinal.ALL[i];
				float rotY = 0f;
				switch (obj.Index)
				{
				case 0:
					rotY = 180f;
					break;
				case 1:
					rotY = 135f;
					break;
				case 2:
					rotY = 90f;
					break;
				case 3:
					rotY = 45f;
					break;
				case 5:
					rotY = 315f;
					break;
				case 6:
					rotY = 270f;
					break;
				case 7:
					rotY = 225f;
					break;
				}
				mesher.AddMeshData(signMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, rotY * ((float)Math.PI / 180f), 0f));
			}
		}
		return false;
	}
}
