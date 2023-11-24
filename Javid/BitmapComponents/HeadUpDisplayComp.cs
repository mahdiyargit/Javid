using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types.Transforms;
using Javid.Parameter;
using Javid.Properties;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace Javid.BitmapComponents
{
    public class HeadUpDisplayComp : GH_Component
    {
        private readonly List<Point3d> _points;
        private readonly List<DisplayBitmap> _bitmaps;
        public HeadUpDisplayComp() : base("Head-Up Display", "HUD", "Display an image in the viewport.", "Javid", "Bitmap")
        {
            _points = new List<Point3d>();
            _bitmaps = new List<DisplayBitmap>();
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
            if (Locked || Hidden ||!string.IsNullOrEmpty(ViewportFilter) 
                && !ViewportFilter.Equals("*") 
                && !Regex.IsMatch(args.Viewport.Name, "^" + Regex.Escape(ViewportFilter).Replace("\\*", ".*") + "$")) return;
            for (var i = 0; i < _points.Count; i++)
                args.Display.DrawBitmap(_bitmaps[i], (int)_points[i].X, (int)_points[i].Y);
        }
        
        protected override void BeforeSolveInstance()
        {
            _points.Clear();
            _bitmaps.Clear();
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap to display", GH_ParamAccess.item);
            pManager.AddRectangleParameter("ImageBounds", "ImgBnd", "Bitmap drawing boundary", GH_ParamAccess.item);
            pManager.AddRectangleParameter("ViewBounds", "VBnd", "View boundary. Use the View Boundary component to get the active or any view boundary.", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            var bounds = Rectangle3d.Unset;
            var view = Rectangle3d.Unset;
            if (!da.GetData(0, ref bmp) ||
                !da.GetData(1, ref bounds) ||
                !da.GetData(2, ref view)) return;
            if (RhinoMath.EpsilonEquals(bounds.Width, 0.0, DocumentTolerance()) ||
                RhinoMath.EpsilonEquals(bounds.Height, 0.0, DocumentTolerance()))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rectangle is degenerate (flat) is one or more directions.");
                return;
            }
            if (!bounds.Transform(Transform.PlanarProjection(Plane.WorldXY)) || !view.BoundingBox.Contains(bounds.BoundingBox))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bounds must be inside the view boundary.");
                return;
            }

            var bb = bounds.BoundingBox;
            bounds = new Rectangle3d(Plane.WorldXY, bb.Min, bb.Max);
            var sx = bounds.Width / bmp.Width;
            var sy = bounds.Height / bmp.Height;
            var scaled = bmp;
            if (!RhinoMath.EpsilonEquals(sx, 1.0, DocumentTolerance()) ||
                !RhinoMath.EpsilonEquals(sy, 1.0, DocumentTolerance()))
            {
                scaled = new Bitmap((int)(bmp.Width * sx), (int)(bmp.Height * sy));
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, 0, 0, scaled.Width, scaled.Height);
                }
            }
            var upperLeft = bounds.Corner(3);
            upperLeft.Y = view.Height - upperLeft.Y;
            _points.Add(upperLeft);
            _bitmaps.Add(new DisplayBitmap(scaled));
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
        protected override Bitmap Icon => Resources.hud;
        public override Guid ComponentGuid => new Guid("3A163DF5-F11B-4A5B-80F6-3B8B1668B8D0");
    }
}
