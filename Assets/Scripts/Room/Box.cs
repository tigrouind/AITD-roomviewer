using UnityEngine;
using System;
using System.Text;

public class Box : MonoBehaviour
{
	private Color32 color;
	private bool highlighted;
	private bool alwaysOnTop;
	private static string[] animTypeInfo = { "ONCE", "REPEAT", "UNINTERRUPT" };
	private static string[] trackModeInfo = { "NONE", "MANUAL", "FOLLOW", "TRACK" };
	private static string[] actionTypeInfo = { "NONE", "PRE_HIT", "HIT", "UNKNOWN", "PRE_FIRE", "FIRE", "PRE_THROW", "THROW", "HIT_OBJ", "DURING_THROW", "PRE_HIT" };
	private static string[] lifeModeInfo = { "FLOOR", "ROOM", "CAMERA" };
	private static string[] particleNames = { "BUBBLES", "BLOOD/DEBRIS", string.Empty, "FLASH", "SMOKE" };
	private static string[] flagsNames = { "ANIM", string.Empty, string.Empty, "BACK", "PUSH", "COLL", "TRIG", "PICK", "GRAV" };
	private static string[] speedNames = { "BACK", "IDLE", "WALK", "WALK", "WALK", "WALK", "RUN" };

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
	public int KeyFrameTime;
	public int KeyFrameLength;
	public Vector3Int Mod;
	public Vector3Int LocalPosition;
	public Vector3Int WorldPosition;
	public Vector3Int BoundingLower;
	public Vector3Int BoundingUpper;
	public Vector2Int Box2DLower;
	public Vector2Int Box2DUpper;
	public Vector3Int Angles;
	public int LastOffset;
	public float LastDistance;
	public int HotBoxSize;
	public Vector3Int HotPosition;
	public Box BoxHotPoint;
	public Box BoxWorldPos;
	public int OldAngle;
	public int NewAngle;
	public int RotateTime;
	public int HitBy;
	public int Hit;
	public int ColBy;
	public Vector3Int Col;
	public int HardCol;
	public int HardTrigger;
	public DosBox DosBox;
	public Vector3Int CameraPosition;
	public Vector3Int CameraRotation;
	public Vector3Int CameraFocal;

	public Vector3Int BoundingPos
	{
		get
		{
			return new Vector3Int(
				(BoundingUpper.x + BoundingLower.x) / 2,
				(BoundingUpper.y + BoundingLower.y) / 2,
				(BoundingUpper.z + BoundingLower.z) / 2
			);
		}
	}

	public Vector3Int BoundingSize
	{
		get
		{
			return new Vector3Int(
				BoundingUpper.x - BoundingLower.x,
				BoundingUpper.y - BoundingLower.y,
				BoundingUpper.z - BoundingLower.z
			);
		}
	}

	public bool HighLight
	{
		set
		{
			if (value != highlighted)
			{
				highlighted = value;
				RefreshMaterial();
			}			
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
			if (value != color.a)
			{
				color = new Color32(color.r, color.g, color.b, value);
				RefreshMaterial();
			}			
		}
	}

	public bool AlwaysOnTop
	{
		set
		{
			if (value != alwaysOnTop)
			{
				alwaysOnTop = value;
				RefreshMaterial();
			}
		}
	}

	public Color32 Color
	{
		set
		{
			if (value.r != color.r || value.g != color.g || value.b != color.b || value.a != color.a)
			{
				color = value;
				RefreshMaterial();
			}			
		}
	}

	string RotateDir
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

	string DashIfEmpty(int value)
	{
		if (value == -1)
			return "▬";
		else
			return value.ToString();
	}

	string GetActorID(int index)
	{
		if(DosBox.SpeedRunMode && index >= 0)
		{
			return index.ToString();
		}

		if(index >= 0 && index < DosBox.Boxes.Length)
		{
			Box box = DosBox.Boxes[index];
			if (box != null)
			{
				return box.ID.ToString();
			}
		}

		return "▬";
	}

	string GetFlags(int flags)
	{
		if(flags == 0)
		{
			return "NONE";
		}

		var result = new StringBuilder();
		int flag = 1;
		for (int i = 0 ; i < flagsNames.Length ; i++)
		{
			if ((flags & flag) != 0 && !string.IsNullOrEmpty(flagsNames[i])) 
			{				
				if (result.Length > 0) 
				{
					result.Append(' ');
				}
				result.Append(flagsNames[i]);
			}
			flag <<= 1;
		}
		
		return result.ToString();
	}

	void RefreshMaterial()
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

		Renderer renderer = GetComponent<Renderer>();
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
		
		if (name == "Actor" && DosBox.IsCDROMVersion && ID == -2 && Anim >= 0 && Anim <= 4 && !string.IsNullOrEmpty(particleNames[Anim]))
		{			
			info.Append(particleNames[Anim], "#{0}", Keyframe);
		}
		else
		{
			info.Append(name.ToUpperInvariant(), "#{0}", ID);
		}
		
