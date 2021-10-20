using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Javid
{
    internal class Domain
    {
        private readonly float _a, _b;
        public Domain(float a, float b)
        {
            _a = a;
            _b = b;
        }
        public bool Includes(float value) => value >= _a && value <= _b;
        public Domain[] DivideDomain(int count)
        {
            var subLength = (_b - _a) / count;
            return Enumerable.Range(0, count).Select(i => new Domain(_a + (i * subLength), _a + (i + 1) * subLength)).ToArray();
        }
    }
    internal readonly struct Pair
    {
        private readonly int _a, _b;
        public Pair(int a, int b)
        {
            _a = a;
            _b = b;
        }
        public override int GetHashCode() => _a ^ _b;
        public override bool Equals(object obj)
        {
            if (obj is Pair p)
                return p._a == _a && p._b == _b || p._a == _b && p._b == _a;
            return false;
        }
    }
    internal static class Converter
    {
        public static Mesh ToMesh(this Bitmap bmp)
        {
            var width = bmp.Width - 1;
            var height = bmp.Height - 1;
            var mesh = Mesh.CreateFromPlane(Plane.WorldXY, new Interval(0, width), new Interval(0, height), width, height);
            var bmpMemory = new GH_MemoryBitmap(bmp);
            mesh.VertexColors.AppendColors(mesh.Vertices.Select(v => bmpMemory.Colour((int)v.X, height - (int)v.Y)).ToArray());
            bmpMemory.Release(false);
            return mesh;
        }
    }
}