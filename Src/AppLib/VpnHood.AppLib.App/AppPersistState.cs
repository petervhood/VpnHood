﻿using System.Text;
using System.Text.Json;
using VpnHood.Core.Common.ApiClients;
using VpnHood.Core.Common.Logging;
using VpnHood.Core.Common.Utils;

namespace VpnHood.AppLib;

internal class AppPersistState(string filePath)
{
    private readonly object _saveLock = new();
    private readonly Data _data = JsonUtils.TryDeserializeFile<Data>(filePath, logger: VhLogger.Instance) ?? new Data();

    private class Data
    {
        public ApiError? ApiError { get; set; }
        public DateTime UpdateIgnoreTime { get; set; } = DateTime.MinValue;
        public string? ClientCountryCode { get; set; }
        public string? ClientCountryCodeByServer { get; set; }
        public bool HasDisconnectedByUser { get; set; }
        public DateTime? ConnectRequestTime { get; set; }
    }

    public ApiError? LastError {
        get => _data.ApiError;
        set {
            if (JsonUtils.JsonEquals(_data.ApiError, value))
                return;

            _data.ApiError = value;
            Save();
        }
    }

    public DateTime UpdateIgnoreTime {
        get => _data.UpdateIgnoreTime;
        set {
            if (_data.UpdateIgnoreTime == value)
                return;

            _data.UpdateIgnoreTime = value;
            Save();
        }
    }

    public string? ClientCountryCode {
        get => _data.ClientCountryCode;
        set {
            if (string.Equals(_data.ClientCountryCode, value, StringComparison.OrdinalIgnoreCase))
                return;

            // set country code and its name
            _data.ClientCountryCode = value?.ToUpper();
            Save();
        }
    }

    public string? ClientCountryCodeByServer {
        get => _data.ClientCountryCodeByServer;
        set {
            if (string.Equals(_data.ClientCountryCodeByServer, value, StringComparison.OrdinalIgnoreCase))
                return;

            // set country code and its name
            _data.ClientCountryCodeByServer = value?.ToUpper();
            Save();
        }
    }

    public bool HasDisconnectedByUser
    {
        get => _data.HasDisconnectedByUser;
        set
        {
            if (_data.HasDisconnectedByUser == value)
                return;

            _data.HasDisconnectedByUser = value;
            Save();
        }
    }

    public DateTime? ConnectRequestTime
    {
        get => _data.ConnectRequestTime;
        set
        {
            if (_data.ConnectRequestTime == value)
                return;

            _data.ConnectRequestTime = value;
            Save();
        }
    }

    internal static AppPersistState Load(string filePath)
    {
        var ret = new AppPersistState(filePath);
        return ret;
    }

    private void Save()
    {
        lock (_saveLock) {
            if (string.IsNullOrEmpty(filePath))
                return; // loading

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
    }
}