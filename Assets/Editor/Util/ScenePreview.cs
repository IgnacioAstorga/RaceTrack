using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ScenePreview {

	private PreviewRenderUtility _previewRenderUtility;

	private List<ScenePreviewModel> _models = new List<ScenePreviewModel>();

	private Vector2 _drag = new Vector2(45, -25);
	private Vector3 _offset = Vector2.zero;
	private float _distance = 15;

	public ScenePreview() {
		_previewRenderUtility = new PreviewRenderUtility();

		_previewRenderUtility.m_Camera.transform.position = new Vector3(0, 0, -_distance);
		_previewRenderUtility.m_Camera.transform.rotation = Quaternion.identity;
		_previewRenderUtility.m_Camera.farClipPlane = 50;
	}

	public Texture GetSceneTexture(Rect rectangle, GUIStyle background) {
		_previewRenderUtility.BeginPreview(rectangle, background);

		foreach (ScenePreviewModel model in _models)
			_previewRenderUtility.DrawMesh(model.mesh, model.matrix, model.material, 0);

		_previewRenderUtility.m_Camera.transform.position = Vector2.zero;
		_previewRenderUtility.m_Camera.transform.rotation = Quaternion.Euler(new Vector3(-_drag.y, -_drag.x, 0));
		_previewRenderUtility.m_Camera.transform.position = _previewRenderUtility.m_Camera.transform.forward * -_distance;
		_previewRenderUtility.m_Camera.transform.position += _previewRenderUtility.m_Camera.transform.right * _offset.x;
		_previewRenderUtility.m_Camera.transform.position += _previewRenderUtility.m_Camera.transform.up * _offset.y;
		_previewRenderUtility.m_Camera.Render();

		return _previewRenderUtility.EndPreview();
	}

	public void AddModel(Mesh mesh, Matrix4x4 matrix, Material material) {
		AddModel(new ScenePreviewModel(mesh, matrix, material));
	}

	public void AddModel(ScenePreviewModel model) {
		_models.Add(model);
	}

	public void ClearModels() {
		_models.Clear();
	}

	public void ReadInput(Rect position) {
		int controlID = GUIUtility.GetControlID("Preview".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(controlID)) {
			case EventType.MouseDown:
				if (position.Contains(current.mousePosition) && position.width > 50f) {
					GUIUtility.hotControl = controlID;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(1);
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID) {
					GUIUtility.hotControl = 0;
					EditorGUIUtility.SetWantsMouseJumping(0);
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID) {
					if (current.button == 0) {
						_drag -= current.delta * (float)((!current.shift) ? 1 : 3) / Mathf.Min(position.width, position.height) * 140f;
						_drag.y = Mathf.Clamp(_drag.y, -90f, 90f);
					}
					if (current.button == 2) {
						float dimensions = 2 * Mathf.Min(position.width, position.height);
						_offset.x -= current.delta.x * _distance / dimensions;
						_offset.y += current.delta.y * _distance / dimensions;
					}
					current.Use();
					GUI.changed = true;
				}
				break;
			case EventType.ScrollWheel:
				if (position.Contains(current.mousePosition)) {
					_distance += current.delta.y;
					_distance = Mathf.Max(0, _distance);
					current.Use();
					GUI.changed = true;
				}
				break;
		}
	}

	public void CleanUp() {
		_previewRenderUtility.Cleanup();
	}
}