﻿using System;
using System.IO;
using VpnHood.Common.Trackers;
using VpnHood.Server.SystemInformation;
using VpnHood.Tunneling.Factory;

namespace VpnHood.Server;

public class ServerOptions
{
    public SocketFactory SocketFactory { get; set; } = new();
    public ITracker? Tracker { get; set; }
    public ISystemInfoProvider? SystemInfoProvider { get; set; }
    public bool AutoDisposeAccessServer { get; set; } = true;
    public TimeSpan ConfigureInterval { get; set; } = TimeSpan.FromSeconds(60);
    public string StoragePath { get; set; } = Directory.GetCurrentDirectory();
    public bool PublicIpDiscovery { get; set; } = true;
    
    public int MaxTcpConnectWaitCount { get; set; } = 500;
    public int MaxTcpChannelCount { get; set; } = 1000;
    public TimeSpan TcpConnectTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public int? MinCompletionPortThreads { get; set; }
    public int? MaxCompletionPortThreads { get; set; }
}