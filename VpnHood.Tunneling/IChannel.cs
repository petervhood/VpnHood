﻿using System;

namespace VpnHood.Tunneling
{
    public interface IChannel : IDisposable
    {
        bool Connected { get; }
        DateTime LastActivityTime { get; }
        long SentByteCount { get; }
        long ReceivedByteCount { get; }
        void Start();
        event EventHandler OnFinished;
    }
}
