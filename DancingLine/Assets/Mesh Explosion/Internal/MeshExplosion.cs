using UnityEngine;

public class MeshExplosion : MonoBehaviour {
	
	MeshExploder.MeshExplosionPreparation preparation;
	
	Mesh mesh;
	
	Vector3[] vertices;
	Vector3[] normals;
	Vector4[] tangents;
	
	Vector3[] triangleRotationAxes;
	float[] triangleSpeeds;
	float[] triangleRotationSpeeds;
	Vector3[] triangleCurrentCentroids;
	
	bool useGravity;
	float explosionTime;
	Transform thisTransform;
	
	public void Go(MeshExploder.MeshExplosionPreparation prep,
		float minSpeed, float maxSpeed, float minRotationSpeed, float maxRotationSpeed,
		bool useGravity) {
		
		preparation = prep;
		
		var frontTriangles = prep.totalFrontTriangles;
		triangleRotationAxes = new Vector3[frontTriangles];
		triangleSpeeds = new float[frontTriangles];
		triangleRotationSpeeds = new float[frontTriangles];
		
		var deltaSpeed = maxSpeed - minSpeed;
		var deltaRotationSpeed = maxRotationSpeed - minRotationSpeed;
		var fixedSpeed = minSpeed == maxSpeed;
		var fixedRotationSpeed = minRotationSpeed == maxRotationSpeed;
		
		this.useGravity = useGravity;
		explosionTime = 0;
		thisTransform = transform;
		
		for (var triangleNumber = 0; triangleNumber < frontTriangles; ++triangleNumber) {
			triangleRotationAxes[triangleNumber] = Random.onUnitSphere;
			triangleSpeeds[triangleNumber] = fixedSpeed ?
				minSpeed : minSpeed + Random.value * deltaSpeed;
			triangleRotationSpeeds[triangleNumber] = fixedRotationSpeed ?
				minRotationSpeed : minRotationSpeed + Random.value * deltaRotationSpeed;
		}
		
		GetComponent<MeshFilter>().mesh = mesh = (Mesh)Object.Instantiate(prep.startMesh);
		triangleCurrentCentroids = (Vector3[])prep.triangleCentroids.Clone();
		
		// It might seem like a waste of memory to keep a copy of these arrays, but actually
		// retrieving them allocates new garbage collected memory every time so it's much better to
		// just retrieve them once rather than retrieving them every frame and creating lots of
		// garbage.
		vertices = mesh.vertices;
		normals = mesh.normals;
		tangents = mesh.tangents;
		
		// Do one frame of explosion to start it off.
		Update();
	}
	
	void Update() {
		if (vertices == null) {
			var componentName = GetType().Name;
			Debug.LogError("The " + componentName + " component should not be used directly." +
				" Add the " + typeof(MeshExploder).Name + " component to your object and it will" +
				" take care of creating the explosion object and adding the " + componentName +
				" component.");
			enabled = false;
			return;
		}
		
		var dt = Time.deltaTime;
		explosionTime += dt;
		
		// This can happen in builds even if it doesn't happen in the editor:
		if (tangents != null && tangents.Length == 0) tangents = null;
		
		var triangleNormals = preparation.triangleNormals;
		
		var gravity = useGravity ?
			thisTransform.InverseTransformDirection(Physics.gravity) :
			default(Vector3);
		
		var frontTriangles = (vertices.Length / 3) / 2;
		var firstVertexIndex = 0;
		for (var triangleNumber = 0; triangleNumber < frontTriangles;
			++triangleNumber, firstVertexIndex += 6) {
			
			var framePositionDelta = triangleSpeeds[triangleNumber] * dt;
			var frameRotationDelta = triangleRotationSpeeds[triangleNumber] * dt;
			
			var normal = triangleNormals[triangleNumber];
			var positionDelta = normal * framePositionDelta;
			if (useGravity) positionDelta += explosionTime * gravity * dt;
			var rotation =
				Quaternion.AngleAxis(frameRotationDelta, triangleRotationAxes[triangleNumber]);
			
			var centroid = triangleCurrentCentroids[triangleNumber];
			var newCentroid = centroid + positionDelta;
			for (var i = 0; i < 3; ++i) {
				var vi = firstVertexIndex + i;
				vertices[vi] = (rotation * (vertices[vi] - centroid)) + newCentroid;
				if (normals != null) normals[vi] = rotation * normals[vi];
				if (tangents != null) tangents[vi] = rotation * tangents[vi];
			}
			triangleCurrentCentroids[triangleNumber] = newCentroid;
		}
		
		SetBackTriangleVertices(vertices, normals, tangents, preparation.totalFrontTriangles);
		
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.tangents = tangents;
		mesh.RecalculateBounds();
	}
	
	public static void SetBackTriangleVertices(
		Vector3[] vertices, Vector3[] normals, Vector4[] tangents, int totalFrontTriangles) {
		
		int vertexIndex = 0;
		for (int triangle = 0; triangle < totalFrontTriangles; ++triangle) {
			var frontTriangleStartVertexIndex = vertexIndex;
			vertexIndex += 3; // Skip the front triangle
			
			for (int i = 0; i < 3; ++i, ++vertexIndex) {
				var frontVertexIndex = ((3 - 1) - i) + frontTriangleStartVertexIndex;
				vertices[vertexIndex] = vertices[frontVertexIndex];
				if (normals != null) normals[vertexIndex] = -normals[frontVertexIndex];
				if (tangents != null) tangents[vertexIndex] = -tangents[frontVertexIndex];
			}
		}
	}
	
}
