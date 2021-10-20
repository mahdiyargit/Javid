using Grasshopper;
using Grasshopper.Kernel;
using Javid.Parameter;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace Javid.BitmapComponents
{
    public class PreviewBitmapComp : GH_Component
    {
        public Bitmap DisplayImage;
        public PreviewBitmapComp() : base("Preview Bitmap", "Preview Bitmap", "Preview Bitmap", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "B", "Bitmap", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void BeforeSolveInstance() => DisplayImage = null;
        protected override void SolveInstance(IGH_DataAccess da)
        {
            da.GetData(0, ref DisplayImage);
        }
        public override void CreateAttributes() => m_attributes = new PreviewBitmapAttributes(this);
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "200%", Menu_Clicked);
            Menu_AppendItem(menu, "100%", Menu_Clicked);
            Menu_AppendItem(menu, "50%", Menu_Clicked);
            Menu_AppendItem(menu, "25%", Menu_Clicked);
            Menu_AppendItem(menu, "Save As", Save_Clicked);
        }
        private void Menu_Clicked(object sender, EventArgs e)
        {
            if (DisplayImage is null) return;
            double factor = 1;
            switch (((ToolStripMenuItem)sender).Text)
            {
                case "200%":
                    factor = 2;
                    break;
                case "100%":
                    factor = 1;
                    break;
                case "50%":
                    factor = 0.5;
                    break;
                case "25%":
                    factor = 0.25;
                    break;
            }
            Attributes.Bounds = new Rectangle(0, 0, (int)(DisplayImage.Width * factor + Params.InputWidth) + 4, (int)(DisplayImage.Height * factor));
            Attributes.Bounds.Inflate(6f, 6f);
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
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("0F2BCCEF-6974-4BDE-AEFA-704E2A450A0B");
    }
}
