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
using AForge.Neuro;
using ConvNetSharp;
using ConvNetSharp.Layers;
using ConvNetSharp.Serialization;


namespace KinectShapeRecognition
{
    public partial class MainWindow : Window
    {
        private static readonly int pixelWidth = 320;
        private static readonly int pixelHeight = 240;
        private static readonly int frameWidth = 3;
        private static readonly int BLACK_COLOUR = 0x000000;
        private static readonly int WHITE_COLOUR = 0xFFFFFF;
        private static readonly string[] objectNames = { "Box", "Mug", "Racket" };
        private static readonly string dataFolder = "../data";

        private KinectSensor sensor;
        private bool isCapturing;
        private bool isFrameEnabled;
        private int frameSize, frameX, frameY;
        private int minDepth, maxDepth;
        private short[] currentDepthArray;
        private short fileNumber;
        private Network network;
        private Net convNet;
        private int networkCounter = 0;
        private int networkFrequency = 15;
        private double recognitionThreshold = 0.4;

        public MainWindow()
        {
            InitializeComponent();
            ReadFrameValues();
            LoadNeuralNetwork();
            // DisplayDataFile(FileNameTextBox.Text);
        }

        private void ReadFileButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayDataFile(FileNameTextBox.Text);

            // short[] depthArray = ReadDataFile(FileNameTextBox.Text);            
            // double[] networkOutput = RecognizeObject(depthArray);
            // DisplayNetworkOutput(networkOutput);
        }

        private void DisplayDataFile(String fileName)
        {
            try
            {
                currentDepthArray = ReadDataFile(fileName);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.ToString());
                Console.WriteLine(ex);
                return;
            }

