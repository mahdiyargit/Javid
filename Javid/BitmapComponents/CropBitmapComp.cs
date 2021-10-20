using Grasshopper.Kernel;
using Javid.Parameter;
using Rhino.Geometry;
using System;
using System.Drawing;
namespace Javid.BitmapComponents
{
    public class CropBitmapComp : GH_Component
    {
        public CropBitmapComp() : base("Crop Bitmap", "CropBmp", "Crop bitmap", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Rectangle", "R", "Rectangle", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Corpped bitmap", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            var rec = Rectangle3d.Unset;
            if (!da.GetData(0, ref bmp) ||
                !da.GetData(1, ref rec))
                return;
            rec.Transform(Transform.PlanarProjection(Plane.WorldXY));
            var bbox = rec.BoundingBox;
            var x = (int)bbox.Min.X;
            var y = (int)(bmp.Height - bbox.Max.Y);
            var width = (int)bbox.Max.X - x;
            var height = (int)(bbox.Max.Y - bbox.Min.Y);
            da.SetData(0, new Bitmap(bmp.Clone(new Rectangle(x, y, width, height), bmp.PixelFormat)));
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("66CE12B9-7885-43DA-A721-2216C1F656D3");
    }
}