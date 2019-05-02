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
	private static string[] animTypeInfo = new string[] { "ONCE", "REPEAT", "UNINTERRUPT" };
	private static string[] trackModeInfo = new string[] { "NONE", "MANUAL", "FOLLOW", "TRACK" };
	private static string[] actionTypeInfo = new string[] { "NONE", "PRE_HIT", "HIT", "UNKNOWN", "PRE_FIRE", "FIRE", "PRE_THROW", "THROW", "HIT_OBJ", "DURING_THROW", "PRE_HIT" };

	public bool ShowAITD1Vars;
	public bool ShowAdditionalInfo;
	public int ID;
	public int Flags;
	public int ColFlags;
	public int Life;
	public int LifeMode;
	public int TrackMode;
	public int Body;
	public int Anim;
	public int NextAnim;
	public int AnimType;
	public int ActionType;
	public int Keyframe;
	public int PreviousAnim;
	public int PreviousKeyFrame;
	public int Endframe;
	public int EndAnim;
	public int TotalFrames;
	public int Speed;
	public int Room;
	public int Floor;
	public uint Chrono;
	public uint RoomChrono;
	public int TrackNumber;
	public int PositionInTrack;
	public int Slot;
	public int HitForce;
	public Timer lastKeyFrameChange = new Timer();
	public Vector3 Mod;
	public Vector3 LocalPosition;
	public Vector3 WorldPosition;
	public Vector3 BoundingLower;
	public Vector3 BoundingUpper;
	public Vector3 Angles;
	public int LastOffset;
	public float LastDistance;
	public int HotBoxSize;
	public Vector3 HotPosition;
	public Box BoxHotPoint;
	public Box BoxWorldPos;
	public int OldAngle; 
	public int NewAngle;
	public int RotateTime;
	public Box Camera;
	public int HitBy;
	public int Hit;
	public int ColBy;
	public int[] Col = new int[3];
	public int HardCol;
	public int HardTrigger;

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
		get
		{
			return highlighted;
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

	public string RotateDir
	{
		get
		{
			int diff = NewAngle - OldAngle;
			if(RotateTime != 0 && diff != 0)
			{
				if(diff > 0)
				{
					return "▲";
				}									
				else
				{
					return "▼";
				}
			}
			else
			{
				return string.Empty;
			}
		}
	}

	public string GetActorID(int index, Box[] actors)
	{
		if(index >= 0 && index < actors.Length)
		{
			return actors[index].ID.ToString();
		}
		else
		{
			return "▬";
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
		Material material = GetComponent<MaterialCache>().GetMaterialFromCache(materialColor, alwaysOnTop);
		if ((renderer.sharedMaterial == null || renderer.sharedMaterial != material))
		{
			renderer.sharedMaterial = material;
		}
	}

	void OnDisable()
	{
		if(BoxHotPoint != null)
		{
			BoxHotPoint.gameObject.SetActive(false);
		}
		if(BoxWorldPos != null)
		{
			BoxWorldPos.gameObject.SetActive(false);
		}
	}

	void OnEnable()
	{
		if(BoxHotPoint != null)
		{
			BoxHotPoint.gameObject.SetActive(true);
		}
		if(BoxWorldPos != null)
		{
			BoxWorldPos.gameObject.SetActive(true);
		}
	}

	public void UpdateText(BoxInfo info, Box[] actors, uint timer)
	{
		info.Clear();
		info.Append("TYPE", name.ToUpperInvariant());
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
				info.Append("ROOM_POS", LocalPosition + Mod);
				info.Append("WORLD_POS", WorldPosition + Mod);
				info.Append("ZV_POS", BoundingPos);
				info.Append("ZV_SIZE", BoundingSize);
				info.Append("MOD", Mod);
				info.Append("OFFSET", LastOffset);
				info.Append("DISTANCE", Mathf.RoundToInt(LastDistance));
				if(Angles.x == 0.0f && Angles.z == 0.0f)
				{
					info.Append("ANGLE", "{0:N1} {1}",
						Angles.y * 360.0f / 1024.0f,
						RotateDir);
				}
				else
				{
					info.Append("ANGLE", "{0:N1} {1:N1} {2:N1}",
						Angles.x * 360.0f / 1024.0f,
						Angles.y * 360.0f / 1024.0f,
						Angles.z * 360.0f / 1024.0f);
				}
			}

			if (Life != -1)
				info.Append("LIFE", Life);
			if (LifeMode != -1)
				info.Append("LIFEMODE", LifeMode);

			if (Body != -1)
			{
				if(Anim != -1)
				{
					if(ShowAITD1Vars && NextAnim != -1)
						info.Append("BODY/ANIM", "{0}; {1}; {2}", Body, Anim, NextAnim);
					else
						info.Append("BODY/ANIM", "{0}; {1}", Body, Anim);

					if (ShowAITD1Vars && AnimType >= 0 && AnimType <= 2)
					{
						info.Append("ANIMTYPE", animTypeInfo[AnimType]);
					}
				}
				else
					info.Append("BODY", Body);
			}

			if (ShowAITD1Vars && Anim != -1)
			{
				if (Keyframe != -1)
				{
					info.Append("KEYFRAME", "{0}/{1}", Keyframe, TotalFrames - 1);
					info.Append("SUB_KEYFRAME", Mathf.FloorToInt(lastKeyFrameChange.Elapsed * 60.0f));
				}

				info.Append("SPEED", Speed);
			}

			if (ShowAITD1Vars && Chrono != 0)
				info.Append("CHRONO", "{0}.{1:D2}", TimeSpan.FromSeconds((timer - Chrono) / 60), (timer - Chrono) % 60);
			if (ShowAITD1Vars && RoomChrono != 0)
				info.Append("ROOM_CHRONO", "{0}.{1:D2}", TimeSpan.FromSeconds((timer - RoomChrono) / 60), (timer - RoomChrono) % 60);
			if (ShowAdditionalInfo && TrackMode >= 0 && TrackMode <= 3)
				info.Append("TRACKMODE", trackModeInfo[TrackMode]);
			if (ShowAITD1Vars && TrackNumber != -1)
				info.Append("TRACKNUMBER", TrackNumber);
			if (ShowAITD1Vars && PositionInTrack != -1)
				info.Append("TRACKPOSITION", PositionInTrack);
			if (ShowAITD1Vars && ActionType >= 0 && ActionType <= 10)
				info.Append("ACTIONTYPE", actionTypeInfo[ActionType]);
			if (ShowAITD1Vars)
				info.Append("HITFORCE", HitForce);
			if (ShowAITD1Vars)
				info.Append("HIT/HITBY", "{0} {1}", GetActorID(Hit, actors), GetActorID(HitBy, actors));
			if (ShowAITD1Vars)
				info.Append("COL/COLBY", "{0} {1} {2} {3}", GetActorID(Col[0], actors), GetActorID(Col[1], actors), GetActorID(Col[2], actors), GetActorID(ColBy, actors));
			if (ShowAITD1Vars)
				info.Append("HARDCOL/TRIG", "{0} {1}", 
					(HardCol == -1) ? "▬" : HardCol.ToString(), 
					(HardTrigger == -1) ? "▬" : HardTrigger.ToString());
			if (ShowAdditionalInfo)
				info.Append("SLOT", Slot);
		}

		info.UpdateText();
	}
}
