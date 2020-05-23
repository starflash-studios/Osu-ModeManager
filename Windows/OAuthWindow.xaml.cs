using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace OsuModeManager {
    public partial class OAuthWindow {
        public static TaskCompletionSource<string> OAuthCodeSource;

        public OAuthWindow() {
            InitializeComponent();
        }

        public static readonly Uri Redirect = new Uri("about:blank");

        public async Task<string> GetOAuthToken(GitHubClient Client, string ClientID, string ClientSecret) {
            if (OAuthCodeSource != null) {
                Debug.WriteLine("Please do not request multiple tokens at once!", "Warning"); 
                return string.Empty;
            }

            OauthLoginRequest Request = new OauthLoginRequest(ClientID) {
                Scopes = { "user", "notifications" }
            };

            Uri OAuthLoginUrl = Client.Oauth.GetGitHubLoginUrl(Request);
            Browser.Address = OAuthLoginUrl.AbsoluteUri;
            //Browser.Navigate(OAuthLoginUrl);

            OAuthCodeSource = new TaskCompletionSource<string>();
            string Code = await OAuthCodeSource.Task;
            Debug.WriteLine("Get code: " + Code);

            if (Code.IsNullOrEmpty()) {
                OAuthCodeSource = null;
                return string.Empty;
            }

            string OAuthToken = await GenerateTokenFromOAuth(Client, ClientID, ClientSecret, Code);

            OAuthCodeSource = null;
            return OAuthToken;
        }

        public static async Task<string> GenerateTokenFromOAuth(GitHubClient Client, string ClientID, string ClientSecret, string Code) {
            OauthTokenRequest Request = new OauthTokenRequest(ClientID, ClientSecret, Code);
            OauthToken Token = await Client.Oauth.CreateAccessToken(Request);

            return Token.AccessToken;
        }

        //https://github.com/starflash-studios/Osu-ModeManager?code=67d7e5fa77a80370145f
        public const string OAuthCodeDecoder = @"(.+?)\?code=(?<OAuthCode>.+?)(?:\?.*?|$)";
        void Browser_LoadingStateChanged(object Sender, CefSharp.LoadingStateChangedEventArgs E) {
            if (OAuthCodeSource == null) { return; }
            string CurrentAddress = E.Browser.FocusedFrame.Url;

            Match CodeMatch = Regex.Match(CurrentAddress, OAuthCodeDecoder);
            if (CodeMatch.Success) {
                Group CodeGroup = CodeMatch.Groups["OAuthCode"];
                if (CodeGroup.Success) {
                    OAuthCodeSource.TrySetResult(CodeGroup.Value);
                }
            }

            Dispatcher.Invoke(() => {
                string BrowserTitle = Browser.Title;
                if (!BrowserTitle.IsNullOrEmpty()) {
                    Title = BrowserTitle;
                }
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        void Browser_Loaded(object Sender, System.Windows.RoutedEventArgs E) => Dispatcher.Invoke(() => Browser.Focus(), System.Windows.Threading.DispatcherPriority.Input);

        void MetroWindow_Closing(object Sender, System.ComponentModel.CancelEventArgs E) => OAuthCodeSource?.TrySetResult(null);
    }
}
