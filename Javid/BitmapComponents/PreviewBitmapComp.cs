using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Javid.Parameter;
using Javid.Properties;

namespace Javid.BitmapComponents
{
    public class PreviewBitmapComp : GH_Component
    {
        public Bitmap DisplayImage;
        public bool LockAspect;
        public PreviewBitmapComp() : base("Preview Bitmap", "Preview Bitmap", "Preview Bitmap", "Javid", "Bitmap")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager) => pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap", GH_ParamAccess.item);
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void BeforeSolveInstance() => DisplayImage = null;
        protected override void SolveInstance(IGH_DataAccess da) => da.GetData(0, ref DisplayImage);
        public override void CreateAttributes() => m_attributes = new PreviewBitmapAttributes(this);
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Maintain Aspect Ratio", Lock_Clicked,
                LockAspect ? Resources.locked : Resources.unlocked, DisplayImage != null, LockAspect);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "200%", Menu_Clicked, Resources.zoomIn);
            Menu_AppendItem(menu, "100%", Menu_Clicked, Resources.zoom);
            Menu_AppendItem(menu, "50%", Menu_Clicked, Resources.zoomout);
            Menu_AppendItem(menu, "25%", Menu_Clicked, Resources.zoomout);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Save As", Save_Clicked, Resources.save);
        }
        private void Lock_Clicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Maintain Aspect Ratio");
            LockAspect = !LockAspect;
            if (!LockAspect) return;
            Attributes.ExpireLayout();
            Instances.RedrawCanvas();
        }
        private void Menu_Clicked(object sender, EventArgs e)
        {
            if (DisplayImage is null) return;
            var factor = 1f;
            switch (((ToolStripMenuItem)sender).Text)
            {
                case "200%":
                    factor = 2f;
                    break;
                case "100%":
                    factor = 1f;
                    break;
                case "50%":
                    factor = 0.5f;
                    break;
                case "25%":
                    factor = 0.25f;
                    break;
            }
            Attributes.Bounds = new RectangleF(0, 0, (DisplayImage.Width * factor) + Params.InputWidth + 4f, DisplayImage.Height * factor);
            Attributes.ExpireLayout();
            Instances.RedrawCanvas();
        }
        public void Save_Clicked(object sender, EventArgs e)
        {
            if (DisplayImage is null) return;
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save As",
                Filter = "BMP (*.BMP) | *.BMP | JPEG (*.JPEG) | *.JPEG | PNG (*.PNG) | *.PNG | TIFF (*.TIFF) | *.TIFF"
            };
            var num = (int)saveFileDialog.ShowDialog();
            if (saveFileDialog.FileName == "") return;
            var fileStream = saveFileDialog.OpenFile();
            switch (saveFileDialog.FilterIndex)
            {
                case 1:
                    DisplayImage.Save(fileStream, ImageFormat.Bmp);
                    break;
                case 2:
                    DisplayImage.Save(fileStream, ImageFormat.Jpeg);
                    break;
                case 3:
                    DisplayImage.Save(fileStream, ImageFormat.Png);
                    break;
                case 4:
                    DisplayImage.Save(fileStream, ImageFormat.Tiff);
                    break;
            }
            fileStream.Close();
            ExpireSolution(true);
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override Bitmap Icon => Resources.preview;
        public override Guid ComponentGuid => new Guid("0F2BCCEF-6974-4BDE-AEFA-704E2A450A0B");
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("LockAspect", LockAspect);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("LockAspect", ref LockAspect);
            return base.Read(reader);
        }
    }
}
