using System;
using Thijs.Core.PropertyAttributes;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [CreateAssetMenu(fileName = "Material Template", menuName = "Database/Voxel/Material Template")]
    public class MaterialTemplate : ScriptableObject
    {
        [SerializeField] private Material typeOneMaterial = null;
        [SerializeField] private Material typeTwoMaterial = null;

        [SerializeField] private PhysicsMaterial2D typeOnePhysicsMaterial = null;
        [SerializeField] private PhysicsMaterial2D typeTwoPhysicsMaterial = null;

        [SerializeField, Layer] private int typeOneLayer = 0;
        [SerializeField, Layer] private int typeTwoLayer = 0;

        public Material GetMaterial(FillType fillType)
        {
            switch (fillType)
            {
                case FillType.TypeOne:
                    return typeOneMaterial;
                case FillType.TypeTwo:
                    return typeTwoMaterial;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillType), fillType, null);
            }
        }

        public PhysicsMaterial2D GetPhysicsMaterial(FillType fillType)
        {
            switch (fillType)
            {
                case FillType.TypeOne:
                    return typeOnePhysicsMaterial;
                case FillType.TypeTwo:
                    return typeTwoPhysicsMaterial;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillType), fillType, null);
            }
        }

        public int GetLayer(FillType fillType)
        {
            switch (fillType)
            {
                case FillType.TypeOne:
                    return typeOneLayer;
                case FillType.TypeTwo:
                    return typeTwoLayer;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillType), fillType, null);
            }
        }
    }
}
