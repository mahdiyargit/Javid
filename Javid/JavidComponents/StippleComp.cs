using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Geometry.Delaunay;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Geometry;

namespace Javid
{
    public class StippleComp : GH_Component
    {
        private Bitmap _bmp;
        private Node2List _boundary;
        private Node2List _nodes;
        public StippleComp() : base("Stipple", "Stipple", "Stipple", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap object", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Iterations", "Iterations",
                "This many internal iterations will be performed for each result’s output", GH_ParamAccess.item, 10);
            pManager.AddBooleanParameter("Run", "Run",
                "If true, this component will continue to iterate until reaching the given line count", GH_ParamAccess.item,
                true);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, false);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Pts", "Pts", "Pts", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var iterations = 10;
            var run = true;
            var reset = false;
            if (!da.GetData(2, ref iterations) ||
                !da.GetData(3, ref run) ||
                !da.GetData(4, ref reset)) return;
            if (reset || _nodes is null)
            {
                _bmp = null;
                da.GetData(0, ref _bmp);
                var rec = _bmp.ToRectangle3d();
                _boundary = new Node2List { Capacity = 4 };
                for (var i = 0; i < 4; i++)
                {
                    var corner = rec.Corner(i);
                    _boundary.Append(new Node2(corner.X, corner.Y));
                }
                var pts = new List<Point3d>();
                da.GetDataList(1, pts);
                _nodes = new Node2List(pts);
            }
            if (run)
            {
                for (var _ = 0; _ < iterations; _++)
                {
                    var connectivity =
                        Solver.Solve_Connectivity(_nodes, 0.001, true);
                    var cells = Grasshopper.Kernel.Geometry.Voronoi.Solver.Solve_Connectivity(_nodes, connectivity,
                        _boundary);
                    var bmpMemory = new GH_MemoryBitmap(_bmp);
                    var h = _bmp.Height - 1;
                    Parallel.For(0, cells.Count, i =>
                    {
                        var edges = cells[i].Edges();
                        var totalNode = new Node2(0.0, 0.0);
                        var totalDarkness = 0.0;
                        foreach (var edge in edges)
                        {
                            var midPt = edge.PointAt(0.5);
                            var darkness = 1 - bmpMemory.Colour((int)midPt.x, h - (int)midPt.y).GetBrightness();
                            totalNode += midPt * darkness;
                            totalDarkness += darkness;
                        }
                        _nodes[i] = totalNode * (1.0 / totalDarkness);
                    });
                    bmpMemory.Release(false);
                }
                ExpireSolution(true);
            }
            da.SetDataList(0, _nodes.Select(node => new Point3d(node.x, node.y, 0.0)));
        }
        protected override Bitmap Icon => Resources.stipple;
        public override Guid ComponentGuid => new Guid("f5415a0e-dde8-4501-b29c-4060a8dd4393");
    }
}