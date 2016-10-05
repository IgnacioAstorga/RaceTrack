using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Shape2DWindow : EditorWindow {
	
	private bool HasSelection { get { return _selection.Count > 0; } }
	private Rect CurrentArea { get { return _areas.Peek(); } }

	private float _scale = 100;
	private float _fixedScale = 100;
	private Vector2 _offset = Vector2.zero;

	private float _pointRadius = 5f;
	private float _lineWidth = 5f;
	private float _normalLength = 0.35f;
	private float _normalHandleSize = 0.75f;

	private float _cursorRectScale = 2f;

	private float _shapeSelectorHeight = 20f;
	private float _selectedPanelHeight = 80f;

	private Stack<Rect> _areas = new Stack<Rect>();

	private Shape2D _shape2D;
	
	private HashSet<int> _selection = new HashSet<int>();
	private Vector2 _selectionStart;

	private Vector2[] points;
	private Vector2[] normalHandles;

	[MenuItem("Window/Shape 2D Editor")]
	public static void ShowWindow() {
		GetWindow<Shape2DWindow>("Shape 2D Editor");
	}

	public void LoadShape2D(Shape2D shape2D) {
		_shape2D = shape2D;
	}

	void OnGUI() {
		_areas.Clear();
		_areas.Push(position);

		// Retrieves the shape
		if (_shape2D == null)
			_selection.Clear();
		_shape2D = (Shape2D)EditorGUILayout.ObjectField(_shape2D, typeof(Shape2D), false);
		if (Selection.activeObject != null && Selection.activeObject is Shape2D)
			LoadShape2D((Shape2D)Selection.activeObject);

		// Draw Shape & Handle Events
		if (_shape2D != null)
			DrawMainPanel();

		Repaint();
	}

	private void BeginArea(Rect rectangle, GUIStyle style = null) {
		_areas.Push(rectangle);
		if (style == null)
			GUILayout.BeginArea(rectangle);
		else
			GUILayout.BeginArea(rectangle, style);
	}

	private void EndArea() {
		_areas.Pop();
		GUILayout.EndArea();
	}

	private void DrawMainPanel() {
		BeginArea(new Rect(0, _shapeSelectorHeight, CurrentArea.width, CurrentArea.height - _shapeSelectorHeight));

		// Draws the background
		DrawBackground(_scale);

		// Transforms the points
		points = new Vector2[_shape2D.points.Length];
		for (int i = 0; i < points.Length; i++) {
			points[i] = _shape2D.points[i] * _scale;
			points[i].y *= -1;
			points[i] += CurrentArea.size / 2 + _offset;
		}

		// Draws the lines
		for (int i = 0; i < _shape2D.lines.Length - 1; i += 2)
			DrawLine(points[_shape2D.lines[i]], points[_shape2D.lines[i + 1]], _lineWidth, Color.blue);

		// Draws the points
		for (int i = 0; i < points.Length; i++) {
			Rect rect = DrawPoint(points[i], _pointRadius, Color.white);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
		}

		// Handles the points events
		HandlePointsEvents();

		// Draws the normals
		normalHandles = new Vector2[_shape2D.normals.Length];
		for (int i = 0; i < _shape2D.normals.Length; i++) {
			normalHandles[i] = NormalToHandle(_shape2D.normals[i], points[i]);

			Vector2 normalOrigin = _shape2D.normals[i] * _pointRadius;
			normalOrigin.y *= -1;
			normalOrigin += points[i];

			DrawLine(normalOrigin, normalHandles[i], _lineWidth * _normalHandleSize, Color.cyan);
			Rect rect = DrawPoint(normalHandles[i], _pointRadius * _normalHandleSize, Color.cyan);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
		}

		// Handles the normals evenets
		HandleNormalsEvents();

		// Draws the selected object information
		DrawSelected();

		// Saves the changes to the shape
		SaveChanges();

		// Manages the draw area events
		HandleMouseEvents(CurrentArea);

		EndArea();
	}

	private void DrawBackground(float scale) {
		while (scale > _fixedScale)
			scale /= 2;
		Vector2 origin = CurrentArea.size / 2 + _offset.Module(scale);

		// Draws the horizontal grid
		for (float x = 0; x < CurrentArea.width / 2; x += scale) {
			Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
			Handles.DrawLine(new Vector2(origin.x + x, 0), new Vector2(origin.x + x, CurrentArea.height));
			Handles.DrawLine(new Vector2(origin.x - x, 0), new Vector2(origin.x - x, CurrentArea.height));
		}

		// Draws the vertical grid
		for (float y = 0; y < CurrentArea.height / 2; y += scale) {
			Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
			Handles.DrawLine(new Vector2(0, origin.y + y), new Vector2(CurrentArea.width, origin.y + y));
			Handles.DrawLine(new Vector2(0, origin.y - y), new Vector2(CurrentArea.width, origin.y - y));
		}

		// Draws the main axis
		Vector2 center = CurrentArea.size / 2 + _offset;
		Handles.color = Color.red;
		Handles.DrawLine(new Vector2(0, center.y), new Vector2(CurrentArea.width, center.y));
		Handles.color = Color.green;
		Handles.DrawLine(new Vector2(center.x, 0), new Vector2(center.x, CurrentArea.height));

		// Draws the ADD and REMOVE rects
		if (Event.current.alt)
			EditorGUIUtility.AddCursorRect(CurrentArea, MouseCursor.ArrowMinus);
		else if (Event.current.control)
			EditorGUIUtility.AddCursorRect(CurrentArea, MouseCursor.ArrowPlus);
	}

	private void DrawLine(Vector2 point1, Vector2 point2,float width, Color color) {
		Handles.color = color;
		Handles.DrawAAPolyLine(width, point1, point2);
	}

	private Rect DrawPoint(Vector2 point, float radius, Color color) {
		Handles.color = color / 4;
		Handles.DrawSolidDisc(point, Vector3.forward, radius);
		Handles.color = color;
		Handles.DrawSolidDisc(point, Vector3.forward, radius - 1);

		radius *= _cursorRectScale;
		return new Rect(point.x - radius, point.y - radius, 2 * radius, 2 * radius);
	}

	private void HandlePointsEvents() {
		// Points events
		float pointRadius = _cursorRectScale * _pointRadius;
		for (int i = 0; i < points.Length; i++) {
			Rect rect = new Rect(points[i].x - pointRadius, points[i].y - pointRadius, 2 * pointRadius, 2 * pointRadius);
			HandleEvents(ref points[i], rect, i);
			if (_selection.Contains(i)) {
				Handles.color = Color.red;
				Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
			}
		}
	}

	private void HandleNormalsEvents() {
		// Normal evenets
		float handleRadius = _cursorRectScale * _pointRadius * _normalHandleSize;
		for (int i = 0; i < normalHandles.Length; i++) {
			Rect rect = new Rect(normalHandles[i].x - handleRadius, normalHandles[i].y - handleRadius, 2 * handleRadius, 2 * handleRadius);
			HandleEvents(ref normalHandles[i], rect, i);
			if (_selection.Contains(i)) {
				Handles.color = Color.red;
				Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
			}
		}
	}

	private void HandleEvents(ref Vector2 point, Rect area, int index) {
		int pointID = GUIUtility.GetControlID("Point".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(pointID)) {
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && current.button == 0) {
					if (current.alt) {
						_selection.Remove(index);
					}
					else {
						if (!current.control) {
							GUIUtility.hotControl = pointID;
							_selection.Clear();
						}
						_selection.Add(index);
					}
					current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == pointID && current.button == 0)
					GUIUtility.hotControl = 0;
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == pointID) {
					point += current.delta;
					current.Use();
					GUI.changed = true;
				}
				break;
		}
	}

	private void HandleMouseEvents(Rect area) {
		// Checks if the event should be considered
		Event current = Event.current;

		// Drag Events
		int dragID = GUIUtility.GetControlID("Drag".GetHashCode(), FocusType.Passive);
		if (GUIUtility.hotControl == dragID)
			EditorGUIUtility.AddCursorRect(area, MouseCursor.Pan);
		switch (current.GetTypeForControl(dragID)) {
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && current.button == 2) {
					GUIUtility.hotControl = dragID;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(1);
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == dragID && current.button == 2) {
					GUIUtility.hotControl = 0;
					EditorGUIUtility.SetWantsMouseJumping(0);
					current.Use();
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == dragID) {
					_offset += current.delta;
					current.Use();
				}
				break;
			case EventType.ScrollWheel:
				_scale = Mathf.Min(600, Mathf.Max(1, _scale - current.delta.y * _scale / 60));
				break;
		}

		// Select Events
		int selectID = GUIUtility.GetControlID("Select".GetHashCode(), FocusType.Passive);
		if (GUIUtility.hotControl == selectID) {
			// Draw selection rectangle
			Rect rect = new Rect();
			rect = rect.FromPoints(_selectionStart, current.mousePosition);
			Color faceColor = new Color(0, 0, 1, 0.1f);
			Color outlineColor = new Color(0, 0, 1, 0.25f);
			Handles.color = Color.white;
			Handles.DrawSolidRectangleWithOutline(rect, faceColor, outlineColor);
		}
		switch (current.GetTypeForControl(selectID)) {
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && current.button == 0) {
					GUIUtility.hotControl = selectID;
					current.Use();
					_selectionStart = current.mousePosition;
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == selectID && current.button == 0) {
					Rect rect = new Rect();
					rect = rect.FromPoints(_selectionStart, current.mousePosition);
					if (!current.alt && !current.control)
						_selection.Clear();
					for (int i = 0; i < points.Length; i++) {
						if (rect.Contains(points[i])) {
							if (current.alt)
								_selection.Remove(i);
							else
								_selection.Add(i);
						}
					}
					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
		}
	}

	private void DrawSelected() {
		// Doesn't draw anything if no element is selected
		if (!HasSelection)
			return;

		// Draws the panel
		BeginArea(new Rect(0, CurrentArea.height - _selectedPanelHeight, CurrentArea.width, _selectedPanelHeight), GUI.skin.box);

		foreach (int index in _selection) {
			// Draws the point field
			points[index] = EditorGUILayout.Vector2Field("Point", points[index]);

			// Draws the normal field
			Vector2 normal = HandleToNormal(normalHandles[index], points[index]);
			normal = EditorGUILayout.Vector2Field("Normal", normal);
			normalHandles[index] = NormalToHandle(normal, points[index]);
		}

		EndArea();
	}

	private Vector2 NormalToHandle(Vector2 normal, Vector2 associatedPoint) {
		Vector2 handle = normal * _normalLength * _fixedScale;
		handle.y *= -1;
		handle += associatedPoint;
		return handle;
	}

	private Vector2 HandleToNormal(Vector2 handle, Vector2 associatedPoint) {
		Vector2 normal = handle;
		normal -= associatedPoint;
		normal.y *= -1;
		return normal.normalized;
	}

	private void SaveChanges() {
		// Records an Undo command
		Undo.RecordObject(_shape2D, "Modify Shape2D");

		// Saves the normals
		for (int i = 0; i < normalHandles.Length; i++)
			_shape2D.normals[i] = HandleToNormal(normalHandles[i], points[i]);

		// Saves the points
		for (int i = 0; i < points.Length; i++) {
			Vector2 point = points[i] - CurrentArea.size / 2 - _offset;
			point.y *= -1;
			_shape2D.points[i] = point / _scale;
		}
	}
}