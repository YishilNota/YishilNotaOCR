using ImageMagick;
using Microsoft.Win32;
using PdfSharp.Pdf.IO;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Tesseract;
using YishilNotaOCR.Models;
using YishilNotaOCR.Utils;

namespace YishilNotaOCR
{
    public partial class FrameOCRPdf : UserControl
    {
        public ObservableCollection<PdfPageItem> PdfPages { get; set; } = new ObservableCollection<PdfPageItem>();
        private string _selectedPdfPath = string.Empty;

        public FrameOCRPdf()
        {
            InitializeComponent();
            ItemsPages.ItemsSource = PdfPages;
            InitializeGhostscript();
        }

        private void InitializeGhostscript()
        {
            try
            {
                string assemblyDir = AppContext.BaseDirectory;
                string gsPath = Path.Combine(assemblyDir, "gs");
                MagickNET.SetGhostscriptDirectory(gsPath);

            }
            catch { }
        }

        private async void BtnStartPdfOcr_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPdfPath))
            {
                txtStatus.Text = "ھۆججەت تاللانمىدى";
                MessageBox.Show("ئالدى بىلەن PDF ھۆججىتىنى تاللاڭ");
                return;
            }

            var selectedPages = new List<int>();
            foreach (var item in PdfPages)
            {
                if (item.IsSelected) selectedPages.Add(item.PageNumber);
            }

            if (selectedPages.Count == 0)
            {
                MessageBox.Show("بىر تەرەپ قىلىدىغان بەتنى تاللاڭ");
                return;
            }

            btnStartOCR.IsEnabled = false;
            txtStatus.Text = "بىر تەرەپ قىلىۋاتىدۇ...";
            pbOCR.Visibility = Visibility.Visible;
            pbOCR.IsIndeterminate = true;
            txtResult.Text = "";

            try
            {
                string result = await ProcessPdfOcrWithStatusAsync(_selectedPdfPath, selectedPages);
                txtResult.Text = result;
                txtStatus.Text = "بىر تەرەپ قىلىپ بولۇندى";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "خاتالىق كۆرۈلدى";
                MessageBox.Show("OCR جەريانىدا خاتالىق كۆرۈلدى: " + ex.Message);
            }
            finally
            {
                btnStartOCR.IsEnabled = true;
                pbOCR.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<string> ProcessPdfOcrWithStatusAsync(string pdfPath, List<int> pageNumbers)
        {
            return await Task.Run(() =>
            {
                StringBuilder fullText = new StringBuilder();
                string tessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                using (var engine = new TesseractEngine(tessPath, "ukij", EngineMode.Default))
                {
                    for (int i = 0; i < pageNumbers.Count; i++)
                    {
                        int pageNum = pageNumbers[i];

                        Dispatcher.Invoke(() =>
                        {
                            txtStatus.Text = $"بىر تەرەپ قىلىۋاتىدۇ: {pageNum}-بەت";
                        });

                        var settings = new MagickReadSettings
                        {
                            Density = new Density(300, 300),
                            FrameIndex = (uint?)(pageNum - 1),
                            FrameCount = 1
                        };

                        using (var images = new MagickImageCollection())
                        {
                            try
                            {
                                images.Read(pdfPath, settings);
                                var image = (MagickImage)images[0];
                                image.Alpha(AlphaOption.Remove);
                                image.BackgroundColor = MagickColors.White;
                                image.ColorType = ColorType.Grayscale;
                                image.AutoThreshold(AutoThresholdMethod.OTSU);

                                byte[] byteData = image.ToByteArray(MagickFormat.Tiff);

                                using (var pix = Pix.LoadFromMemory(byteData))
                                {
                                    using (var page = engine.Process(pix))
                                    {
                                        fullText.AppendLine(page.GetText());
                                        fullText.AppendLine();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                fullText.AppendLine($"[خاتالىق: {pageNum}-بەت] " + ex.Message);
                            }
                        }
                    }
                }
                return fullText.ToString();
            });
        }

        private void BtnLoadPdf_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "PDF Files|*.pdf" };
            if (dlg.ShowDialog() == true)
            {
                _selectedPdfPath = dlg.FileName;
                txtFileName.Text = System.IO.Path.GetFileName(_selectedPdfPath);

                txtStatus.Text = "ھۆججەت مۇۋەپپەقىيەتلىك يۈكلەندى";
                LoadPdfMetadata(_selectedPdfPath);
            }
        }

        private async void LoadPdfMetadata(string path)
        {
            PdfPages.Clear();
            txtStatus.Text = "بەت ئۇچۇرلىرىنى ئوقۇۋاتىدۇ...";

            try
            {
                int totalPages = 0;
                await Task.Run(() =>
                {
                    using (var pdf = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                    {
                        totalPages = pdf.PageCount;
                    }
                });

                for (int i = 1; i <= totalPages; i++)
                {
                    PdfPages.Add(new PdfPageItem { PageNumber = i, IsSelected = false, Thumbnail = null });
                }

                _ = Task.Run(async () =>
                {
                    for (int i = 0; i < totalPages; i++)
                    {
                        var thumb = await RenderPageThumbnailAsync(path, i);
                        int index = i;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (index < PdfPages.Count) PdfPages[index].Thumbnail = thumb;
                        });
                    }
                    Dispatcher.Invoke(() => txtStatus.Text = "تەييار");
                });
            }
            catch (Exception ex)
            {
                txtStatus.Text = "PDF ئوقۇش مەغلۇپ بولدى";
            }
        }

        private async void BtnDownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            string url = txtPdfUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                txtStatus.Text = "ئادرېس خاتالاندى";
                MessageBox.Show("توغرا بولغان تور ئادرېسىنى كىرگۈزۈڭ (http://...)");
                return;
            }

            btnStartOCR.IsEnabled = false;
            txtStatus.Text = "ھۆججەت چۈشۈرۈلۈۋاتىدۇ...";
            pbOCR.Visibility = Visibility.Visible;
            pbOCR.IsIndeterminate = true;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync(url);

                    string fileName = Path.GetFileName(new Uri(url).LocalPath);
                    if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        fileName = $"Web_File_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                    string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
                    File.WriteAllBytes(tempFilePath, data);

                    _selectedPdfPath = tempFilePath;
                    txtFileName.Text = fileName;
                    txtStatus.Text = "چۈشۈرۈش تامام، بەتلەرنى يۈكلەۋاتىدۇ...";

                    LoadPdfMetadata(_selectedPdfPath);
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "چۈشۈرۈش مەغلۇپ بولدى";
                MessageBox.Show("چۈشۈرۈش جەريانىدا خاتالىق كۆرۈلدى: " + ex.Message);
            }
            finally
            {
                pbOCR.Visibility = Visibility.Collapsed;
                btnStartOCR.IsEnabled = true;
            }
        }
        private async Task<BitmapSource> RenderPageThumbnailAsync(string path, int pageIndex)
        {
#pragma warning disable CS8603
            return await Task.Run(() =>
            {
                try
                {
                    var settings = new MagickReadSettings
                    {
                        Density = new Density(72, 72),
                        FrameIndex = (uint?)pageIndex,
                        FrameCount = 1
                    };
                    using (var images = new MagickImageCollection())
                    {
                        images.Read(path, settings);
                        var img = (MagickImage)images[0];
                        img.Resize(100, 0);
                        img.Format = MagickFormat.Bmp;
                        img.Alpha(AlphaOption.Remove);
                        img.BackgroundColor = MagickColors.White;
                        byte[] data = img.ToByteArray();
                        var bitmap = new BitmapImage();
                        using (var ms = new MemoryStream(data))
                        {
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                        }
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch { return null; }
            });
#pragma warning restore CS8603
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResult.Text))
            {
                Clipboard.SetText(txtResult.Text);
                txtStatus.Text = "كۆچۈرۈلدى";
            }
        }

        private void BtnExportTxt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtResult.Text)) return;

            SaveFileDialog saveDlg = new SaveFileDialog { Filter = "Text Files|*.txt", FileName = "OCR_Result.txt" };
            if (saveDlg.ShowDialog() == true)
            {
                File.WriteAllText(saveDlg.FileName, txtResult.Text, Encoding.UTF8);
                txtStatus.Text = "ساقلاندى";
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtResult.Text)) return;

            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "EPUB Book|*.epub",
                FileName = txtFileName.Text.Replace(".pdf", "") + ".epub"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    EpubGenerator.CreateEpub(sfd.FileName, "OCR Book", txtResult.Text);
                    txtStatus.Text = "ئېلېكترونلۇق كىتاب مۇۋەپپەقىيەتلىك ياسالدى";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خاتالىق: " + ex.Message);
                }
            }
        }
    }
}