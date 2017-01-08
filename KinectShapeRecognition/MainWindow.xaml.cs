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
            String textData = File.ReadAllText(@"data\table_pen_0.txt");
            var depthArray = textData.Split(',')
                .Where(s => !String.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToArray();
//            DisplayDepthArrayInGreyscale(depthArray);
            DisplayDepthArrayInColour(depthArray);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DisplayDepthArrayInGreyscale(int[] depthArray)
        {
            int bytesPerPixel = sizeof(short);
            int dpiX = 96, dpiY = dpiX;
            int maxDepth = depthArray.Max();
            int maxColour = 1 << 16;
            short[] colourArray = depthArray.Select(d => (short) ((double)d / maxDepth * maxColour)).ToArray();

            image.Source = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                dpiX,
                dpiY,
                PixelFormats.Gray16,
                null,
                colourArray,
                pixelWidth * bytesPerPixel
                );
        }

        private void DisplayDepthArrayInColour(int[] depthArray)
        {
            int bytesPerPixel = sizeof(int);
            int dpiX = 96, dpiY = dpiX;
            int largePrime = 3121;
            int[] colourArray = depthArray
                .Select(i => HsvToBgr32(i * largePrime, 1, 1))
                .ToArray();

            image.Source = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                dpiX,
                dpiY,
                PixelFormats.Bgr32,
                null,
                colourArray,
                pixelWidth * bytesPerPixel
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