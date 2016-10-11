using UnityEngine;

public class ScenePreviewModel {

	public Mesh mesh;
	public Matrix4x4 matrix;
	public Material material;

	public ScenePreviewModel(Mesh mesh, Matrix4x4 matrix, Material material) {
		this.mesh = mesh;
		this.matrix = matrix;
		this.material = material;
	}
}