using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DancingLineManager : MonoBehaviour
{

    public GameObject prefab;
    public Transform _myTrans;
    public GameObject decoratePrefab;  //向上漂浮的装饰物
    public GameObject mainCamera;
    public GameObject dieEffect;
    public Image theEnd;
    public GameObject buttons;
    public AudioSource[] gameAudio;

    

    private bool direction = false;    //true表示向+z方向，false表示向-x方向
    private bool start = false;
    private bool alive;
    private bool isAttach = true;
    private GameObject cube;
    private Vector3 offset;
    private float cameraSpeed;
    private float t1;

    private bool isLoad;

    List<GameObject> go = new List<GameObject>();

    void Start()
    {
        alive = true;
        isLoad = true;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && gameAudio[0].isPlaying == false)
        {
            gameAudio[0].Play();
        }
        control();
        MoveMent();
        if (isLoad)
        {
            offset = mainCamera.transform.position - _myTrans.position;
            cameraSpeed = 0.2f;
            isLoad = false;
        }

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, offset + _myTrans.position, cameraSpeed);

        foreach (GameObject tempgo in go)
        {
            tempgo.transform.Translate(Vector3.up * Time.deltaTime * 10f);
        }

        if (Input.GetKeyDown(KeyCode.R) == true)
        {
            SceneManager.LoadScene("Winter");
        }
    }

    /// <summary>
    /// 方块运动
    /// </summary>
    void MoveMent()
    {
        if (direction && start && alive)
        {
            _myTrans.Translate(Vector3.right * Time.deltaTime *15);
            //InvokeRepeating("ClonePrefab",0.0f,5f);
            //Instantiate(prefab, _myTrans.position, Quaternion.identity);

            //cube = Instantiate(prefab, _myTrans.position, Quaternion.identity) as GameObject;
            //Destroy(cube, 3.0f);
            GameObject obj = ObjectPooler.current.GetPooledObject();

            if (obj == null)
                return;

            obj.transform.position = _myTrans.position;
            obj.transform.rotation = _myTrans.rotation;
            obj.SetActive(true);
           
            InitDecorate(obj, direction);
        }
        else if (direction == false && start && alive)
        {
            _myTrans.Translate(Vector3.forward * Time.deltaTime * 15);

            //cube = Instantiate(prefab, _myTrans.position, Quaternion.identity) as GameObject;
            //Destroy(cube, 3.0f);
            GameObject obj = ObjectPooler.current.GetPooledObject();
            
            if (obj == null)
                return;

            obj.transform.position = _myTrans.position;
            obj.transform.rotation = _myTrans.rotation;
            obj.SetActive(true);

            
            InitDecorate(obj, direction);
        }
        else if (start && isAttach == false)
        {
            _myTrans.Translate(Vector3.forward * Time.deltaTime * 3.6f);   //由于方块有向前的恒定速度，而且在自由落体（竖直方向加速），所以会一抖一抖的
        }

    }

    void control()
    {
        if (Input.GetKeyDown(KeyCode.Space) && alive)   //当方块在地面上时，点击空格键是有效的，在空中是无效的
        {
            start = true;
            direction = !direction;
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// 生成装饰物并运动
    /// </summary>
    void InitDecorate(GameObject parent,bool dir)
    {

        if (Input.GetKeyDown(KeyCode.Space) == true)
        {
            if (dir)
            {
                go.Add(Instantiate(decoratePrefab, parent.transform.position + new Vector3(8, -9, -6), parent.transform.rotation));
            }
            else
            {
                go.Add(Instantiate(decoratePrefab, parent.transform.position + new Vector3(-6, -9, 8), parent.transform.rotation));
            }
        }
        
    }
    void FixedUpdate()
    {

        Vector3 fwd = transform.TransformDirection(Vector3.down);

        if (Physics.Raycast(transform.position, fwd, 0.2f))
        {
            isAttach = true;
            this.GetComponent<Collider>().enabled = true;
        }
        else if (!Physics.Raycast(transform.position, fwd, 2))
        {
            isAttach = false;
            this.GetComponent<Collider>().enabled = false;
        }

        
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "wall")
        {
            start = false;
            alive = false;
            gameAudio[1].Play();
            dieEffect.SetActive(true);

            StartCoroutine(VolumeDecrease());
            StartCoroutine(MoveToPosition());
            StartCoroutine(MoveButton());
        }
        else if (collision.collider.tag == "Finish")
        {
            offset = offset + offset + offset + offset;
            cameraSpeed = 0.01f;
        }    
    }

    void LanchTheEndInterface()
    {
        theEnd.rectTransform.position = Vector3.MoveTowards(theEnd.rectTransform.position,new Vector3(0,0,0),Time.deltaTime * 100);
    }

    IEnumerator MoveToPosition()
    {
        while (theEnd.rectTransform.localPosition != new Vector3(0,0,0))
        {
            theEnd.rectTransform.localPosition = Vector3.MoveTowards(theEnd.rectTransform.localPosition, new Vector3(0,0,0), 500 * Time.deltaTime);
            yield return 0;
        } 
    }

    IEnumerator MoveButton()
    {
        while (buttons.transform.position != new Vector3(0, -222, 0))
        {
            buttons.transform.localPosition = Vector3.MoveTowards(buttons.transform.localPosition, new Vector3(0, -222, 0), 197.22f * Time.deltaTime);
            yield return 0;
        }
    }

    IEnumerator VolumeDecrease()
    {
        while (gameAudio[0].volume != 0.4f)
        {
            t1 += 0.6f * Time.deltaTime;
            gameAudio[0].volume = Mathf.Lerp(1.0f,0.4f,t1);
            yield return 0;
        }
    }

   public void Retry()
    {
        SceneManager.LoadScene("Winter");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
