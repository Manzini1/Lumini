using Godot;
using System;
using System.Collections.Generic;

namespace Game.Data;

public static class BeatmapData
{
	public static float[] LoadBeatsFromJson(string jsonPath)
	{
		var file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
		if (file == null)
			throw new Exception($"Não consegui abrir beatmap: {jsonPath}");

		string text = file.GetAsText();
		file.Close();

		Variant parsed = Json.ParseString(text);

		// Formato A: [0.52, 0.97, ...]
		if (parsed.VariantType == Variant.Type.Array)
		{
			var arr = parsed.AsGodotArray();
			return ToFloatArray(arr);
		}

		// Formatos B/C: { ... }
		if (parsed.VariantType == Variant.Type.Dictionary)
		{
			var dict = parsed.AsGodotDictionary();

			// Formato B: { "beats": [ ... ] }
			if (dict.ContainsKey("beats"))
			{
				var arr = (Godot.Collections.Array)dict["beats"];
				return ToFloatArray(arr);
			}

			// Formato C (seu): { "markers": [ { "time": 1.36 }, ... ] }
			if (dict.ContainsKey("markers"))
			{
				var markers = (Godot.Collections.Array)dict["markers"];
				return ExtractTimesFromMarkers(markers, jsonPath);
			}

			throw new Exception($"JSON não tem 'beats' nem 'markers': {jsonPath}");
		}

		throw new Exception($"Formato de beatmap inválido: {jsonPath}");
	}

	private static float[] ToFloatArray(Godot.Collections.Array arr)
	{
		var list = new List<float>(arr.Count);
		foreach (var v in arr)
			list.Add((float)v.AsDouble());
		return list.ToArray();
	}

	private static float[] ExtractTimesFromMarkers(Godot.Collections.Array markers, string jsonPath)
	{
		var list = new List<float>(markers.Count);

		foreach (var m in markers)
		{
			if (m.VariantType != Variant.Type.Dictionary)
				continue;

			var md = m.AsGodotDictionary();

			if (!md.ContainsKey("time"))
				continue;

			list.Add((float)md["time"].AsDouble());
		}

		if (list.Count == 0)
			throw new Exception($"'markers' existe, mas nenhum item tem 'time': {jsonPath}");

		list.Sort();
		return list.ToArray();
	}
}
