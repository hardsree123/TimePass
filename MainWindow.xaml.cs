using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Vision.Motion;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindowsInput;
using WindowsInput.Native;

namespace MD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCaptureDevice outerVdoSource = null;
        private bool viewing = false;
        private MotionDetector md;
        public MainWindow()
        {
            InitializeComponent();
            LoadUsbCameras();
        }
        System.Drawing.Rectangle[] imageRect = new System.Drawing.Rectangle[2];
        System.Drawing.Pen pen;
        private void LoadUsbCameras()
        {
            var videoDeviceList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo fc in videoDeviceList)
            {
                vdoOuterDeviceList.Items.Add(fc.Name);
            }
            imageRect[0] = new System.Drawing.Rectangle(0, 0, 150, 250);
            imageRect[1] = new System.Drawing.Rectangle(490, 0, 150, 250);
            pen = new System.Drawing.Pen(System.Drawing.Color.Red);
        }
        private void vdoOuterDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { 
            TwoFramesDifferenceDetector twoFramesDifference = new TwoFramesDifferenceDetector() { SuppressNoise = true };
            md = new MotionDetector(twoFramesDifference,
                new MotionBorderHighlighting());
            md.MotionZones = imageRect;
        }
        private void RecordOuterCamFeed_Click(object sender, RoutedEventArgs e)
        {
            if (viewing)
            {
                RecordOuterCamFeed.Content = "View cam feed";
                outerVdoSource.Stop();
                outerVdoSource = null;
                Feed.Source = null;
            }
            else
            {
                var videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                outerVdoSource = new VideoCaptureDevice(videoDevicesList[vdoOuterDeviceList.SelectedIndex].MonikerString);
                outerVdoSource.NewFrame += new NewFrameEventHandler(outervideo_NewFrame);
                outerVdoSource.Start();
                RecordOuterCamFeed.Content = "Click to stop...";
                this.viewing = true;
            }
        }

        private Bitmap LoadMotionFrames(Bitmap v)
        {
            try
            {
                Graphics _graphics = Graphics.FromImage(v);
                _graphics.DrawRectangles(pen, imageRect);
                double mv = md.ProcessFrame(v);
                if (mv > 0.002)
                {
                    InputSimulator inp = new InputSimulator();
                    inp.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                    return v;
                }
                return v;
            }
            finally
            {
            }
        }

        private void outervideo_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // get new frame
            System.Drawing.Image imgforms = LoadMotionFrames((Bitmap)eventArgs.Frame.Clone());

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();

            MemoryStream ms = new MemoryStream();
            imgforms.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);

            bi.StreamSource = ms;
            bi.EndInit();

            //Using the freeze function to avoid cross thread operations 
            bi.Freeze();

            //Calling the UI thread using the Dispatcher to update the 'Image' WPF control         
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Feed.Source = bi; /*frameholder is the name of the 'Image' WPF control*/
            }));
        }

    }
}
