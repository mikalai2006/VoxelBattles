//using Cysharp.Threading.Tasks;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;

//public class ObjectIPoolable : MonoBehaviour
//{
//    public GameObject prefab;

//    // Переводим внутренние очереди на работу со структурами PoolNode вместо GameObject
//    private Queue<PoolNode> pool = new Queue<PoolNode>();
//    public Queue<PoolNode> Pool => pool;

//    [Header("Статистика")]
//    [SerializeField] private int count = 50;       // Стартовое количество объектов
//    [SerializeField, ReadOnly] private int poolLength;
//    [SerializeField, ReadOnly] private int countUsed;

//    [Header("Настройки скрытия (Телепортации)")]
//    [Tooltip("Использовать ли логику включения - отключения объекта")]
//    [SerializeField] bool useSetActive = false; // По умолчанию выключено для оптимизации вокселей
//    [SerializeField] Vector3 hidePosition = new Vector3(100000f, 100000f, 100000f);

//    private CancellationTokenSource cancelToken;

//    void Awake()
//    {
//        cancelToken = new CancellationTokenSource();
//    }

//    void OnDestroy()
//    {
//        cancelToken.Cancel();
//        cancelToken.Dispose();

//        // Уничтожаем игровые объекты из нод при удалении пула
//        while (pool.Count > 0)
//        {
//            PoolNode node = pool.Dequeue();
//            if (node.GameObject != null)
//            {
//                Destroy(node.GameObject);
//            }
//        }
//    }

//    void Start()
//    {
//        InitPool();
//    }

//    /// <summary>
//    /// Извлечь готовую ноду с компонентами из пула (0 вызовов GetComponent)
//    /// </summary>
//    public async UniTask<PoolNode> GetObject()
//    {
//        PoolNode node;

//        if (pool.Count > 0)
//        {
//            node = pool.Dequeue();
//        }
//        else
//        {
//            // Если пул пуст, создаем новую ноду, отправляем в пул и сразу забираем
//            node = CreateElement(poolLength);
//            await ReturnObject(node, 0);
//            node = pool.Dequeue();
//        }

//        // АКТИВАЦИЯ ОБЪЕКТА
//        if (useSetActive)
//        {
//            node.GameObject.SetActive(true);
//        }
//        else
//        {
//            // Если объект использует интерфейс вокселей, будим его UniTask корутины
//            if (node.poolable != null)
//            {
//                // По умолчанию спавним в нуле, координаты вы зададите сразу после GetObject()
//                node.poolable.Spawn(Vector3.zero, Quaternion.identity);
//            }
//        }

//        CreateStat();
//        return node;
//    }

//    /// <summary>
//    /// Вернуть ноду в пул с задержкой без ошибок Disposed
//    /// </summary>
//    public async UniTask ReturnObject(PoolNode node, int msTime = 0)
//    {
//        if (node.GameObject == null) return;

//        // ОПТИМИЗАЦИЯ: SuppressCancellationThrow предотвращает спам ошибок ObjectDisposedException
//        var isCanceled = await UniTask.Delay(msTime, cancellationToken: cancelToken.Token)
//            .SuppressCancellationThrow();

//        if (isCanceled) return;

//        // СБРОС ФИЗИКИ (Используем кэшированную ссылку rb вместо GetComponent)
//        if (node.Rigidbody != null)
//        {
//            if (!node.Rigidbody.isKinematic)
//            {
//                node.Rigidbody.linearVelocity = Vector3.zero;
//                node.Rigidbody.angularVelocity = Vector3.zero;
//            }
//            else
//            {
//                node.Transform.position = Vector3.zero;
//                node.Transform.rotation = Quaternion.identity;
//            }
//        }

//        // ДЕАКТИВАЦИЯ ОБЪЕКТА
//        if (useSetActive)
//        {
//            node.GameObject.SetActive(false);
//        }
//        else
//        {
//            // Пул с телепортацией: усыпляем UniTask/Update и переносим в архив под карту
//            if (node.poolable != null)
//            {
//                node.poolable.DeSpawn(hidePosition);
//            }
//            else
//            {
//                node.Transform.position = hidePosition;
//            }
//        }

//        node.Transform.SetParent(transform);

//        pool.Enqueue(node);
//        CreateStat();
//    }

//    void CreateStat()
//    {
//        poolLength = pool.Count;
//        countUsed = count - poolLength;
//    }

//    /// <summary>
//    /// Внутренний фабричный метод создания ноды и кэширования
//    /// </summary>
//    private PoolNode CreateElement(int index = 0)
//    {
//        GameObject obj = Instantiate(prefab, transform);
//        obj.name = $"{prefab.name}-{index}";

//        // Кэшируем все тяжелые ссылки ровно ОДИН раз при создании
//        return new PoolNode(obj);
//    }

//    public void InitPool()
//    {
//        for (int i = 0; i < count; i++)
//        {
//            PoolNode node = CreateElement(i);
//            ReturnObject(node, 0).Forget();
//        }
//    }
//}
