using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class Palette
{
	public static Texture2D GetPaletteTexture()
	{
		Color32[] colors = LoadPalette();

		var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
		texture.SetPixels32(colors);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.Apply();
		return texture;
	}

	static Color32[] LoadPalette()
	{
		string filePath = Config.GetPath("ITD_RESS.PAK");
		if (File.Exists(filePath))
		{
			using (var pak = new UnPAK(filePath))
			{
				var entries = pak.GetEntriesSize();
				for (int i = entries.Count - 1; i >= 0; i--)
				{
					if (entries[i] == 768)
					{
						return LoadPalette(pak.GetEntry(i), 0);
					}
				}
			}
		}

		Regex match = new Regex(@"CAMERA\d\d\.PAK", RegexOptions.IgnoreCase);
		var cameraFiles = Directory.GetFiles(Config.BaseDirectory)
			.Where(x => match.IsMatch(Path.GetFileName(x)))
			.OrderByDescending(x => x);

		foreach (var cameraFile in cameraFiles)
		{
			using (var pak = new UnPAK(cameraFile))
			{
				var entries = pak.GetEntriesSize();
				for (int i = 0; i < entries.Count; i++)
				{
					if (entries[i] == 64768)
					{
						var buffer = pak.GetEntry(i);
						return LoadPalette(buffer, 64000);
					}
				}
			}
		}

		throw new FileNotFoundException("ITD_RESS.PAK (AITD1) or CAMERAxx.PAK (>= AITD2) not found");
	}

	static Color32[] LoadPalette(byte[] buffer, int offset)
	{
		bool mapTo255 = true;
		var src = offset;
		var colors = new Color32[256];
		for (int i = 0; i < 256; i++)
		{
			byte r = buffer[src++];
			byte g = buffer[src++];
			byte b = buffer[src++];

			if (r > 63 || g > 63 || b > 63)
			{
				mapTo255 = false;
			}

			colors[i] = new Color32(r, g, b, 255);
		}

		if (mapTo255)
		{
			for (int i = 0; i < 256; i++)
			{
				Color32 c = colors[i];
				colors[i] = new Color32((byte)(c.r << 2 | c.r >> 4), (byte)(c.g << 2 | c.g >> 4), (byte)(c.b << 2 | c.b >> 4), 255);
			}
		}

		return colors;
	}

	public static Color32 GetRawPaletteColor(Color32[] paletteColors, int colorIndex, int polyType)
	{
		if (polyType == 3 || polyType == 6 || polyType == 4 || polyType == 5)
		{
			return paletteColors[(colorIndex / 16) * 16 + 8];
		}

		return paletteColors[colorIndex];
	}

	public static Color32 GetPaletteColor(Color32[] paletteColors, int colorIndex, int polyType, bool detailLevel)
	{
		Color32 color = paletteColors[colorIndex];

		if (polyType == 1 && detailLevel)
		{
			//noise
			color.r = (byte)(colorIndex % 16 * 16);
			color.g = (byte)(colorIndex / 16 * 16);
		}
		else if (polyType == 2)
		{
			//transparency
			color.a = 128;
		}
		else if ((polyType == 3 || polyType == 6 || polyType == 4 || polyType == 5) && detailLevel)
		{
			//horizontal or vertical gradient
			color.r = (byte)((polyType == 5) ? 127 : 255); //vertical gradient x2
			color.b = (byte)(colorIndex / 16 * 16); //vertical palette index
			color.a = (byte)(colorIndex % 16 * 16); //horizontal palette index
		}

		return color;
	}
}