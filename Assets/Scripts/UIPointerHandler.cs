using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

[System.Serializable]
public class PointerEventHandler : UnityEvent<PointerEventData>
{
}

public class UIPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public PointerEventHandler PointerEnter = new PointerEventHandler();

	public PointerEventHandler PointerExit = new PointerEventHandler();

	public void OnPointerEnter(PointerEventData eventData)
	{
		PointerEnter.Invoke(eventData);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		PointerExit.Invoke(eventData);
	}
}