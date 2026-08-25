using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Mikalai2006.VoxelBase
{
    [RequireComponent(typeof(SphereCollider))]
    public class ContainerSphere : Container
    {
        private SphereCollider sphereCollider;

        public override void Initialize(MeshConfig config, Vector3 position, VoxelMeshRender _vmr, Camera camera, Func<List<RemoveVoxel>, float, Vector3, Vector3, Transform, UniTask> callbackCreateExplodeVoxels)
        {
            base.Initialize(config, position, _vmr, camera, callbackCreateExplodeVoxels);
            
            if (!config.existCollider)
            {
                sphereCollider.enabled = false;
            } else
            {
                sphereCollider.isTrigger = config.isTrigger;
            }

            if (config.physMaterial != null)
            {
                sphereCollider.material = config.physMaterial;
            }

            if (_vmr.isDisableCollider)
            {
                sphereCollider.enabled = false;
            }
        }

        protected override void ConfigureComponents()
        {
            base.ConfigureComponents();
            
            sphereCollider = GetComponent<SphereCollider>();

            if (sphereCollider != null)
            {
                sphereCollider.radius = Mathf.Max(meshConfig.sOVoxelData.Bounds.x, meshConfig.sOVoxelData.Bounds.y, meshConfig.sOVoxelData.Bounds.z);
                sphereCollider.center = new Vector3(meshConfig.sOVoxelData.Bounds.x / 2f, meshConfig.sOVoxelData.Bounds.y / 2f, meshConfig.sOVoxelData.Bounds.z / 2f);
            }
        }

        //public override async UniTask<Mesh> UploadMeshGreedy(bool isDrawMesh)
        //{
        //    Mesh mesh = await base.UploadMeshGreedy(isDrawMesh);

        //    return mesh;
        //}

        public override MeshData UploadMesh(bool isDrawMesh)
        {
            base.UploadMesh(isDrawMesh);

            return meshData;
        }

        /// <summary>
        /// Определяет находится ли точка внутри sphere коллайдера.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override bool PointInCollider(Vector3 point)
        {
            // Get the closest point on the collider to the given point.
            Vector3 closestPoint = sphereCollider.ClosestPoint(point);

            // Check if the test point is inside the collider by comparing its distance to the closest point.
            // If the distance is very small, the point is inside.
            return Vector3.Distance(point, closestPoint) < 0.001f;
        }

    }
}