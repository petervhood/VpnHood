using Android.Runtime;
using VpnHood.App.Client;
using VpnHood.AppLib;
using VpnHood.AppLib.Droid.Common;
using VpnHood.AppLib.Droid.Common.Constants;
using VpnHood.AppLib.Services.Ads;

namespace VpnHood.App.Connect.Droid.Google;

[Application(
    Label = AppConfigs.AppName,
    Icon = AndroidAppConstants.Icon,
    Banner = AndroidAppConstants.Banner,
    NetworkSecurityConfig = AndroidAppConstants.NetworkSecurityConfig,
    SupportsRtl = AndroidAppConstants.SupportsRtl,
    Debuggable = AppConfigs.IsDebugMode,
    AllowBackup = AndroidAppConstants.AllowBackup)]
public class App(IntPtr javaReference, JniHandleOwnership transfer)
    : Application(javaReference, transfer)
{
    private static AppOptions CreateAppOptions()
    {
        // load app configs
        var appConfigs = AppConfigs.Load();
        var storageFolderPath = AppOptions.BuildStorageFolderPath("VpnHoodConnect");

        // load app settings and resources
        var resources = ConnectAppResources.Resources;
        resources.Strings.AppName = AppConfigs.AppName;

        return new AppOptions(appId: appConfigs.AppId, "VpnHoodConnect", AppConfigs.IsDebugMode) {
            StorageFolderPath = storageFolderPath,
            AccessKeys = appConfigs.DefaultAccessKey != null ? [appConfigs.DefaultAccessKey] : [],
            Resources = resources,
            UiName = "VpnHoodConnect",
            IsAddAccessKeySupported = false,
            AccountProvider = null,
            AdProviderItems = [],
            AllowEndPointTracker = false,
            AdjustForSystemBars = false,
            PremiumFeatures = ConnectAppResources.PremiumFeatures,
            WebUiPort = appConfigs.WebUiPort,
            AllowRecommendUserReviewByServer = false,
            AdOptions = new AppAdOptions {
                PreloadAd = false,
                RejectAdBlocker = false
            },
        };
    }

    public override void OnCreate()
    {
        // init app
        VpnHoodAndroidApp.Init(CreateAppOptions);
        base.OnCreate();
    }

    public override void OnTerminate()
    {
        if (VpnHoodAndroidApp.IsInit)
            VpnHoodAndroidApp.Instance.Dispose();
    }

    /*private static AppAdProviderItem[] CreateAppAdProviderItems(AppConfigs appConfigs)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var items = new List<AppAdProviderItem>();

        // interstitial
        items.Add(new AppAdProviderItem {
            AdProvider = AdMobInterstitialAdProvider.Create(appConfigs.AdMobInterstitialAdUnitId),
            ExcludeCountryCodes = ["CN", "RU"],
            ProviderName = "AdMob"
        });

        // rewarded ad
        items.Add(new AppAdProviderItem {
            AdProvider = AdMobRewardedAdProvider.Create(appConfigs.AdMobRewardedAdUnitId),
            ExcludeCountryCodes = ["CN", "RU"],
            ProviderName = "AdMob-Rewarded"
        });

        items.Add(new AppAdProviderItem {
            AdProvider = new InternalInAdProvider(),
            ProviderName = "InternalAd",
            IsFallback = true
        });


        /*var initializeTimeout = TimeSpan.FromSeconds(5);
        if (InMobiAdProvider.IsAndroidVersionSupported)
            items.Add(new AppAdProviderItem {
                AdProvider = InMobiAdProvider.Create(
                    appConfigs.InmobiAccountId, appConfigs.InmobiPlacementId, initializeTimeout, appConfigs.InmobiIsDebugMode),
                ExcludeCountryCodes = ["CN", "RU"],
                ProviderName = "InMobi"
            });

        // if (ChartboostAdProvider.IsAndroidVersionSupported)
        //     items.Add(new AppAdProviderItem {
        //         AdProvider = ChartboostAdProvider.Create(appConfigs.ChartboostAppId, appConfigs.ChartboostAppSignature,
        //             appConfigs.ChartboostAdLocation, initializeTimeout),
        //         ExcludeCountryCodes = ["IR", "CN"],
        //         ProviderName = "Chartboost"
        //     });

        return items.ToArray();
    }*/

    /*private static IAppAccountProvider? CreateAppAccountProvider(AppConfigs appConfigs, string storageFolderPath)
    {
        try {
            var authenticationExternalProvider = new GooglePlayAuthenticationProvider(appConfigs.GoogleSignInClientId);
            var authenticationProvider = new StoreAuthenticationProvider(storageFolderPath,
                new Uri(appConfigs.StoreBaseUri),
                appConfigs.StoreAppId, authenticationExternalProvider,
                ignoreSslVerification: appConfigs.StoreIgnoreSslVerification);

            var googlePlayBillingProvider = TryCreateBillingClient(authenticationProvider, appConfigs);
            var accountProvider = new StoreAccountProvider(authenticationProvider, googlePlayBillingProvider,
                appConfigs.StoreAppId);

            return accountProvider;
        }
        catch (Exception ex) {
            VhLogger.Instance.LogError(ex, "Could not create AppAccountService.");
            return null;
        }
    }*/

    /*private static IAppBillingProvider? TryCreateBillingClient(
        IAppAuthenticationProvider authenticationProvider, AppConfigs appConfigs)
    {
        try {
            return new GooglePlayBillingProvider(authenticationProvider, appConfigs.GooglePlayProductIds);
        }
        catch (Exception ex) {
            VhLogger.Instance.LogError(ex, "Could not create GooglePlayBillingProvider.");
            return null;
        }
    }*/
}