using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Javid.Parameter;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Javid.BitmapComponents
{
    public class OpenGifComp : GH_Component
    {
        public OpenGifComp() : base("Open GIF", "GIF", "Get GIF frame", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var paramFilePath = new Param_FilePath
            {
                FileFilter = "GIF (*.GIF) |*.GIF;",
                ExpireOnFileEvent = true
            };
            pManager.AddParameter(paramFilePath, "File", "F", "Location of GIF file", GH_ParamAccess.item);
            pManager.AddNumberParameter("Frames", "F", "Frame factors", GH_ParamAccess.item, 0.0);
            pManager.AddBooleanParameter("Normalized", "N", "If True, the frame factor is normalized (0.0 ~ 1.0)", GH_ParamAccess.item, true);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("FrameCount", "C", "Number of frames", GH_ParamAccess.item);
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var path = string.Empty;
            if (!da.GetData(0, ref path) || !File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Gif file does not exist.");
                return;
            }
            var frame = 0.0;
            var normalized = true;
            if (!da.GetData(1, ref frame) ||
                !da.GetData(2, ref normalized)) return;
            var gif = Image.FromFile(path);
            var dim = new FrameDimension(gif.FrameDimensionsList[0]);
            var count = gif.GetFrameCount(dim);
            frame = normalized ? frame * (count - 1) : frame;
            if (frame > count - 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Frame must be smaller than {count}");
                frame -= ((int)frame / count * count);
            }
            gif.SelectActiveFrame(dim, (int)frame);
            da.SetData(0, count);
            da.SetData(1, gif);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("D4D333EB-2372-4205-AE93-411C2088FDE5");
    }
}