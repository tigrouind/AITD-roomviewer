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
	public Vector3 LastMod;

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

	public void UpdateText(BoxInfo sb, uint timer, uint timerForKeyFrame)
	{
		sb.Clear();
		sb.AppendFormat("TYPE/ID", "{0}; {1}", name.ToUpper(), ID);
		if (name == "Collider" || name == "Trigger" || name == "Actor")
		{
			sb.AppendFormat("FLAGS", "0x{0:X4}", Flags);
		}

		if (name == "Actor")
		{
			sb.AppendFormat("COL_FLAGS", "0x{0:X4}", ColFlags);

			if (ShowAdditionalInfo)
			{
				sb.AppendFormat("ROOM", "E{0}R{1}", Floor, Room);
				sb.AppendFormat("ROOM_POS", "{0} {1} {2}", LocalPosition.x, LocalPosition.y, LocalPosition.z);
				sb.AppendFormat("WORLD_POS", "{0} {1} {2}", WorldPosition.x, WorldPosition.y, WorldPosition.z);
				sb.AppendFormat("ZV_POS", "{0} {1} {2}", BoundingPos.x, BoundingPos.y, BoundingPos.z);
				sb.AppendFormat("ZV_SIZE", "{0} {1} {2}", BoundingSize.x, BoundingSize.y, BoundingSize.z);
				sb.AppendFormat("MOD", "{0} {1} {2} ({3})", Mod.x, Mod.y, Mod.z, Mathf.FloorToInt(LastMod.magnitude));
				sb.AppendFormat("ANGLE", "{0:N1} {1:N1} {2:N1}",
					Angles.x * 360.0f / 1024.0f,
					Angles.y * 360.0f / 1024.0f,
					Angles.z * 360.0f / 1024.0f);
			}

			if (Body != -1)
			{
				if(Anim != -1)
					sb.AppendFormat("BODY/ANIM", "{0}; {1}", Body, Anim);
				else
					sb.Append("BODY", Body);
			}
			if (Life != -1)
				sb.Append("LIFE", Life);
			if (LifeMode != -1)
				sb.Append("LIFEMODE", LifeMode);
			if (Anim != -1)
			{
				if (Keyframe != -1)
				{
					sb.Append("KEYFRAME", Keyframe + "/" + (TotalFrames - 1));
					if (ShowAdditionalInfo)
					{				
						sb.Append("SUB_KEYFRAME", Math.Max(timerForKeyFrame - LastKeyFrameChange, 0));
					}
				}
				if (ShowAdditionalInfo)
				{
					sb.Append("SPEED", Speed);
				}
			}

			if (ShowAdditionalInfo)
			{
				if (Chrono != 0)
					sb.Append("CHRONO", TimeSpan.FromSeconds((timer - Chrono) / 60));
				if (RoomChrono != 0)
					sb.Append("ROOM_CHRONO", TimeSpan.FromSeconds((timer - RoomChrono) / 60));
				if (TrackMode != -1)
					sb.Append("TRACKMODE", TrackMode);
				if (TrackNumber != -1)
					sb.Append("TRACKNUMBER", TrackNumber);
				if (PositionInTrack != -1)
					sb.Append("TRACKPOSITION", PositionInTrack);
				sb.Append("SLOT", Slot);
			}
		}

		sb.UpdateText();
	}
}
