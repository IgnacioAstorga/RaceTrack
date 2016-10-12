using UnityEngine;
using System.Collections.Generic;

public static class MonoBehaviourExtension {

	public static T[] GetComponentsInChildrenOnly<T>(this MonoBehaviour mono, bool includeInactive = true) {
		List<T> components = new List<T>();

		Transform transform = mono.transform;
		int childCount = transform.childCount;
		for (int childIndex = 0; childIndex < childCount; childIndex++) {
			Transform child = transform.GetChild(childIndex);
			if (child.gameObject.activeInHierarchy) {
				T component = child.GetComponent<T>();
				if (component != null)
					components.Add(component);
			}
		}

		return components.ToArray();
	}
}