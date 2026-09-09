using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// Этот компонент — чистая структура. Полностью совместим с Сетью, Ghost-префабами и Burst!
public struct AAA_ChunkMeshLink : IComponentData
{
    public int PoolInstanceId; // Уникальный ID меша в нашем пуле
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ClientMeshPoolSystem : SystemBase
{
    private readonly Stack<Mesh> _availableMeshes = new Stack<Mesh>();
    // Хранилище активных мешей, чтобы находить их по ID без привязки к managed-компонентам на Entity
    private readonly Dictionary<int, Mesh> _activeMeshes = new Dictionary<int, Mesh>();

    private readonly Stack<int> _availableIds = new Stack<int>();
    private int _idCounter = 0;

    public static ClientMeshPoolSystem Get(World world) => world.GetOrCreateSystemManaged<ClientMeshPoolSystem>();

    protected override void OnCreate()
    {
        for (int i = 0; i < 100; i++)
        {
            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            _availableMeshes.Push(mesh);

            // Заранее генерируем ID под стартовые меши
            _idCounter++;
            _availableIds.Push(_idCounter);
        }

        base.OnCreate();
    }

    // Безопасное получение меша для сетевой сущности
    public Mesh GetMesh(out int meshId)
    {
        Mesh mesh = null;
        if (_availableMeshes.Count > 0)
        {
            mesh = _availableMeshes.Pop();
        }

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.MarkDynamic();
        }
        else
        {
            mesh.Clear();
        }

        if (_availableIds.Count > 0)
        {
            meshId = _availableIds.Pop();
        }
        else
        {
            _idCounter++;
            meshId = _idCounter;
        }

        //_idCounter++;
        //meshId = _idCounter;
        _activeMeshes[meshId] = mesh;
        return mesh;
    }


    public bool TryGetActiveMesh(int id, out Mesh mesh)
    {
        return _activeMeshes.TryGetValue(id, out mesh);
    }

    /// <summary>
    /// Возвращает меш в пул
    /// </summary>
    /// <param name="meshId"></param>
    public void ReturnToPool(int id)
    {
        if (_activeMeshes.TryGetValue(id, out Mesh mesh))
        {
            if (mesh != null)
            {
                mesh.Clear();
                _availableMeshes.Push(mesh);
            }
            _activeMeshes.Remove(id);

            _availableIds.Push(id);
        }
    }


    protected override void OnUpdate()
    {
        // 1. Берем ECB из стандартной сетевой системы барьеров
        var ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbSystem.CreateCommandBuffer();

        //// 2. Ищем сущности, у которых БЫЛ меш, но сами данные чанка исчезли (деспавн сети)
        //// Использование SystemAPI.Query БЕЗОПАСНО для сети и не ломает потоки
        //foreach (var (meshLink, entity) in SystemAPI.Query<RefRO<ChunkMeshLink>>().WithAll<MaterialMeshInfo>().WithEntityAccess())
        //{
        //    int id = meshLink.ValueRO.PoolInstanceId;

        //    // Возвращаем меш в пул
        //    if (_activeMeshes.TryGetValue(id, out Mesh mesh))
        //    {
        //        if (mesh != null)
        //        {
        //            mesh.Clear();
        //            _availableMeshes.Push(mesh);
        //        }
        //        _activeMeshes.Remove(id);
        //    }

        //    // БЕЗОПАСНО ДЛЯ СЕТИ: Записываем команду на удаление компонента в конец кадра
        //    ecb.RemoveComponent<ChunkMeshLink>(entity);
        //}
    }

    protected override void OnDestroy()
    {
#if UNITY_EDITOR
        int countAvai = 0;
        int count = 0;
#endif
        foreach (var m in _availableMeshes) if (m != null)
            {
                Object.DestroyImmediate(m);
#if UNITY_EDITOR
                countAvai++;
#endif
            }
        foreach (var m in _activeMeshes.Values) if (m != null)
            {
                Object.DestroyImmediate(m);
#if UNITY_EDITOR
                count++;
#endif

            }

#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning($"Mesh OnDestroy: countAvai={countAvai}, count={count}");
#endif

        _availableIds.Clear();
    }
}
