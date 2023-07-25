using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace JAProj {
    public partial class MainWindow : Window {
        [DllImport(@"C:\Users\Matrix\Desktop\ja_sem5\OUTPUT\JAProj\x64\Release\JACpp.dll")] static unsafe private extern void passImageToCpp(byte* InputArray, byte* OutputArray, int width, int height, int start, int stop);

        [DllImport(@"C:\Users\Matrix\Desktop\ja_sem5\OUTPUT\JAProj\x64\Release\JAAsm.dll")] static unsafe private extern void passImageToAsm(byte* InputArray, byte* OutputArray, int width, int height, int start, int stop);

        private struct BmpStructure {
            public UInt16 BM { get; set; }
            public UInt32 FileSize { get; set; }
            public UInt32 Unused { get; set; }
            public UInt32 Offset { get; set; }
            public UInt32 InfoHeaderSize { get; set; }
            public UInt32 Width { get; set; }
            public UInt32 Height { get; set; }
            public UInt16 Planes { get; set; }
            public UInt16 BitsPerPixel { get; set; }
            public UInt32 Compression { get; set; }
            public UInt32 ImageSize { get; set; }
            public UInt32 HorizontalResolution { get; set; }
            public UInt32 VerticalResolution { get; set; }
            public UInt32 ColorsUsed { get; set; }
            public UInt32 ColorsImportant { get; set; }
            public byte[] ByteArray { get; set; }
        }
        
        private bool BmpToStruct(byte[] array, ref BmpStructure bmp) {
            if (array.Length >= 54) {
                BinaryReader reader = new BinaryReader(new MemoryStream(array));
                bmp.BM = reader.ReadUInt16();
                bmp.FileSize = reader.ReadUInt32();
                bmp.Unused = reader.ReadUInt32();
                bmp.Offset = reader.ReadUInt32();
                bmp.InfoHeaderSize = reader.ReadUInt32();
                bmp.Width = reader.ReadUInt32();
                bmp.Height = reader.ReadUInt32();
                bmp.Planes = reader.ReadUInt16();
                bmp.BitsPerPixel = reader.ReadUInt16();
                bmp.Compression = reader.ReadUInt32();
                bmp.ImageSize = reader.ReadUInt32();
                bmp.HorizontalResolution = reader.ReadUInt32();
                bmp.VerticalResolution = reader.ReadUInt32();
                bmp.ColorsUsed = reader.ReadUInt32();
                bmp.ColorsImportant = reader.ReadUInt32();
                if (bmp.BM == 19778 && bmp.Unused == 0 && bmp.Offset == 54 && bmp.InfoHeaderSize == 40 && bmp.Planes == 1 && bmp.BitsPerPixel == 24 && bmp.Compression == 0 && bmp.ColorsUsed == 0 && bmp.ColorsImportant == 0 && bmp.Width >= 3 && bmp.Height >= 3) {
                    UInt32 PayloadSize = bmp.Height * (bmp.Width * 3 + ((4 - ((bmp.Width * 3) % 4)) % 4));
                    if (bmp.ImageSize == PayloadSize && bmp.FileSize == PayloadSize + 54 && array.Length == PayloadSize + 54) {
                        bmp.ByteArray = reader.ReadBytes((int) PayloadSize);
                        reader.Close();
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else {
                    return false;
                }
            }
            else {
                return false;
            }
        }
        
        private byte[] CreateDecreasedBmp(ref byte[] array, ref BmpStructure bmp) {
            UInt32 payloadsize = ((bmp.Width - 2) * 3 + ((4 - (((bmp.Width - 2) * 3) % 4)) % 4)) * ((bmp.Height - 2));
            if (payloadsize == array.Length) {
                byte[] ret = BitConverter.GetBytes(bmp.BM);
                ret = ret.Concat(BitConverter.GetBytes((UInt32) 54 + payloadsize)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.Unused)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.Offset)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.InfoHeaderSize)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.Width - 2)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.Height - 2)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.Planes)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.BitsPerPixel)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.Compression)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes((UInt32) payloadsize)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.HorizontalResolution)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.VerticalResolution)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.ColorsUsed)).ToArray();
                ret = ret.Concat(BitConverter.GetBytes(bmp.ColorsImportant)).ToArray();
                return ret.Concat(array).ToArray();
            }
            else {
                byte[] ret = BitConverter.GetBytes(bmp.BM);
                return ret;
            }
        }
        
        private BmpStructure InputBmp = new BmpStructure();
        
        private byte[] OutputBmp;
        
        private bool ImageIsLoaded = false;
        
        private bool ImageIsProcessed = false;

        public MainWindow() {
            InitializeComponent();
        }

        private BitmapImage ConvertByteArrayToBitMapImage(byte[] imageByteArray) {
            BitmapImage img = new BitmapImage();
            using (MemoryStream memStream = new MemoryStream(imageByteArray)) {
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = memStream;
                img.EndInit();
                img.Freeze();
            }
            return img;
        }

        private void Button_OpenFile(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Image";
            dialog.DefaultExt = ".bmp";
            dialog.Filter = "BMP images (.bmp)|*.bmp";
            bool? result = dialog.ShowDialog();
            if (result == true) {
                string filename = dialog.FileName;
                byte[] array = File.ReadAllBytes(filename);
                this.InputBmp = new BmpStructure();
                if (this.BmpToStruct(array, ref InputBmp)) {
                    this.ImageIsLoaded = true;
                    Uri uri = new Uri(filename);
                    BitmapImage bitmap = new BitmapImage(uri);
                    InputImage.Source = bitmap;

                    // calculate max number of threads
                    int MaxNumOfThreads;
                    if (this.InputBmp.Height < 66) {
                        MaxNumOfThreads = ((int)this.InputBmp.Height) - 2;
                    }
                    else {
                        MaxNumOfThreads = 64;
                    }
                    Slider.Maximum = Convert.ToDouble(MaxNumOfThreads);

                }
                else {
                    this.ImageIsLoaded = false;
                }
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            NumOfThreadsLabel.Content = Slider.Value;
        }

        private void Button_RunCpp(object sender, RoutedEventArgs e) {
            if (this.ImageIsLoaded == true) {
                int NumOfThreads = Convert.ToInt32(NumOfThreadsLabel.Content);
                List<int> arrayStart = new List<int>();
                List<int> arrayStop = new List<int>();
                int h = (int)this.InputBmp.Height - 2;
                int start = 0;
                int stop = h / NumOfThreads;
                for (int x = 0; x < NumOfThreads - 1; x++) {
                    arrayStart.Add(start);
                    arrayStop.Add(stop);
                    start = stop;
                    stop += h / NumOfThreads;
                }
                arrayStart.Add(start);
                arrayStop.Add(h);
                arrayStart.Reverse();
                arrayStop.Reverse();
                List<Thread> Threads = new List<Thread>();
                byte[] OutputArray = new byte[((this.InputBmp.Width - 2) * 3 + ((4 - (((this.InputBmp.Width - 2) * 3) % 4)) % 4)) * ((this.InputBmp.Height - 2))];
                for (int x = 0; x < arrayStart.Count; x++) {
                    int a = arrayStart[x];
                    int b = arrayStop[x];
                    Thread t = new Thread(() => {
                        unsafe { fixed (byte* ptrInputArray = this.InputBmp.ByteArray, ptrOutputArray = OutputArray) {
                                passImageToCpp(ptrInputArray, ptrOutputArray, (int)this.InputBmp.Width, (int)this.InputBmp.Height, a, b);
                            }
                        }
                    });
                    Threads.Add(t);
                }
                long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                for (int x = 0; x < Threads.Count; x++) {
                    Threads[x].Start();
                }
                for (int x = 0; x < Threads.Count; x++) {
                    Threads[x].Join();
                }
                long stopTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                this.OutputBmp = this.CreateDecreasedBmp(ref OutputArray, ref this.InputBmp);
                OutputImage.Source = ConvertByteArrayToBitMapImage(this.OutputBmp);
                this.ImageIsProcessed = true;
                TimeLabel.Content = stopTime - startTime;
            }
            else {
                TimeLabel.Content = "The image has not been loaded";
            }
        }
        
        private void Button_RunAsm(object sender, RoutedEventArgs e) {
            if (this.ImageIsLoaded == true) {
                int NumOfThreads = Convert.ToInt32(NumOfThreadsLabel.Content);
                List<int> arrayStart = new List<int>();
                List<int> arrayStop = new List<int>();
                int h = (int)this.InputBmp.Height - 2;
                int start = 0;
                int stop = h / NumOfThreads;
                for (int x = 0; x < NumOfThreads - 1; x++) {
                    arrayStart.Add(start);
                    arrayStop.Add(stop);
                    start = stop;
                    stop += h / NumOfThreads;
                }
                arrayStart.Add(start);
                arrayStop.Add(h);
                arrayStart.Reverse();
                arrayStop.Reverse();
                List<Thread> Threads = new List<Thread>();
                byte[] OutputArray = new byte[((this.InputBmp.Width - 2) * 3 + ((4 - (((this.InputBmp.Width - 2) * 3) % 4)) % 4)) * ((this.InputBmp.Height - 2))];
                for (int x = 0; x < arrayStart.Count; x++) {
                    int a = arrayStart[x];
                    int b = arrayStop[x];
                    Thread t = new Thread(() => {
                        unsafe {
                            fixed (byte* ptrInputArray = this.InputBmp.ByteArray, ptrOutputArray = OutputArray) {
                                passImageToAsm(ptrInputArray, ptrOutputArray, (int)this.InputBmp.Width, (int)this.InputBmp.Height, a, b);
                            }
                        }
                    });
                    Threads.Add(t);
                }
                long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                for (int x = 0; x < Threads.Count; x++) {
                    Threads[x].Start();
                }
                for (int x = 0; x < Threads.Count; x++) {
                    Threads[x].Join();
                }
                long stopTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                this.OutputBmp = this.CreateDecreasedBmp(ref OutputArray, ref this.InputBmp);
                OutputImage.Source = ConvertByteArrayToBitMapImage(this.OutputBmp);
                this.ImageIsProcessed = true;
                TimeLabel.Content = stopTime - startTime;
            }
            else {
                TimeLabel.Content = "The image has not been loaded";
            }
        }
        
        private void Button_Save(object sender, RoutedEventArgs e) {
            if (this.ImageIsProcessed) {
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = "Image";
                dialog.DefaultExt = ".bmp";
                dialog.Filter = "BMP images (.bmp)|*.bmp";
                bool? result = dialog.ShowDialog();
                if (result == true) {
                    string filename = dialog.FileName;
                    File.WriteAllBytes(filename, this.OutputBmp);
                }
            }
            else {
                TimeLabel.Content = "The image has not been processed";
            }
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs args) {
            InputImage.Margin = new Thickness(10, 50, args.NewSize.Width / 2, 90);
            OutputImage.Margin = new Thickness(args.NewSize.Width / 2, 50, 10, 90);
        }
    }
}
