﻿using UnityEngine;
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
	public Timer lastKeyFrameChange = new Timer();
	public Vector3 LocalPosition;
	public Vector3 WorldPosition;
	public Vector3 BoundingLower;
	public Vector3 BoundingUpper;
	public Vector3 Angles;
	public Vector3 Mod;
	public int LastOffset;

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

		if (alwaysOnTop)
		{
			materialColor = new Color32(materialColor.r, materialColor.g, materialColor.b, 254);
		}

		Renderer renderer = this.GetComponent<Renderer>();
		if ((renderer.sharedMaterial == null || renderer.sharedMaterial.color != materialColor))
		{
			renderer.sharedMaterial = GetComponent<MaterialCache>().GetMaterialFromCache(materialColor);
		}
	}

	public void UpdateText(BoxInfo info, uint timer)
	{
		info.Clear();
		info.Append("TYPE", name.ToUpper());
		info.Append("ID", ID);
		if (name == "Collider" || name == "Trigger" || name == "Actor")
		{
			info.Append("FLAGS", "0x{0:X4}", Flags);
		}

		if (name == "Actor")
		{
			info.Append("COL_FLAGS", "0x{0:X4}", ColFlags);

			if (ShowAdditionalInfo)
			{
				info.Append("ROOM", "E{0}R{1}", Floor, Room);
				info.Append("ROOM_POS", "{0} {1} {2}", LocalPosition.x, LocalPosition.y, LocalPosition.z);
				info.Append("WORLD_POS", "{0} {1} {2}", WorldPosition.x, WorldPosition.y, WorldPosition.z);
				info.Append("ZV_POS", "{0} {1} {2}", BoundingPos.x, BoundingPos.y, BoundingPos.z);
				info.Append("ZV_SIZE", "{0} {1} {2}", BoundingSize.x, BoundingSize.y, BoundingSize.z);
				info.Append("OFFSET", LastOffset);
				info.Append("MOD", "{0} {1} {2}", Mod.x, Mod.y, Mod.z);
				info.Append("ANGLE", "{0:N1} {1:N1} {2:N1}",
					Angles.x * 360.0f / 1024.0f,
					Angles.y * 360.0f / 1024.0f,
					Angles.z * 360.0f / 1024.0f);
			}

			if (Body != -1)
			{
				if(Anim != -1)
					info.Append("BODY/ANIM", "{0}; {1}", Body, Anim);
				else
					info.Append("BODY", Body);
			}
			if (Life != -1)
				info.Append("LIFE", Life);
			if (LifeMode != -1)
				info.Append("LIFEMODE", LifeMode);
			if (Anim != -1)
			{
				if (Keyframe != -1)
				{
					info.Append("KEYFRAME", Keyframe + "/" + (TotalFrames - 1));
					if (ShowAdditionalInfo)
					{				
						info.Append("SUB_KEYFRAME", Mathf.FloorToInt(lastKeyFrameChange.Elapsed * 60.0f));
					}
				}
				if (ShowAdditionalInfo)
				{
					info.Append("SPEED", Speed);
				}
			}

			if (ShowAdditionalInfo)
			{
				if (Chrono != 0)
					info.Append("CHRONO", TimeSpan.FromSeconds((timer - Chrono) / 60));
				if (RoomChrono != 0)
					info.Append("ROOM_CHRONO", TimeSpan.FromSeconds((timer - RoomChrono) / 60));
				if (TrackMode != -1)
					info.Append("TRACKMODE", TrackMode);
				if (TrackNumber != -1)
					info.Append("TRACKNUMBER", TrackNumber);
				if (PositionInTrack != -1)
					info.Append("TRACKPOSITION", PositionInTrack);
				info.Append("SLOT", Slot);
			}
		}

		info.UpdateText();
	}
}
