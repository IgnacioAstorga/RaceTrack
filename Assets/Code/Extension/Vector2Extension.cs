using UnityEngine;

public static class Vector2Extension {

	public static Vector2 Module(this Vector2 vector, float value) {
		Vector2 module = vector;
		module.x %= value;
		module.y %= value;
		return module;
	}
}