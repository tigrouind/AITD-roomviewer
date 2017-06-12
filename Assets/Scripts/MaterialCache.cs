using System;
using UnityEngine;
using System.Collections.Generic;

public class MaterialCache : MonoBehaviour
{
    private static Dictionary<Color32, Material> materialsCache = new Dictionary<Color32, Material>();
    public Material TransparentMaterial;
    public Material OpaqueMaterial;
    public Material AlwaysOnTopMaterial;

    public Material GetMaterialFromCache(Color32 color)
    {
        Material material;
        if (!materialsCache.TryGetValue(color, out material))
        {
            if (color.a == 255)
            {
                material = new Material(OpaqueMaterial);
            }
            else if (color.a == 254)
            {
                material = new Material(AlwaysOnTopMaterial);
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