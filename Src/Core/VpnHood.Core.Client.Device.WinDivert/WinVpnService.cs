﻿using VpnHood.Core.Client.Device.Adapters;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Host;
using VpnHood.Core.Common.Sockets;

namespace VpnHood.Core.Client.Device.WinDivert;

public class WinVpnService : IVpnServiceHandler, IAsyncDisposable
{
    private readonly VpnServiceHost _vpnServiceHost;
    public bool IsDisposed { get; private set; }

    public WinVpnService(
        string configFolder)
    {
        _vpnServiceHost = new VpnServiceHost(configFolder, this, new SocketFactory(), withLogger: false);
    }

    public void OnConnect()
    {
        _vpnServiceHost.Connect();
    }

    public void OnDisconnect()
    {
        _vpnServiceHost.Disconnect();
    }

    public IVpnAdapter CreateAdapter()
    {
        return new WinDivertVpnAdapter();
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