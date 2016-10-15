using UnityEngine;

[ExecuteInEditMode]
public class Shape2DExtrude : MonoBehaviour {

	public bool chainSegments = true;
	public bool loopChain = false;

	private Shape2DExtrudeSegment[] segments;

	void Awake() {
		segments = this.GetComponentsInChildrenOnly<Shape2DExtrudeSegment>();
	}

	void Start() {
		if (chainSegments)
			ChainSegments();
	}

	void Update() {
		if (!Application.isPlaying) {
			Awake();
			Start();
		}
	}

	private void ChainSegments() {
		// For each segment...
		for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++) {

			// Finds the control points of each affected segment
			Shape2DExtrudeControlPoint[] segmentControlPoints = segments[segmentIndex].GetControlPoints();
			Shape2DExtrudeControlPoint[] nextSegmentControlPoints;

			if (segmentIndex != segments.Length - 1)
				// Each segment is attached to the next one
				nextSegmentControlPoints = segments[segmentIndex + 1].GetControlPoints();
			else if (loopChain)
				// If looping, the last segment is attached to the first one
				nextSegmentControlPoints = segments[0].GetControlPoints();
			else
				// If not loopint, the last segment is free
				break;

			// Sets the segment's las control point's position and rotation to the next segment's first control point's
			Transform segmentTransform = segmentControlPoints[segmentControlPoints.Length - 1].GetTransform();
			Transform nextSegmentTransform = nextSegmentControlPoints[0].GetTransform();
			segmentTransform.position = nextSegmentTransform.position;
			segmentTransform.rotation = nextSegmentTransform.rotation;
			segmentTransform.localScale = nextSegmentTransform.localScale;
		}
	}
}