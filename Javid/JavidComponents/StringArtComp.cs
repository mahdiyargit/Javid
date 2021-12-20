using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;

namespace Javid.JavidComponents
{
    public class StringArtComp : GH_Component
    {
        private Bitmap _bmp;
        private Bitmap _oBmp;
        private Node2[] _pinNodes;
        private Point3d[] _pinPoints;
        private List<int> _pins;
        private HashSet<Pair> _pairs;
        private double[][] _distances;
        private List<Line> _lines;
        public StringArtComp() : base("String Art", "StrArt", "Recreates the bitmap with the string pattern through chosen pins.", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap object", GH_ParamAccess.item);
            pManager.AddPointParameter("Pins", "Pins", "Pin points to place on the bitmap", GH_ParamAccess.list);
            pManager.AddIntegerParameter("LineCount", "LineCount", "Total number of strings", GH_ParamAccess.item, 2000);
            pManager.AddNumberParameter("LineWidth", "LineWidth", "The width of the string", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("LineWeight", "LineWeight",
                "The weight a single thread has in terms of darkness (0-255)", GH_ParamAccess.item, 50);
            pManager.AddIntegerParameter("SkipLatestPins", "SkipLatest", "Number of the latest pins to ignore",
                GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("SkipNeighbors", "SkipNeighbors",
                "Ignore N nearest neighbors to this starting point", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("Method", "Method", "The method used to calculate darkness level(sum/average)", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Iterations", "Iterations",
                "This many internal iterations will be performed for each result’s output", GH_ParamAccess.item, 10);
            pManager.AddBooleanParameter("Run", "Run",
                "If true, this component will continue to iterate until reaching the given line count", GH_ParamAccess.item,
                true);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, false);
            var paramInteger = (Param_Integer)pManager[7];
            paramInteger.AddNamedValue("Average", 0);
            paramInteger.AddNamedValue("Sum", 1);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("PinsOrder", "Pins", "Order of the pins", GH_ParamAccess.item);
            pManager.AddLineParameter("Lines", "Lines", "Strings created the output bitmap", GH_ParamAccess.list);
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var lineCount = 2000;
            var lineWidth = 1.0;
            var lineWeight = 50;
            var skipLatest = 3;
            var skipNeighbors = 20;
            var method = 0;
            var iterations = 10;
            var run = true;
            var reset = false;
            if (!da.GetData(2, ref lineCount) ||
                !da.GetData(3, ref lineWidth) ||
                !da.GetData(4, ref lineWeight) ||
                !da.GetData(5, ref skipLatest) ||
                !da.GetData(6, ref skipNeighbors) ||
                !da.GetData(7, ref method) ||
                !da.GetData(8, ref iterations) ||
                !da.GetData(9, ref run) ||
                !da.GetData(10, ref reset)) return;
            if (reset || _bmp is null)
            {
                Bitmap bmp = null;
                var pts = new List<Point3d>();
                if (!da.GetData(0, ref bmp) ||
                    !da.GetDataList(1, pts)) return;
                bmp = new Bitmap(bmp);
                var bmpMemory = new GH_MemoryBitmap(bmp);
                bmpMemory.Filter_GreyScale();
                bmpMemory.Release(true);
                _pinPoints = Point3d.CullDuplicates(pts, 1.05).Where(p => bmp.ToRectangle3d().Contains(p) == PointContainment.Inside)
                    .ToArray();
                if (_pinPoints.Length == 0) return;
                if (_pinPoints.Length != pts.Count)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some points were outside the region or closer than 1.0 units to other points");
                var bbox = new BoundingBox(_pinPoints);
                var x = (int)bbox.Min.X;
                var y = (int)(bmp.Height - bbox.Max.Y);
                var width = (int)bbox.Max.X - x;
                var height = (int)(bbox.Max.Y - bbox.Min.Y);
                _bmp = bmp.Clone(new Rectangle(x, y, width, height), bmp.PixelFormat);
                _oBmp = new Bitmap(_bmp.Width, _bmp.Height);
                using (var graphics = Graphics.FromImage(_oBmp))
                {
                    graphics.Clear(Color.White);
                }
                _pinNodes = _pinPoints.Select(p => new Node2(p.X - x, bmp.Height - p.Y - y)).ToArray();
                _distances = new double[_pinNodes.Length][];
                for (var i = 0; i < _pinNodes.Length - 1; i++)
                {
                    _distances[i] = new double[_pinNodes.Length - i - 1];
                    for (var j = i + 1; j < _pinNodes.Length; j++)
                    {
                        _distances[i][j - i - 1] = _pinNodes[i].Distance(_pinNodes[j]);
                    }
                }
                _pins = new List<int> { 0 };
                _pairs = new HashSet<Pair>();
                _lines = new List<Line>();
            }
            if (_pinPoints.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No point inside the region");
                return;
            }
            if (run && _pins.Count < lineCount)
            {
                for (var k = 0; k < iterations; k++)
                {
                    var lastPin = _pins.Last();
                    var lastPins = _pins.Skip(_pins.Count - Math.Max(1, skipLatest));
                    var linesDarkness = new int[_pinNodes.Length];
                    var bmpMemory = new GH_MemoryBitmap(_bmp, WrapMode.Clamp);
                    try
                    {
                        Parallel.For(0, _pinNodes.Length, i =>
                        {
                            if (lastPins.Contains(i) ||
                                Math.Abs(lastPin - i) < skipNeighbors ||
                                _pairs.Contains(new Pair(lastPin, i))) return;
                            var points = DivideLine2(lastPin, i).ToArray();
                            if (!points.Any())
                                linesDarkness[i] = 0;
                            else if (method == 0)
                                linesDarkness[i] = (int)points.Average(p => bmpMemory.PixelValue(p));
                            else
                                linesDarkness[i] = points.Sum(p => bmpMemory.PixelValue(p));
                        });
                    }
                    finally
                    {
                        bmpMemory.Release(false);
                    }
                    var nextPin = Array.IndexOf(linesDarkness, linesDarkness.Max());
                    using (var graphics = Graphics.FromImage(_bmp))
                    {
                        var pen = new Pen(Color.FromArgb(lineWeight, Color.White), (float)lineWidth);
                        graphics.DrawLine(pen, (float)_pinNodes[lastPin].x, (float)_pinNodes[lastPin].y,
                            (float)_pinNodes[nextPin].x, (float)_pinNodes[nextPin].y);
                    }
                    using (var graphics = Graphics.FromImage(_oBmp))
                    {
                        var pen = new Pen(Color.FromArgb(lineWeight, Color.Black), (float)lineWidth);
                        graphics.DrawLine(pen, (float)_pinNodes[lastPin].x, (float)_pinNodes[lastPin].y,
                            (float)_pinNodes[nextPin].x, (float)_pinNodes[nextPin].y);
                    }
                    _pins.Add(nextPin);
                    _pairs.Add(new Pair(lastPin, nextPin));
                    _lines.Add(new Line(_pinPoints[lastPin], _pinPoints[nextPin]));
                }
                ExpireSolution(true);
            }
            da.SetDataList(0, _pins.Select(i => new GH_Integer(i)));
            da.SetDataList(1, _lines.Select(line => new GH_Line(line)));
            da.SetData(2, _oBmp);
        }
        private IEnumerable<Node2> DivideLine2(int start, int end)
        {
            var indices = new[] { start, end };
            Array.Sort(indices);
            var d = _distances[indices[0]][indices[1] - indices[0] - 1];
            return Enumerable.Range(1, (int)d - 1).Select(i =>
            {
                var f = i / d;
                return (1 - f) * _pinNodes[start] + f * _pinNodes[end];
            });
        }
        protected override Bitmap Icon => Resources.stringArt;
        public override Guid ComponentGuid => new Guid("21ACEC0B-B54C-47B7-BF77-6F9C2F998863");
    }
}