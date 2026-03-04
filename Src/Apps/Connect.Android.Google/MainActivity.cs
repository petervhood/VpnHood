namespace VpnHood.App.Connect.Droid.Google;

[Activity(
    MainLauncher = true,
    Label = AppConfigs.AppName)]

// ReSharper disable once UnusedMember.Global
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        var btnConnect = new Button(this)
        {
            Text = "Connect"
        };

        layout.AddView(btnConnect);
        SetContentView(layout);
    }
    
}

/*public class MainActivity : AndroidAppMainActivity
{
    protected override AndroidAppMainActivityHandler CreateMainActivityHandler()
    {
        return new AndroidAppWebViewMainActivityHandler(this, new AndroidMainActivityWebViewOptions());
    }
}*/