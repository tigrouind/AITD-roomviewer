using System;
using System.Collections.Generic;
using UnityEngine;


public class BoxComparer : IComparer<RaycastHit>
{
	public int Room;

	public int Compare(RaycastHit a, RaycastHit b)
	{
		Box boxA = a.collider.GetComponent<Box>();
		Box boxB = b.collider.GetComponent<Box>();

		//actors have priority over the rest
		int isActorA = boxA.name == "Actor" ? 0 : 1;
		int isActorB = boxB.name == "Actor" ? 0 : 1;
		if (isActorA != isActorB)
		{
			return isActorA.CompareTo(isActorB);
		}

		//if objects are too close each other, check current room
		int aCurrentRoom = boxA.Room == Room ? 0 : 1;
		int bCurrentRoom = boxB.Room == Room ? 0 : 1;
		if (aCurrentRoom != bCurrentRoom)
		{
			return aCurrentRoom.CompareTo(bCurrentRoom);
		}

		// check distance
		if (Mathf.Abs(a.distance - b.distance) >= 0.0005f)
		{
			return a.distance.CompareTo(b.distance);
		}

		//highlighted box has priority
		int highlightA = boxA.HighLight ? 0 : 1;
		int highlightB = boxB.HighLight ? 0 : 1;
		if (highlightA != highlightB)
		{
			return highlightA.CompareTo(highlightB);
		}

		return 0;
	}
}

