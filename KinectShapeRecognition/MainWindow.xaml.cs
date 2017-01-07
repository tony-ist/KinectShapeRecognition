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
        private static readonly int bytesPerPixel = 2;
        
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
            image.Source = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                96, 96,
                PixelFormats.Gray16,
                null,
                depthArray,
                pixelWidth * bytesPerPixel
            );
        }
    }
}
