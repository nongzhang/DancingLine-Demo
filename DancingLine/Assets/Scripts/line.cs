using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class line : MonoBehaviour {

	public GameObject dline,tail,mcamera,road,decorates,dieeff;

	private GameObject tempgo,tempdia,tempcr;

	public bool direction,alive,start,load,roadmaker;

	public float cameraspeed,temprm;

	public Vector3 offset,tempcrgo;

	private GameObject[] dia,cr;

	List<GameObject> go =new List<GameObject>();

	void Start () 
	{
		start = false;
		load = true;
		alive = true;
		dia = GameObject.FindGameObjectsWithTag ("dia");
		cr = GameObject.FindGameObjectsWithTag ("crown");
	}

	void Update () 
	{
		if (load == true) 
		{
			offset = mcamera.transform.position - dline.transform.position;
			cameraspeed = 0.2f;  //0.03
			load = false;
		}

		if(Input.GetKeyDown(KeyCode.R)==true){
			SceneManager.LoadScene ("main");
		}

		if (start == false && alive == true) 
		{
			if (Input.GetMouseButtonDown(0) == true || Input.GetKeyDown (KeyCode.Space) == true) 
			{
				start = true;
				dline.GetComponent<AudioSource> ().enabled = true;
			}
		}

		if (start == true) 
		{
			if (Input.GetMouseButtonDown (0) == true || Input.GetKeyDown (KeyCode.Space) == true ) 
			{
				if (direction == true) 
				{
					direction = false;
				} 
				else 
				{
					direction = true;
				}
			}
		}

       
        if (roadmaker)
        {
            Debug.Log("roadMaker Is True");
            temprm += 1;
            if (temprm % 6 == 1 || Input.GetMouseButtonDown(0) == true || Input.GetKeyDown(KeyCode.Space) == true)
            {
                GameObject.Instantiate(road, dline.transform.position + new Vector3(3, 0, -3), dline.transform.rotation);
                GameObject.Instantiate(road, dline.transform.position + new Vector3(-3, 0, 3), dline.transform.rotation);
            }
        }
        else
        {
            if (start && alive)
            {
               // GameObject.Instantiate(tail, dline.transform.position, dline.transform.rotation);
                
                GameObject cube = Instantiate(tail, dline.transform.position, dline.transform.rotation);
                Destroy(cube, 3.0f);
                if (Input.GetMouseButtonDown(0) == true || Input.GetKeyDown(KeyCode.Space) == true)
                {
                    if (direction == true)
                    {
                        go.Add(GameObject.Instantiate(decorates, dline.transform.position + new Vector3(8, -9, -5), dline.transform.rotation));
                    }
                    else
                    {
                        go.Add(GameObject.Instantiate(decorates, dline.transform.position + new Vector3(-5, -9, 8), dline.transform.rotation));
                    }
                }
            }
            else
            {
                return;
            }
            
        }
    }

	void FixedUpdate () 
	{
		mcamera.transform.position = Vector3.Lerp (mcamera.transform.position, offset + dline.transform.position, cameraspeed);

		foreach (GameObject tempgo in go)
		{
			tempgo.transform.position += new Vector3 (0, 0.2f, 0);
		}

		foreach (GameObject tempdia in dia)
		{
			tempdia.transform.localEulerAngles += new Vector3 (0, 2, 0);
			if (Mathf.Abs (dline.transform.position.x - tempdia.transform.position.x) < 1 && Mathf.Abs (dline.transform.position.z - tempdia.transform.position.z) < 1 && tempdia.transform.localScale.z >0) 
			{
				tempdia.transform.localScale -= new Vector3 (0.3f, 0.3f, 0.3f);
			}
		}
		foreach (GameObject tempcr in cr)
		{
			tempcr.transform.localEulerAngles += new Vector3 (0, 2, 0);
			if (Mathf.Abs (dline.transform.position.x - tempcr.transform.position.x) < 2 && Mathf.Abs (dline.transform.position.z - tempcr.transform.position.z) < 2 && tempcr.transform.localScale.z >0) 
			{
				tempcr.transform.localScale -= new Vector3 (0.26f, 0.26f, 0.26f);
				tempcrgo = tempcr.transform.position;
			}
			if (tempcr.transform.localScale.z <=0) 
			{
				tempcrgo += new Vector3 (Random.Range (-2f, 2f), Random.Range (-1f, 2f), Random.Range (-2f, 2f));
				tempcr.GetComponent<Light> ().enabled = true;
				tempcr.transform.position = Vector3.Lerp (tempcr.transform.position, tempcrgo, 0.02f);

			}
		}

		if (start == true && alive == true) 
		{
			if (direction == true) 
			{
				dline.transform.position += new Vector3 (0.3f,0,0);
			}
			else
			{
				dline.transform.position += new Vector3 (0,0,0.3f);
			}
		}
	}

	void OnCollisionEnter (Collision x) 
	{
		if (x.collider.tag == "wall") 
		{
            alive = false;
            mcamera.GetComponent<AudioSource>().enabled = true;
            dieeff.SetActive(true);
        }
		if (x.collider.tag == "Finish") 
		{
			offset = offset + offset + offset + offset;
			cameraspeed = 0.01f;
		}
	}

    IEnumerator MyCoroutine(GameObject obj)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(3f);
    }
}
