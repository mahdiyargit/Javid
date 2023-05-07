using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Javid.Parameter;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel.Data;
using AForge.Video.DirectShow;
using Grasshopper;
using AForge.Video;
using Grasshopper.GUI;
using System.Text;

namespace Javid.BitmapComponents
{
    public sealed class CameraStreamParam : GH_Param<GH_Bitmap>
    {
        private FilterInfoCollection _devices;
        private VideoCaptureDevice _source;
        private Bitmap _bmp;
        private GH_Document _doc;

        public CameraStreamParam() : base(new GH_InstanceDescription("Camera Stream", "Cam", "Stream Video From Camera",
            "Javid", "Bitmap"))
        {
        }

        private void FirstStart()
        {
            Play = false;
            _devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (_devices.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No video capture device found");
                return;
            }
            DeviceNames = new List<string>();
            FrameSizes = new List<List<string>>();
            foreach (FilterInfo device in _devices)
            {
                var source = new VideoCaptureDevice(device.MonikerString);
                var vcs = source.VideoCapabilities;
                if (vcs.Length == 0) continue;
                FrameSizes.Add(vcs.Select(vc => vc.FrameSize).Select(s => $"{s.Width}×{s.Height}").ToList());
                DeviceNames.Add(device.Name);
            }
            if (DeviceNames.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No video capture device found");
                return;
            }
            ChangeSource(0, 0);
        }
        public List<string> DeviceNames { get; private set; }
        public List<List<string>> FrameSizes { get; private set; }
        private int _selectedDevice;
        private int _selectedFrameSize;
        public int SelectedDevice
        {
            get => _selectedDevice;
            set => ChangeSource(value, _selectedFrameSize);
        }
        public int SelectedFrameSize
        {
            get => _selectedFrameSize;
            set => ChangeSource(_selectedDevice, value);
        }
        
