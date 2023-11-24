using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Javid.Properties;

namespace Javid.BitmapComponents
{
    public class BitmapFromColorsComp : GH_Component
    {
        public BitmapFromColorsComp() : base("Bitmap From Colors", "BmpColors", "Create a bitmap from a list of colors", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Colors", "C", "Colors", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Width", "W", "The width, in pixels", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("Height", "H", "The Height, in pixels", GH_ParamAccess.item, 100);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap object", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var colors = new List<GH_Colour>();
            var width = 100;
            var height = 100;
            if (!da.GetDataList(0, colors) ||
                !da.GetData(1, ref width) ||
                !da.GetData(2, ref height)) return;
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpMemory = new GH_MemoryBitmap(bmp);
            Parallel.For(0, height, j =>
            {
                for (var i = 0; i < width; i++)
                    bmpMemory.Colour(i, (height - 1 - j), colors[(j * width + i) % colors.Count].Value);
            });
            bmpMemory.Release(true);
            da.SetData(0, bmp);
        }
        protected override Bitmap Icon => Resources.create;
        public override Guid ComponentGuid => new Guid("78CE40E6-95BF-43D4-9357-A1840B730C28");
    }
}