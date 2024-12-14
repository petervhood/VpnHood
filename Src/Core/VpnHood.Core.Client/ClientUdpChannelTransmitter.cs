﻿using System.Net;
using System.Net.Sockets;
using VpnHood.Core.Tunneling.Channels;

namespace VpnHood.Core.Client;

public class ClientUdpChannelTransmitter(UdpChannel udpChannel, UdpClient udpClient, byte[] serverKey)
    : UdpChannelTransmitter(udpClient, serverKey)
{
    protected override void OnReceiveData(ulong sessionId, IPEndPoint remoteEndPoint, long channelCryptorPosition,
        byte[] buffer, int bufferIndex)
    {
        udpChannel.OnReceiveData(channelCryptorPosition, buffer, bufferIndex);
    }
}