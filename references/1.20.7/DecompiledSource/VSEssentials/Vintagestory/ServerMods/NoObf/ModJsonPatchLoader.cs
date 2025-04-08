using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonPatch.Operations;
using JsonPatch.Operations.Abstractions;
using Newtonsoft.Json.Linq;
using Tavis;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

public class ModJsonPatchLoader : ModSystem
{
	private ICoreAPI api;

	private ITreeAttribute worldConfig;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override double ExecuteOrder()
	{
		return 0.05;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		this.api = api;
		worldConfig = api.World.Config;
		if (worldConfig == null)
		{
			worldConfig = new TreeAttribute();
		}
		ApplyPatches();
	}

	public void ApplyPatches(string forPartialPath = null)
	{
		List<IAsset> many = api.Assets.GetMany("patches/");
		int appliedCount = 0;
		int notfoundCount = 0;
		int errorCount = 0;
		int totalCount = 0;
		int unmetConditionCount = 0;
		HashSet<string> loadedModIds = new HashSet<string>(api.ModLoader.Mods.Select((Mod m) => m.Info.ModID).ToList());
		foreach (IAsset asset in many)
		{
			JsonPatch[] patches = null;
			try
			{
				patches = asset.ToObject<JsonPatch[]>();
			}
			catch (Exception e)
			{
				api.Logger.Error("Failed loading patches file {0}:", asset.Location);
				api.Logger.Error(e);
			}
			for (int i = 0; patches != null && i < patches.Length; i++)
			{
				JsonPatch patch = patches[i];
				if (!patch.Enabled)
				{
					continue;
				}
				if (patch.Condition != null)
				{
					IAttribute attr = worldConfig[patch.Condition.When];
					if (attr == null)
					{
						continue;
					}
					if (patch.Condition.useValue)
					{
						patch.Value = new JsonObject(JToken.Parse(attr.ToJsonToken()));
					}
					else if (!patch.Condition.IsValue.Equals(attr.GetValue()?.ToString() ?? "", StringComparison.InvariantCultureIgnoreCase))
					{
						api.Logger.VerboseDebug("Patch file {0}, patch {1}: Unmet IsValue condition ({2}!={3})", asset.Location, i, patch.Condition.IsValue, attr.GetValue()?.ToString() ?? "");
						unmetConditionCount++;
						continue;
					}
				}
				if (patch.DependsOn != null)
				{
					bool enabled = true;
					PatchModDependence[] dependsOn = patch.DependsOn;
					foreach (PatchModDependence dependence in dependsOn)
					{
						bool loaded = loadedModIds.Contains(dependence.modid);
						enabled = enabled && (loaded ^ dependence.invert);
					}
					if (!enabled)
					{
						unmetConditionCount++;
						api.Logger.VerboseDebug("Patch file {0}, patch {1}: Unmet DependsOn condition ({2})", asset.Location, i, string.Join(",", patch.DependsOn.Select((PatchModDependence pd) => (pd.invert ? "!" : "") + pd.modid)));
						continue;
					}
				}
				if (forPartialPath == null || patch.File.PathStartsWith(forPartialPath))
				{
					totalCount++;
					ApplyPatch(i, asset.Location, patch, ref appliedCount, ref notfoundCount, ref errorCount);
				}
			}
		}
		StringBuilder sb = new StringBuilder();
		sb.Append("JsonPatch Loader: ");
		if (totalCount == 0)
		{
			sb.Append("Nothing to patch");
		}
		else
		{
			sb.Append($"{totalCount} patches total");
			if (appliedCount > 0)
			{
				sb.Append($", successfully applied {appliedCount} patches");
			}
			if (notfoundCount > 0)
			{
				sb.Append($", missing files on {notfoundCount} patches");
			}
			if (unmetConditionCount > 0)
			{
				sb.Append($", unmet conditions on {unmetConditionCount} patches");
			}
			if (errorCount > 0)
			{
				sb.Append($", had errors on {errorCount} patches");
			}
			else
			{
				sb.Append(string.Format(", no errors", errorCount));
			}
		}
		api.Logger.Notification(sb.ToString());
		api.Logger.VerboseDebug("Patchloader finished");
	}

