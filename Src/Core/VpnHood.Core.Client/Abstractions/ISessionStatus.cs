﻿using VpnHood.Core.Client.ConnectorServices;
using VpnHood.Core.Common.Messaging;

namespace VpnHood.Core.Client.Abstractions;

public interface ISessionStatus
{
    ConnectorStat ConnectorStat { get; }
    Traffic Speed { get; }
    Traffic SessionTraffic { get; }
    Traffic CycleTraffic { get; }
    Traffic TotalTraffic { get; }
    int TcpTunnelledCount { get; }
    int TcpPassthruCount { get; }
    int DatagramChannelCount { get; }
    bool IsUdpMode { get; }
    bool IsWaitingForAd { get; }
    bool CanExtendByRewardedAd { get; }
    long SessionMaxTraffic { get; }
    DateTime? SessionExpirationTime { get; }
    int? ActiveClientCount { get; }
}