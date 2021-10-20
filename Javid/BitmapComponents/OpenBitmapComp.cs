using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Javid.Parameter;
using System;
using System.Drawing;
using System.IO;

namespace Javid.BitmapComponents
{
    public class OpenBitmapComp : GH_Component
    {
        public OpenBitmapComp() : base("Open Bitmap", "Open", "Open a bitmap file and return a bitmap object", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var paramFilePath = new Param_FilePath
            {
                FileFilter = "All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                ExpireOnFileEvent = true
            };
            pManager.AddParameter(paramFilePath, "File", "F", "Location of image file", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap object", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var path = string.Empty;
            if (!da.GetData(0, ref path) || !File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image file does not exist.");
                return;
            }
            da.SetData(0, new Bitmap(path));
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("B3465481-73DB-40E3-8F80-9655F3CEC7DD");
    }
}