using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Javid.Parameter;
using Rhino.Geometry;
using System.ComponentModel;
using System.Drawing;
namespace Javid.Parameter
{
    public class GH_Bitmap : GH_Goo<Bitmap>
    {
        public GH_Bitmap() { }
        public GH_Bitmap(Bitmap bitmap) => m_value = bitmap;
        public GH_Bitmap(GH_Bitmap ghBitmap) => m_value = ghBitmap.m_value;
        public override IGH_GooProxy EmitProxy() => new GH_BitmapProxy(this);
        public override bool CastFrom(object source)
        {
            switch (source)
            {
                case null:
                    m_value = null;
                    return true;
                case Bitmap bitmap:
                    m_value = bitmap;
                    return true;
                case GH_Bitmap ghBitmap:
                    m_value = ghBitmap.m_value;
                    return true;
                default:
                    return false;
            }
        }
        public override bool CastTo<T>(ref T target)
        {
            if (typeof(T).IsAssignableFrom(typeof(GH_Rectangle)))
            {
                target = (T)(object)new GH_Rectangle(m_value.ToRectangle3d());
                return true;
            }
            if (typeof(T).IsAssignableFrom(typeof(GH_Mesh)))
            {
                target = (T)(object)new GH_Mesh(m_value.ToMesh());
                return true;
            }
            if (typeof(T).IsAssignableFrom(typeof(Bitmap)))
            {
                target = (T)(object)m_value;
                return true;
            }
            if (typeof(T).IsAssignableFrom(typeof(GH_Point)))
            {
                target = (T)(object)new GH_Point(new Point3d((m_value.Width - 1) * 0.5, (m_value.Height - 1) * 0.5, 0.0));
                return true;
            }
            if (typeof(T).IsAssignableFrom(typeof(GH_Plane)))
            {
                target = (T)(object)new GH_Plane(new Plane(new Point3d((m_value.Width - 1) * 0.5, (m_value.Height - 1) * 0.5, 0.0), Vector3d.ZAxis));
                return true;
            }
            if (typeof(T).IsAssignableFrom(typeof(GH_Surface)))
            {
                target = (T)(object)new GH_Surface(new PlaneSurface(Plane.WorldXY, new Interval(0, m_value.Width - 1), new Interval(0, m_value.Height - 1)));
                return true;
            }
            if (typeof(T).IsAssignableFrom(typeof(GH_Interval2D)))
            {
                target = (T)(object)new GH_Interval2D(m_value.ToUVInterval());
                return true;
            }
            return false;
        }
        public override bool Write(GH_IWriter writer)
        {
            if (m_value != null)
                writer.SetDrawingBitmap("Bitmap", m_value);
            return true;
        }
        public override bool Read(GH_IReader reader)
        {
            m_value = reader.GetDrawingBitmap("Bitmap");
            return true;
        }
        public override IGH_Goo Duplicate() => new GH_Bitmap(m_value);
        public override string ToString() => m_value == null ? "Null Bitmap" : $"Bitmap (w={m_value.Width}, h={m_value.Height})";
        public override bool IsValid => !(m_value is null);
        public override string TypeName => "Bitmap";
        public override string TypeDescription => "Bitmap";
    }
}
public class GH_BitmapProxy : GH_GooProxy<GH_Bitmap>
{
    public GH_BitmapProxy(GH_Bitmap owner) : base(owner) { }
    public override void Construct()
    {
        var bitmap = GH_BitmapGetter.GetBitmap();
        if (bitmap == null) return;
        Owner.Value = bitmap.Value;
    }
    [Description("Bitmap")]
    [Category("Properties")]
    public Bitmap Bitmap => Owner.Value;
}
