//using Cysharp.Threading.Tasks;
//using System.Collections.Generic;
//using UnityEngine;


//namespace Mikalai2006.VoxelBase
//{
//    public static class HelperVoxel
//    {
//        #region Static Variables
//        public static readonly Vector3[] voxelVertices = new Vector3[8]
//        {
//            new Vector3(0,0,0),//0
//            new Vector3(1,0,0),//1
//            new Vector3(0,1,0),//2
//            new Vector3(1,1,0),//3

//            new Vector3(0,0,1),//4
//            new Vector3(1,0,1),//5
//            new Vector3(0,1,1),//6
//            new Vector3(1,1,1),//7
//        };

//        public static readonly Vector3[] voxelFaceChecks = new Vector3[6]
//        {
//            new Vector3(0,0,-1),//back
//            new Vector3(0,0,1),//front
//            new Vector3(-1,0,0),//left
//            new Vector3(1,0,0),//right
//            new Vector3(0,-1,0),//bottom
//            new Vector3(0,1,0)//top
//        };

//        // static readonly int[,] voxelVertexIndex = new int[6, 4]
//        // {
//        //     {0,1,2,3},
//        //     {4,5,6,7},
//        //     {4,0,6,2},
//        //     {5,1,7,3},
//        //     {0,1,4,5},
//        //     {2,3,6,7},
//        // };
//        public static readonly int[] voxelVertexIndex = new int[24]
//        {
//            0,1,2,3,
//            4,5,6,7,
//            4,0,6,2,
//            5,1,7,3,
//            0,1,4,5,
//            2,3,6,7,
//        };

//        public static readonly Vector2[] voxelUVs = new Vector2[4]
//        {
//            new Vector2(0,0),
//            new Vector2(0,1),
//            new Vector2(1f/256f,0),
//            new Vector2(1f/256f,1)
//        };

//        public static readonly int[] voxelTris = new int[36]
//        {
//            0,2,3,0,3,1,
//            0,1,2,1,3,2,
//            0,2,3,0,3,1,
//            0,1,2,1,3,2,
//            0,1,2,1,3,2,
//            0,2,3,0,3,1,
//        };

//        public static bool AreListsEqual<T>(List<T> list1, List<T> list2)
//        {
//            if (list1.Count != list2.Count)
//                return false;

//            for (int i = 0; i < list1.Count; i++)
//            {
//                if (!EqualityComparer<T>.Default.Equals(list1[i], list2[i]))
//                    return false;
//            }
//            return true;
//        }

//        public static bool AreArraysEqual<T>(T[] list1, T[] list2)
//        {
//            if (list1.Length != list2.Length)
//                return false;

//            for (int i = 0; i < list1.Length; i++)
//            {
//                if (!EqualityComparer<T>.Default.Equals(list1[i], list2[i]))
//                    return false;
//            }
//            return true;
//        }

//        public static bool AreColorEqual(Voxel[] list1, Voxel[] list2)
//        {
//            if (list1.Length != list2.Length)
//                return false;
//            // Debug.Log($"{GetArrayHashCode(list1) == GetArrayHashCode(list2)}, {GetArrayHashCode(list1)}, {GetArrayHashCode(list2)}");

//            for (int i = 0; i < list1.Length; i++)
//            {
//                // if (list1[i].color.b >= 255 && list2[i].color.b >= 255)
//                // {
//                //     Debug.Log($"{list1[i].color.b}-{list2[i].color.b}");
//                // }

//                if ((
//                    list1[i].color.r != list2[i].color.r
//                    || list1[i].color.g != list2[i].color.g
//                    || list1[i].color.b != list2[i].color.b
//                    || list1[i].color.a != list2[i].color.a) && list1[i].color.b != 255 && list2[i].color.b != 255)
//                    return false;
//            }

//            return true;
//        }

//        /// <summary>
//        /// Вспомогательная функция для сравнения двух цветов с заданным допуском.
//        /// </summary>
//        /// <param name="a"></param>
//        /// <param name="b"></param>
//        /// <param name="tolerance"></param>
//        /// <returns></returns>
//        public static bool AreColorsApproximatelyEqual(Color a, Color b, float tolerance = 0.001f)
//        {
//            // Check if the absolute difference of each channel is within the tolerance
//            if (!Mathf.Approximately(a.r, b.r)) return false;
//            if (!Mathf.Approximately(a.g, b.g)) return false;
//            if (!Mathf.Approximately(a.b, b.b)) return false;
//            if (!Mathf.Approximately(a.a, b.a)) return false;

//            return true;
//        }

//        /// <summary>
//        /// Есть ли цвета, отличные от прозрачного и заданного
//        /// </summary>
//        /// <param name="list"></param>
//        /// <param name="list2"></param>
//        /// <returns></returns>
//        public static bool AreExistColors(Voxel[] list)
//        {
//            if (list.Length == 0)
//                return false;

