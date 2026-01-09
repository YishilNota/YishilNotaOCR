using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace YishilNotaOCR
{
    public partial class MainWindow : Window
    {
        private readonly FrameBook _frameBook = new FrameBook();
        private readonly FrameCreateQR _frameCreateQR = new FrameCreateQR();
        private readonly FrameOCRPdf _frameOCRPdf = new FrameOCRPdf();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                ListViewMenu.SelectedIndex = 0;
                NavigateToPage(0);
            };
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    this.DragMove();
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                               ? WindowState.Normal
                               : WindowState.Maximized;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewMenu == null || GridContent == null) return;

            int index = ListViewMenu.SelectedIndex;
            NavigateToPage(index);
        }

        private void NavigateToPage(int index)
        {
            GridContent.Children.Clear();

            switch (index)
            {
                case 0:
                    GridContent.Children.Add(_frameCreateQR);
                    break;
                case 1:
                    GridContent.Children.Add(_frameOCRPdf);
                    break;
                case 2:
                    GridContent.Children.Add(_frameBook);
                    break;
            }
        }

        private async void Logo_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var container = new StackPanel { Margin = new Thickness(30), Width = 420 };

            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
            header.Children.Add(new PackIcon { Kind = PackIconKind.Leaf, Width = 32, Height = 32 });
            var titleStack = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
            titleStack.Children.Add(new TextBlock
            {
                Text = "OCR يېشىل نوتا",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#2C2C2C")
            });
            titleStack.Children.Add(new TextBlock { Text = "YishilNotaOCR v1.0.0", FontSize = 12, Foreground = Brushes.Gray });
            header.Children.Add(titleStack);
            container.Children.Add(header);

            container.Children.Add(new TextBlock { Text = "ئىشلىتىلگەن تېخنىكىلار (Dependencies):", FontWeight = FontWeights.SemiBold, FlowDirection = FlowDirection.RightToLeft, Margin = new Thickness(0, 0, 0, 10), FontSize = 13 });
            var chipPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };

            var libs = new[] {
                "MaterialDesign(MIT)",
                "WebView2",
                "PdfiumViewer (Apache 2.0)",
                "Tesseract",
                "ukij.traineddata"
            };

            foreach (var lib in libs)
            {
                _ = chipPanel.Children.Add(new Chip
                {
                    Content = lib,
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = (Brush)new BrushConverter().ConvertFromString("#F5F5F5"),
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#616161")

                });
            }
            container.Children.Add(chipPanel);

            var disclaimerBorder = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#FAFAFA"), // 极浅灰
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#EEEEEE"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 25)
            };

            var disclaimerContent = new StackPanel();
            disclaimerContent.Children.Add(new TextBlock
            {
                Text = "مەسئۇلىيەتنى رەت قىلىش باياناتى",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(0, 0, 0, 8)
            });
            disclaimerContent.Children.Add(new TextBlock
            {
                Text = "بۇ يۇمشاق دېتال پەقەت ئۆگىنىش ۋە تەتقىقات ئۈچۈن لايىھەلەنگەن. ئىشلەتكۈچىلەر چۈشۈرگەن ھۆججەتلەرنىڭ نەشر ھوقۇقىغا ئۆزى مەسئۇل بولىدۇ.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                LineHeight = 20,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#424242"),
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(0, 0, 0, 12)
            });

            disclaimerContent.Children.Add(new TextBlock
            {
                Text = "Disclaimer: This software is for educational purposes only. Users are responsible for copyright compliance. The developer assumes no legal liability for misuse.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray
            });

            disclaimerBorder.Child = disclaimerContent;
            container.Children.Add(disclaimerBorder);

            var closeBtn = new Button
            {
                Content = "OK",
                Command = DialogHost.CloseDialogCommand,
                Style = (Style)FindResource("MaterialDesignFlatMidBgButton"),
                Background = (Brush)new BrushConverter().ConvertFromString("#4CAF50"),
                Foreground = Brushes.White,
                Height = 40,
            };
            container.Children.Add(closeBtn);

            await DialogHost.Show(container, "RootDialog");
        }
    }
}