using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Broking : MonoBehaviour {

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            BroadcastMessage("Explode");
           this.gameObject.SetActive(false);
        }
    }
}
