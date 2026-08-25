//using System.Collections.Generic;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;


//namespace Mikalai2006.VoxelBase
//{
//    public class VoxelMesh
//    {
//        private Dictionary<float3, Voxel> data;
//        public Dictionary<float3, Voxel> Data => data;
//        private NativeArray<Voxel> arrayVoxels;
//        // public NativeArray<Voxel> ArrayVoxels => arrayVoxels;
//        private NativeArray<VoxelColors> arrayVoxelColors;

//        private MeshDataWithSub meshData = new MeshDataWithSub();
//        private MeshConfig meshConfig;

//        public void OnDestroy()
//        {
//            arrayVoxels.Dispose();
//            arrayVoxelColors.Dispose();

//            meshData.Destroy();
//        }

//        public void Initialize(MeshConfig config)
//        {
//            meshConfig = config;

//            data = new Dictionary<float3, Voxel>();
//        }

//        public void SetData()
//        {
//            // Vector3Int[] voxelList = meshConfig.sOVoxelData.groups[indexSubMesh].voxels.AsParallel().ToArray();

//            arrayVoxelColors = new NativeArray<VoxelColors>(meshConfig.sOVoxelData.groups.Count + 1, Allocator.Persistent);

//            // create array voxels for jobs.
//            arrayVoxels = new NativeArray<Voxel>(meshConfig.sOVoxelData.Bounds.x * meshConfig.sOVoxelData.Bounds.y * meshConfig.sOVoxelData.Bounds.z, Allocator.Persistent);

//            // parse list voxels and create data. 
//            for (int j = 0; j < meshConfig.sOVoxelData.groups.Count; j++)
//            {
//                Color color = meshConfig.sOVoxelData.groups[j].color;
//                color.a = 1;
//                arrayVoxelColors[j + 1] = new VoxelColors()
//                {
//                    color = color,
//                    type = (VoxelType)(j + 1)
//                };


//                for (int i = 0; i < meshConfig.sOVoxelData.groups[j].voxels.Count; i++)
//                {

//                    Vector3Int pos = Vector3Int.FloorToInt(meshConfig.sOVoxelData.groups[j].voxels[i]);
//                    var vox = new Voxel() // * scale
//                    {
//                        ID = 1,
//                        color = color, //meshConfig.sOVoxelData.colors.ElementAt(i),
//                        type = (VoxelType)(j + 1),
//                        position = pos,
//                        IndexSubMesh = j,
//                    };
//                    this[meshConfig.sOVoxelData.groups[j].voxels[i]] = vox;

//                    arrayVoxels[VoxelHelpers.To1D(pos.x, pos.y, pos.z, meshConfig.sOVoxelData.Bounds.x, meshConfig.sOVoxelData.Bounds.y)] = vox;
//                }
//            }
//        }

//        public void ClearData()
//        {
//            // vertices.Dispose();
//            // triangles.Dispose();
//            data.Clear();
//        }

//        public MeshDataWithSub GenerateMesh()
//        {
//            float startTime = Time.realtimeSinceStartup;
//            meshData.ClearData();

//            Vector3 blockPos;
//            Voxel block;

//            int counter = 0;
//            Vector3[] faceVertices = new Vector3[4];
//            Vector2[] faceUVs = new Vector2[4];

//            // VoxelColor voxelColor;
//            // Color voxelColorAlpha;
//            // Vector2 voxelSmoothness;

//            foreach (KeyValuePair<float3, Voxel> kvp in data)
//            {
//                // Проверяйте только сплошные блоки.
//                if (!kvp.Value.isSolid)
//                {
//                    continue;
//                }

//                blockPos = kvp.Key;
//                block = kvp.Value;

//                // // проверяем есть ли в словаре запись для индекса подсетки.
//                // if (!meshData.triangles.ContainsKey(block.IndexSubMesh))
//                // {
//                //     meshData.triangles[block.IndexSubMesh] = new List<int>();
//                // }

//                // voxelColor = WorldManager.Instance.WorldColors[block.ID - 1];
//                // voxelColorAlpha = voxelColor.color;
//                // voxelColorAlpha.a = 1;
//                // voxelSmoothness = new Vector2(voxelColor.metallic, voxelColor.smoothness);
//                // Iterate over each face direction
//                for (int i = 0; i < 6; i++)
//                {
//                    // Проверьте, есть ли сплошной блок напротив этой грани.
//                    if (this[blockPos + HelperVoxel.voxelFaceChecks[i]].isSolid)
//                    {
//                        continue;
//                    }

//                    //Draw this face

//                    // Соберите соответствующие вершины из вершин по умолчанию и добавьте позицию блока.
//                    for (int j = 0; j < 4; j++)
//                    {
//                        faceVertices[j] = HelperVoxel.voxelVertices[HelperVoxel.voxelVertexIndex[j + i * 4]] + blockPos;
//                        faceUVs[j] = HelperVoxel.voxelUVs[j];
//                    }

