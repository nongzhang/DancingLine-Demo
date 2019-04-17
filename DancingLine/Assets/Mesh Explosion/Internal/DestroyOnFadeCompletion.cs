using UnityEngine;

public class DestroyOnFadeCompletion : MonoBehaviour {

	void FadeCompleted() {
		Object.Destroy(gameObject);
	}

}
