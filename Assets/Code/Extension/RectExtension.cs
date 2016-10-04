using UnityEngine;

public static class RectExtension {

	public static Rect FromPoints(this Rect rect, Vector2 start, Vector2 end) {
		rect.x = Mathf.Min(start.x, end.x);
		rect.y = Mathf.Min(start.y, end.y);
		rect.width = Mathf.Abs(end.x - start.x);
		rect.height = Mathf.Abs(end.y - start.y);
		return rect;
	}
}