using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
            int previousLength = 0;
            for (int i = 0; i < generateForFillTypes.Length; i++)
            {
                Execute(generateForFillTypes[i]);

                int length = triangleIndices.Length - previousLength;
                triangleLengths.Add(length);
                previousLength = length;
            }
        }

        private void Execute(FillType fillType)
        {
            Polygon polygon;
            NativeMultiHashMapIterator<int> iterator;
            if (!polygons.TryGetFirstValue((int) fillType, out polygon, out iterator))
                return;

            AddPolygonData(polygon);

            while (polygons.TryGetNextValue(out polygon, ref iterator))
            {
                AddPolygonData(polygon);
            }
        }

        private void AddPolygonData(Polygon polygon)
        {
            switch (polygon.type)
            {
                case PolygonType.OneCorner:
                    AddOneCorner(polygon);
                    break;
                case PolygonType.TwoCorners:
                    AddQuad(polygon);
                    break;
                case PolygonType.CrossCorners:
                    AddCrossCorners(polygon);
                    break;
                case PolygonType.ThreeCorners:
                    AddThreeCorners(polygon);
                    break;
                case PolygonType.AllCorners:
                    AddQuad(polygon);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddOneCorner(Polygon polygon)
        {
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.one.x, polygon.one.y));
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.two.x, polygon.two.y));
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.three.x, polygon.three.y));
        }

        private void AddQuad(Polygon polygon)
        {
            vertices.Add(new Vector3(polygon.one.x, polygon.one.y));
            vertices.Add(new Vector3(polygon.two.x, polygon.two.y));
            vertices.Add(new Vector3(polygon.three.x, polygon.three.y));
            vertices.Add(new Vector3(polygon.four.x, polygon.four.y));

            triangleIndices.Add(vertices.Length - 4);
            triangleIndices.Add(vertices.Length - 3);
            triangleIndices.Add(vertices.Length - 2);

            triangleIndices.Add(vertices.Length - 4);
            triangleIndices.Add(vertices.Length - 2);
            triangleIndices.Add(vertices.Length - 1);
        }

        private void AddCrossCorners(Polygon polygon)
        {
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.one.x, polygon.one.y));
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.two.x, polygon.two.y));
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.three.x, polygon.three.y));

            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.four.x, polygon.four.y));
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.five.x, polygon.five.y));
            triangleIndices.Add(vertices.Length);
            vertices.Add(new Vector3(polygon.six.x, polygon.six.y));
        }

        private void AddThreeCorners(Polygon polygon)
        {
            vertices.Add(new Vector3(polygon.one.x, polygon.one.y));
            vertices.Add(new Vector3(polygon.two.x, polygon.two.y));
            vertices.Add(new Vector3(polygon.three.x, polygon.three.y));
            vertices.Add(new Vector3(polygon.four.x, polygon.four.y));
            vertices.Add(new Vector3(polygon.five.x, polygon.five.y));

            triangleIndices.Add(vertices.Length - 5);
            triangleIndices.Add(vertices.Length - 4);
            triangleIndices.Add(vertices.Length - 3);

            triangleIndices.Add(vertices.Length - 5);
            triangleIndices.Add(vertices.Length - 3);
            triangleIndices.Add(vertices.Length - 2);

            triangleIndices.Add(vertices.Length - 5);
            triangleIndices.Add(vertices.Length - 2);
            triangleIndices.Add(vertices.Length - 1);
        }
    }
}
