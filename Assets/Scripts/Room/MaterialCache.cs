using System;
using UnityEngine;
using System.Collections.Generic;

public class MaterialCache : MonoBehaviour
{
	private static Dictionary<KeyValuePair<Color32, bool>, Material> materialsCache = new Dictionary<KeyValuePair<Color32, bool>, Material>();
	public Material TransparentMaterial;
	public Material OpaqueMaterial;
	public Material AlwaysOnTopMaterial;

	public Material GetMaterialFromCache(Color32 color, bool alwaysOnTop)
	{
		Material material;
		var key = new KeyValuePair<Color32, bool>(color, alwaysOnTop);
		if (!materialsCache.TryGetValue(key, out material))
		{
			if (alwaysOnTop)
			{
				material = new Material(AlwaysOnTopMaterial);
			}
			else
			{
				if (color.a == 255)
				{
					material = new Material(OpaqueMaterial);
				}
				else
				{
					material = new Material(TransparentMaterial);
				}
			}

			material.color = color;
			materialsCache.Add(key, material);
		}

		return material;
	}
}