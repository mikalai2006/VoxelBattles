using System.Collections.Generic;
using UnityEngine;

namespace Mikalai2006.VoxelBase {
public struct MeshData
    {
        public Mesh mesh;
        public List<Vector3> vertices;
        public int subMeshCount;
        public List<int> triangles;
        public List<int> trianglesAlpha;
        // public Dictionary<int, List<int>> triangles;
        public List<Vector2> UVs;
        public List<Vector2> UVs2;
        public List<Color> colors;
        public bool Initialized;

        public void ClearData()
        {
            if (!Initialized)
            {
                vertices = new List<Vector3>();
                triangles = new List<int>(); //new Dictionary<int, List<int>>();
                trianglesAlpha = new List<int>();
                UVs = new List<Vector2>();
                UVs2 = new List<Vector2>();
                colors = new List<Color>();

                Initialized = true;
                mesh = new Mesh();
            }
            else
            {
                vertices.Clear();
                triangles.Clear();

                trianglesAlpha.Clear();
                UVs.Clear();
                UVs2.Clear();
                colors.Clear();

                mesh.Clear();
            }
        }
        public void UploadMesh(bool sharedVertices = false)
        {
            mesh.SetVertices(vertices);

            mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles, 0, false);
            mesh.SetTriangles(trianglesAlpha, 1, false);

            // subMeshCount = triangles.Count;
            // mesh.subMeshCount = triangles.Count;
            // for (int i = 0; i < triangles.Count; i++)
            // {
            //     mesh.SetTriangles(triangles[i], i, false);
            // }

            mesh.SetColors(colors);

            mesh.SetUVs(0, UVs);
            mesh.SetUVs(2, UVs2);

            mesh.Optimize();

            mesh.RecalculateNormals();

            mesh.RecalculateBounds();

            mesh.UploadMeshData(false);
        }
    }
//public struct MeshDataWithSub
    //{
    //    public Mesh mesh;
    //    public int subMeshCount;
    //    // public List<Vector3> vertices;
    //    // public List<int> triangles;
    //    // // public Dictionary<int, List<int>> triangles;
    //    // public List<Vector2> UVs;
    //    // public List<Vector2> UVs2;
    //    // public List<Color> colors;
    //    public NativeList<Vector3> vertices;
    //    public NativeList<int> triangles;
    //    public NativeList<Vector2> uvs;
    //    public NativeList<Color> colors;
    //    public NativeList<Vector2> UVs;
    //    public NativeList<Vector2> UVs2;
    //    public bool Initialized;

    //    public void ClearData()
    //    {
    //        if (!Initialized)
    //        {
    //            // vertices = new List<Vector3>();
    //            // triangles = new List<int>(); //new Dictionary<int, List<int>>(); // 
    //            // UVs = new List<Vector2>();
    //            // UVs2 = new List<Vector2>();
    //            // colors = new List<Color>();
    //            vertices = new NativeList<Vector3>(Allocator.Persistent);
    //            triangles = new NativeList<int>(Allocator.Persistent);
    //            UVs = new NativeList<Vector2>(Allocator.Persistent);
    //            UVs2 = new NativeList<Vector2>(Allocator.Persistent);
    //            colors = new NativeList<Color>(Allocator.Persistent);

    //            Initialized = true;
    //            mesh = new Mesh();
    //            mesh.MarkDynamic();
    //            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    //        }
    //        else
    //        {
    //            vertices.Clear();
    //            triangles.Clear();
    //            UVs.Clear();
    //            UVs2.Clear();
    //            colors.Clear();

    //            mesh.Clear();
    //        }
    //    }

    //    public void Destroy()
    //    {
    //        vertices.Dispose();
    //        triangles.Dispose();
    //        UVs.Dispose();
    //        UVs2.Dispose();
    //        colors.Dispose();
    //    }

    //    public void UploadMesh(bool sharedVertices = false)
    //    {
    //        mesh.SetVertices(vertices.AsArray());


    //        mesh.SetTriangles(triangles.ToArray(Allocator.Temp).ToArray(), subMeshCount);

    //        // subMeshCount = triangles.Count;
    //        // mesh.subMeshCount = triangles.Count;
    //        // for (int i = 0; i < triangles.Count; i++)
    //        // {
    //        //     mesh.SetTriangles(triangles[i], i, false);
    //        // }

    //        mesh.SetColors(colors.AsArray());

    //        mesh.SetUVs(0, UVs.AsArray());
    //        mesh.SetUVs(2, UVs2.AsArray());

    //        mesh.Optimize();

    //        mesh.RecalculateNormals();

    //        mesh.RecalculateBounds();

    //        mesh.UploadMeshData(false);
    //    }
    //}
}