        public bool Play { get; set; }
        private void ChangeSource(int device, int frameSize)
        {
            if (_source != null && _source.IsRunning)
            {
                if (SelectedDevice == device && SelectedFrameSize == frameSize) 
                    return;
                _source.NewFrame -= NewFrame;
                _source.SignalToStop();
                _source.WaitForStop();
                _source = null;
            }
            _selectedDevice = device;
            if (FrameSizes[_selectedDevice].Count < frameSize)
                frameSize = FrameSizes[_selectedDevice].Count - 1;
            _selectedFrameSize = frameSize;
            _source = new VideoCaptureDevice(_devices[SelectedDevice].MonikerString);
            _source.VideoResolution = _source.VideoCapabilities[SelectedFrameSize];
            _source.NewFrame += NewFrame;
            _source.Start();
        }
        protected override void CollectVolatileData_Custom()
        {
            if (_doc == null)
            {
                _doc = OnPingDocument();
                Instances.DocumentServer.DocumentRemoved += OnDocRemoved;
                _doc.ObjectsDeleted += ObjectsDeleted;
                FirstStart();
            }
            m_data.Clear();
            m_data.Append(new GH_Bitmap(_bmp), new GH_Path(0));
            if (Play)
                _doc.ScheduleSolution(_interval, ExpireSolutionCallback);
        }
        public void Release()
        {
            Instances.DocumentServer.DocumentRemoved -= OnDocRemoved;
            if (_doc != null)
                _doc.ObjectsDeleted -= ObjectsDeleted;
            _doc = null;
            if (_source == null) return;
            _source.NewFrame -= NewFrame;
            _source.SignalToStop();
            _source.WaitForStop();
            _source = null;
        }
        public void OnDocRemoved(GH_DocumentServer ds, GH_Document doc) => Release();
        public void ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (!e.Objects.Contains(this)) return;
            Release();
        }
        private void NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!Play || Locked) return;
            _bmp = (Bitmap)eventArgs.Frame.Clone();
        }
        private void ExpireSolutionCallback(GH_Document doc)
        {
            ClearData();
            ExpireDownStreamObjects();
        }

        private int _interval = 30;

        public string IntervalString
        {
            get
            {
                string intervalString;
                if (_interval < 1000)
                    intervalString = $"{_interval} ms";
                else
                {
                    var timeSpan = new TimeSpan(10000L * _interval);
                    intervalString = timeSpan.Milliseconds == 0 ? (timeSpan.TotalSeconds != 1.0 ? (timeSpan.TotalMinutes != 1.0 ? (timeSpan.TotalSeconds >= 60.0 ? (timeSpan.TotalMinutes >= 60.0 ? (timeSpan.Minutes != 0 || timeSpan.Seconds != 0 ? string.Format("{0:0}:{1:00}:{2:00}", Math.Floor(timeSpan.TotalHours), timeSpan.Minutes, timeSpan.Seconds) : (timeSpan.TotalHours != 1.0 ? string.Format("{0:0} hours", timeSpan.TotalHours) : "1 hour")) : (timeSpan.Seconds != 0 ? string.Format("{0:0}:{1:0}", timeSpan.Minutes, timeSpan.Seconds) : string.Format("{0:0} minutes", timeSpan.TotalMinutes))) : string.Format("{0:0} seconds", timeSpan.TotalSeconds)) : "1 minute") : "1 second") : string.Format("{0} ms", _interval);
                }
                return intervalString;
            }
        }
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            var item = Menu_AppendItem(menu, "Interval");
            item.ToolTipText = @"Specify the delay between frame updates.";
            menu = item.DropDown;
            Menu_AppendItem(menu, "20 ms", ManualItemClicked, true, _interval == 20).Tag = 20;
            Menu_AppendItem(menu, "30 ms", ManualItemClicked, true, _interval == 30).Tag = 30;
            Menu_AppendItem(menu, "50 ms", ManualItemClicked, true, _interval == 50).Tag = 50;
            Menu_AppendItem(menu, "100 ms", ManualItemClicked, true, _interval == 100).Tag = 100;
            Menu_AppendItem(menu, "200 ms", ManualItemClicked, true, _interval == 200).Tag = 200;
            Menu_AppendItem(menu, "500 ms", ManualItemClicked, true, _interval == 500).Tag = 500;
            Menu_AppendSeparator(menu); 
            Menu_AppendItem(menu, "1 second", ManualItemClicked, true, _interval == 1000).Tag = 1000;
            Menu_AppendItem(menu, "2 seconds", ManualItemClicked, true, _interval == 2000).Tag = 2000;
            Menu_AppendItem(menu, "5 seconds", ManualItemClicked, true, _interval == 5000).Tag = 5000;
            Menu_AppendItem(menu, "10 seconds", ManualItemClicked, true, _interval == 10000).Tag = 10000;
            Menu_AppendItem(menu, "30 seconds", ManualItemClicked, true, _interval == 30000).Tag = 30000;
            Menu_AppendSeparator(menu);
            var stringBuilder = new StringBuilder();
            var timeSpan = TimeSpan.FromMilliseconds(_interval);
            if (timeSpan.TotalHours >= 1.0)
            {
                var int32 = Convert.ToInt32(Math.Floor(timeSpan.TotalHours));
                stringBuilder.Append($"{int32} * (60 * 60 * 1000)");
                timeSpan -= TimeSpan.FromHours(int32);
            }
            if (timeSpan.TotalMinutes >= 1.0)
            {
                var int32 = Convert.ToInt32(Math.Floor(timeSpan.TotalMinutes));
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(" + ");
                stringBuilder.Append($"{int32} * (60 * 1000)");
                timeSpan -= TimeSpan.FromMinutes(int32);
            }
            if (timeSpan.TotalSeconds >= 1.0)
            {
                var int32 = Convert.ToInt32(Math.Floor(timeSpan.TotalSeconds));
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(" + ");
                stringBuilder.Append($"{int32} * 1000");
                timeSpan -= TimeSpan.FromSeconds(int32);
            }
            if (timeSpan.TotalMilliseconds >= 1.0)
            {
                var int32 = Convert.ToInt32(Math.Floor(timeSpan.TotalMilliseconds));
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(" + ");
                stringBuilder.Append($"{int32}");
            }
            Menu_AppendTextItem(menu, stringBuilder.ToString(), Menu_Interval_KeyDown, null, true).ToolTipText = @"Specify a custom interval in milliseconds.";
        }
        private void ManualItemClicked(object sender, EventArgs e)
        {
            _interval = (int)((ToolStripItem)sender).Tag;
            Instances.RedrawCanvas();
        }

        private void Menu_Interval_KeyDown(GH_MenuTextBox sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return) return;
            if (!GH_Convert.ToInt32(sender.Text, out var d, GH_Conversion.Both)) return;
            _interval = d;
            Instances.RedrawCanvas();
        }
        public override void CreateAttributes() => Attributes  = new CameraStreamAttributes(this);
        public override Guid ComponentGuid => new Guid("0CE8AAA1-876D-4120-912D-7577CB8CF4A1");
        public override GH_ParamKind Kind => GH_ParamKind.floating;
        protected override Bitmap Icon => Properties.Resources.camera;
        public override GH_ParamData DataType => GH_ParamData.local;
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override bool IconCapableUI => false;
    }
}