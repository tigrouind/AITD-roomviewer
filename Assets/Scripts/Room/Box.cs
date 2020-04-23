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
	private static string[] lifeModeInfo = new string[] { "FLOOR", "ROOM", "CAMERA" };

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
	public int EndFrame;
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
	public Vector2 Box2DLower;
	public Vector2 Box2DUpper;
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
	public Vector3 Col;
	public int HardCol;
	public int HardTrigger;
	public DosBox DosBox;

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

	public string DashIfEmpty(int value)
	{
		if (value == -1)
			return "▬";
		else
			return value.ToString();
	}

	public string GetActorID(int index)
	{
		if(DosBox.SpeedRunMode && index >= 0)
		{
			return index.ToString();
		}

		if(index >= 0 && index < DosBox.Boxes.Length)
		{
			return DosBox.Boxes[index].ID.ToString();
		}

		return "▬";
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

	public void UpdateText(BoxInfo info)
	{
		info.Clear();
		info.Append("TYPE", name.ToUpperInvariant());
		info.Append("ID", ID);

		if (name == "Collider" || name == "Trigger")
		{
			info.Append("FLAGS", "0x{0:X4}", Flags);
		}

		if (name == "Camera" && DosBox != null && DosBox.ShowAdditionalInfo)
		{
			Vector3 position = Camera.transform.localPosition * 1000.0f;
			Vector3 rotation = Camera.transform.rotation.eulerAngles;
			info.Append("POSITION", "{0:F0} {1:F0} {2:F0}", position.x, -position.y, position.z);
			info.Append("ANGLE", "{0:N1} {1:N1} {2:N1}",
				rotation.x > 180.0f ? (rotation.x - 360.0f) : rotation.x,
				(-rotation.y + 540.0f) % 360.0f,
				rotation.z > 180.0f ? (rotation.z - 360.0f) : rotation.z);
		}

		if (name == "Actor")
		{
			if(DosBox.ShowAdditionalInfo)
			{
				info.Append("FLAGS/COL", "0x{0:X4} 0x{1:X4}", Flags, ColFlags);
			}
			else
			{
				info.Append("FLAGS", "0x{0:X4}", Flags);
			}

			if (DosBox.ShowAdditionalInfo)
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
			{
				if(DosBox.ShowAdditionalInfo && LifeMode >= 0 && LifeMode <= 2)
				{
					info.Append("LIFE/LIFEMODE", "{0}; {1}", Life, lifeModeInfo[LifeMode]);
				}
				else
				{
					info.Append("LIFE", Life);
				}

			}

			if (Body != -1)
			{
				if(Anim != -1)
				{
					if(DosBox.ShowAITD1Vars && NextAnim != -1)
						info.Append("BODY/ANIM", "{0}; {1}; {2}", Body, Anim, NextAnim);
					else
						info.Append("BODY/ANIM", "{0}; {1}", Body, Anim);

					if (DosBox.ShowAITD1Vars && AnimType >= 0 && AnimType <= 2)
					{
						info.Append("ANIMTYPE", animTypeInfo[AnimType]);
					}
				}
				else
					info.Append("BODY", Body);
			}

			if (DosBox.ShowAITD1Vars && Anim != -1)
			{
				if (Keyframe != -1)
				{
					info.Append("KEYFRAME", "{0}/{1}; {2} {3}", Keyframe, TotalFrames - 1, EndFrame, EndAnim);
					info.Append("FRAME", Mathf.FloorToInt(lastKeyFrameChange.Elapsed * 60.0f));
				}

				info.Append("SPEED", Speed);
			}

			if (DosBox.ShowAITD1Vars && Chrono != 0)
				info.Append("CHRONO", "{0}.{1:D2}", TimeSpan.FromSeconds((DosBox.InternalTimer1 - Chrono) / 60), (DosBox.InternalTimer1 - Chrono) % 60);
			if (DosBox.ShowAITD1Vars && RoomChrono != 0)
				info.Append("ROOM_CHRONO", "{0}.{1:D2}", TimeSpan.FromSeconds((DosBox.InternalTimer1 - RoomChrono) / 60), (DosBox.InternalTimer1 - RoomChrono) % 60);
			if (DosBox.ShowAdditionalInfo && TrackMode >= 0 && TrackMode <= 3)
				info.Append("TRACKMODE", trackModeInfo[TrackMode]);
			if (DosBox.ShowAITD1Vars && TrackNumber != -1)
				info.Append("TRACKNUM/POS", "{0} {1}", DashIfEmpty(TrackNumber), DashIfEmpty(PositionInTrack));
			if (DosBox.ShowAITD1Vars && ActionType >= 0 && ActionType <= 10)
				info.Append("ACTIONTYPE", actionTypeInfo[ActionType]);
			if (DosBox.ShowAITD1Vars)
				info.Append("2DBOX", "{0} {1}; {2} {3}", Box2DLower.x, Box2DLower.y, Box2DUpper.x, Box2DUpper.y);
			if (DosBox.ShowAITD1Vars)
				info.Append("HITFORCE", HitForce);
			if (DosBox.ShowAITD1Vars)
				info.Append("HIT/BY COL/BY", "{0} {1}  {2} {3} {4} {5}", GetActorID(Hit), GetActorID(HitBy), GetActorID((int)Col.x), GetActorID((int)Col.y), GetActorID((int)Col.z), GetActorID(ColBy));
			if (DosBox.ShowAITD1Vars)
				info.Append("HARDCOL/TRIG", "{0} {1}", DashIfEmpty(HardCol), DashIfEmpty(HardTrigger));
			if (DosBox.ShowAdditionalInfo)
				info.Append("SLOT", Slot);
		}

		info.UpdateText();
	}
}
