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

	public bool ShowAdditionalInfo;
	public int ID;
	public int Flags;
	public int ColFlags;
	public int Life;
	public int LifeMode;
	public int TrackMode;
	public int Body;
	public int Anim;
	public int Keyframe;
	public int TotalFrames;
	public int Speed;
	public int Room;
	public int Floor;
	public uint Chrono;
	public uint RoomChrono;
	public int TrackNumber;
	public int PositionInTrack;
	public int Slot;
	public uint LastKeyFrameChange;
	public Vector3 LocalPosition;
	public Vector3 WorldPosition;
	public Vector3 BoundingLower;
	public Vector3 BoundingUpper;
	public Vector3 Angles;
	public Vector3 Mod;

	public Vector3 BoundingPos
	{
		get
		{
			return new Vector3(
				(int)((BoundingUpper.x + BoundingLower.x)) / 2,
				(int)((BoundingUpper.y + BoundingLower.y)) / 2,
				(int)((BoundingUpper.z + BoundingLower.z)) / 2
			);
		}
	}

	public Vector3 BoundingSize
	{
		get
		{
			return new Vector3(
				(BoundingUpper.x - BoundingLower.x),
				(BoundingUpper.y - BoundingLower.y),
				(BoundingUpper.z - BoundingLower.z)
			);
		}
	}

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

		Renderer renderer = this.GetComponent<Renderer>();
		if ((renderer.sharedMaterial == null || renderer.sharedMaterial.color != materialColor))
		{
			renderer.sharedMaterial = GetComponent<MaterialCache>().GetMaterialFromCache(materialColor, alwaysOnTop);
		}
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

			if (ShowAdditionalInfo)
			{
				sb.AppendFormat("\r\nROOM = E{0}R{1}", Floor, Room);
				sb.AppendFormat("\r\nROOM_POS = {0} {1} {2}", LocalPosition.x, LocalPosition.y, LocalPosition.z);
				sb.AppendFormat("\r\nWORLD_POS = {0} {1} {2}", WorldPosition.x, WorldPosition.y, WorldPosition.z);
				sb.AppendFormat("\r\nZV_POS = {0} {1} {2}", BoundingPos.x, BoundingPos.y, BoundingPos.z);
				sb.AppendFormat("\r\nZV_SIZE = {0} {1} {2}", BoundingSize.x, BoundingSize.y, BoundingSize.z);
				sb.AppendFormat("\r\nMOD = {0} {1} {2}", Mod.x, Mod.y, Mod.z);
				sb.AppendFormat("\r\nANGLE = {0:N1} {1:N1} {2:N1}",
					Angles.x * 360.0f / 1024.0f,
					Angles.y * 360.0f / 1024.0f,
					Angles.z * 360.0f / 1024.0f);
			}

			if (Body != -1)
				sb.Append("\r\nBODY = " + Body);
			if (Life != -1)
				sb.Append("\r\nLIFE = " + Life);
			if (LifeMode != -1)
				sb.Append("\r\nLIFEMODE = " + LifeMode);
			if (Anim != -1)
			{
				sb.Append("\r\nANIM = " + Anim);
				if (Keyframe != -1)
				{
					sb.Append("\r\nKEYFRAME = " + Keyframe + "/" + (TotalFrames - 1));
					if (ShowAdditionalInfo)
					{				
						sb.Append("\r\nSUB_KEYFRAME = " + Math.Max(timer - LastKeyFrameChange, 0));
					}
				}
				if (ShowAdditionalInfo)
				{
					if (Speed != -1)
						sb.Append("\r\nSPEED = " + Speed);
				}
			}

			if (ShowAdditionalInfo)
			{
				if (Chrono != 0)
					sb.AppendFormat("\r\nCHRONO = {0}", TimeSpan.FromSeconds((timer - Chrono) / 60));
				if (RoomChrono != 0)
					sb.AppendFormat("\r\nROOM_CHRONO = {0}", TimeSpan.FromSeconds((timer - RoomChrono) / 60));
				if (TrackMode != -1)
					sb.Append("\r\nTRACKMODE = " + TrackMode);
				if (TrackNumber != -1)
					sb.Append("\r\nTRACKNUMBER = " + TrackNumber);
				if (PositionInTrack != -1)
					sb.Append("\r\nTRACKPOSITION = " + PositionInTrack);
				sb.Append("\r\nSLOT = " + Slot);
			}
		}

		return sb.ToString();
	}
}
