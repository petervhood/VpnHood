﻿using VpnHood.Core.Adapters.Abstractions;
using VpnHood.Core.Client.VpnServices.Abstractions;

namespace VpnHood.Core.Client.VpnServices.Host;

public interface IVpnServiceHandler
{
    IVpnAdapter CreateAdapter();
    void ShowNotification(ConnectionInfo connectionInfo);
    void StopNotification();
    void StopSelf();
}