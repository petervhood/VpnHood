﻿using System.Net.Sockets;
using PacketDotNet;

namespace VpnHood.Core.VpnAdapters.Abstractions;

public class NullVpnAdapter : IVpnAdapter
{
    public event EventHandler<PacketReceivedEventArgs>? PacketReceivedFromInbound;
    public event EventHandler? Disposed;
    public virtual bool Started { get; set; }
    public virtual bool IsDnsServersSupported { get; set; } = true;
    public virtual bool CanProtectSocket { get; set; } = true;
    public virtual bool CanSendPacketToOutbound { get; set; }

    public virtual Task StartCapture(VpnAdapterOptions options, CancellationToken cancellationToken)
    {
        Started = true;
        _ = PacketReceivedFromInbound; //prevent not used warning
        return Task.CompletedTask;
    }

    public virtual Task StopCapture(CancellationToken cancellationToken)
    {
        Started = false;    
        return Task.CompletedTask;
    }

    public virtual void ProtectSocket(Socket socket)
    {
        // nothing
    }

    public virtual void SendPacketToInbound(IPPacket ipPacket)
    {
        // nothing
    }

    public virtual void SendPacketToInbound(IList<IPPacket> packets)
    {
        // nothing
    }

    public virtual void SendPacketToOutbound(IPPacket ipPacket)
    {
        // nothing
    }

    public virtual void SendPacketToOutbound(IList<IPPacket> ipPackets)
    {
        // nothing
    }

    private bool _disposed;
    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopCapture(CancellationToken.None);
        Disposed?.Invoke(this, EventArgs.Empty);
    }
}