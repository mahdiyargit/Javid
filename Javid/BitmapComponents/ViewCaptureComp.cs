using System;
using System.Drawing;
using Grasshopper.Kernel;
using Javid.Parameter;
using Javid.Properties;
using Rhino;
using Rhino.Display;

namespace Javid.BitmapComponents
{
    public class ViewCaptureComp : GH_Component
    {
        public ViewCaptureComp() : base("View Capture", "View Capture", "Generate high resolution output of a RhinoViewport.", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Viewport", "V", "Optional named view or viewport name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Width", "W", "Optional width", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Height", "H", "Optional height", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grid", "G", "Grid", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Axes", "A", "CPlane axes", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Transparent", "T", "Transparent", GH_ParamAccess.item, true);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap object", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var viewCapture = new ViewCapture();
            var name = string.Empty;
            var view = da.GetData(0, ref name) ? RhinoDoc.ActiveDoc.Views.Find(name, false) : RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Specified Viewport not found.");
                return;
            }
            var width = 0;
            viewCapture.Width = da.GetData(1, ref width) ? width : view.ActiveViewport.Size.Width;
            var height = 0;
            viewCapture.Height = da.GetData(2, ref height) ? height : view.ActiveViewport.Size.Height;
            var grid = false;
            da.GetData(3, ref grid);
            viewCapture.DrawGrid = grid;
            var axes = false;
            da.GetData(4, ref axes);
            viewCapture.DrawGridAxes = axes;
            var transparent = true;
            da.GetData(5, ref transparent);
            viewCapture.TransparentBackground = transparent;
            da.SetData(0, viewCapture.CaptureToBitmap(view));
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Resources.viewCapture;
        public override Guid ComponentGuid => new Guid("C757EFA8-C0E8-4122-8E2D-671A4DE1F6B5");
    }
}