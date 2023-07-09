using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

class Texture
{
	public static int LoadTextures(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (var pak = new UnPAK(filePath))
			{
				return pak.EntryCount;
			}
		}

		return default(int);
	}

	public static void LoadTextures(byte[] buffer, Color32[] paletteColors, string textureFolder, int textureCount, bool detailLevel,
		out int uvStart, out int texAHeight, out int texBHeight, out Texture2D texA, out Texture2D texB)
	{
		var offset = buffer[0xE];
		paletteColors[0] = Color.clear;

		using (var pak = new UnPAK(textureFolder))
		{
			texA = LoadTexture(pak, buffer.ReadUnsignedShort(offset + 12), paletteColors, textureCount, detailLevel);
			texB = LoadTexture(pak, buffer.ReadUnsignedShort(offset + 14), paletteColors, textureCount, detailLevel);
		}

		uvStart = buffer.ReadShort(offset + 6);
		texAHeight = texA.height;
		texBHeight = texB.height;
	}

	static Texture2D LoadTexture(UnPAK pak, int textureIndex, Color32[] paletteColors, int textureCount, bool detailLevel)
	{
		if (textureIndex >= 0 && textureIndex < textureCount)
		{
			var tex256 = pak.GetEntry(textureIndex);
			FixBlackBorders(tex256);

			var texSize = tex256.Length;
			Color32[] textureData = new Color32[texSize];
			for (int i = 0; i < tex256.Length; i++)
			{
				textureData[i] = paletteColors[tex256[i]];
			}

			Texture2D tex = new Texture2D(256, texSize / 256, TextureFormat.ARGB32, false);
			tex.filterMode = detailLevel ? FilterMode.Bilinear : FilterMode.Point;
			tex.SetPixels32(textureData);
			tex.Apply();

			return tex;
		}

		return EmptyTexture();
	}

	static Texture2D EmptyTexture()
	{
		Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
		tex.SetPixel(1, 1, Color.magenta);
		tex.Apply();
		return tex;
	}

	static void FixBlackBorders(byte[] tex256)
	{
		int width = 256;
		int height = tex256.Length / 256;

		byte[] result = tex256.ToArray();
		for (int j = 1; j < height - 1; j++)
		{
			for (int i = 1; i < width - 1; i++)
			{
				int n = i + j * 256;
				if (tex256[n] == 0) //black/transparent
				{
					if (tex256[n + 1] != 0) result[n] = tex256[n + 1];
					else if (tex256[n - 1] != 0) result[n] = tex256[n - 1];
					else if (tex256[n + 256] != 0) result[n] = tex256[n + 256];
					else if (tex256[n - 256] != 0) result[n] = tex256[n - 256];
					else if (tex256[n + 256 + 1] != 0) result[n] = tex256[n + 256 + 1];
					else if (tex256[n + 256 - 1] != 0) result[n] = tex256[n + 256 - 1];
					else if (tex256[n - 256 + 1] != 0) result[n] = tex256[n - 256 + 1];
					else if (tex256[n - 256 - 1] != 0) result[n] = tex256[n - 256 - 1];
				}
			}
		}

		Array.Copy(result, tex256, tex256.Length);
	}
}