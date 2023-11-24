using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Grasshopper.Kernel;
using Javid.Parameter;
using Javid.Properties;
using Rhino;
using Rhino.Geometry;
using Matrix = System.Drawing.Drawing2D.Matrix;

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
            pManager.AddNumberParameter("Sx", "Sx", "Scale factor in the x direction", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Sy", "Sy", "Scale factor in the y direction", GH_ParamAccess.item, 1.0);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Cropped bitmap", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;  
            var rec = Rectangle3d.Unset;
            var sx = double.NaN;
            var sy = double.NaN;
            if (!da.GetData(0, ref bmp) ||
                !da.GetData(1, ref rec) ||
                !da.GetData(2, ref sx) ||
                !da.GetData(3, ref sy) ||
                bmp is null || !rec.IsValid) return;
            if (RhinoMath.EpsilonEquals(sx, 0.0, DocumentTolerance()) ||
                RhinoMath.EpsilonEquals(sy, 0.0, DocumentTolerance()))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cannot scale with factor zero");
                return;
            }
            if (!rec.Transform(Transform.PlanarProjection(Plane.WorldXY)) || !bmp.ToRectangle3d().BoundingBox.Contains(rec.BoundingBox))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rectangle must be inside Bitmap borders");
                return;
            }
            var rotated = bmp;
            var center = new PointF((float)rec.Center.X, bmp.Height - 1 - (float)rec.Center.Y);
            var angle = Vector3d.VectorAngle(rec.Plane.XAxis, Vector3d.XAxis, Vector3d.ZAxis);
            if (!RhinoMath.EpsilonEquals(angle, 0.0, DocumentAngleTolerance() * 0.1))
            {
                rotated = new Bitmap(bmp.Width, bmp.Height);
                rotated.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
                using (var g = Graphics.FromImage(rotated))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    var m = new Matrix();
                    m.RotateAt(-(float)RhinoMath.ToDegrees(angle), center);
                    g.Transform = m;
                    g.DrawImage(bmp, 0, 0);
                }
            }
            var cropped = new Bitmap(rotated.Clone(new RectangleF(center.X - (float)rec.Width / 2, center.Y - (float)rec.Height / 2,
                        (float)rec.Width + 1, (float)rec.Height + 1), bmp.PixelFormat));
            if (sx < 0.0)
            {
                cropped.RotateFlip(RotateFlipType.RotateNoneFlipX);
                sx = -sx;
            }
            if (sy < 0.0)
            {
                cropped.RotateFlip(RotateFlipType.RotateNoneFlipY);
                sy = -sy;
            }
            var scaled = cropped;
            if (!RhinoMath.EpsilonEquals(sx, 1.0, DocumentTolerance()) ||
                !RhinoMath.EpsilonEquals(sy, 1.0, DocumentTolerance()))
            {
                scaled = new Bitmap((int)(cropped.Width * sx), (int)(cropped.Height * sy));
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(cropped, 0, 0, scaled.Width, scaled.Height);
                }
            }
            da.SetData(0, scaled);
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override Bitmap Icon => Resources.crop;
        public override Guid ComponentGuid => new Guid("66CE12B9-7885-43DA-A721-2216C1F656D3");
    }
}