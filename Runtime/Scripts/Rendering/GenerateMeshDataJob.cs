using System;
using System.Numerics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Vector3 = UnityEngine.Vector3;

namespace Thijs.Framework.MarchingSquares
{
    public struct GenerateMeshDataJob : IJob
    {
        [ReadOnly] public NativeArray<FillType> generateForFillTypes;
        [ReadOnly] public NativeMultiHashMap<int, Polygon> polygons;

        public NativeList<Vector3> vertices;
        public NativeList<int> triangleIndices;
        public NativeList<int> triangleLengths;

        public void Execute()
        {
            NativeHashMap<Vector3, int> vertexCache = new NativeHashMap<Vector3, int>(polygons.Length * 6, Allocator.Temp);

            int previousLength = 0;
            for (int i = 0; i < generateForFillTypes.Length; i++)
            {
                Execute(generateForFillTypes[i], vertexCache);

                int length = triangleIndices.Length - previousLength;
                triangleLengths.Add(length);
                previousLength = length;

                vertexCache.Clear();
            }

            vertexCache.Dispose();
        }

        private void Execute(FillType fillType, NativeHashMap<Vector3, int> vertexCache)
        {
            Polygon polygon;
            NativeMultiHashMapIterator<int> iterator;
            if (!polygons.TryGetFirstValue((int) fillType, out polygon, out iterator))
                return;

            AddPolygonData(polygon, vertexCache);

            while (polygons.TryGetNextValue(out polygon, ref iterator))
            {
                AddPolygonData(polygon, vertexCache);
            }
        }

        private void AddPolygonData(Polygon polygon, NativeHashMap<Vector3, int> vertexCache)
        {
            switch (polygon.type)
            {
                case PolygonType.OneCorner:
                    AddOneCorner(polygon, vertexCache);
                    break;
                case PolygonType.TwoCorners:
                    AddQuad(polygon, vertexCache);
                    break;
                case PolygonType.CrossCorners:
                    AddCrossCorners(polygon, vertexCache);
                    break;
                case PolygonType.ThreeCorners:
                    AddThreeCorners(polygon, vertexCache);
                    break;
                case PolygonType.AllCorners:
                    AddQuad(polygon, vertexCache);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddOneCorner(Polygon polygon, NativeHashMap<Vector3, int> vertexCache)
        {
            int index = AddVertex(new Vector3(polygon.one.x, polygon.one.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.two.x, polygon.two.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.three.x, polygon.three.y), vertexCache);
            triangleIndices.Add(index);
        }

        private void AddQuad(Polygon polygon, NativeHashMap<Vector3, int> vertexCache)
        {
            int one = AddVertex(new Vector3(polygon.one.x, polygon.one.y), vertexCache);
            int two = AddVertex(new Vector3(polygon.two.x, polygon.two.y), vertexCache);
            int three = AddVertex(new Vector3(polygon.three.x, polygon.three.y), vertexCache);
            int four = AddVertex(new Vector3(polygon.four.x, polygon.four.y), vertexCache);

            triangleIndices.Add(one);
            triangleIndices.Add(two);
            triangleIndices.Add(three);

            triangleIndices.Add(one);
            triangleIndices.Add(three);
            triangleIndices.Add(four);
        }

        private void AddCrossCorners(Polygon polygon, NativeHashMap<Vector3, int> vertexCache)
        {
            int index = AddVertex(new Vector3(polygon.one.x, polygon.one.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.two.x, polygon.two.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.three.x, polygon.three.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.four.x, polygon.four.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.five.x, polygon.five.y), vertexCache);
            triangleIndices.Add(index);

            index = AddVertex(new Vector3(polygon.six.x, polygon.six.y), vertexCache);
            triangleIndices.Add(index);
        }

        private void AddThreeCorners(Polygon polygon, NativeHashMap<Vector3, int> vertexCache)
        {
            int one = AddVertex(new Vector3(polygon.one.x, polygon.one.y), vertexCache);
            int two = AddVertex(new Vector3(polygon.two.x, polygon.two.y), vertexCache);
            int three = AddVertex(new Vector3(polygon.three.x, polygon.three.y), vertexCache);
            int four = AddVertex(new Vector3(polygon.four.x, polygon.four.y), vertexCache);
            int five = AddVertex(new Vector3(polygon.five.x, polygon.five.y), vertexCache);

            triangleIndices.Add(one);
            triangleIndices.Add(two);
            triangleIndices.Add(three);

            triangleIndices.Add(one);
            triangleIndices.Add(three);
            triangleIndices.Add(four);

            triangleIndices.Add(one);
            triangleIndices.Add(four);
            triangleIndices.Add(five);
        }

        private int AddVertex(Vector3 vertex, NativeHashMap<Vector3, int> vertexCache)
        {
            if (vertexCache.TryGetValue(vertex, out int index))
                return index;

            vertexCache.TryAdd(vertex, vertices.Length);
            vertices.Add(vertex);
            return vertices.Length - 1;
        }
    }
}