//                    for (int j = 0; j < 6; j++)
//                    {
//                        meshData.vertices.Add(faceVertices[HelperVoxel.voxelTris[j + i * 6]]);
//                        meshData.UVs.Add(faceUVs[HelperVoxel.voxelTris[j + i * 6]]);
//                        meshData.colors.Add(block.color); //voxelColorAlpha);
//                        meshData.UVs2.Add(new Vector2(0.4f, 0.75f)); // voxelSmoothness

//                        // if (!meshData.triangles.ContainsKey(block.IndexSubMesh))
//                        // {
//                        //     meshData.triangles[block.IndexSubMesh] = new List<int>();
//                        // }
//                        // meshData.triangles[block.IndexSubMesh].Add(counter++);
//                        meshData.triangles.Add(counter++);
//                    }
//                }

//            }
//            // NativeQueue<float3> voxelJobDatas = new NativeQueue<float3>(Allocator.Persistent);

//            // var meshGenerationJob = new GenerateMeshDataJob
//            // {
//            //     voxelJobDatas = voxelJobDatas.AsParallelWriter(),
//            //     voxels = arrayVoxels,
//            //     width = meshConfig.sOVoxelData.Bounds.x,

//            // };

//            // JobHandle handle = meshGenerationJob.Schedule(arrayVoxels.Length, 64); // Schedule for parallel processing
//            // handle.Complete();

//            // int counter = 0;
//            // Vector3[] faceVertices = new Vector3[4];
//            // Vector2[] faceUVs = new Vector2[4];
//            // // foreach (KeyValuePair<float3, Voxel> kvp in data)
//            // while (voxelJobDatas.TryDequeue(out float3 positionVoxel))
//            // {
//            //     var voxel = data[positionVoxel];
//            //     // Проверяйте только сплошные блоки.
//            //     if (!voxel.isSolid)
//            //     {
//            //         continue;
//            //     }

//            //     // voxelColor = WorldManager.Instance.WorldColors[block.ID - 1];
//            //     // voxelColorAlpha = voxelColor.color;
//            //     // voxelColorAlpha.a = 1;
//            //     // voxelSmoothness = new Vector2(voxelColor.metallic, voxelColor.smoothness);
//            //     // Iterate over each face direction
//            //     for (int i = 0; i < 6; i++)
//            //     {
//            //         // Проверьте, есть ли сплошной блок напротив этой грани.
//            //         if (this[(Vector3)positionVoxel + HelperVoxel.voxelFaceChecks[i]].isSolid)
//            //         {
//            //             continue;
//            //         }

//            //         //Draw this face

//            //         // Соберите соответствующие вершины из вершин по умолчанию и добавьте позицию блока.
//            //         for (int j = 0; j < 4; j++)
//            //         {
//            //             faceVertices[j] = HelperVoxel.voxelVertices[HelperVoxel.voxelVertexIndex[j + i * 4]] + (Vector3)positionVoxel;
//            //             faceUVs[j] = HelperVoxel.voxelUVs[j];
//            //         }

//            //         for (int j = 0; j < 6; j++)
//            //         {
//            //             meshData.vertices.Add(faceVertices[HelperVoxel.voxelTris[j + i * 6]]);
//            //             meshData.UVs.Add(faceUVs[HelperVoxel.voxelTris[j + i * 6]]);
//            //             meshData.colors.Add(voxel.color); //voxelColorAlpha);
//            //             meshData.UVs2.Add(new Vector2(0.4f, 0.75f)); // voxelSmoothness

//            //             // if (!meshData.triangles.ContainsKey(block.IndexSubMesh))
//            //             // {
//            //             //     meshData.triangles[block.IndexSubMesh] = new List<int>();
//            //             // }
//            //             // meshData.triangles[block.IndexSubMesh].Add(counter++);
//            //             meshData.triangles.Add(counter++);
//            //         }
//            //     }
//            // }
//            // Debug.Log($"voxelJobDatas count ={voxelJobDatas.Count}");

//            Debug.Log($"VoxelMesh: Time generate mesh: {(Time.realtimeSinceStartup - startTime) * 1000f} ms.\r\n rrayVoxels.Count={arrayVoxels.Length}, Create {meshData.vertices.Length} vertices, {meshData.triangles.Length} triangles");

//            return meshData;
//        }


//    //     public Mesh.MeshDataArray GenerateMesh2()
//    //     {
//    //         // 1. Allocate Writable MeshData
//    //         Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
//    //         Mesh.MeshData meshData2 = meshDataArray[0];

//    //         // Calculate counts based on resolution
//    //         int resolution = meshConfig.sOVoxelData.Bounds.x * meshConfig.sOVoxelData.Bounds.y * meshConfig.sOVoxelData.Bounds.z;
//    //         int vertexCount = resolution;
//    //         int indexCount = resolution * vertexCount * 6; // 2 triangles per quad, 3 indices per triangle