		if (name == "Camera" && DosBox.ShowAdditionalInfo)
		{
			Vector3Int position = CameraPosition * 10;
			Vector3 rotation = (Vector3)CameraRotation * (360.0f / 1024.0f);
			info.Append("POSITION", "{0} {1} {2}", position.x, -position.y, -position.z);
			info.Append("ANGLE", "{0:N1} {1:N1} {2:N1}",
				rotation.x > 180.0f ? (rotation.x - 360.0f) : rotation.x,
				(-rotation.y + 540.0f) % 360.0f,
				rotation.z > 180.0f ? (rotation.z - 360.0f) : rotation.z);
			info.Append("FOCAL", "{0} {1} {2}", CameraFocal.y, CameraFocal.z, CameraFocal.x);
		}

		if (name == "Actor")
		{
			if (DosBox.ShowAdditionalInfo)
			{
				info.Append("FLAGS/COL", "{0}; {1}", GetFlags(Flags), ColFlags != 0 ? "Y" : "N");
			}
			else
			{
				info.Append("FLAGS", GetFlags(Flags));
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

			if (Body != -1 && ID >= 0)
			{
				if(Anim != -1)
				{
					if(DosBox.ShowAITD1Vars && NextAnim != -1)
					{
						info.Append("BODY/ANIM", "{0}; {1}; {2}", Body, Anim, NextAnim);
					}						
					else
					{
						info.Append("BODY/ANIM", "{0}; {1}", Body, Anim);
					}						

					if (DosBox.ShowAITD1Vars && AnimType >= 0 && AnimType <= 2)
					{
						info.Append("ANIMTYPE", animTypeInfo[AnimType]);
					}
				}
				else
				{
					info.Append("BODY", Body);
				}					
			}

			if (DosBox.ShowAITD1Vars && Anim != -1 && (Flags & 1) == 1) //animated
			{
				if (Keyframe != -1)
				{
					info.Append("KEYFRAME", "{0}/{1}; {2} {3}", Keyframe, TotalFrames - 1, EndFrame, EndAnim);
					info.Append("FRAME", "{0}/{1}", Math.Min(Math.Max(DosBox.InternalTimer2 - KeyFrameTime, 0), KeyFrameLength), KeyFrameLength);
				}

				if (Speed >= -1 && Speed <= 5)
				{
					info.Append("SPEED", "{0} ({1})", speedNames[Speed + 1], Speed);	
				}				
			}

			if(DosBox.ShowAITD1Vars)
			{
				if (Chrono != 0)
				{
					info.Append("CHRONO", "{0}.{1:D2}", TimeSpan.FromSeconds((DosBox.InternalTimer1 - Chrono) / 60), (DosBox.InternalTimer1 - Chrono) % 60);
				}
					
				if (RoomChrono != 0)
				{
					info.Append("ROOM_CHRONO", "{0}.{1:D2}", TimeSpan.FromSeconds((DosBox.InternalTimer1 - RoomChrono) / 60), (DosBox.InternalTimer1 - RoomChrono) % 60);
				}					
			}

			if (DosBox.ShowAdditionalInfo && TrackMode >= 0 && TrackMode <= 3)
				info.Append("TRACKMODE", trackModeInfo[TrackMode]);

			if(DosBox.ShowAITD1Vars)
			{
				if (TrackNumber != -1)
				{
					if (TrackMode == 3) //track
					{
						info.Append("TRACKNUM/POS", "{0} {1}", TrackNumber, PositionInTrack);
					}
						
					if (TrackMode == 2) //follow
					{
						info.Append("TRACKNUM", "{0}", TrackNumber);				
					}						
				}
									
				if (ActionType >= 0 && ActionType <= 10)
				{
					info.Append("ACTIONTYPE", actionTypeInfo[ActionType]);
				}					
					
				info.Append("2DBOX", "{0} {1}; {2} {3}", DashIfEmpty(Box2DLower.x), DashIfEmpty(Box2DLower.y), DashIfEmpty(Box2DUpper.x), DashIfEmpty(Box2DUpper.y));
				info.Append("HITFORCE", HitForce);
				info.Append("HIT/BY COL/BY", "{0} {1}  {2} {3} {4} {5}", GetActorID(Hit), GetActorID(HitBy), GetActorID((int)Col.x), GetActorID((int)Col.y), GetActorID((int)Col.z), GetActorID(ColBy));
				info.Append("HARDCOL/TRIG", "{0} {1}", DashIfEmpty(HardCol), DashIfEmpty(HardTrigger));
			}
			
			if (DosBox.ShowAdditionalInfo)
			{
				info.Append("SLOT", Slot);
			}				
		}

		info.UpdateText();
	}
}
