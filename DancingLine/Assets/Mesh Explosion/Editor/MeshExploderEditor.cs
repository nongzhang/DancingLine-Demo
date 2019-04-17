using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshExploder)),
CanEditMultipleObjects]
public class MeshExploderEditor : Editor {
	
	const string speedLabelText =
		"The speed of each triangle in the explosion is a (uniformly) random value between the minimum and maximum. They can be the same if you want every triangle to have the same speed, and one or both can be negative if you want the triangles to implode inwards rather than explode outwards.";
	static GUIContent minSpeedLabel = new GUIContent("Minimum Speed", speedLabelText);
	static GUIContent maxSpeedLabel = new GUIContent("Maximum Speed", speedLabelText);
	const string rotationSpeedLabelText =
		"The rotation speed of each triangle is a uniformly random value between the minimum and maximum. The units are degrees per second.";
	static GUIContent minRotationSpeedLabel = new GUIContent("Minimum Rotation Speed",
		rotationSpeedLabelText);
	static GUIContent maxRotationSpeedLabel = new GUIContent("Maximum Rotation Speed",
		rotationSpeedLabelText);
	static GUIContent fadeTimeLabel = new GUIContent("Fade Time",
		"After the wait time the triangles will fade to transparent for this long (in seconds) and when they are completely transparent the explosion GameObject is destroyed. If this is zero then the explosion will never end.");
	static GUIContent fadeWaitTimeLabel = new GUIContent("Fade Wait Time",
		"The exploding triangles will stay opaque this long (in seconds) before they start fading.");
	static GUIContent typeLabel = new GUIContent("Type",
		"Visual explosions are fast; physics explosions interact with the environment.");
	static GUIContent useNormalsLabel = new GUIContent("Use Normals",
		"Usually the direction of each exploding triangle is the direction in a straight line from the origin of the mesh to the center of the triangle. If you enable this option then the triangle instead travels in the direction of its surface normal.");
	static GUIContent useMeshBoundsCenterLabel = new GUIContent("Use Mesh Bounds Center",
		"Usually the exploding triangles travel away from the origin of the mesh. If you use this option the triangles travel away from the center of the mesh instead (specifically from the center of the mesh's bounding box).");
	static GUIContent allowShadowsLabel = new GUIContent("Allow Shadows",
		"Normally Mesh Explosion turns off shadows on the explosion object, but enabling this option prevents that. Transparent things don't cast or receive shadows so when fading starts it's often noticeable when the shadows disappear suddenly. In most cases it's probably better to leave this option turned off so that shadows are disabled straight away at the start of the explosion when it is less noticeable. This option has no effect on non-Pro versions of Unity, of course.");
	static GUIContent shadersAlreadyHandleTransparencyLabel = new GUIContent(
		"Shaders Already Handle Transparency",
		"In order for the fading effect to work, Mesh Explosion needs to use shaders that support transparency. It looks at the shaders used by your mesh's materials and tries to find a transparent equivalent, which works fine for the standard Unity shaders like Diffuse, Specular, etc., and their mobile equivalents, and if that's working then you don't need to worry about this option. If, however, you are using custom shaders, Mesh Explosion may not be able to find a transparent equivalent, and will issue a warning to that effect. If your shaders already support transparency just fine (they need to have a standard color parameter named _Color that accepts an alpha) then enable this option to turn off shader replacement and the warning. If your shaders don't support transparency and you can't change them for ones that do then I think there's a solution and it will be in a future version of the package. Please send me an email if you need this so I can prioritize it.");
	static GUIContent useGravityLabel = new GUIContent("Simulate Gravity",
		"When this option is enabled gravity is applied to the explosion fragments so that they fall realistically.");
	static GUIContent colliderThicknessLabel = new GUIContent("Collider Thickness",
		"This option determines that thickness of the colliders that are added to the fragments when doing physics explosions. If it is too small then the fragments will fall through other objects; if it is too large then it will be visible that the fragments are not touching surfaces that they are resting on.");
	
	const int indentIncrement = 2;
	
