using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Javid.Properties;

namespace Javid.Parameter
{
    public class GH_BitmapParam : GH_PersistentParam<GH_Bitmap>
    {
        public GH_BitmapParam() : base(new GH_InstanceDescription("Bitmap", "Bmp", "Contains a collection of Bitmap objects", "Params", "Primitive"))
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override GH_GetterResult Prompt_Singular(ref GH_Bitmap value) => GH_GetterResult.cancel;
        protected override GH_GetterResult Prompt_Plural(ref List<GH_Bitmap> values) => GH_GetterResult.cancel;
        protected override ToolStripMenuItem Menu_CustomSingleValueItem() => new ToolStripMenuItem("Select one Bitmap", null, Menu_SingleExistingItemClicked);
        protected override ToolStripMenuItem Menu_CustomMultiValueItem() => new ToolStripMenuItem("Set Multiple Bitmaps", null, Menu_MultipleItemClicked);
        private void Menu_SingleExistingItemClicked(object sender, EventArgs e)
        {
            var mesh = GH_BitmapGetter.GetBitmap();
            if (mesh is null) return;
            RecordPersistentDataEvent("Select one existing file");
            PersistentData.Clear();
            PersistentData.Append(mesh);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
            ExpireSolution(true);
        }
        private void Menu_MultipleItemClicked(object sender, EventArgs e)
        {
            var meshes = GH_BitmapGetter.GetBitmaps();
            if (meshes is null || !meshes.Any()) return;
            RecordPersistentDataEvent("Select many existing files");
            PersistentData.Clear();
            PersistentData.AppendRange(meshes);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
            ExpireSolution(true);
        }
        protected override Bitmap Icon => Resources.bmp;
        public override Guid ComponentGuid => new Guid("F0FE1ED5-6F96-4322-9555-108DE0763964");
    }
}
