using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Javid.Parameter;
using Javid.Properties;
using Rhino.Collections;
using Rhino.Geometry;

namespace Javid.JavidComponents
{
    public class PixelArt : GH_Component
    {
        public PixelArt() : base("PixelArt", "PixelArt", "PixelArt", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap object", GH_ParamAccess.item);
            pManager.AddColourParameter("Palette", "Palette", "Color Palette", GH_ParamAccess.list,
                new List<Color> { Color.Blue, Color.Green, Color.Red, Color.Orange, Color.Yellow, Color.White });
            pManager.AddIntegerParameter("Ex", "Ex", "Ex", GH_ParamAccess.item, 20);
            pManager.AddNumberParameter("Size", "Size", "Optional mesh face size", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            var colors = new List<Color>();
            var ex = 20;
            if (!da.GetData(0, ref bmp) ||
                !da.GetDataList(1, colors) ||
                !da.GetData(2, ref ex) ||
                bmp is null || colors.Count < 1 || ex < 1) return;
            var palette = new Point3dList(colors.Count);
            for (var i = 0; i < colors.Count; i++)
                palette.Add(colors[i].ToPoint3d());
            var ey = (int)(ex * ((float)bmp.Height / bmp.Width));
            bmp = new Bitmap(bmp, new Size(ex, ey));
            var bmpMemory = new GH_MemoryBitmap(bmp);
            var closestIndices = Enumerable.Range(0, ex * ey)
                .Select(i => palette.ClosestIndex(bmpMemory.Colour(i - i / ex * ex, i / ex).ToPoint3d())).ToArray();
            Enumerable.Range(0, ex * ey).ToList().ForEach(i =>
                bmpMemory.Colour(i - i / ex * ex, i / ex, colors[closestIndices[i]]));
            bmpMemory.Release(true);
            da.SetData(0, bmp);
            var size = double.NaN;
            if (!da.GetData(3, ref size) || size == 0.0 || double.IsNaN(size)) return;
            var mesh = Mesh.CreateFromPlane(Plane.WorldXY, new Interval(0, ex * size), new Interval(ey * size, 0), ex, ey);
            mesh.VertexColors.CreateMonotoneMesh(Color.White);
            mesh.Unweld(0.0, false);
            Parallel.For(0, mesh.Faces.Count, i =>
            {
                mesh.VertexColors.SetColor(mesh.Faces[i], colors[closestIndices[i]]);
            });
            da.SetData(1, mesh);
        }
        protected override Bitmap Icon => Resources.pixelart;
        public override Guid ComponentGuid => new Guid("F4B7695F-0846-4D2B-BF60-2739DE98B2C2");
    }
}