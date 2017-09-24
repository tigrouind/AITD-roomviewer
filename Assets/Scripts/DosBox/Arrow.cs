using System;
using UnityEngine;

public class Arrow : MonoBehaviour
{
	public Material AlwaysOnTopMaterial;

	public Material TransparentMaterial;

	public bool AlwaysOnTop
	{
		set
		{
			GetComponent<Renderer>().sharedMaterial = value ? AlwaysOnTopMaterial : TransparentMaterial;
		}
	}
}