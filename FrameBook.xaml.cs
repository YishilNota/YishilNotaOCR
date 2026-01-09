using Microsoft.Web.WebView2.Core;
using System.Windows.Input;

namespace YishilNotaOCR
{
    public partial class FrameBook : System.Windows.Controls.UserControl
    {
        double zoom = 1.0;
        private const string BOOL_LINK = "https://openlibrary.org/";

        public FrameBook()
        {
            InitializeComponent();
            Loaded += FrameBook_Loaded;
        }

        private async void FrameBook_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await Web.EnsureCoreWebView2Async();

            Web.Source = new System.Uri(BOOL_LINK);

            Web.NavigationCompleted += async (s, args) =>
            {
                if (args.IsSuccess)
                {
                    string script = @"
                        document.addEventListener('click', function(e) {
                        var target = e.target.closest('a');
                        if (target && (target.href.includes('.pdf') || target.innerText.includes('چۈشۈرۈش'))) {
                            window.chrome.webview.postMessage(target.href);
                        }
                        }, true);
                        ";

                    await Web.CoreWebView2.ExecuteScriptAsync(script);
                }
            };

            Web.CoreWebView2.WebMessageReceived += (s, args) =>
            {
                string capturedUrl = args.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(capturedUrl))
                {
                    System.Windows.Clipboard.SetText(capturedUrl);
                    System.Windows.MessageBox.Show("PDF ئۇلىنىشى كۆچۈرۈلدى:\n" + capturedUrl);
                }
            };
        }

        private void ZoomIn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            zoom += 0.1;
            Web.ZoomFactor = zoom;
        }

        private void ZoomOut_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            zoom -= 0.1;
            if (zoom < 0.5) zoom = 0.5;
            Web.ZoomFactor = zoom;
        }

        private void NavigateToUrl()
        {
            string url = txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }
                Web.Source = new System.Uri(url);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("ئادرېس خاتالاندى: " + ex.Message);
            }
        }
        private void BtnGo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigateToUrl();
        }

        private void TxtUrl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                NavigateToUrl();
                Keyboard.ClearFocus();
            }
        }

        private void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            string downloadUrl = e.DownloadOperation.Uri;

            if (downloadUrl.ToLower().EndsWith(".pdf") || e.DownloadOperation.MimeType == "application/pdf")
            {
                e.Cancel = true;
                System.Windows.Clipboard.SetText(downloadUrl);
                System.Windows.MessageBox.Show("PDF ئادرېسى كۆچۈرۈلدى، OCR بېتىگە چاپلىسىڭىز بولىدۇ:\n" + downloadUrl);
            }
        }
    }
}
