using Thijs.Core.PropertyAttributes;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [CreateAssetMenu(fileName = "Tile Template", menuName = "Database/Voxel/Tile Template")]
    public class TileTemplate : ScriptableObject
    {
        [SerializeField] private string[] names = {};
        [SerializeField] private Material[] materials = {};
        [SerializeField] private PhysicsMaterial2D[] physicsMaterials = {};
        [SerializeField, Layer] private int[] layers = {};

        public string[] Names => names;

        public Material GetMaterial(FillType fillType)
        {
            int index = (int) fillType;
            if (index < materials.Length)
                return materials[index];
            return null;
        }

        public PhysicsMaterial2D GetPhysicsMaterial(FillType fillType)
        {
            int index = (int) fillType;
            if (index < physicsMaterials.Length)
                return physicsMaterials[index];
            return null;
        }

        public int GetLayer(FillType fillType)
        {
            int index = (int) fillType;
            if (index < layers.Length)
                return layers[index];
            return Layers.DEFAULT;
        }

        public string GetName(FillType fillType)
        {
            int index = (int) fillType;
            if (index < names.Length)
            {
                string result = names[index];
                if (!string.IsNullOrEmpty(result))
                    return result;
            }
            return names.ToString();
        }
    }
}
