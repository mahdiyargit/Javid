using Grasshopper.Kernel;
using Javid.Parameter;
using Rhino.Geometry;
using System;
using System.Drawing;
namespace Javid.BitmapComponents
{
    public class DisplayBitmapComp : GH_Component
    {
        private Mesh _mesh;
        public DisplayBitmapComp() : base("Display Bitmap", "BmpDis", "Preview bitmap in the viewport", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap to display", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void BeforeSolveInstance() => _mesh = null;
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            da.GetData(0, ref bmp);
            _mesh = bmp?.ToMesh();
        }
        public override bool IsPreviewCapable => true;
        public override BoundingBox ClippingBox => _mesh?.GetBoundingBox(true) ?? BoundingBox.Empty;
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (_mesh is null || Hidden || Locked) return;
            args.Display.DrawMeshFalseColors(_mesh);
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("D488CB4D-0565-474C-8E8A-7F039D827680");
    }
}