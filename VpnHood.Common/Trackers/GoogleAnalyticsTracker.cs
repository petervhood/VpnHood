﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VpnHood.Common.Trackers;

public class GoogleAnalyticsTracker : ITracker
{
    private static readonly Lazy<HttpClient> HttpClientLazy = new(() => new HttpClient());
    private static HttpClient HttpClient => HttpClientLazy.Value;

    public GoogleAnalyticsTracker(string trackId, string anonyClientId, string appName, string appVersion,
        string? userAgent = null, string? screenRes = null, string? culture = null)
    {
        TrackId = trackId;
        AnonyClientId = anonyClientId;
        UserAgent = userAgent ?? Environment.OSVersion.ToString().Replace(" ", "");
        AppName = appName;
        AppVersion = appVersion;
        ScreenRes = screenRes;
        Culture = culture;
    }

    public string TrackId { get; set; }
    public string UserAgent { get; set; }
    public string AnonyClientId { get; set; }
    public string? ScreenRes { get; set; }
    public string AppName { get; set; }
    public string AppVersion { get; set; }
    public string? Culture { get; set; }
    public bool IsEnabled { get; set; } = true;
    public IPAddress? IpAddress { get; set; }

    public Task<bool> TrackEvent(string category, string action, string? label = null, int? value = null)
    {
        return Track("event", category, action, label, value);
    }

    // ReSharper disable once UnusedMember.Global
    public Task<bool> TrackPageView(string category, string action, string? label = null, int? value = null)
    {
        return Track("pageview", category, action, label, value);
    }


    public Task<bool> Track(string type, string category, string action, string? label, int? value)
    {
        var data = new TrackData(type, category, action)
        {
            Label = label,
            Value = value
        };

        return Track(data);
    }

    public Task<bool> Track(TrackData trackData)
    {
        var tracks = new[] {trackData};
        return Track(tracks);
    }

    public async Task<bool> Track(TrackData[] tracks)
    {
        if (!IsEnabled) return false;

        var ret = true;
        var content = "";

        if (tracks.Length == 0) throw new ArgumentException("array can not be empty! ", nameof(tracks));
        for (var i = 0; i < tracks.Length; i++)
        {
            var trackData = tracks[i];

            content += GetPostDataString(trackData) + "\r\n";
            if ((i + 1) % 20 == 0 || i == tracks.Length - 1)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.google-analytics.com/batch");
                requestMessage.Headers.Add("User-Agent", UserAgent);
                requestMessage.Content = new StringContent(content.Trim(), Encoding.UTF8);
                try
                {
                    var res = await HttpClient.SendAsync(requestMessage);
                    ret &= res.StatusCode == HttpStatusCode.OK;
                }
                catch
                {
                    ret = false;
                }

                content = "";
            }
        }

        return ret;
    }

    private string GetPostDataString(TrackData trackData)
    {
        // the request body we want to send
        var postData = new Dictionary<string, string>
        {
            {"v", "1"},
            {"tid", TrackId},
            {"cid", AnonyClientId},
            {"t", trackData.Type},
            {"an", AppName},
            {"av", AppVersion}
        };
        if (!string.IsNullOrEmpty(ScreenRes)) postData.Add("sr", ScreenRes);
        if (!string.IsNullOrEmpty(Culture)) postData.Add("ul", Culture);
        if (!string.IsNullOrEmpty(trackData.Category)) postData.Add("ec", trackData.Category);
        if (!string.IsNullOrEmpty(trackData.Action)) postData.Add("ea", trackData.Action);
        if (!string.IsNullOrEmpty(trackData.PageUrl)) postData.Add("dl", trackData.PageUrl);
        if (!string.IsNullOrEmpty(trackData.PageTitle)) postData.Add("dt", trackData.PageTitle);
        if (IpAddress!= null) postData.Add("uip", IpAddress.ToString());
        
        //label and value
        if (!string.IsNullOrEmpty(trackData.Label)) postData.Add("el", trackData.Label);
        if (trackData.Value.HasValue) postData.Add("ev", trackData.Value.ToString());

        //create post string
        var ret = string.Join('&', postData.Select(x => x.Key + "=" + x.Value));
        return ret;
    }

}