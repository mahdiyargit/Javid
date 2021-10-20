using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace Javid.BitmapComponents
{
    public sealed class PreviewBitmapAttributes : GH_ResizableAttributes<PreviewBitmapComp>
    {
        private Rectangle _bmpBox;
        public PreviewBitmapAttributes(PreviewBitmapComp owner) : base(owner)
        {
            Bounds = new Rectangle(0, 0, 150, 150);
            Bounds.Inflate(6f, 6f);
        }
        public override void AppendToAttributeTree(List<IGH_Attributes> attributes)
        {
            base.AppendToAttributeTree(attributes);
            Owner.Params.Input[0].Attributes.AppendToAttributeTree(attributes);
        }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            Bounds = Owner.DisplayImage is null
                ? new Rectangle(0, 0, 150, 150)
                : new Rectangle(0, 0, Owner.DisplayImage.Width + (int)Owner.Params.InputWidth + 4, Owner.DisplayImage.Height);
            Bounds.Inflate(6f, 6f);
            ExpireLayout();
            Instances.RedrawCanvas();
            return GH_ObjectResponse.Handled;
        }
        protected override void Layout()
        {
            base.Layout();
            var bounds = Bounds;
            GH_ComponentAttributes.LayoutInputParams(Owner, bounds);
            bounds.X += Owner.Params.InputWidth + 4f;
            GH_ComponentAttributes.LayoutInputParams(Owner, bounds);
            var w = Math.Max(Bounds.Width, MinimumSize.Width);
            var h = Math.Max(Bounds.Height, MinimumSize.Height);
            Bounds = new RectangleF(Pivot.X, Pivot.Y, w, h);
            var left = Owner.Params.Input[0].Attributes.Bounds.Right;
            var right = Bounds.Right;
            var top = Bounds.Top;
            var bottom = Bounds.Bottom;
            _bmpBox = right <= left + 1.0 ? Rectangle.Empty : GH_Convert.ToRectangle(RectangleF.FromLTRB(left, top, right, bottom));
        }
        protected override Size MinimumSize => new Size(100, 100);
        protected override Padding SizingBorders => new Padding(0, 6, 6, 6);
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            switch (channel)
            {
                case GH_CanvasChannel.Wires:
                    Owner.Params.Input[0].Attributes.RenderToCanvas(canvas, GH_CanvasChannel.Wires);
                    break;
                case GH_CanvasChannel.Objects:
                    var bounds = Bounds;
                    if (!canvas.Viewport.IsVisible(ref bounds, 10f))
                        break;
                    var palette = GH_Palette.Transparent;
                    switch (Owner.RuntimeMessageLevel)
                    {
                        case GH_RuntimeMessageLevel.Warning:
                            palette = GH_Palette.Warning;
                            break;
                        case GH_RuntimeMessageLevel.Error:
                            palette = GH_Palette.Error;
                            break;
                    }
                    var ghCapsule = GH_Capsule.CreateCapsule(Bounds, palette);
                    ghCapsule.SetJaggedEdges(false, true);
                    ghCapsule.AddInputGrip(Owner.Params.Input[0].Attributes.InputGrip.Y);
                    ghCapsule.Render(graphics, Selected, Owner.Locked, true);
                    GH_ComponentAttributes.RenderComponentParameters(canvas, graphics, Owner,
                        GH_CapsuleRenderEngine.GetImpliedStyle(palette, Owner.Attributes));
                    ghCapsule.Dispose();
                    var rec = _bmpBox;
                    rec.Inflate(-6, -6);
                    if (Owner.DisplayImage != null && !Owner.Locked)
                    {
                        var destRect = (RectangleF)rec;
                        destRect.Inflate(1f, 1f);
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        var displayImage = Owner.DisplayImage;
                        graphics.DrawImage(displayImage, destRect, new RectangleF(-0.5f, -0.5f, displayImage.Width, displayImage.Height),
                            GraphicsUnit.Pixel);
                    }
                    GH_GraphicsUtil.ShadowRectangle(graphics, rec, 15);
                    graphics.DrawRectangle(Pens.Black, rec);
                    break;
            }
        }
    }
}
