using System.Drawing;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Javid.Parameter
{
    public class GH_Bitmap : GH_Goo<Bitmap>
    {
        public GH_Bitmap() { }
        public GH_Bitmap(Bitmap bitmap) => m_value = bitmap;
        public GH_Bitmap(GH_Bitmap ghBitmap) => m_value = ghBitmap.m_value;
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
        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Rectangle)))
            {
                target = (Q)(object)new GH_Rectangle(new Rectangle3d(Plane.WorldXY, m_value.Width - 1, m_value.Height - 1));
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
            {
                target = (Q)(object)new GH_Mesh(m_value.ToMesh());
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(Bitmap)))
            {
                target = (Q)(object)m_value;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
            {
                target = (Q)(object)new GH_Point(new Point3d((m_value.Width - 1) * 0.5, (m_value.Height - 1) * 0.5, 0.0));
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
            {
                target = (Q)(object)new GH_Plane(new Plane(new Point3d((m_value.Width - 1) * 0.5, (m_value.Height - 1) * 0.5, 0.0), Vector3d.ZAxis));
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
            {
                target = (Q)(object)new GH_Surface(new PlaneSurface(Plane.WorldXY, new Interval(0, m_value.Width - 1), new Interval(0, m_value.Height - 1)));
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
        public override string ToString() => $"Bitmap (w={m_value.Width}, h={m_value.Height})";
        public override bool IsValid => !(m_value is null);
        public override string TypeName => "Bitmap";
        public override string TypeDescription => "Bitmap";
    }
}
