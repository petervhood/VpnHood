﻿using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Net;
using Android.Content;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using VpnHood.Client.App.UI;
using Android.Webkit;
using VpnHood.Client.Device.Android;

namespace VpnHood.Client.App.Android
{
    [Activity(Label = "@string/app_name",
        Icon = "@mipmap/ic_launcher",
        Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
        MainLauncher = true, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.UserPortrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.LayoutDirection | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.FontScale | ConfigChanges.Locale | ConfigChanges.Navigation | ConfigChanges.UiMode)]
    public class MainActivity : Activity
    {
        private VpnHoodAppUI _appUi;
        private const int REQUEST_VpnPermission = 10;
        private AndroidDevice Device => (AndroidDevice)AndroidApp.Current.Device;
        
        public WebView WebView { get; private set; }
        public Color BackgroudColor => Resources.GetColor(Resource.Color.colorBackground, null);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // initialize web view
            InitSplashScreen();

            // manage VpnPermission
            Device.OnRequestVpnPermission += Device_OnRequestVpnPermission;

            // Initialize UI
            _appUi = VpnHoodAppUI.Init(Resources.Assets.Open("SPA.zip"));
            InitWebUI();
        }

        private void Device_OnRequestVpnPermission(object sender, System.EventArgs e)
        {
            var intent = VpnService.Prepare(this);
            if (intent == null)
            {
                Device.VpnPermissionGranted();
            }
            else
            {
                StartActivityForResult(intent, REQUEST_VpnPermission);
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == REQUEST_VpnPermission && resultCode == Result.Ok)
                Device.VpnPermissionGranted();
            else
                Device.VpnPermissionRejected();
        }

        protected override void OnDestroy()
        {
            Device.OnRequestVpnPermission -= Device_OnRequestVpnPermission;
            _appUi.Dispose();
            _appUi = null;
            base.OnDestroy();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void InitSplashScreen()
        {
            var imageView = new ImageView(this);
            imageView.SetImageResource(Resource.Mipmap.ic_launcher_round);
            imageView.LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            imageView.SetScaleType(ImageView.ScaleType.CenterInside);
            //imageView.SetBackgroundColor(Color);
            SetContentView(imageView);
        }

        private void InitWebUI()
        {
            var webViewClient = new MyWebViewClient();
            webViewClient.PageLoaded += WebViewClient_PageLoaded;

            WebView = new WebView(this);
            WebView.SetBackgroundColor(BackgroudColor);
            WebView.SetWebViewClient(webViewClient);
            WebView.Settings.JavaScriptEnabled = true;
            WebView.Settings.DomStorageEnabled = true;
            WebView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            WebView.Settings.SetSupportMultipleWindows(true);
            WebView.SetLayerType(LayerType.Hardware, null);
#if DEBUG
            WebView.SetWebContentsDebuggingEnabled(true);
#endif
            WebView.LoadUrl($"{_appUi.Url}?nocache={_appUi.SpaHash}");
        }

        private void WebViewClient_PageLoaded(object sender, System.EventArgs e)
        {
            SetContentView(WebView);
            Window.SetStatusBarColor(BackgroudColor);
        }

        public override void OnBackPressed()
        {
            if (WebView.CanGoBack())
                WebView.GoBack();
            else
                base.OnBackPressed();
        }
    }
}