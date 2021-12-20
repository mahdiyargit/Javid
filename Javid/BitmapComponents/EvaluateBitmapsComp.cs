using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Javid.BitmapComponents
{
    public class EvaluateBitmapsComp : GH_Component
    {
        private WrapMode _wrapMode;
        private bool _interpolate;
        public EvaluateBitmapsComp() : base("Evaluate Bitmaps", "EvalBmps",
            "Evaluate bitmaps color at {uv} coordinates", "Javid", "Bitmap")
        {
            _wrapMode = WrapMode.Tile;
            _interpolate = false;
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmaps", "B", "Bitmaps", GH_ParamAccess.item);
            pManager.AddInterval2DParameter("Domain²", "I²", "Optional two-dimensional domain of {u} and {v}.\n" +
                                                             "If none provided, parametrized value will be used.\n" +
                                                             "You can also directly connect bitmap, this way image dimensions will be used.", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "uvs", "{uv} coordinates to evaluate", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Colors", "C", "Colors at {uv} coordinates.", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            var pts = new List<GH_Point>();
            if (!da.GetData(0, ref bmp) ||
                !da.GetDataList(2, pts)) return;
            var d = new GH_Interval2D(new UVInterval(new Interval(0.0, 1.0), new Interval(0.0, 1.0)));
            da.GetData(1, ref d);
            if (bmp is null) return;
            var uMin = d.Value.U.Min;
            var uStep = 1.0 / d.Value.U.Length;
            var vMin = d.Value.V.Min;
            var vStep = 1.0 / d.Value.V.Length;
            var w = bmp.Width - 1;
            var h = bmp.Height - 1;
            var colors = new List<GH_Colour>();
            var bmpMemory = new GH_MemoryBitmap(bmp, _wrapMode);
            foreach (var p in pts)
            {
                var x = (p.Value.X - uMin) * uStep * w;
                var y = h - ((p.Value.Y - vMin) * vStep) * h;
                var c = Color.Transparent;
                if (_interpolate)
                    bmpMemory.Sample(x, y, ref c);
                else
                    bmpMemory.Sample((int)x, (int)y, ref c);
                colors.Add(new GH_Colour(c));
            }
            bmpMemory.Release(false);
            da.SetDataList(0, colors);
        }
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Clamp", Menu_ClampClicked, Resources.Clamp, true, _wrapMode == WrapMode.Clamp);
            Menu_AppendItem(menu, "Tile", Menu_TileClicked, Resources.Tile, true, _wrapMode == WrapMode.Tile);
            Menu_AppendItem(menu, "Flip", Menu_FlipClicked, Resources.Flip, true, _wrapMode == WrapMode.TileFlipXY);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Interpolate", Menu_InterpolateClicked, true, _interpolate);
        }
        private void Menu_ClampClicked(object sender, EventArgs args)
        {
            if (_wrapMode == WrapMode.Clamp) return;
            RecordUndoEvent("Wrap Mode");
            _wrapMode = WrapMode.Clamp;
            ExpireSolution(true);
        }
        private void Menu_TileClicked(object sender, EventArgs args)
        {
            if (_wrapMode == WrapMode.Tile) return;
            RecordUndoEvent("Wrap Mode");
            _wrapMode = WrapMode.Tile;
            ExpireSolution(true);
        }
        private void Menu_FlipClicked(object sender, EventArgs args)
        {
            if (_wrapMode == WrapMode.TileFlipXY) return;
            RecordUndoEvent("Wrap Mode");
            _wrapMode = WrapMode.TileFlipXY;
            ExpireSolution(true);
        }
        private void Menu_InterpolateClicked(object sender, EventArgs args)
        {
            RecordUndoEvent("Interpolation");
            _interpolate = !_interpolate;
            ExpireSolution(true);
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override Bitmap Icon => Resources.evaluates;
        public override Guid ComponentGuid => new Guid("CCC2BDB9-4811-4E32-9ABB-AF4A74F7BA07");
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("Interpolate", _interpolate);
            switch (_wrapMode)
            {
                case WrapMode.Clamp:
                    writer.SetInt32("Wrap", 0);
                    break;
                case WrapMode.Tile:
                    writer.SetInt32("Wrap", 1);
                    break;
                case WrapMode.TileFlipXY:
                    writer.SetInt32("Wrap", 2);
                    break;
            }
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("Interpolate", ref _interpolate);
            if (reader.ItemExists("Wrap"))
            {
                switch (reader.GetInt32("Wrap"))
                {
                    case 0:
                        _wrapMode = WrapMode.Clamp;
                        break;
                    case 1:
                        _wrapMode = WrapMode.Tile;
                        break;
                    case 2:
                        _wrapMode = WrapMode.TileFlipXY;
                        break;
                }
            }
            return base.Read(reader);
        }
    }
}