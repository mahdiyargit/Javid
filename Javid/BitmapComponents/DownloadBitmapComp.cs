using System;
using System.Drawing;
using System.Net;
using Grasshopper.Kernel;
using Javid.Parameter;
using Javid.Properties;

namespace Javid.BitmapComponents
{
    public class DownloadBitmapComp : GH_Component
    {
        public DownloadBitmapComp() : base("Download Bitmap", "DownBmp", "Downloads an image from a URL", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "URL", "URL", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap object", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var url = string.Empty;
            if (!da.GetData(0, ref url)) return;
            try
            {
                Bitmap bmp;
                using (var webClient = new WebClient())
                {
                    using (var stream = webClient.OpenRead(url))
                    {
                        bmp = stream == null ? null : new Bitmap(stream);
                    }
                }
                if (bmp is null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bitmap is null");
                    return;
                }
                da.SetData(0, bmp);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
            }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Resources.download;
        public override Guid ComponentGuid => new Guid("B7FC16C7-405E-43F9-B2DD-518B39F60AAE");
    }
}