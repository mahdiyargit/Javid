using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Display;
using Rhino.Geometry;

namespace Javid.BitmapComponents
{
    public class BillboardDisplayComp : GH_Component
    {
        private readonly List<Point3d> _points;
        private readonly List<DisplayBitmap> _bitmaps;
        private readonly List<double> _sizes;
        private readonly List<bool> _foregrounds;
        private readonly List<bool> _worlds;
        public BillboardDisplayComp() : base("Billboard Display", "Billboard", "Display a screen-oriented image.", "Javid", "Bitmap")
        {
            _points = new List<Point3d>();
            _bitmaps = new List<DisplayBitmap>();
            _sizes = new List<double>();
            _foregrounds = new List<bool>();
            _worlds = new List<bool>();
            ViewportFilter = string.Empty;
        }
        public override void AddedToDocument(GH_Document document)
        {
            if (Locked) RemoveHandlers();
            else AddHandlers();
        }
        private void AddHandlers()
        {
            RemoveHandlers();
            DisplayPipeline.DrawForeground += DisplayPipelineOnDrawForeground;
        }
        private void RemoveHandlers() => DisplayPipeline.DrawForeground -= DisplayPipelineOnDrawForeground;
        public override void RemovedFromDocument(GH_Document document) => RemoveHandlers();
        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            switch (context)
            {
                case GH_DocumentContext.Open:
                case GH_DocumentContext.Loaded:
                case GH_DocumentContext.Unlock:
                    if (Locked)
                    {
                        RemoveHandlers();
                        break;
                    }
                    AddHandlers();
                    break;
                case GH_DocumentContext.Close:
                case GH_DocumentContext.Unloaded:
                case GH_DocumentContext.Lock:
                    RemoveHandlers();
                    break;
            }
        }
        private void DisplayPipelineOnDrawForeground(object sender, DrawEventArgs args)
        {
            if (Locked || Hidden || !_foregrounds.Contains(true) 
                ||!string.IsNullOrEmpty(ViewportFilter) 
                && !ViewportFilter.Equals("*") 
                && !Regex.IsMatch(args.Viewport.Name, "^" + Regex.Escape(ViewportFilter).Replace("\\*", ".*") + "$")) return;
            for (var i = 0; i < _points.Count; i++)
            {
                if (!_foregrounds[i]) continue;
                args.Display.DrawSprite(_bitmaps[i], _points[i], (float)_sizes[i], _worlds[i]);
            }
        }
        
        protected override void BeforeSolveInstance()
        {
            _points.Clear();
            _bitmaps.Clear();
            _sizes.Clear();
            _foregrounds.Clear();
            _worlds.Clear();
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap to display", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "Point", "Bitmap location", GH_ParamAccess.item);
            pManager.AddNumberParameter("Size", "Size", "Outer size", GH_ParamAccess.item, 10.0);
            pManager.AddBooleanParameter("Foreground", "Foreground", "Set to true to put the image in the foreground.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("WorldSpace", "WorldSpace", "Set true for World Space,Set to false for Screen Space.", GH_ParamAccess.item, true);
            pManager.HideParameter(1);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            var point = Point3d.Unset;
            var size = 10.0;
            var foreground = false;
            var world = true;
            if (!da.GetData(0, ref bmp)
                || !da.GetData(1, ref point)
                || !da.GetData(2, ref size)
                || !da.GetData(3, ref foreground)
                || !da.GetData(4, ref world)) return;
            _bitmaps.Add(new DisplayBitmap(bmp));
            _points.Add(point);
            _sizes.Add(size);
            _foregrounds.Add(foreground);
            _worlds.Add(world);
        }
        public override BoundingBox ClippingBox => Locked ? BoundingBox.Empty : new BoundingBox(_points);
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (Locked || Hidden || !_foregrounds.Contains(false) 
                || !string.IsNullOrEmpty(ViewportFilter)
                && !ViewportFilter.Equals("*")
                && !Regex.IsMatch(args.Viewport.Name, "^" + Regex.Escape(ViewportFilter).Replace("\\*", ".*") + "$")) return;
            for (var i = 0; i < _points.Count; i++)
            {
                if (_foregrounds[i]) continue;
                args.Display.DrawSprite(_bitmaps[i], _points[i], (float)_sizes[i], _worlds[i]);
            }
        }
        public string ViewportFilter { get; set; }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("ViewportFilter", ViewportFilter);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            ViewportFilter = string.Empty;
            if (reader.ItemExists("ViewportFilter"))
                ViewportFilter = reader.GetString("ViewportFilter");
            Message = ViewportFilter;
            return base.Read(reader);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Viewport Filter");
            Menu_AppendTextItem(menu, ViewportFilter, ViewportFilterKeyDown, null, false);
        }
        private void ViewportFilterKeyDown(GH_MenuTextBox sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return || ViewportFilter == sender.Text)
                return;
            RecordUndoEvent("Filter: " + sender.Text);
            ViewportFilter = sender.Text;
            Message = sender.Text;
            Attributes.ExpireLayout();
            Instances.RedrawAll();
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override Bitmap Icon => Resources.billboardDisplay;
        public override Guid ComponentGuid => new Guid("86E7F274-3182-4491-871A-E5A683444BBD");
    }
}
