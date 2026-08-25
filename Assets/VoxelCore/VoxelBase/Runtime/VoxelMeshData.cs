using Unity.Collections;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    public struct VoxelMeshData
    {
        public Mesh mesh;

        // NativeList'ы идеально подходят для передачи в Burst-джобы как ref/out параметры
        public NativeList<Vector3> vertices;
        public NativeList<Color32> colors; // Color32 весит в 4 раза меньше, чем Color (float4)
        public NativeList<int> opaqueTriangles;
        public NativeList<int> transparentTriangles;
        public bool Initialized;

        public void Initialize(string name, int estimatedSize = 2000)
        {
            if (!Initialized)
            {
                // Первая инициализация при создании объекта в пуле
                vertices = new NativeList<Vector3>(estimatedSize, Allocator.Persistent);
                colors = new NativeList<Color32>(estimatedSize, Allocator.Persistent);
                opaqueTriangles = new NativeList<int>(estimatedSize * 2, Allocator.Persistent);
                transparentTriangles = new NativeList<int>(estimatedSize, Allocator.Persistent);

                mesh = new Mesh();
                mesh.name = "VoxelPooledMesh";
                mesh.MarkDynamic();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                Initialized = true;
            }
            else
            {
                // Если объект достали из пула под НОВЫЙ конфиг, который БОЛЬШЕ предыдущего,
                // мы превентивно увеличиваем Capacity списков, чтобы избежать Resize внутри Job.
                if (vertices.Capacity < estimatedSize)
                {
                    vertices.Capacity = estimatedSize;
                    colors.Capacity = estimatedSize;
                    opaqueTriangles.Capacity = estimatedSize * 2;
                    transparentTriangles.Capacity = estimatedSize;
                }
            }
        }

        public void ClearData()
        {
            if (!Initialized)
            {
                Initialize("Default");
                return;
            }

            // Метод Clear() у NativeList обнуляет счетчик, но СОХРАНЯЕТ выделенную память (Capacity).
            // В следующий раз джоба запишет данные без единой аллокации (0 GC Alloc).
            vertices.Clear();
            colors.Clear();
            opaqueTriangles.Clear();
            transparentTriangles.Clear();

            mesh.Clear();
        }

        public void UploadMesh()
        {
            // 1. Привязываем вершины и цвета через AsArray()
            // Важно: Unity автоматически обрежет меш по переданной длине NativeArray,
            // но .AsArray() возвращает массив полной емкости (Capacity).
            // Поэтому сначала делаем GetSubArray, чтобы отсечь пустой хвост!

            var activeVertices = vertices.AsArray().GetSubArray(0, vertices.Length);
            var activeColors = colors.AsArray().GetSubArray(0, colors.Length);

            mesh.SetVertices(activeVertices);
            mesh.SetColors(activeColors);

            mesh.subMeshCount = 2;

            // 2. Устанавливаем треугольники через SetIndices (передает NativeArray без ошибок)
            // Параметры: (нативный массив, топология, индекс сабмеша, calculateBounds)
            var activeOpaque = opaqueTriangles.AsArray().GetSubArray(0, opaqueTriangles.Length);
            var activeTransparent = transparentTriangles.AsArray().GetSubArray(0, transparentTriangles.Length);

            mesh.SetIndices(activeOpaque, MeshTopology.Triangles, 0, false);
            mesh.SetIndices(activeTransparent, MeshTopology.Triangles, 1, false);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mesh.UploadMeshData(false);
        }

        // Очистка памяти при уничтожении машины или менеджера пула
        public void Dispose()
        {
            if (!Initialized) return;

            if (vertices.IsCreated) vertices.Dispose();
            if (colors.IsCreated) colors.Dispose();
            if (opaqueTriangles.IsCreated) opaqueTriangles.Dispose();
            if (transparentTriangles.IsCreated) transparentTriangles.Dispose();

            if (mesh != null) Object.Destroy(mesh);
            Initialized = false;
        }
    }
}
