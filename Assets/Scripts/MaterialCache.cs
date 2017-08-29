using System;
using UnityEngine;
using System.Collections.Generic;

public class MaterialCache : MonoBehaviour
{
    private static Dictionary<Color32, Material> materialsCache = new Dictionary<Color32, Material>();
    public Material TransparentMaterial;
    public Material OpaqueMaterial;
    public Material AlwaysOnTopMaterial;

    public Material GetMaterialFromCache(Color32 color, bool alwaysOnTop)
    {
        Material material;
        if (!materialsCache.TryGetValue(color, out material))
        {
            if (alwaysOnTop)
            {
                material = new Material(AlwaysOnTopMaterial);
            }
            else if (color.a == 255)
            {
                material = new Material(OpaqueMaterial);
            }
            else 
            {
                material = new Material(TransparentMaterial);
            }

            material.color = color;
            materialsCache.Add(color, material);
        }

        return material;
    }
}