﻿using Ga4.Trackers;
using VpnHood.Core.Client.Device.Adapters;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Host;
using VpnHood.Test.Providers;

namespace VpnHood.Test.Device;

public class TestVpnService
    : IVpnServiceHandler, IAsyncDisposable
{
    private readonly Func<IVpnAdapter> _vpnAdapterFactory;
    private readonly ITracker? _tracker;
    private readonly VpnServiceHost _vpnServiceHost;
    public bool IsDisposed { get; private set; }

    // config folder should be read from static place in read environment, because service can be started independently
    public TestVpnService(
        string configFolder,
        Func<IVpnAdapter> vpnAdapterFactory,
        ITracker? tracker)
    {
        _vpnAdapterFactory = vpnAdapterFactory;
        _tracker = tracker;
        _vpnServiceHost = new VpnServiceHost(configFolder, this, new TestSocketFactory(), withLogger: false);
    }

    // it is not async to simulate real environment
    public void OnConnect()
    {
        _vpnServiceHost.Connect();
    }

    public void OnStop()
    {
        _vpnServiceHost.Disconnect();
    }

    public ITracker? CreateTracker()
    {
        return _tracker;
    }

    public IVpnAdapter CreateAdapter()
    {
        return _vpnAdapterFactory();
    }

    public void ShowNotification(ConnectionInfo connectionInfo)
    {
    }

    public void StopNotification()
    {
    }

    public void StopSelf()
    {
        _ = DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;
        await _vpnServiceHost.DisposeAsync();
        IsDisposed = true;
    }
}