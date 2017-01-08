using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectShapeRecognition
{
    public partial class MainWindow : Window
    {
        private static readonly int pixelWidth = 320;
        private static readonly int pixelHeight = 240;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            String textData = File.ReadAllText(@"data\air_pen_0.txt");
            var depthArray = textData.Split(',').Where(s => !String.IsNullOrEmpty(s)).Select(short.Parse).ToArray();
            DisplayDepthArray(depthArray);
        }

        private void DisplayDepthArray(short[] depthArray)
        {
            int bytesPerPixel = sizeof (short);
            int dpiX = 96, dpiY = dpiX;

            image.Source = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                dpiX,
                dpiY,
                PixelFormats.Gray16,
                null,
                depthArray,
                pixelWidth*bytesPerPixel
                );
        }

        private void DisplayBgr(int[] array)
        {
            int bytesPerPixel = sizeof (int);
            int dpiX = 96, dpiY = dpiX;

            image.Source = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                dpiX,
                dpiY,
                PixelFormats.Bgr32,
                null,
                array,
                pixelWidth*bytesPerPixel
                );
        }
    }
}