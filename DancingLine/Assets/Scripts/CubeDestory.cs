using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeDestory : MonoBehaviour {

    public float time = 3f;

    private void OnEnable()
    {
        Invoke("Destroy", time);
    }

    private void Destroy()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}
