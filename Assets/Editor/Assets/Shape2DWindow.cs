using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class Shape2DWindow : EditorWindow {
	
	private bool HasSelection { get { return _selection.Count > 0; } }
	private Rect CurrentArea { get { return _areas.Peek(); } }

	private float _scale = 100;
	private float _oldScale;
	private float _fixedScale = 100;
	private Vector2 _offset = Vector2.zero;
	private Vector2 _oldOffset;

	private float _pointRadius = 5f;
	private float _lineWidth = 5f;
	private float _normalLength = 0.35f;
	private float _normalHandleSize = 0.75f;

	private float _cursorRectScale = 2f;

	private float _shapeSelectorHeight = 20f;
	private float _selectedPanelHeight = 80f;

	private Stack<Rect> _areas = new Stack<Rect>();
	private Rect _mainAreaRect;

	private Shape2D _shape2D;
	
	private HashSet<int> _selection = new HashSet<int>();
	private Vector2 _selectionStart;
	private bool _selectionDragged;

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
		_mainAreaRect = new Rect(0, _shapeSelectorHeight, CurrentArea.width, CurrentArea.height - _shapeSelectorHeight);
		BeginArea(_mainAreaRect);

		// Saves the offset and scale values. Transforms the values
		_oldOffset = _offset;
		_oldScale = _scale;
		TransformPoints();
		TransformNormals();

		// Draws the background
		DrawBackground(_scale);

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
		for (int i = 0; i < _shape2D.normals.Length; i++) {
			Vector2 normalOrigin = _shape2D.normals[i].normalized * _pointRadius;
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

		// Manages the mouse and keyboard events
		HandleMouseEvents(CurrentArea);
		HandleKeyboardEvents();

		// Saves the changes to the shape
		SaveChanges();

		EndArea();
	}

	private void TransformPoints() {
		points = new Vector2[_shape2D.points.Length];
		for (int i = 0; i < _shape2D.points.Length; i++)
			points[i] = PointToScreen(_shape2D.points[i]);
	}

	private void TransformNormals() {
		normalHandles = new Vector2[_shape2D.normals.Length];
		for (int i = 0; i < _shape2D.normals.Length; i++)
			normalHandles[i] = NormalToHandle(_shape2D.normals[i], points[i]);
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
			Vector2 savedPosition = points[i];
			points[i] = HandleEvents(points[i], rect, i);
			if (_selection.Contains(i)) {
				Handles.color = Color.red;
				Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
			}

			// Moves all the selected points
			if (savedPosition != points[i] && _selection.Count > 1) {
				Vector2 movement = points[i] - savedPosition;
				foreach (int index in _selection) {
					normalHandles[i] += movement;
					if (index != i)
						points[index] += movement;
				}
			}
		}
	}

	private void HandleNormalsEvents() {
		// Normal evenets
		float handleRadius = _cursorRectScale * _pointRadius * _normalHandleSize;
		for (int i = 0; i < normalHandles.Length; i++) {
			Rect rect = new Rect(normalHandles[i].x - handleRadius, normalHandles[i].y - handleRadius, 2 * handleRadius, 2 * handleRadius);
			Vector2 savedHandle = normalHandles[i];
			normalHandles[i] = HandleEvents(normalHandles[i], rect, i);
			if (_selection.Contains(i)) {
				Handles.color = Color.red;
				Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
			}

			// Moves all the selected points
			if (savedHandle != normalHandles[i] && _selection.Count > 1) {
				foreach (int index in _selection)
					if (index != i)
						normalHandles[index] += normalHandles[i] - savedHandle;
			}
		}
	}

	private Vector2 HandleEvents(Vector2 point, Rect area, int index) {
		int pointID = GUIUtility.GetControlID("Point".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(pointID)) {
			case EventType.ContextClick:
				if (area.Contains(current.mousePosition)) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Delete point"), false, DeletePoint, index);
					if (_selection.Count > 1)
						menu.AddItem(new GUIContent("Delete selected points"), false, DeleteSelectedPoints);
					menu.ShowAsContext();
					current.Use();
				}
				break;
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition)) {
					if (current.button == 0) {
						if (current.alt)
							_selection.Remove(index);
						else if (current.control)
							_selection.Add(index);
						else {
							if (_selection.Count <= 1)
								_selection.Clear();
							_selectionDragged = false;
							GUIUtility.hotControl = pointID;
							_selection.Add(index);
						}
						current.Use();
					}
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == pointID) {
					if (current.button == 0) {
						GUIUtility.hotControl = 0;
						if (!current.control && _selection.Count > 1 && !_selectionDragged) {
							_selection.Clear();
							_selection.Add(index);
						}
						current.Use();
					}
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == pointID) {
					point = current.mousePosition;
					_selectionDragged = true;
					current.Use();
				}
				break;
		}
		return point;
	}

	private void CreatePoint(object position) {
		try {
			_shape2D.AddPoint(ScreenToPoint((Vector2)position));
			TransformPoints();
			TransformNormals();
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid position: " + position + "\n" + e);
		}
	}

	private void DeletePoint(object pointIndex) {
		try {
			int index = Convert.ToInt32(pointIndex);
			_shape2D.DeletePoint(index);
			_selection.Remove(index);
			TransformPoints();
			TransformNormals();
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + pointIndex + "\n" + e);
		}
	}

	private void DeleteSelectedPoints() {
		int[] selectionCopy = new int[_selection.Count];
		_selection.CopyTo(selectionCopy);
		Array.Sort(selectionCopy);
		for (int i = selectionCopy.Length - 1; i >= 0; i--)
			DeletePoint(selectionCopy[i]);
	}

	private void HandleMouseEvents(Rect area) {
		// Drag Events
		Event current = Event.current;
		int dragID = GUIUtility.GetControlID("Drag".GetHashCode(), FocusType.Passive);
		if (GUIUtility.hotControl == dragID)
			EditorGUIUtility.AddCursorRect(area, MouseCursor.Pan);
		switch (current.GetTypeForControl(dragID)) {
			case EventType.ContextClick:
				if (area.Contains(current.mousePosition)) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Create point"), false, CreatePoint, current.mousePosition);
					if (_selection.Count > 1)
						menu.AddItem(new GUIContent("Delete selected points"), false, DeleteSelectedPoints);
					menu.ShowAsContext();
					current.Use();
				}
				break;
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

	private void HandleKeyboardEvents() {
		// Drag Events
		Event current = Event.current;
		int keyID = GUIUtility.GetControlID("Key".GetHashCode(), FocusType.Passive);
		switch (current.GetTypeForControl(keyID)) {
			case EventType.KeyDown:
				if (current.isKey && current.keyCode == KeyCode.Delete && HasSelection) {
					DeleteSelectedPoints();
					current.Use();
				}
				else if (current.isKey && current.keyCode == KeyCode.Escape && HasSelection) {
					_selection.Clear();
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
		EditorGUILayout.BeginVertical();

		// Draws the point field
		Vector2[] selectedPoints;
		int[] pointIndices = GetSelectedPoints(out selectedPoints);
		DrawMultiVector2("Point", ref selectedPoints);
		for (int i = 0; i < selectedPoints.Length; i++) {
			normalHandles[pointIndices[i]] += selectedPoints[i] - points[pointIndices[i]];
			points[pointIndices[i]] = selectedPoints[i];
		}
		
		// Draws the normal field
		Vector2[] selectedNormals;
		int[] normalIndices = GetSelectedNormals(out selectedNormals);
		DrawMultiVector2("Normal", ref selectedNormals);
		for (int i = 0; i < selectedNormals.Length; i++)
			_shape2D.normals[normalIndices[i]] = selectedNormals[i];

		EditorGUILayout.EndVertical();
		EndArea();
	}

	private void DrawMultiVector2(string label, ref Vector2[] collection) {
		// Checks which values are not the same
		float x = collection[0].x;
		bool sameX = true;
		float y = collection[0].y;
		bool sameY = true;
		for (int i = 1; i < collection.Length; i++) {
			sameX &= x == collection[i].x;
			sameY &= y == collection[i].y;
			if (!sameX && !sameY)
				break;
		}

		// Draws the field
		EditorGUILayout.LabelField(label);
		EditorGUILayout.BeginHorizontal();
		if (sameX) {
			float userX = EditorGUILayout.FloatField("X", x);
			for (int i = 0; i < collection.Length; i++)
				collection[i].x = userX;
		}
		else {
			string userX = EditorGUILayout.TextField("X", "-");

			// Modifies the user modified values
			if (x.ToString() != userX) {
				float newX;
				if (float.TryParse(userX, out newX)) {
					for (int i = 0; i < collection.Length; i++)
						collection[i].x = newX;
				}
			}
		}
		if (sameY) {
			float userY = EditorGUILayout.FloatField("Y", y);
			for (int i = 0; i < collection.Length; i++)
				collection[i].y = userY;
		}
		else {
			string userY = EditorGUILayout.TextField("Y", "-");

			// Modifies the user modified values
			if (x.ToString() != userY) {
				float newY;
				if (float.TryParse(userY, out newY)) {
					for (int i = 0; i < collection.Length; i++)
						collection[i].y = newY;
				}
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	private int[] GetSelectedPoints(out Vector2[] selectedPoints) {
		selectedPoints = new Vector2[_selection.Count];
		int[] indices = new int[_selection.Count];
		int i = 0;
		foreach (int index in _selection) {
			indices[i] = index;
			selectedPoints[i] = points[index];
			i++;
		}
		return indices;
	}

	private int[] GetSelectedNormals(out Vector2[] selectedNormals) {
		selectedNormals = new Vector2[_selection.Count];
		int[] indices = new int[_selection.Count];
		int i = 0;
		foreach (int index in _selection) {
			indices[i] = index;
			selectedNormals[i] = _shape2D.normals[index];
			i++;
		}
		return indices;
	}

	private Vector2 PointToScreen(Vector2 point) {
		point *= _oldScale;
		point.y *= -1;
		point += _mainAreaRect.size / 2 + _offset;
		return point;
	}

	private Vector2 ScreenToPoint(Vector2 point) {
		point -= _mainAreaRect.size / 2 + _oldOffset;
		point.y *= -1;
		point /= _oldScale;
		return point;
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
		_shape2D.normals = new Vector2[normalHandles.Length];
		for (int i = 0; i < normalHandles.Length; i++)
			_shape2D.normals[i] = HandleToNormal(normalHandles[i], points[i]);

		// Saves the points
		_shape2D.points = new Vector2[points.Length];
		for (int i = 0; i < points.Length; i++)
			_shape2D.points[i] = ScreenToPoint(points[i]);
	}
}