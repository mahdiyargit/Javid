using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Geometry;

namespace Javid.JavidComponents
{
    public class UndulantComp : GH_Component
    {
        public UndulantComp() : base("Undulant", "Undulant", "Undulant", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Ex", "Ex", "Ex", GH_ParamAccess.item, 75);
            pManager.AddIntegerParameter("Ey", "Ey", "Ey", GH_ParamAccess.item, 75);
            pManager.AddNumberParameter("Amplitude", "Amplitude", "Amplitude", GH_ParamAccess.item, 0.5);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Curves", "Curves", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            const int count = 30;
            const double step = 1.0 / count;
            Bitmap bmp = null;
            var ex = 75;
            var ey = 75;
            var amp = 0.5;
            if (!da.GetData(0, ref bmp) ||
                !da.GetData(1, ref ex) ||
                !da.GetData(2, ref ey) ||
                !da.GetData(3, ref amp)) return;
            double width = bmp.Width - 1;
            double height = bmp.Height - 1;
            var rec = new Rectangle3d(Plane.WorldXY, width, height);
            var yScale = height / (ey - 1);
            var polyLines = new Polyline[ey];
            for (var i = 0; i < ey; i++)
            {
                polyLines[i] = new Polyline(ex + 1);
                for (var j = 0; j <= ex; j++)
                    polyLines[i].Add(j * width / ex, i * yScale, 0.0);
            }
            var bmpMemory = new GH_MemoryBitmap(bmp);
            var b = new double[ey][];
            for (var i = 0; i < ey; i++)
            {
                b[i] = new double[ex];
                for (var j = 0; j < ex; j++)
                {
                    var pt = polyLines[i].SegmentAt(j).PointAt(0.5);
                    b[i][j] = 1 - bmpMemory.Colour((int)pt.X, (int)(height - pt.Y)).GetBrightness();
                }
            }
            bmpMemory.Release(false);
            var bflat = b.SelectMany(a => a);
            var min = bflat.Min();
            var max = bflat.Max();
            var waveLength = new double[ey][];
            for (var i = 0; i < ey; i++)
            {
                waveLength[i] = new double[ex];
                for (var j = 0; j < ex; j++)
                {
                    b[i][j] = (b[i][j] - min) / (max - min);
                    waveLength[i][j] = Math.Round(b[i][j] * 7) * 2 * Math.PI;
                }
            }
            var pls = new GH_Curve[ey];
            for (var i = 0; i < ey; i++)
            {
                var pts = new List<Point3d>();
                for (var j = 0; j < ex; j++)
                {
                    var line = polyLines[i].SegmentAt(j);
                    pts.AddRange(Enumerable.Range(0, count).Select(n =>
                    {
                        var t = n * step;
                        var pt = line.PointAt(t);
                        pt.Y += Math.Sin(waveLength[i][j] * t) * amp * yScale;
                        return pt;
                    }));
                }
                pls[i] = new GH_Curve(new Polyline(pts).ToNurbsCurve());
            }
            da.SetDataList(0, pls);
        }
        protected override Bitmap Icon => Resources.undulant;
        public override Guid ComponentGuid => new Guid("B7CB593F-05B4-4FB8-9A10-7204298FCB98");
    }
}