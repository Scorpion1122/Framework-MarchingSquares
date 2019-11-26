using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [RequireComponent(typeof(TileTerrain)), DefaultExecutionOrder(-11)]
    public abstract class TileTerrainComponent : MonoBehaviour
    {
        public TileTerrain TileTerrain { get; private set; }

        protected virtual void Awake()
        {
            TileTerrain = GetComponent<TileTerrain>();
        }
    }
}