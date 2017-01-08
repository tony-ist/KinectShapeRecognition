using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Console.WriteLine("{0:X}", HsvToBgr32(0, 1, 1));
            Console.WriteLine("{0:X}", HsvToBgr32(180, 1, 1));
            Console.WriteLine("{0:X}", HsvToBgr32(0, 0.5, 0.5));
            Console.WriteLine("{0:X}", 0xFF << 16);
            String textData = File.ReadAllText(@"data\air_pen_0.txt");
            var depthArray = textData.Split(',').Where(s => !String.IsNullOrEmpty(s)).Select(short.Parse).ToArray();
//            DisplayDepthArray(depthArray);
//            DisplayDepthArray(Enumerable.Range(0, 320 * 240).Select(x => (short)x).ToArray());
//            DisplayDepthArray(Enumerable.Repeat(17064, 320 * 240).Select(x => (short) x).ToArray());
//            DisplayDepthArray(Enumerable.Repeat(-10000, 100).Concat(Enumerable.Repeat(0,320*240=100)).ToArr());
            DisplayBgr(Enumerable.Range(0, 320*240).Select(i => HsvToBgr32(i, 1, 1)).ToArray());
//            DisplayBgr(Enumerable.Range(0, 320*240).Select(i => ~(i + 16000000)).ToArray());
//            DisplayBgr(Enumerable.Repeat(0x0000ff, 320*240).ToArray());
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
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

        private static int HsvToBgr32(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue/60))%6;
            double f = hue/60 - Math.Floor(hue/60);

            value = value*255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value*(1 - saturation));
            int q = Convert.ToInt32(value*(1 - f*saturation));
            int t = Convert.ToInt32(value*(1 - (1 - f)*saturation));

            switch (hi)
            {
                case 0:
                    return (v << 16) + (t << 8) + p;
                case 1:
                    return (q << 16) + (v << 8) + p;
                case 2:
                    return (p << 16) + (v << 8) + t;
                case 3:
                    return (p << 16) + (q << 8) + v;
                case 4:
                    return (t << 16) + (p << 8) + v;
                default:
                    return (v << 16) + (p << 8) + q;
            }
        }
    }
}