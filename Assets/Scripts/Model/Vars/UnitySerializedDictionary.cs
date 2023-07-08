using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
	[SerializeField]
	List<TKey> keyData = new List<TKey>();

	[SerializeField]
	List<TValue> valueData = new List<TValue>();

	void ISerializationCallbackReceiver.OnBeforeSerialize()
	{
		keyData = Keys.ToList();
		valueData = Values.ToList();
	}

	void ISerializationCallbackReceiver.OnAfterDeserialize()
	{
		Clear();
		for (int i = 0; i < keyData.Count && i < valueData.Count; i++)
		{
			Add(keyData[i], valueData[i]);
		}
	}
}