//            for (int i = 0; i < list.Length; i++)
//            {
//                if ((
//                    list[i].color.r > 0
//                    || list[i].color.g > 0
//                    || list[i].color.a > 0) && list[i].color.b != 255 )
//                    return true;
//            }

//            return false;
//        }

//        // Метод для вычисления хеш-кода на основе содержимого массива
//        public static int GetArrayHashCode<T>(T[] array)
//        {
//            if (array == null)
//            {
//                return 0;
//            }

//            // Для примера используем простой метод: суммирование хеш-кодов элементов
//            int hash = 17; // Начальное значение
//            foreach (var element in array)
//            {
//                hash = hash * 31 + (element?.GetHashCode() ?? 0);
//            }
//            return hash;
//        }
//        #endregion

//        // Helper function to check if a point is inside a sphere
//        public static bool IsInsideSphere(Vector3 point, Vector3 sphereCenter, float sphereRadius)
//        {
//            return (point - sphereCenter).sqrMagnitude <= (sphereRadius * sphereRadius); //Vector3.Distance(point, sphereCenter) <= sphereRadius;
//        }

//        /// <summary>
//        /// Takes 3D indexes and returns a 1D index based on them
//        /// </summary>
//        /// <param name="x"></param>
//        /// <param name="y"></param>
//        /// <param name="z"></param>
//        /// <param name="xMax"></param>
//        /// <param name="yMax"></param>
//        /// <returns>1D index calulation</returns>
//        public static int To1D(int x, int y, int z, int xMax, int yMax)
//        {
//            //return (z * xMax * yMax) + (y * xMax) + x;
//            return x + xMax * (y + yMax * z);
//        }

//        /// <summary>
//        /// Takes 2D indexes and returns a 1D index based on them
//        /// </summary>
//        /// <param name="x"></param>
//        /// <param name="y"></param>
//        /// <returns>1D index calulation</returns>
//        public static int To1D(int x, int y, int width)
//        {
//            return y * width + x;
//        }

//        /// <summary>
//        /// Универсальный статический метод для нанесения урона и расчета взрыва воксельных контейнеров.
//        /// </summary>
//        /// <param name="hit">Данные о попадании из Raycast или SphereCast</param>
//        /// <param name="attacker">GameObject, который наносит урон (сама пуля/снаряд)</param>
//        /// <param name="explodeRadius">Радиус взрыва из настроек Scriptable Object пули</param>
//        public static void ProcessVoxelHit(Collider collider, Vector3 point, Vector3 normal, GameObject attacker, float explodeRadius)
//        {
//            if (collider == null) return;

//            // 2. Ловим физический взрыв (float.MaxValue) по любой из осей
//            // 3.402823E+38f — это константа float.MaxValue в C#
//            if (point.x >= 3.402823E+38f || point.x <= -3.402823E+38f ||
//                point.y >= 3.402823E+38f || point.y <= -3.402823E+38f ||
//                point.z >= 3.402823E+38f || point.z <= -3.402823E+38f ||
//                float.IsNaN(point.x))
//            {
//                // Резервный план: если PhysX выдал ошибку, берем чистую позицию самого объекта.
//                // Это спасет геометрию звука от NaN и подставит стабильные Normal Floats.
//                point = attacker.transform.position;
//            }

//            GameObject hitObject = collider.gameObject;
//            Container voxelContainer = hitObject.GetComponent<Container>();

//            if (voxelContainer != null)
//            {
//                if (voxelContainer.IsDestructible())
//                {
//                    // Переводим точку попадания в локальные координаты воксельного объекта
//                    Vector3 localPoint = hitObject.transform.InverseTransformPoint(point);

//                    // Запускаем воксельный взрыв асинхронно
//                    voxelContainer.ExposionVoxels(
//                        attacker,
//                        localPoint,
//                        true,
//                        hitObject,
//                        explodeRadius,
//                        attacker.transform.forward,
//                        normal
//                    ).Forget();

//                    // Автоматически определяем аудио-материал на основе слоя Unity
//                    VoxelMaterialType materialType = VoxelMaterialType.Brick;
//                    if (hitObject.layer == LayerMask.NameToLayer("Tree"))
//                    {
//                        materialType = VoxelMaterialType.Wood;
//                    }

//                    // Запускаем 3D звук разрушения вокселей
//                    if (VoxelAudioManager.Instance != null)
//                    {
//                        VoxelAudioManager.Instance.PlayDestructionSound3DAsync(
//                            attacker.transform.position,
//                            materialType,
//                            explodeRadius
//                        ).Forget();
//                    }
//                }
//            }
//        }

//    }

//}