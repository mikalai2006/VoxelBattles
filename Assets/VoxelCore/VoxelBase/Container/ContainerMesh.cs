using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    [RequireComponent(typeof(MeshCollider))]
    public class ContainerMesh : Container
    {
        private MeshCollider meshCollider;
        public override void Initialize(MeshConfig config, Vector3 position, VoxelMeshRender _vmr, Camera camera, Func<List<RemoveVoxel>, float, Vector3, Vector3, Transform, UniTask> callbackCreateExplodeVoxels)
        {
            base.Initialize(config, position, _vmr, camera, callbackCreateExplodeVoxels);

            if (!config.existCollider)
            {
                meshCollider.enabled = false;
            } else
            {
                meshCollider.convex = config.isConvex;
                meshCollider.isTrigger = config.isTrigger;
                // meshCollider.providesContacts = true;
            }
            
            if (config.physMaterial != null)
            {
                meshCollider.material = config.physMaterial;
            }
            // else if (_levelManager != null)
            // {
            //     meshCollider.convex = config.isConvex;
            //     
            // }

            if (_vmr.isDisableCollider)
            {
                meshCollider.enabled = false;
            }
        }


        protected override void ConfigureComponents()
        {
            base.ConfigureComponents();
            
            meshCollider = GetComponent<MeshCollider>();
        }

        public override async UniTask UploadMeshGreedy(bool isDrawMesh)
        {
            await base.UploadMeshGreedy(isDrawMesh);
            
            if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertices.Length > 3)
            {
                // meshData.mesh.Optimize();
                meshCollider.sharedMesh = meshFilter.sharedMesh; //meshData.mesh;
            }
        }

        public override MeshData UploadMesh(bool isDrawMesh)
        {
            base.UploadMesh(isDrawMesh);

            if (meshData.vertices.Count > 3)
            {
                // meshData.mesh.Optimize();
                meshCollider.sharedMesh = meshData.mesh;
            }

            return meshData;
        }

        /// <summary>
        /// Определяет находится ли точка внутри mesh коллайдера.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override bool PointInCollider(Vector3 point)
        {
            // Get the closest point on the collider to the given point.
            Vector3 closestPoint = meshCollider.ClosestPoint(point);

            // Check if the test point is inside the collider by comparing its distance to the closest point.
            // If the distance is very small, the point is inside.
            return Vector3.Distance(point, closestPoint) < 0.1f;
        }

    }
}