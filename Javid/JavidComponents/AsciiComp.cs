using Grasshopper.Kernel;
using Javid.Parameter;
using System;
using System.Drawing;
using System.Linq;
namespace Javid.JavidComponents
{
    public class AsciiComp : GH_Component
    {
        public AsciiComp() : base("ASCII", "ASCII", "Recreates the bitmap with the ASCII characters.", "Javid", "Javid")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GH_BitmapParam(), "Bitmap", "Bitmap", "Bitmap object", GH_ParamAccess.item);
            pManager.AddTextParameter("Characters", "Chars", "ASCII characters to use ", GH_ParamAccess.item,
                "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ");
            pManager.AddIntegerParameter("Ex", "Ex", "Ex", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("Ey", "Ey", "Ey", GH_ParamAccess.item, 50);
            pManager.AddBooleanParameter("Invert", "Invert", "Invert", GH_ParamAccess.item, false);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ASCII", "ASCII", "ASCII", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess da)
        {
            Bitmap bmp = null;
            var characters = string.Empty;
            var ex = 100;
            var ey = 50;
            var invert = false;
            if (!da.GetData(0, ref bmp) ||
                !da.GetData(1, ref characters) ||
                !da.GetData(2, ref ex) ||
                !da.GetData(3, ref ey) ||
                !da.GetData(4, ref invert)) return;
            if (invert)
                characters = Reverse(characters);
            bmp = new Bitmap(bmp, new Size(ex, ey));
            var bmpMemory = new GH_MemoryBitmap(bmp);
            bmpMemory.Filter_GreyScale();
            var brights = Enumerable.Range(0, ex * ey).Select(i => bmpMemory.R(i - i / ex * ex, i / ex)).ToArray();
            bmpMemory.Release(false);
            var domains = new Domain(brights.Min(), brights.Max()).DivideDomain(characters.Length);
            var output = string.Empty;
            for (var i = 0; i < brights.Length; i++)
            {
                for (var j = 0; j < characters.Length; j++)
                {
                    if (!domains[j].Includes(brights[i])) continue;
                    output += characters[j];
                    break;
                }
            }
            da.SetDataList(0, Enumerable.Range(0, ey).Select(i => output.Substring(i * ex, ex)));
        }
        public static string Reverse(string s)
        {
            var charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("4D1E1ABD-911E-4BA9-BD6C-8C8A444AB6FF");
    }
}