	public void ApplyPatch(int patchIndex, AssetLocation patchSourcefile, JsonPatch jsonPatch, ref int applied, ref int notFound, ref int errorCount)
	{
		if (((!jsonPatch.Side.HasValue) ? jsonPatch.File.Category.SideType : jsonPatch.Side.Value) != EnumAppSide.Universal && jsonPatch.Side != api.Side)
		{
			return;
		}
		if (jsonPatch.File == null)
		{
			api.World.Logger.Error("Patch {0} in {1} failed because it is missing the target file property", patchIndex, patchSourcefile);
			return;
		}
		AssetLocation loc = jsonPatch.File.Clone();
		if (jsonPatch.File.Path.EndsWith('*'))
		{
			foreach (IAsset val in api.Assets.GetMany(jsonPatch.File.Path.TrimEnd('*'), jsonPatch.File.Domain, loadAsset: false))
			{
				jsonPatch.File = val.Location;
				ApplyPatch(patchIndex, patchSourcefile, jsonPatch, ref applied, ref notFound, ref errorCount);
			}
			jsonPatch.File = loc;
			return;
		}
		if (!loc.Path.EndsWithOrdinal(".json"))
		{
			loc.Path += ".json";
		}
		IAsset asset = api.Assets.TryGet(loc);
		if (asset == null)
		{
			if (jsonPatch.File.Category == null)
			{
				api.World.Logger.VerboseDebug("Patch {0} in {1}: File {2} not found. Wrong asset category", patchIndex, patchSourcefile, loc);
			}
			else
			{
				EnumAppSide catSide = jsonPatch.File.Category.SideType;
				if (catSide != EnumAppSide.Universal && api.Side != catSide)
				{
					api.World.Logger.VerboseDebug("Patch {0} in {1}: File {2} not found. Hint: This asset is usually only loaded {3} side", patchIndex, patchSourcefile, loc, catSide);
				}
				else
				{
					api.World.Logger.VerboseDebug("Patch {0} in {1}: File {2} not found", patchIndex, patchSourcefile, loc);
				}
			}
			notFound++;
			return;
		}
		Operation op = null;
		switch (jsonPatch.Op)
		{
		case EnumJsonPatchOp.Add:
			if (jsonPatch.Value == null)
			{
				api.World.Logger.Error("Patch {0} in {1} failed probably because it is an add operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
				errorCount++;
				return;
			}
			op = new AddReplaceOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		case EnumJsonPatchOp.AddEach:
			if (jsonPatch.Value == null)
			{
				api.World.Logger.Error("Patch {0} in {1} failed probably because it is an add each operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
				errorCount++;
				return;
			}
			op = new AddEachOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		case EnumJsonPatchOp.Remove:
			op = new RemoveOperation
			{
				Path = new JsonPointer(jsonPatch.Path)
			};
			break;
		case EnumJsonPatchOp.Replace:
			if (jsonPatch.Value == null)
			{
				api.World.Logger.Error("Patch {0} in {1} failed probably because it is a replace operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
				errorCount++;
				return;
			}
			op = new ReplaceOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		case EnumJsonPatchOp.Copy:
			op = new CopyOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				FromPath = new JsonPointer(jsonPatch.FromPath)
			};
			break;
		case EnumJsonPatchOp.Move:
			op = new MoveOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				FromPath = new JsonPointer(jsonPatch.FromPath)
			};
			break;
		case EnumJsonPatchOp.AddMerge:
			op = new AddMergeOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		}
		PatchDocument patchdoc = new PatchDocument(op);
		JToken token;
		try
		{
			token = JToken.Parse(asset.ToText());
		}
		catch (Exception e2)
		{
			api.World.Logger.Error("Patch {0} (target: {2}) in {1} failed probably because the syntax of the value is broken:", patchIndex, patchSourcefile, loc);
			api.World.Logger.Error(e2);
			errorCount++;
			return;
		}
		try
		{
			patchdoc.ApplyTo(token);
		}
		catch (PathNotFoundException p)
		{
			api.World.Logger.Error("Patch {0} (target: {4}) in {1} failed because supplied path {2} is invalid: {3}", patchIndex, patchSourcefile, jsonPatch.Path, p.Message, loc);
			errorCount++;
			return;
		}
		catch (Exception e)
		{
			api.World.Logger.Error("Patch {0} (target: {2}) in {1} failed, following Exception was thrown:", patchIndex, patchSourcefile, loc);
			api.World.Logger.Error(e);
			errorCount++;
			return;
		}
		string text = token.ToString();
		asset.Data = Encoding.UTF8.GetBytes(text);
		asset.IsPatched = true;
		applied++;
	}
}
