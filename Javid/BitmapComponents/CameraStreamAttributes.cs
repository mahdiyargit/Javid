using System;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Drawing;
using Grasshopper.GUI;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Javid.BitmapComponents
{
    public class CameraStreamAttributes : GH_Attributes<CameraStreamParam>
    {
        public CameraStreamAttributes(CameraStreamParam owner) : base(owner)
        {
        }

        public override bool HasInputGrip => false;

        protected override void Layout()
        {
            var point = Pivot;
            PlayBox = new RectangleF(point, new Size(24, 24));
            point.X = PlayBox.Right;
            CamBox = new RectangleF(point, new Size(24, 24));
            point.X = CamBox.Right;
            ResBox = new RectangleF(point, new Size(24, 24));
            point.X = ResBox.Right;
            TimeBox = new RectangleF(point, new Size(70, 24));
            Bounds = RectangleF.Union(PlayBox, TimeBox);
        }

        private RectangleF PlayBox { get; set; }

        private RectangleF CamBox { get; set; }

        private RectangleF ResBox { get; set; }

        private RectangleF TimeBox { get; set; }
        

        private bool OverPlayBox(PointF point) => PlayBox.Width > 0.0 && PlayBox.Contains(point);

        private bool OverCamBox(PointF point) => CamBox.Width > 0.0 && CamBox.Contains(point);

        private bool OverResBox(PointF point) => ResBox.Width > 0.0 && ResBox.Contains(point);

        private static Bitmap DrawIcon(Action<Graphics, GH_PaletteStyle, RectangleF> method)
        {
            var bitmap = new Bitmap(36, 36, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.ScaleTransform(1.5f, 1.5f);
                var ghPaletteStyle = new GH_PaletteStyle(Color.LightGray);
                method(graphics, ghPaletteStyle, new RectangleF(0.0f, 0.0f, 24f, 24f));
            }
            return bitmap;
        }

        public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
        {
            if (OverResBox(point))
            {
                {
                    e.Icon = DrawIcon(RenderResSymbol);
                    e.Title = "Resolution";
                    e.Text = "Select the desired camera resolution.";
                }
            }
            else if (OverCamBox(point))
            {
                e.Icon = DrawIcon(RenderCamSymbol);
                e.Title = "Camera";
                e.Text = "Select the Camera.";
            }
            else if (OverPlayBox(point))
            {
                if (Owner.Play)
                {
                    e.Icon = DrawIcon(RenderPauseSymbol);
                    e.Title = "Pause";
                    e.Text = "Pause the camera stream.";
                }
                else
                {
                    e.Icon = DrawIcon(RenderPlaySymbol);
                    e.Title = "Resume";
                    e.Text = "Resume the camera stream.";
                }
            }
            else
                base.SetupTooltip(point, e);
        }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Handled;
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (OverCamBox(e.CanvasLocation))
                {
                    {
                        var menu = new ToolStripDropDownMenu();
                        for (var i = 0; i < Owner.DeviceNames.Count; i++)
                        {
                            var item = new ToolStripMenuItem(Owner.DeviceNames[i], null, CamMenuItem_Click)
                            {
                                Checked = i == Owner.SelectedDevice,
                                Tag = i
                            };
                            menu.Items.Add(item);
                        }
                        menu.Show(sender, e.ControlLocation);
                    }
                    return GH_ObjectResponse.Handled;
                }
                if (OverResBox(e.CanvasLocation))
                {
                    {
                        var menu = new ToolStripDropDownMenu();
                        var frameSizes = Owner.FrameSizes[Owner.SelectedDevice];
                        for (var i = 0; i < frameSizes.Count; i++)
                        {
                            var item = new ToolStripMenuItem(frameSizes[i], null, ResMenuItem_Click)
                            {
                                Checked = i == Owner.SelectedFrameSize,
                                Tag = i
                            };
                            menu.Items.Add(item);
                        }
                        menu.Show(sender, e.ControlLocation);
                    }
                    return GH_ObjectResponse.Handled;
                }
                if (OverPlayBox(e.CanvasLocation))
                {
                    Owner.Play = !Owner.Play;
                    Owner.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }
        private void CamMenuItem_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            if (item.Checked || !(item.Tag is int index)) return;
            Owner.SelectedDevice = index;
        }
        private void ResMenuItem_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            if (item.Checked || !(item.Tag is int index)) return;
            Owner.SelectedFrameSize = index;
        }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel != GH_CanvasChannel.Objects) return;
            var viewport = canvas.Viewport;
            var bounds = Bounds;
            ref var local = ref bounds;
            var num = viewport.IsVisible(ref local, 10f) ? 1 : 0;
            Bounds = bounds;
            if (num == 0)
                return;
            canvas.SetSmartTextRenderingHint();
            using (var capsule = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Normal, 3, 0))
            {
                var impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(capsule.Palette, Selected, false, true);
                if (Owner.Locked || Owner.DeviceNames == null)
                {
                    var outlineShape = capsule.OutlineShape;
                    capsule.AddOutputGrip(OutputGrip);
                    capsule.RenderEngine.RenderGrips(graphics);
                    using (var hatchBrush = new HatchBrush(HatchStyle.DarkUpwardDiagonal, Color.DimGray, Owner.DeviceNames == null ? Color.Red : Color.White))
                        graphics.FillPath(hatchBrush, outlineShape);
                    capsule.RenderEngine.RenderOutlines(graphics, graphics.Transform.Elements[0], impliedStyle);
                }
                else
                {
                    var element = graphics.Transform.Elements[0];
                    capsule.AddOutputGrip(OutputGrip);
                    capsule.RenderEngine.RenderGrips(graphics);
                    if (Owner.DeviceNames.Count > Owner.SelectedDevice && Owner.FrameSizes[Owner.SelectedDevice].Count > Owner.SelectedFrameSize)
                        capsule.RenderEngine.RenderMessage(graphics, Owner.DeviceNames[Owner.SelectedDevice] + '\n' + Owner.FrameSizes[Owner.SelectedDevice][Owner.SelectedFrameSize], impliedStyle);
                    capsule.RenderEngine.RenderBackground(graphics, element, impliedStyle);
                    capsule.RenderEngine.RenderOutlines(graphics, element, impliedStyle);
                    if (GH_Canvas.ZoomFadeMedium == 0)
                        return;
                    using (var solidBrush = new SolidBrush(impliedStyle.Text))
                    {
                        if (TimeBox.Width > 16.0)
                        {
                            var intervalString = Owner.IntervalString;
                            if (!string.IsNullOrEmpty(intervalString))
                                graphics.DrawString(intervalString, GH_FontServer.StandardAdjusted, solidBrush, TimeBox, GH_TextRenderingConstants.CenterCenter);
                        }
                    }
                    RenderCamSymbol(graphics, impliedStyle, CamBox);
                    RenderResSymbol(graphics, impliedStyle, ResBox);
                    if (Owner.Play)
                        RenderPauseSymbol(graphics, impliedStyle, PlayBox);
                    else
                        RenderPlaySymbol(graphics, impliedStyle, PlayBox);
                }
            }
        }
        private static void RenderButtonDisc(Graphics graphics, GH_PaletteStyle style, RectangleF box)
        {
            using (var solidBrush = new SolidBrush(style.Text))
            {
                box.Inflate(-3f, -3f);
                graphics.FillEllipse(solidBrush, box);
            }
        }
        private static void RenderPlaySymbol(Graphics graphics, GH_PaletteStyle style, RectangleF box)
        {
            if (box.Width <= 0.0)
                return;
            RenderButtonDisc(graphics, style, box);
            using (var solidBrush = new SolidBrush(style.Fill))
            {
                var pointF1 = new PointF(box.X + 9f, box.Y + 8f);
                var pointF2 = new PointF(box.X + 17f, box.Y + 12f);
                var pointF3 = new PointF(box.X + 9f, box.Y + 16f);
                graphics.FillPolygon(solidBrush, new PointF[3]
                {
                    pointF1,
                    pointF2,
                    pointF3
                });
            }
        }
        private static void RenderPauseSymbol(Graphics graphics, GH_PaletteStyle style, RectangleF box)
        {
            if (box.Width <= 0.0)
                return;
            RenderButtonDisc(graphics, style, box);
            using (var solidBrush = new SolidBrush(style.Fill))
            {
                var location1 = new PointF(box.X + 8f, box.Y + 8f);
                var location2 = new PointF(box.X + 13f, box.Y + 8f);
                graphics.FillRectangle(solidBrush, new RectangleF(location1, new SizeF(3f, 8f)));
                graphics.FillRectangle(solidBrush, new RectangleF(location2, new SizeF(3f, 8f)));
            }
        }
        private static void RenderCamSymbol(Graphics graphics, GH_PaletteStyle style, RectangleF box)
        {
            if (box.Width <= 0.0) return;
            RenderButtonDisc(graphics, style, box);
            using (var pen = new Pen(style.Fill, 2f))
            {
                box.Inflate(-7f, -7f);
                graphics.DrawEllipse(pen, box);
                box.Inflate(-4f, -4f);
                graphics.DrawEllipse(pen, box);
            }
        }
        private static void RenderResSymbol(Graphics graphics, GH_PaletteStyle style, RectangleF box)
        {
            if (box.Width <= 0.0) return;
            RenderButtonDisc(graphics, style, box);
            using (var solidBrush = new SolidBrush(style.Fill))
                graphics.FillRectangle(solidBrush, new RectangleF(new PointF(box.X + 6f, box.Y + 8f), new SizeF(12f, 8f)));
        }
    }
}