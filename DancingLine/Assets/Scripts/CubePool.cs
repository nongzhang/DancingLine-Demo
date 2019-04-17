using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePool : MonoBehaviour
{

    public static CubePool cubePoolInstance;

    public GameObject cubePrefab;

    public int poolSize = 20;

    public bool lockPoolSize = false;   //是否锁定池的大小

    private List<GameObject> poolObjects;

    private int currentIndex;

    private void Awake()
    {
        cubePoolInstance = this;
        poolObjects = new List<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(cubePrefab);
            obj.SetActive(false);
            poolObjects.Add(obj);
        }
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public GameObject GetPoolObject()
    {
        for (int i = 0; i < poolObjects.Count; i++)
        {
            int item = (currentIndex + i) % poolObjects.Count;
            if (!poolObjects[item].activeInHierarchy)
            {
                currentIndex = (item + i) % poolObjects.Count;

                return poolObjects[item];
            }
        }
        if (!lockPoolSize)
        {
            GameObject obj = Instantiate(cubePrefab);

            poolObjects.Add(obj);
            return obj;

        }

        return null;
    }
}
