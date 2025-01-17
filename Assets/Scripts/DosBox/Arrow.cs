using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
	private static readonly Dictionary<long, Material> materialsCache = new Dictionary<long, Material>();

	public Material AlwaysOnTopMaterial;

	public Material TransparentMaterial;

	public void RefreshMaterial(bool alwaysOnTop, bool highlighted)
	{
		Material mat;
		long key = (alwaysOnTop ? 2 : 0) | (highlighted ? 1 : 0);
		if (!materialsCache.TryGetValue(key, out mat))
		{
			if (alwaysOnTop)
			{
				mat = new Material(AlwaysOnTopMaterial);
				mat.SetFloat("_OffsetUnits", highlighted ? -1000000 : -500000);
			}
			else
			{
				mat = new Material(TransparentMaterial);
			}

			materialsCache.Add(key, mat);
		}

		GetComponent<Renderer>().sharedMaterial = mat;
	}
}