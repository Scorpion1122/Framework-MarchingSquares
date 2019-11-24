using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Thijs.Framework.MarchingSquares
{
    public class ColliderPool : IDisposable
    {
        private GameObject root;
        private List<EdgeCollider2D> colliders;
        private int useCount;

        public ColliderPool(Transform parent, int layer)
        {
            //root = new GameObject($"Collider Pool {layer}");
            root = new GameObject($"Collider Pool: {LayerMask.LayerToName(layer)}");
            root.transform.SetParent(parent, false);
            root.layer = layer;
            root.hideFlags = HideFlags.DontSave;
            
            colliders = new List<EdgeCollider2D>();
        }
        
        public void ResetUsage()
        {
            useCount = 0;
        }

        public void ClearUnused()
        {
            for (int i = colliders.Count - 1; i > useCount; i--)
            {
                if (Application.isPlaying)
                    Object.Destroy(colliders[i]);
                else
                    Object.DestroyImmediate(colliders[i]);
                colliders.RemoveAt(i);
            }
        }

        public void AddEdge(PhysicsMaterial2D material, Vector2[] points)
        {
            EdgeCollider2D edgeCollider = GetNextEdgeCollider();
            edgeCollider.sharedMaterial = material;
            edgeCollider.points = points;
        }

        private EdgeCollider2D GetNextEdgeCollider()
        {
            if (colliders.Count <= useCount)
                colliders.Add(root.AddComponent<EdgeCollider2D>());
            useCount++;
            return colliders[useCount - 1];
        }

        public void Dispose()
        {
            if (Application.isPlaying)
                Object.Destroy(root);
            else
                Object.DestroyImmediate(root);
            colliders.Clear();
        }
    }
}