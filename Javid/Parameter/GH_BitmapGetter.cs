using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper;

namespace Javid.Parameter
{
    public sealed class GH_BitmapGetter
    {
        public static GH_Bitmap GetBitmap()
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = @"Select one existing file",
                Filter = @"All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                CheckFileExists = true
            };
            return openFileDialog.ShowDialog(Instances.DocumentEditor) != DialogResult.OK ? null : new GH_Bitmap(Converter.GH_BitmapFromFile(openFileDialog.FileName));
        }
        public static List<GH_Bitmap> GetBitmaps()
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = @"Select many existing file",
                Filter = @"All image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                CheckFileExists = true
            };
            return openFileDialog.ShowDialog(Instances.DocumentEditor) != DialogResult.OK ? null : openFileDialog.FileNames.Select(Converter.GH_BitmapFromFile).ToList();
        }
    }
}