using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ImageCollage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }



        async Task CreateCollage(IReadOnlyList<StorageFile> files)
        {
            progressIndicator.Visibility = Windows.UI.Xaml.Visibility.Visible;
            var sampleDataGroups = files;
            if (sampleDataGroups.Count() == 0) return;

            try
            {
                // Do a square-root of the number of images to get the
                // number of images on x and y axis
                int number = (int)Math.Ceiling(Math.Sqrt((double)files.Count));
                // Calculate the width of each small image in the collage
                int numberX = (int)(ImageCollage.ActualWidth / number);
                int numberY = (int)(ImageCollage.ActualHeight / number);
                // Initialize an empty WriteableBitmap.
                WriteableBitmap destination = new WriteableBitmap(numberX * number, numberY * number);
                int col = 0; // Current Column Position
                int row = 0; // Current Row Position
                destination.Clear(Colors.White); // Set the background color of the image to white
                WriteableBitmap bitmap; // Temporary bitmap into which the source
                // will be copied
                foreach (var file in files)
                {
                    // Create RandomAccessStream reference from the current selected image
                    RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromFile(file);
                    int wid = 0;
                    int hgt = 0;
                    byte[] srcPixels;
                    // Read the image file into a RandomAccessStream
                    using (IRandomAccessStreamWithContentType fileStream = await streamRef.OpenReadAsync())
                    {
                        // Now that you have the raw bytes, create a Image Decoder
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                        // Get the first frame from the decoder because we are picking an image
                        BitmapFrame frame = await decoder.GetFrameAsync(0);
                        // Convert the frame into pixels
                        PixelDataProvider pixelProvider = await frame.GetPixelDataAsync();
                        // Convert pixels into byte array
                        srcPixels = pixelProvider.DetachPixelData();
                        wid = (int)frame.PixelWidth;
                        hgt = (int)frame.PixelHeight;
                        // Create an in memory WriteableBitmap of the same size
                        bitmap = new WriteableBitmap(wid, hgt);
                        Stream pixelStream = bitmap.PixelBuffer.AsStream();
                        pixelStream.Seek(0, SeekOrigin.Begin);
                        // Push the pixels from the original file into the in-memory bitmap
                        pixelStream.Write(srcPixels, 0, (int)srcPixels.Length);
                        bitmap.Invalidate();

                        if (row < number)
                        {
                            // Resize the in-memory bitmap and Blit (paste) it at the correct tile
                            // position (row, col)
                            destination.Blit(new Rect(col * numberX, row * numberY, numberX, numberY),
                                bitmap.Resize(numberX, numberY, WriteableBitmapExtensions.Interpolation.Bilinear),
                                new Rect(0, 0, numberX, numberY));
                            col++;
                            if (col >= number)
                            {
                                row++;
                                col = 0;
                            }
                        }
                    }
                }

                ImageCollage.Source = destination;
                ((WriteableBitmap)ImageCollage.Source).Invalidate();
                progressIndicator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // TODO: Log Error, unable to render image
                throw;
            }
        }


        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.SettingsIdentifier = "picker1";
            filePicker.CommitButtonText = "Select for Collage";

            var files = await filePicker.PickMultipleFilesAsync();
            await CreateCollage(files);
        }

        async Task SaveImage(WriteableBitmap saveImage)
        {
            FileSavePicker picker = new FileSavePicker();
            picker.FileTypeChoices.Add("JPG File", new List<string>() {
                ".jpg" 
            });
            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(
                        BitmapEncoder.JpegEncoderId, stream);
                    Stream pixelStream = saveImage.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)saveImage.PixelWidth, (uint)saveImage.PixelHeight, 96.0, 96.0, pixels);
                    await encoder.FlushAsync();
                }
            }
        }

        private async void SaveFiles_Click(object sender, RoutedEventArgs e)
        {
            await SaveImage((WriteableBitmap)ImageCollage.Source);
        }
    }
}

