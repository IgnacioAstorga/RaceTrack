using UnityEngine;
using System.Collections.Generic;

public static class RectExtension {

	public static Rect FromPoints(this Rect rect, Vector2 start, Vector2 end) {
		rect.x = Mathf.Min(start.x, end.x);
		rect.y = Mathf.Min(start.y, end.y);
		rect.width = Mathf.Abs(end.x - start.x);
		rect.height = Mathf.Abs(end.y - start.y);
		return rect;
	}

	public static Rect FromPoints(this Rect rect, IEnumerable<Vector2> points) {
		Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
		Vector2 max = new Vector2(float.MinValue, float.MinValue);
		foreach (Vector2 point in points) {
			min.x = Mathf.Min(point.x, min.x);
			min.y = Mathf.Min(point.y, min.y);
			max.x = Mathf.Max(point.x, max.x);
			max.y = Mathf.Max(point.y, max.y);
		}
		return rect.FromPoints(min, max);
	}

	public static Rect Expand(this Rect rect, float radius) {
		return rect.Expand(new Vector2(radius, radius));
	}

	public static Rect Expand(this Rect rect, Vector2 size) {
		rect.x -= size.x;
		rect.y -= size.y;
		rect.width += 2 * size.x;
		rect.height += 2 * size.y;
		return rect;
	}
}