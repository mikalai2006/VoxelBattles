using UnityEngine;

public struct PoolNode
{
    public readonly GameObject GameObject;
    public readonly Transform Transform;
    public readonly Rigidbody Rigidbody;
    public readonly Collider Collider;
    public readonly Renderer Renderer;
    public readonly VirtualChild VirtualChild;

    // ПРЯМАЯ ССЫЛКА НА БАЗОВЫЙ КЛАСС: 0 вызовов GetComponent в рантайме!
    public readonly PoolEntity EntityLogic;
    public int Version;

    public PoolNode(GameObject go)
    {
        GameObject = go;
        Transform = go.transform;
        Rigidbody = go.GetComponent<Rigidbody>();
        Collider = go.GetComponent<Collider>();
        Renderer = go.GetComponent<Renderer>();
        Version = 1;

        VirtualChild = go.GetComponent<VirtualChild>();
        if (VirtualChild == null) VirtualChild = go.AddComponent<VirtualChild>();

        // Находим ЛЮБОЙ скрипт, унаследованный от PoolEntity (выполняется строго 1 раз при старте)
        EntityLogic = go.GetComponent<PoolEntity>();

        // Намертво связываем их, чтобы MyPoolEntity внутри ребенка никогда не был null!
        if (VirtualChild != null)
        {
            VirtualChild.SetPoolEntity(EntityLogic);
        }

#if UNITY_EDITOR
        if (EntityLogic == null)
        {
            Debug.LogError($"[UltraPool] На префабе {go.name} отсутствует компонент, наследуемый от PoolEntity!");
        }
#endif
    }
}
