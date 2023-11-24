using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Geometry;

namespace Javid.JavidComponents
{
    public class DifferentialGrowthComp : GH_Component
    {
        private List<Point3d> _pts;
        private Bitmap _bmp;
        public DifferentialGrowthComp() : base("Differential Growth", "DiffGrow", "Description", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "Curve", "Curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Boundary", "Boundary", "Optional boundary curve", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Start", "Start", "Number of starting points", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("End", "End", "Maximum number of points", GH_ParamAccess.item, 50);
            pManager.AddNumberParameter("Min", "Min", "Smallest circle radius", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Max", "Max", "Largest circle radius", GH_ParamAccess.item, 2.0);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) => pManager.AddCircleParameter("Circles", "Circles", "Circles", GH_ParamAccess.list);

        protected override void SolveInstance(IGH_DataAccess da)
        {

            Curve crv = null;
            Curve boundary = null;
            var start = 20;
            var end = 50;
            var min = 1.0;
            var max = 2.0;
            var run = true;
            var reset = false;
            if (!da.GetData(1, ref crv) ||
                !da.GetData(3, ref start) ||
                !da.GetData(4, ref end) ||
                !da.GetData(5, ref min) ||
                !da.GetData(6, ref max) ||
                !da.GetData(7, ref run) ||
                !da.GetData(8, ref reset)) return;
            var isBoundary = da.GetData(2, ref boundary);
            if (reset || _bmp is null)
            {
                Bitmap bmp = null;
                if (!da.GetData(0, ref bmp)) return;
                bmp = new Bitmap(bmp);
                var memory = new GH_MemoryBitmap(bmp);
                memory.Filter_GreyScale();
                memory.Release(true);
                crv.DivideByCount(start, true, out var pts);
                _pts = pts.ToList();
                if (isBoundary)
                    _pts = Point3d.CullDuplicates(pts, 1.05).Where(p => boundary.Contains(p, Plane.WorldXY, 0.1) == PointContainment.Inside)
                        .ToList();
                else
                    _pts = Point3d.CullDuplicates(pts, 1.05).Where(p => bmp.ToRectangle3d().Contains(p) == PointContainment.Inside)
                            .ToList();
                if (_pts.Count == 0) return;
                if (_pts.Count != pts.Length)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some points were outside the region or closer than 1.0 units to other points");
                _bmp = bmp;
            }
            var counts = new int[_pts.Count];
            var vectors = new Vector3d[_pts.Count];
            var rads = new double[_pts.Count];
            var bmpMemory = new GH_MemoryBitmap(_bmp, WrapMode.Clamp);
            var h = bmpMemory.Height;
            try
            {
                Parallel.For(0, _pts.Count, i =>
                {
                    rads[i] = bmpMemory.Colour((int)_pts[i].X, h - 1 - (int)_pts[i].Y).GetBrightness() * (max - min) + min;
                });
            }
            finally
            {
                bmpMemory.Release(false);
            }
            var rtree = RTree.CreateFromPointArray(_pts);
            var neighbors = new List<int>[_pts.Count];
            Parallel.For(0, _pts.Count, i =>
            {
                neighbors[i] = new List<int>();
                rtree.Search(new Sphere(_pts[i], rads[i] + max), (sender, args) =>
                {
                    if (args.Id > i) neighbors[i].Add(args.Id);
                });
            });
            Parallel.For(0, _pts.Count, i =>
            {
                foreach (var j in neighbors[i])
                {
                    var vector = _pts[i] - _pts[j];
                    var d = vector.SquareLength;
                    if (d >= Math.Pow(rads[i] + rads[j], 2)) continue;
                    counts[i]++;
                    counts[j]++;
                    vector.Unitize();
                    vector *= (rads[i] + rads[j] - Math.Sqrt(d)) * 0.5;
                    vectors[i] += vector;
                    vectors[j] -= vector;
                }
            });
            if (isBoundary)
            {
                Parallel.For(0, _pts.Count, i =>
                {
                    if (counts[i] == 0) return;
                    vectors[i] /= counts[i];
                    if (boundary.ClosestPoint(_pts[i], out var t, rads[i]))
                    {
                        var cp = boundary.PointAt(t);
                        vectors[i].Transform(Transform.PlanarProjection(new Plane(cp, _pts[i] - cp)));
                    }

                    _pts[i] += vectors[i];
                });
            }
            else
            {
                var rec = _bmp.ToRectangle3d();
                Parallel.For(0, _pts.Count, i =>
                {
                    if (counts[i] == 0) return;
                    vectors[i] /= counts[i];
                    if (Math.Min(_pts[i].X, rec.Width - _pts[i].X) < rads[i]) vectors[i].X = 0;
                    if (Math.Min(_pts[i].Y, rec.Height - _pts[i].Y) < rads[i]) vectors[i].Y = 0;
                    _pts[i] += vectors[i];
                });
            }

            da.SetDataList(0, _pts.Select((t, i) => new GH_Circle(new Circle(t, rads[i]))));
            var newPts = new List<Point3d>();
            var newIndices = new List<int>();
            if (_pts.Count < end)
            {
                for (var i = 0; i < _pts.Count - 1; i++)
                {
                    var d = _pts[i].DistanceToSquared(_pts[i + 1]);
                    if (d < Math.Pow(rads[i] + rads[i + 1], 2)) continue;
                    newPts.Add((_pts[i] + _pts[i + 1]) * 0.5);
                    newIndices.Add(i + 1 + newIndices.Count);
                }

                for (var i = 0; i < newPts.Count; i++)
                    _pts.Insert(newIndices[i], newPts[i]);
            }
            if (run)
                ExpireSolution(true);
        }
        protected override Bitmap Icon => Resources.differential;
        public override Guid ComponentGuid => new Guid("7aa0d24c-8c00-41cc-8e4e-4634f4e9678c");
    }
}
