using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    private Queue<GameObject> pool = new Queue<GameObject>();
    public Queue<GameObject> Pool => pool;
    [SerializeField] private int count;
    [SerializeField] private int poolLength;
    [SerializeField] private int countUsed;
    public List<GameObject> poolObjs = new List<GameObject>();
    System.Threading.CancellationTokenSource cancelToken;
    [Tooltip("Использовать ли логику включения - отключения объекта")]
    [SerializeField] bool useSetActive = true;
    [SerializeField] Vector3 hidePosition;

    void Awake()
    {
        cancelToken = new System.Threading.CancellationTokenSource();
        if (hidePosition == Vector3.zero)
        {
            hidePosition = new Vector3(100000,100000,100000);
        }
    }

    void OnDestroy()
    {
        cancelToken.Cancel();
        cancelToken.Dispose();

        foreach (GameObject go in pool)
        {
            Destroy(go);
        }
    }

    void Start()
    {
        InitPool();
    }

    public async UniTask<GameObject> GetObject()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (useSetActive)
            {
                obj.SetActive(true);
            } else
            {
                obj.transform.position = hidePosition;
            }
            
            // countUsed++;
            CreateStat();

            return obj;
        }

        GameObject objNew = CreateElement(poolLength);
        
        await ReturnObject(objNew);

        objNew = pool.Dequeue();
        // countUsed++;
        CreateStat();

        return objNew;
    }

    public async UniTask ReturnObject(GameObject obj, int msTime = 0)
    {
        // Проверяем, существует ли источник и не был ли он отменен/уничтожен
        if (cancelToken != null && !cancelToken.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(msTime, cancellationToken: cancelToken.Token);
                Rigidbody rb = obj.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // Debug.Log($"<color=yellow>Reset rigidbody {obj.name}</color>");

                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        obj.transform.position = new Vector3(0f, 0f, 0f);
                        obj.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                    }
                    // bool isKinematic = rb.isKinematic;
                    // rb.isKinematic = true;
                    // rb.transform.position = new Vector3(0f, 0f, 0f);
                    // rb.transform.rotation = Quaternion.Euler(new Vector3(0f,0f,0f));
                    // rb.linearVelocity = new Vector3(0f,0f,0f);
                    // rb.angularVelocity = new Vector3(0f,0f,0f);

                    // if (!isKinematic)
                    // {
                    //     rb.isKinematic = false; // Re-enable physics
                    // }

                    // rb.WakeUp();
                }
                obj.SetActive(false);
                obj.transform.SetParent(transform);

                pool.Enqueue(obj);
                CreateStat();

                // poolObjs.Clear();
                // foreach (GameObject item in pool)
                // {
                //     poolObjs.Add(item);
                // }

            }
            catch (System.ObjectDisposedException)
            {
                // Перестраховка на случай, если Dispose вызвался ровно в миллисекунду выполнения Delay
                return;
            }
        }

        //await UniTask.Delay(msTime, cancellationToken: cancelToken.Token);

        //if (cancelToken != null && !cancelToken.Token.IsCancellationRequested)
        //{

        //    Rigidbody rb = obj.GetComponent<Rigidbody>();

        //    if (rb != null)
        //    {
        //        // Debug.Log($"<color=yellow>Reset rigidbody {obj.name}</color>");

        //        if (!rb.isKinematic)
        //        {
        //            rb.linearVelocity = Vector3.zero;
        //            rb.angularVelocity = Vector3.zero;
        //        }
        //        else
        //        {
        //            obj.transform.position = new Vector3(0f, 0f, 0f);
        //            obj.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        //        }
        //        // bool isKinematic = rb.isKinematic;
        //        // rb.isKinematic = true;
        //        // rb.transform.position = new Vector3(0f, 0f, 0f);
        //        // rb.transform.rotation = Quaternion.Euler(new Vector3(0f,0f,0f));
        //        // rb.linearVelocity = new Vector3(0f,0f,0f);
        //        // rb.angularVelocity = new Vector3(0f,0f,0f);

        //        // if (!isKinematic)
        //        // {
        //        //     rb.isKinematic = false; // Re-enable physics
        //        // }

        //        // rb.WakeUp();
        //    }
        //    obj.SetActive(false);

        //    pool.Enqueue(obj);
        //    CreateStat();

        //    // poolObjs.Clear();
        //    // foreach (GameObject item in pool)
        //    // {
        //    //     poolObjs.Add(item);
        //    // }
        //}
    }

    void CreateStat()
    {
        poolLength = pool.Count;
        countUsed = count - poolLength;
    }

    GameObject CreateElement(int index = 0)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.name = $"{prefab.name}-{index}";
        return obj;
    }

    public void InitPool()
    {
        for (int i = 0; i < count; i++)
        {
            // GameObject obj = Instantiate(prefab, transform);
            // obj.name = $"bullet-{i}";
            GameObject obj = CreateElement(i);

            ReturnObject(obj, 0).Forget();
        }
    }
}