            Redraw();
        }

        private short[] ReadDataFile(String fileName)
        {
            String textData = File.ReadAllText(String.Format(@"{0}\{1}", dataFolder, fileName));

            return textData.Split(',')
                .Where(s => !String.IsNullOrEmpty(s))
                .Select(short.Parse)
                .ToArray();   
        }

        private void FilterDepth(short[] depthArray)
        {
            IterateFrame((x, y) =>
            {
                int index = GetIndex(x, y, pixelWidth);

                if (depthArray[index] < minDepth || depthArray[index] > maxDepth)
                {
                    depthArray[index] = 0;
                }
            });
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.KinectSensors[0];
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.DepthFrameReady += DepthFrameReady;
            sensor.Start();
            StartButton.IsEnabled = false;
            isCapturing = true;
            SaveFrameButton.IsEnabled = true;
        }

        private void LoadNeuralNetwork()
        {
            //FileStream networkFileStream = File.OpenRead(String.Format(@"{0}\neural-network-40-180-2.txt", dataFolder));
            //network = Network.Load(networkFileStream);
            //
            //String convNetJson = File.ReadAllText(String.Format(@"{0}\conv-neural-network.json", dataFolder));
            //convNet = SerializationExtensions.FromJSON(convNetJson);

            FileStream convNetBin = File.OpenRead(String.Format(@"{0}\conv-neural-network-bin", dataFolder));
            convNet = (Net)SerializationExtensions.LoadBinary(convNetBin);
        }

        private void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            DepthImageFrame imageFrame = e.OpenDepthImageFrame();

            if (imageFrame == null)
            {
                return;
            }

            if (currentDepthArray == null)
            {
                currentDepthArray = new short[imageFrame.PixelDataLength];                
            }

            imageFrame.CopyPixelDataTo(currentDepthArray);
            Redraw();

            if (networkCounter % networkFrequency == 0 )
            {
                double[] networkOutput = RecognizeFrameObject();
                DisplayNetworkOutput(networkOutput);
            }

            networkCounter++;
        }

        private void DisplayNetworkOutput(double[] networkOutput)
        {
            string objectName = GetObjectName(networkOutput);
            ObjectLabel.Content = objectName;
            VectorLabel.Content = string.Join(", ", networkOutput.Select(x => x.ToString("N2")).ToArray());   
        }

        private void Redraw()
        {
            if (currentDepthArray == null)
            {
                return;
            }

            short[] filteredDepthArray = currentDepthArray;

            if (isFrameEnabled)
            {
                // TODO: Disable array copy if performance is low
                // filteredDepthArray = new short[currentDepthArray.GetLength(0)];
                // Array.Copy(currentDepthArray, filteredDepthArray, currentDepthArray.GetLength(0));
                FilterDepth(filteredDepthArray);
            }

//             DisplayDepthArrayInGreyscale(filteredDepthArray);
            DisplayDepthArrayInColour(filteredDepthArray);
//             DisplayDepthArrayInColourExplicit(filteredDepthArray);
        }

        private void DisplayDepthArrayInGreyscale(short[] depthArray)
        {
            int bytesPerPixel = sizeof(short);
            PixelFormat pixelFormat = PixelFormats.Gray16;
            int maxDepth = depthArray.Max();
            int maxColour = 1 << 16;
            short[] colourArray = depthArray
                .Select(d => (short) GetColourForDepthOrDefault(d, x => x / maxDepth * maxColour))
                .ToArray();

            DisplayColourArray(colourArray, bytesPerPixel, pixelFormat);
        }

        private void DisplayDepthArrayInColour(short[] depthArray)
        {
            int bytesPerPixel = sizeof(int);
            PixelFormat pixelFormat = PixelFormats.Bgr32;
            int minDepth = depthArray.Where(x => x > 0).DefaultIfEmpty((short) 0).Min();
            int maxDepth = depthArray.Max();
            int[] colourArray = depthArray
                .Select(d => GetColourForDepthOrDefault(d, x => HsvToBgr32((double)(x - minDepth) / (maxDepth - minDepth) * 360, 1, 1)))
                .ToArray();

            DisplayColourArray(colourArray, bytesPerPixel, pixelFormat);
        }

        private void DisplayDepthArrayInColourExplicit(short[] depthArray)
        {
            int bytesPerPixel = sizeof(int);
            PixelFormat pixelFormat = PixelFormats.Bgr32;
            int largePrime = 3121;
            int[] colourArray = depthArray
                .Select(d => GetColourForDepthOrDefault(d, x => HsvToBgr32(x * largePrime, 1, 1)))
                .ToArray();

            DisplayColourArray(colourArray, bytesPerPixel, pixelFormat);
        }

        private int GetColourForDepthOrDefault(int depth, Func<int, int> getDefault)
        {
            return depth == 0 || depth == -8 ? WHITE_COLOUR : getDefault.Invoke(depth);
        }

        private void DisplayColourArray(Array colourArray, int bytesPerPixel, PixelFormat pixelFormat)
        {
            if (isFrameEnabled)
            {
                DrawFrame(colourArray, BLACK_COLOUR);
            }

            int dpiX = 96, dpiY = dpiX;

            image.Source = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                dpiX,
                dpiY,
                pixelFormat,
                null,
                colourArray,
                pixelWidth * bytesPerPixel
                );
        }

        private void DrawFrame(Array colourArray, int frameColour)
        {
            DrawRectangle(colourArray, frameX - frameWidth, frameY - frameWidth, frameX + frameSize + frameWidth + 1, frameY, frameColour);
            DrawRectangle(colourArray, frameX - frameWidth, frameY - frameWidth, frameX, frameY + frameSize + frameWidth + 1, frameColour);
            DrawRectangle(colourArray, frameX + frameSize + 1, frameY, frameX + frameSize + frameWidth + 1, frameY + frameSize + frameWidth + 1, frameColour);
            DrawRectangle(colourArray, frameX, frameY + frameSize + 1, frameX + frameSize + frameWidth + 1, frameY + frameSize + frameWidth + 1, frameColour);
        }

        private void DrawRectangle(Array colourArray, int x1, int y1, int x2, int y2, int colour)
        {
            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    DrawPoint(colourArray, x, y, colour);
                }
            }
        }

        private void DrawPoint(Array colourArray, int x, int y, int colour)
        {
            int index = GetIndex(x, y, pixelWidth);
            if (index >= 0 && index < colourArray.GetLength(0))
            {
                colourArray.SetValue(colour, index);   
            }
        }

        private static int GetIndex(int x, int y, int width)
        {
            return y*width + x;
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

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ReadFrameValues();
            Redraw();
        }

        private void ReadFrameValues()
        {
            try
            {
                isFrameEnabled = (bool) IsFrameEnabledCheckBox.IsChecked;
                frameSize = int.Parse(FrameSizeTextBox.Text);
                frameX = pixelWidth/2 - 1 - frameSize/2;
                frameY = pixelHeight/2 - 1 - frameSize/2;
                minDepth = int.Parse(MinDepthTextBox.Text);
                maxDepth = int.Parse(MaxDepthTextBox.Text);
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.ToString());
                Console.WriteLine(ex);
            }
        }

        private void SaveFrameButton_Click(object sender, RoutedEventArgs e)
        {
            short[] frameDepthArray = currentDepthArray;

            if (isFrameEnabled)
            {
                frameDepthArray = GetFrameContent();
            }

            String fileName = String.Format(@"{0}\data{1}.txt", dataFolder, fileNumber);
            SerializeArray(frameDepthArray, fileName);
            fileNumber++;
        }

        private short[] GetFrameContent()
        {
            short[] frameDepthArray = new short[frameSize * frameSize];
            IterateFrame((x, y) =>
            {
                int depth = currentDepthArray[GetIndex(x, y, pixelWidth)];
                int frameIndex = GetIndex(x - frameX - 1, y - frameY - 1, frameSize);
                frameDepthArray[frameIndex] = (short)(depth < minDepth || depth > maxDepth ? 0 : depth);
            });
            return frameDepthArray;
        }

        private void IterateFrame(Action<int, int> step)
        {
            int startX = frameX + 1;
            int startY = frameY + 1;

            for (int y = startY; y < startY + frameSize; y++)
            {
                for (int x = startX; x < startX + frameSize; x++)
                {
                    int index = GetIndex(x, y, pixelWidth);

                    if (index < 0 || index >= currentDepthArray.GetLength(0))
                    {
                        continue;
                    }

                    step.Invoke(x, y);
                }
            }
        }

        private void SerializeArray(Array array, String fileName)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var element in array)
            {
                builder.Append(element.ToString() + ',');
            }

            File.WriteAllText(fileName, builder.ToString());
        }

        private double[] RecognizeFrameObject()
        {
            short[] frameDepthArray = GetFrameContent();

            return RecognizeObject(frameDepthArray);
        }

        private double[] RecognizeObject(short[] depthArray)
        {
            // double[] networkInput = FormatNeuralNetworkInput(depthArray);
            // double[] output = network.Compute(networkInput);

            Volume networkInput = FormatConvNeuralNetworkInput(depthArray);
            IVolume output = convNet.Forward(networkInput);
            double[] result = FormatConvNeuralNetworkOutput(output);

            return result;
        }

        private string GetObjectName(double[] networkOutput)
        {
            int maxIndex = networkOutput.ToList().IndexOf(networkOutput.Max());

            if (networkOutput[maxIndex] < recognitionThreshold)
            {
                return "Unknown";
            }

            string objectName = objectNames[maxIndex];
            return objectName;
        }

        private double[] FormatNeuralNetworkInput(short[] depthArray)
        {
            return depthArray.Select(d => (double) d).ToArray();
        }

        private Volume FormatConvNeuralNetworkInput(short[] depthArray)
        {
            int minDepth = depthArray.Where(x => x > 0).DefaultIfEmpty((short)0).Min();
            var result = new Volume(frameSize, frameSize, 1, 0.0);
            for (int j = 0; j < depthArray.Length; j++)
            {
                result.Set(j, depthArray[j] > 0 ? depthArray[j] - minDepth + 1 : 0);
            }
            return result;
        }

        private double[] FormatConvNeuralNetworkOutput(IVolume output)
        {
            return new [] { output.Get(0, 0, 0), output.Get(0, 0, 1), output.Get(0, 0, 2)};
        }
    }
}