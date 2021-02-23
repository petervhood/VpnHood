﻿using System;

namespace VpnHood.Server.App
{
    class AppSettings
    {
        public Uri RestBaseUrl { get; set; }
        public string RestAuthHeader { get; set; }
        public string RestCertificateThumbprint { get; set; }
        public ushort Port { get; set; } = 443;
        public bool IsAnonymousTrackerEnabled { get; set; } = true;
        public string SslCertificatesPassword { get; set; }
        public bool IsDiagnoseMode { get; set; }
    }
}
