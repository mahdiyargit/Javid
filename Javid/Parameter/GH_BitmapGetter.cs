using Grasshopper;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Javid.Parameter
{
    public sealed class GH_BitmapGetter
    {
        public static GH_Bitmap GetBitmap()
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Select one existing file",
                Filter = "All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                CheckFileExists = true
            };
            return openFileDialog.ShowDialog(Instances.DocumentEditor) != DialogResult.OK ? null : new GH_Bitmap(new Bitmap(openFileDialog.FileName));
        }
        public static List<GH_Bitmap> GetBitmaps()
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select many existing file",
                Filter = "All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                CheckFileExists = true
            };
            return openFileDialog.ShowDialog(Instances.DocumentEditor) != DialogResult.OK ? null : openFileDialog.FileNames.Select(fileName => new GH_Bitmap(new Bitmap(fileName))).ToList();
        }
    }
}