	SerializedProperty minSpeedProp;
	SerializedProperty maxSpeedProp;
	SerializedProperty minRotationSpeedProp;
	SerializedProperty maxRotationSpeedProp;
	SerializedProperty fadeWaitTimeProp;
	SerializedProperty fadeTimeProp;
	SerializedProperty useGravityProp;
	SerializedProperty typeProp;
	SerializedProperty useNormalsProp;
	SerializedProperty useMeshBoundsCenterProp;
	SerializedProperty allowShadowsProp;
	SerializedProperty shadersAlreadyHandleTransparencyProp;
	SerializedProperty colliderThicknessProp;
	
	bool showAdvancedOptions = false;
	
	public void OnEnable() {
		minSpeedProp = serializedObject.FindProperty("minSpeed");
		maxSpeedProp = serializedObject.FindProperty("maxSpeed");
		minRotationSpeedProp = serializedObject.FindProperty("minRotationSpeed");
		maxRotationSpeedProp = serializedObject.FindProperty("maxRotationSpeed");
		fadeWaitTimeProp = serializedObject.FindProperty("fadeWaitTime");
		fadeTimeProp = serializedObject.FindProperty("fadeTime");
		useGravityProp = serializedObject.FindProperty("useGravity");
		typeProp = serializedObject.FindProperty("type");
		useNormalsProp = serializedObject.FindProperty("useNormals");
		useMeshBoundsCenterProp = serializedObject.FindProperty("useMeshBoundsCenter");
		allowShadowsProp = serializedObject.FindProperty("allowShadows");
		shadersAlreadyHandleTransparencyProp =
			serializedObject.FindProperty("shadersAlreadyHandleTransparency");
		colliderThicknessProp = serializedObject.FindProperty("colliderThickness");
	}
	
	public override void OnInspectorGUI() {
		serializedObject.Update();
		
		EditorGUIUtility.LookLikeControls(250);
		++EditorGUI.indentLevel;
		
		EditorGUILayout.LabelField("Explosion:");
		EditorGUI.indentLevel += indentIncrement;
		EditorGUILayout.PropertyField(typeProp, typeLabel);
		var usePhysics = typeProp.enumValueIndex == (int)MeshExploder.ExplosionType.Physics;
		if (usePhysics || typeProp.hasMultipleDifferentValues) {
			EditorGUILayout.HelpBox("Physics explosions create a new physics object for each fragment of the explosion so they are only suitable for use on very low-polygon meshes.", MessageType.Info);
		}
		if (!usePhysics || typeProp.hasMultipleDifferentValues) {
			EditorGUILayout.PropertyField(useGravityProp, useGravityLabel);
		}
		EditorGUI.indentLevel -= indentIncrement;
		
		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField("Fragments:");
		EditorGUI.indentLevel += indentIncrement;
		EditorGUILayout.PropertyField(minSpeedProp, minSpeedLabel);
		EditorGUILayout.PropertyField(maxSpeedProp, maxSpeedLabel);
		EditorGUILayout.PropertyField(minRotationSpeedProp, minRotationSpeedLabel);
		EditorGUILayout.PropertyField(maxRotationSpeedProp, maxRotationSpeedLabel);
		EditorGUILayout.PropertyField(fadeTimeProp, fadeTimeLabel);
		if (fadeTimeProp.hasMultipleDifferentValues || fadeTimeProp.floatValue != 0) {
			EditorGUILayout.PropertyField(fadeWaitTimeProp, fadeWaitTimeLabel);
		}
		EditorGUI.indentLevel -= indentIncrement;
		
		EditorGUILayout.Space();
		
		--EditorGUI.indentLevel;
		showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options:");
		++EditorGUI.indentLevel;
		if (showAdvancedOptions) {
			EditorGUI.indentLevel += indentIncrement;
			if (usePhysics || typeProp.hasMultipleDifferentValues) {
				EditorGUILayout.PropertyField(colliderThicknessProp, colliderThicknessLabel);
			}
			EditorGUILayout.PropertyField(useNormalsProp, useNormalsLabel);
			EditorGUILayout.PropertyField(useMeshBoundsCenterProp, useMeshBoundsCenterLabel);
			EditorGUILayout.PropertyField(allowShadowsProp, allowShadowsLabel);
			EditorGUILayout.PropertyField(
				shadersAlreadyHandleTransparencyProp, shadersAlreadyHandleTransparencyLabel);
			EditorGUI.indentLevel -= indentIncrement;
		}
		
		serializedObject.ApplyModifiedProperties();
	}
	
}
