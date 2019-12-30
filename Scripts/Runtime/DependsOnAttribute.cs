using System;

namespace Thijs.Framework.MarchingSquares
{
    public class DependsOnAttribute : Attribute
    {
        public Type Type { get; private set; }

        public DependsOnAttribute(Type type)
        {
            Type = type;
        }
    }
}
