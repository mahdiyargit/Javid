using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using Grasshopper.Kernel;
using Javid.Properties;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace Javid.BitmapComponents
{
    public class ViewBoundsComp : GH_Component
    {
        private bool _autoUpdate;
        private bool _expired;
        private RhinoView _view;
        public ViewBoundsComp() : base("View Bounds", "View Bnd", "Gets the size of the active/custom view, in pixels.", "Javid", "Bitmap")
        {
            _autoUpdate = false;
            _expired = true;
            _view = RhinoDoc.ActiveDoc.Views.ActiveView;
        }
        public override void AddedToDocument(GH_Document document)
        {
            if (Locked) RemoveHandlers();
            else AddHandlers();
        }
        private void AddHandlers()
        {
            RemoveHandlers();
            RhinoView.Modified += RhinoViewOnModified;
        }
        public override void ExpireSolution(bool recompute)
        {
            _expired = true;
            base.ExpireSolution(recompute);
        }
        private void RhinoViewOnModified(object sender, ViewEventArgs e)
        {
            if (!_autoUpdate || _expired || !Equals(e.View, _view)) return;
            ExpireSolution(true);
        }
        private void RemoveHandlers() => RhinoView.Modified -= RhinoViewOnModified;
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
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View", "View", "Viewport name. The active viewport will be used if left blank.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("AutUpdate", "AU", "If true, the component will be updated when the viewport changes.", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) => 
            pManager.AddRectangleParameter("ViewBounds", "VBnd", "The viewport boundary dimensions.", GH_ParamAccess.item);
        protected override void SolveInstance(IGH_DataAccess da)
        {
            if (!da.GetData(1, ref _autoUpdate)) return;
            var name = string.Empty;
            _view = da.GetData(0, ref name) ? RhinoDoc.ActiveDoc.Views.Find(name, false) : RhinoDoc.ActiveDoc.Views.ActiveView;
            if (_view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Specified viewport not found.");
                return;
            }
            _expired = false;
            da.SetData(0, _view.Bounds);
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override Bitmap Icon => Resources.viewBounds;
        public override Guid ComponentGuid => new Guid("9855202F-66C4-438C-8EF3-F069517D2623");
    }
}