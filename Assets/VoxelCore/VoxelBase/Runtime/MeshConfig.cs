using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    [System.Serializable]
    public struct MeshConfig
    {
        [Header("Опции меша")]
        public SOVoxelData sOVoxelData;
        public Material _material;
        public Material _materialAlpha;
        [Tooltip("Рендерит все объекты модели в один меш и использует цвет вершин. Нужно, чтобы материал поддерживал шейдер с Vertex Color")]
        public bool isOneMesh;
        [Tooltip("Упрощение меша (Оптимизация количества вершин)")]
        public bool isGreedy;
        [Tooltip("Отбрасывать ли тени")]
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        [Tooltip("Если это тайл, то использовать пивот, как половина максимального измерения по X,Z. Например: 32x32x20 - пивот -> 15.5x0.5x15.5")]
        public bool isTile;

        [Space(5)]
        [Header("Опции колайдера")]
        public bool existCollider;
        public TypeCollider typeCollider;
        public PhysicsMaterial physMaterial;
        public bool isConvex;
        public bool isTrigger;

        [Space(5)]
        [Header("Опции твердого тела")]
        public bool isRigidbody;
        public bool isKinematic;
        public int mass;
        // public bool freezeRotation;
        // public RigidbodyConstraints constraints;
        public bool isGravity;
        public bool FreezePositionX;
        public bool FreezePositionY;
        public bool FreezePositionZ;
        public bool FreezeRotationX;
        public bool FreezeRotationY;
        public bool FreezeRotationZ;
        // [Tooltip("Принудительное включение GPU Instances.Включение этой опции принудительно заставит меши использовать GPU Instances, если он активирован в материале")]
        // public bool enableGPUInstances;
        // public float emissionValue;
        public RigidbodyInterpolation rigidbodyInterpolation;
        public CollisionDetectionMode collisionDetection;

        [Space(5)]
        [Header("Другие опции")]
        [Tooltip("Указывает разрушаемый ли объект")]
        public bool isDestructible;
        [Header("Масштаб объекта")]
        [Tooltip("Устанавливать ли глобальный масштаб к Wrapper. Это нужно делать на объектах, которые не имеют много вложенных VoxelMeshRender.")]
        public bool useGlobalScale;
        [Tooltip("Если он больше 0, то устанавливается для объекта")]
        [Range(0,1)] public float customScale;
        // [Tooltip("Список новых цветов (если заданы цвета здесь, они перезапишут цвета из sOVoxelData используя соответствующие индексы) (В идеале число цветов здесь и в sOVoxelData должны совпадать)")]
        // public MeshConfigModify meshConfigModify;
    }

    public enum TypeCollider
    {
        MeshCollider,
        BoxCollider,
        SphereCollider
    }
}
