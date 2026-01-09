using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Tesseract;

namespace YishilNotaOCR
{
    public partial class FrameCreateQR : UserControl
    {
        private string _imagePath = string.Empty;

        public FrameCreateQR()
        {
            InitializeComponent();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "رەسىم ھۆججىتى|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "رەسىم تاللاڭ"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadImage(dlg.FileName);
            }
        }

        private void LoadImage(string path)
        {
            try
            {
                if (!File.Exists(path)) return;

                _imagePath = path;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();

                imgPreview.Source = bitmap;
                textHint.Visibility = Visibility.Collapsed;
                txtResult.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("رەسىمنى يۈكلىيەلمىدى: " + ex.Message);
            }
        }


        private async void BtnOcr_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_imagePath))
            {
                MessageBox.Show("ئالدى بىلەن رەسىم يۈكلەڭ");
                return;
            }

            var btn = sender as Button;
            btn.IsEnabled = false;
            txtResult.Text = "بىر تەرەپ قىلىۋاتىدۇ، سەل كۈتۈڭ...";

            try
            {
                string result = await Task.Run(() => RunOcrProcess());

                if (string.IsNullOrWhiteSpace(result))
                {
                    txtResult.Text = "تېكىستنى تونۇيالمىدى";
                }
                else
                {
                    txtResult.Text = result.Trim();
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = $"خاتالىق كۆرۈلدى: {ex.Message}";
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private string RunOcrProcess()
        {
            string tessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            if (!Directory.Exists(tessPath))
                throw new DirectoryNotFoundException("tessdata مۇندەرىجىسى تېپىلمىدى");

            using var engine = new TesseractEngine(tessPath, "ukij", EngineMode.Default);

            using var pix = Pix.LoadFromFile(_imagePath);
            using var page = engine.Process(pix);

            return page.GetText();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtResult.Text))
            {
                Clipboard.SetText(txtResult.Text);
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    LoadImage(files[0]);
                }
            }
        }

        private void ImageBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private bool IsImageFile(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp";
        }


        private void ImageBorder_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;

            string file = files[0];

            if (!IsImageFile(file))
                return;

            LoadImage(file);
        }

    }
}