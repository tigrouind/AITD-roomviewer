using System;
using UnityEngine;
using System.Collections.Generic;

public class MaterialCache : MonoBehaviour
{
	private static readonly Dictionary<long, Material> materialsCache = new Dictionary<long, Material>();
	public Material TransparentMaterial;
	public Material OpaqueMaterial;
	public Material AlwaysOnTopMaterial;

	public Material GetMaterialFromCache(Color32 color, bool alwaysOnTop, bool highlighted)
	{
		Material material;
		var key = (highlighted ? 0L : 1L) << 33 | (alwaysOnTop ? 0L : 1L) << 32 | (long)color.r << 24 | (long)color.g << 16 | (long)color.b << 8 | color.a;
		if (!materialsCache.TryGetValue(key, out material))
		{
			if (alwaysOnTop)
			{
				material = new Material(AlwaysOnTopMaterial);
				material.SetFloat("_OffsetUnits", highlighted ? - 1000000 : -500000);
			}
			else if (color.a == 255)
			{
				if (highlighted)
				{
					material = new Material(AlwaysOnTopMaterial);
					material.SetFloat("_OffsetUnits", -1000);
				}
				else
				{
					material = new Material(OpaqueMaterial);
				}
			}
			else
			{
				material = new Material(TransparentMaterial);
				if (highlighted)
				{
					material.renderQueue++;
				}
			}

			material.color = color;
			materialsCache.Add(key, material);
		}

		return material;
	}
}