using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

public class Box : MonoBehaviour
{	
	private Color32 color;
	private bool highlighted;
	private bool alwaysOnTop;

	private static Dictionary<Color32, Material> materialsCache = new Dictionary<Color32, Material>();
	public Material TransparentMaterial;
	public Material OpaqueMaterial;
	public Material AlwaysOnTopMaterial;

	public int ID;
	public int Flags;
    public int ColFlags;
	public int Life;
	public int LifeMode;
	public int TrackMode;
	public int Body;
	public int Anim;
	public int Frame;
	public int TotalFrames;
	public int Speed;
	public int Room;
	public uint Chrono;
	public uint RoomChrono;
	public int TrackNumber;
	public Vector3 LocalPosition;
	public Vector3 WorldPosition;
	public Vector3 BoundingPos;
	public Vector3 BoundingSize;
	public Vector3 Angles;
	public Vector3 Mod;

	public bool HighLight
	{
		set
		{
			highlighted = value;
			RefreshMaterial();
		}
	}

	public byte Alpha
	{
		set
		{
			color = new Color32(color.r, color.g, color.b, value);
			RefreshMaterial();
		}
	}

	public bool AlwaysOnTop
	{
		set
		{
			alwaysOnTop = value;
			RefreshMaterial();
		}
	}

	public Color32 Color
	{
		set
		{
			color = value;
			RefreshMaterial();
		}
	}

	private void RefreshMaterial()
	{
		Color32 materialColor = color;
		if (highlighted)
		{
			if (materialColor.a == 255)
			{
				materialColor = new Color32((byte)(Math.Min(materialColor.r + 75, 255)),
					(byte)(Math.Min(materialColor.g + 75, 255)), 
					(byte)(Math.Min(materialColor.b + 75, 255)),
					materialColor.a);
			}
			else
			{
				materialColor = new Color32(materialColor.r, materialColor.g, materialColor.b, (byte)(Math.Min(materialColor.a + 100, 255)));
			}
		}

		if(alwaysOnTop)
		{
			materialColor = new Color32(materialColor.r, materialColor.g, materialColor.b, 254);
		}

		Renderer renderer = this.GetComponent<Renderer>();
		if ((renderer.sharedMaterial == null || renderer.sharedMaterial.color != materialColor))
		{				
			renderer.sharedMaterial = GetMaterialFromCache(materialColor);
		}
	}

	private Material GetMaterialFromCache(Color32 color)
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

	public string ToString(uint timer)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(name.ToUpper() + "\r\nID = " + ID);   
		if (name == "Collider" || name == "Trigger" || name == "Actor")
		{
			sb.AppendFormat("\r\nFLAGS = 0x{0:X4}", Flags);   
		}

		if (name == "Actor")
		{
			sb.AppendFormat("\r\nCOL_FLAGS = 0x{0:X4}", ColFlags);
			sb.AppendFormat("\r\nROOM_POS = {0} {1} {2}", LocalPosition.x, LocalPosition.y, LocalPosition.z);
			sb.AppendFormat("\r\nWORLD_POS = {0} {1} {2}", WorldPosition.x, WorldPosition.y, WorldPosition.z);
			sb.AppendFormat("\r\nBBOX_POS = {0} {1} {2}", BoundingPos.x, BoundingPos.y, BoundingPos.z);
			sb.AppendFormat("\r\nBBOX_SIZE = {0} {1} {2}", BoundingSize.x, BoundingSize.y, BoundingSize.z);
			//sb.AppendFormat("\r\nMOD = {0} {1} {2}", Mod.x, Mod.y, Mod.z);
			sb.AppendFormat("\r\nANGLE = {0:N1} {1:N1} {2:N1}", Angles.x , Angles.y, Angles.z);
			if (Body != -1)
				sb.Append("\r\nBODY = " + Body);
			if (Life != -1)
				sb.Append("\r\nLIFE = " + Life);
			if (LifeMode != -1)
				sb.Append("\r\nLIFEMODE = " + LifeMode);
			if (Anim != -1)
			{
				sb.Append("\r\nANIM = " + Anim);
				if(Frame != -1)
				{
					sb.Append("\r\nFRAME = " + Frame + "/" + (TotalFrames - 1));
				}
				if (Speed != -1)
					sb.Append("\r\nSPEED = " + Speed);
			}

			if (Chrono != 0)
				sb.AppendFormat("\r\nCHRONO = {0}", TimeSpan.FromSeconds((timer - Chrono) / 60));
			if (RoomChrono != 0)
				sb.AppendFormat("\r\nROOM_CHRONO = {0}", TimeSpan.FromSeconds((timer - RoomChrono) / 60));
			if (TrackMode != -1)
				sb.Append("\r\nTRACKMODE = " + TrackMode);
			if (TrackNumber != -1)
				sb.Append("\r\nTRACKNUMBER = " + TrackNumber);           
		}

		return sb.ToString();
	}
}
