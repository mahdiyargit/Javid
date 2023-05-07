using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Rhino.Geometry;
using Plane = Rhino.Geometry.Plane;

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
            return Enumerable.Range(0, count).Select(i => new Domain(_a + (i * subLength), _a + (i + 1) * subLength))
                .ToArray();
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
            var mesh = Mesh.CreateFromPlane(Plane.WorldXY, new Interval(0, width), new Interval(0, height), width,
                height);
            var bmpMemory = new GH_MemoryBitmap(bmp);
            mesh.VertexColors.AppendColors(mesh.Vertices.Select(v => bmpMemory.Colour((int)v.X, height - (int)v.Y))
                .ToArray());
            bmpMemory.Release(false);
            return mesh;
        }

        public static UVInterval ToUVInterval(this Bitmap bmp) =>
            new UVInterval(new Interval(0, bmp.Width - 1), new Interval(0, bmp.Height - 1));

        public static Rectangle3d ToRectangle3d(this Bitmap bmp) => new Rectangle3d(Plane.WorldXY,
            new Interval(0, bmp.Width - 1), new Interval(0, bmp.Height - 1));

        public static Point3d ToPoint3d(this Color c) => new Point3d(c.R, c.G, c.B);

        public static int PixelValue(this GH_MemoryBitmap bmpMemory, Node2 p)
        {
            var c = Color.Transparent;
            bmpMemory.Sample((int)p.x, (int)p.y, ref c);
            return 255 - c.R;
        }
        public static GH_Bitmap GH_BitmapFromFile(string path)
        {
            using (var img = Image.FromFile(path))
                return new GH_Bitmap(new Bitmap(img));
        }
    }
}