//    //         // 2. Set Buffer Parameters
//    //         meshData2.SetVertexBufferParams(vertexCount,
//    //             new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
//    //             new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
//    //             new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));
//    //         meshData2.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

//    //         // 3. Get NativeArray views for the job
//    //         NativeArray<Vector3> vertices = meshData2.GetVertexData<Vector3>();
//    //         NativeArray<Vector3> normals = meshData2.GetVertexData<Vector3>(1); // Index 1 for normals
//    //         NativeArray<Vector2> uvs = meshData2.GetVertexData<Vector2>(2);     // Index 2 for UVs
//    //         NativeArray<uint> triangles = meshData2.GetIndexData<uint>();

//    //         // 4. Schedule the Burst-compiled Job
//    //         var meshGenerationJob = new GenerateMeshDataJob
//    //         {
//    //             Resolution = resolution,
//    //             Vertices = vertices,
//    //             Normals = normals,
//    //             UVs = uvs,
//    //             Triangles = triangles
//    //         };

//    //         JobHandle handle = meshGenerationJob.Schedule(vertexCount, 64); // Schedule for parallel processing
//    //         handle.Complete(); // Wait for the job to complete (for simplicity; in real game, chain jobs)

//    //         // // 5. Apply the data to the Mesh and dispose
//    //         // Mesh mesh = new Mesh();
//    //         // Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, new[] { mesh });

//    //         // // Assign the generated mesh to the MeshFilter
//    //         // targetMeshFilter.mesh = mesh;

//    //         // // Recalculate bounds and normals if not done in job
//    //         // mesh.RecalculateBounds();

//    //         return meshDataArray;
//    // }


//        public MeshDataWithSub UploadMeshGreedy()
//        {
//            meshData.ClearData();

//            // meshData.colors.Clear();
//            // meshData.mesh.SetColors(meshData.colors);
//            float startTime = Time.realtimeSinceStartup;

//            // Voxel[] voxArray = new Voxel[_sOVoxelData.Bounds.x * _sOVoxelData.Bounds.y * _sOVoxelData.Bounds.z];
//            var mesh = meshData.mesh; //meshFilter.sharedMesh;
//            var meshArray = Mesh.AllocateWritableMeshData(mesh);
//            var _job = new MeshGreedyJob();
//            _job.mesh = meshArray[0];
//            _job.chunkSize = new int3(meshConfig.sOVoxelData.Bounds.x, meshConfig.sOVoxelData.Bounds.y, meshConfig.sOVoxelData.Bounds.z);
//            _job.blockSize = 1;
//            _job.voxelColors = arrayVoxelColors;

//            // Debug.Log($"Time greedy mesh step0: {(Time.realtimeSinceStartup - startTime) * 1000f} ms");
//            // Parallel.For(0, data.Count, (g) =>
//            // {
//            //     Vector3Int pos = Vector3Int.FloorToInt(data.ElementAt(g).Key);
//            //     voxArray[Helpers.To1D(pos.x, pos.y, pos.z, _sOVoxelData.Bounds.x, _sOVoxelData.Bounds.y)] = data.ElementAt(g).Value;
//            // });
//            // Debug.Log($"Time greedy mesh step1: {(Time.realtimeSinceStartup - startTime) * 1000f} ms");
//            _job.voxels = arrayVoxels; // new NativeArray<Voxel>(voxArray, Allocator.TempJob);
//            _job.Schedule().Complete();

//            // Debug.Log($"Time greedy mesh step2: {(Time.realtimeSinceStartup - startTime) * 1000f} ms");
//            Mesh.ApplyAndDisposeWritableMeshData(meshArray, mesh);

//            // FIXME: For some reason setting bounds directly doesn't work so this is needed as a workaround, investigate
//            mesh.RecalculateBounds();
//            meshData.mesh = mesh;
//            // Debug.Log($"vertices={mesh.vertices.Length}, colors={mesh.colors.Length}");

//            // _job.voxels.Dispose();
//            Debug.Log($"Time greedy mesh: {(Time.realtimeSinceStartup - startTime) * 1000f} ms");

//            return meshData;
//        }

//        public Voxel this[Vector3 index]
//        {
//            get
//            {
//                if (data.ContainsKey(index))
//                    return data[index];
//                else
//                    return emptyVoxel;
//            }

//            set
//            {
//                if (data.ContainsKey(index))
//                    data[index] = value;
//                else
//                    data.Add(index, value);
//            }
//        }

//        public void SetVoxelData(Vector3 pos, Voxel voxelData = default)
//        {
//            Vector3Int posInt = Vector3Int.FloorToInt(pos);
//            arrayVoxels[VoxelHelpers.To1D(posInt.x, posInt.y, posInt.z, meshConfig.sOVoxelData.Bounds.x, meshConfig.sOVoxelData.Bounds.y)] = voxelData;
//        }

//        public static Voxel emptyVoxel = new Voxel() { ID = 0 };
        
        

//    }

//}