using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;

namespace Javid.Parameter
{
    public class GH_BitmapParam : GH_PersistentParam<GH_Bitmap>
    {
        public GH_BitmapParam() : base(new GH_InstanceDescription("Bitmap", "Bitmap", "Contains a collection of Bitmap objects", "Params", "Primitive"))
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override GH_GetterResult Prompt_Singular(ref GH_Bitmap value)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Select one existing file",
                Filter = "All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog(Instances.DocumentEditor) != DialogResult.OK)
                return GH_GetterResult.cancel;
            value = new GH_Bitmap(new Bitmap(openFileDialog.FileName));
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_Bitmap> values)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select many existing file",
                Filter = "All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog(Instances.DocumentEditor) != DialogResult.OK)
                return GH_GetterResult.cancel;
            values = openFileDialog.FileNames.Select(fileName => new GH_Bitmap(new Bitmap(fileName))).ToList();
            return GH_GetterResult.success;
        }
        public override Guid ComponentGuid => new Guid("F0FE1ED5-6F96-4322-9555-108DE0763964");
    }
}
