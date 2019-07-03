using System;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [CreateAssetMenu(fileName = "Material Template", menuName = "Database/Voxel/Material Template")]
    public class MaterialTemplate : ScriptableObject
    {
        public Material typeOneMaterial;
        public Material typeTwoMaterial;

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
            return null;
        }
    }
}
