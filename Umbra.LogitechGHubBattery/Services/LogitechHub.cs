using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using Umbra.Common;
using Websocket.Client;

namespace Umbra.LogitechGHubBattery.Services;

public class DeviceInfo
{
    [JsonProperty("id")] public string Id { get; set; } = "";
    [JsonProperty("connectionType")] public string ConnectionType { get; set; } = "";
    [JsonProperty("hasWirelessInterface")] public bool HasWirelessInterface { get; set; }
    [JsonProperty("displayConnectionType")] public string DisplayConnectionType { get; set; } = "";
    [JsonProperty("deviceType")] public string DeviceType { get; set; } = "";
    [JsonProperty("displayName")] public string DisplayName { get; set; } = "";
    [JsonProperty("deviceUnitId")] public string DeviceUnitId { get; set; } = "";
}

public class BatteryState
{
    [JsonProperty("percentage")] public double Percentage { get; set; }
}

[Service]
public class LogitechHub : IDisposable
{
    private const string WsUrl = "ws://localhost:9010";
    private readonly WebsocketClient? _webSocket;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<string, object> _cacheWrapper = new();

    public LogitechHub()
    {
        _webSocket = new(new Uri(WsUrl), () =>
            {
                var client = new ClientWebSocket();
                client.Options.AddSubProtocol("json");
                return client;
            }
        );
        _webSocket.MessageReceived.Subscribe(OnMessage, OnError);
        _webSocket.ReconnectTimeout = TimeSpan.FromSeconds(10);
        _webSocket.ErrorReconnectTimeout = TimeSpan.FromSeconds(10);
        _webSocket.LostReconnectTimeout = TimeSpan.FromSeconds(10);
        _webSocket.Start().Wait();
        FetchDeviceInfos().Wait();
    }

    private void OnMessage(ResponseMessage msg) {
        if (msg.Text == null) return;
        try {
            dynamic? json = JsonConvert.DeserializeObject(msg.Text);
            string? id = json?.path;

            if (id != null && _pendingRequests.Remove(id, out var tcs)) {
                tcs.TrySetResult(msg.Text);
            }
        } catch (Exception ex) {
            OnError(ex);
        }
    }

    private void OnError(Exception exception) {
        Logger.Error($"Error: {exception}");
    }

    private T UpdateCache<T>(string key, T value)
    {
        if (!_cacheWrapper.TryGetValue(key, out var obj))
        {
            _cacheWrapper[key] = new CachedValue<T>(value);
            return value;
        }
        if (obj is CachedValue<T> cached)
        {
            cached.Value = value;
            cached.LastFetch = DateTime.UtcNow;
        }
        
        return value;
    }

    private CachedValue<T> GetCache<T>(string key, T value) where T : class?
    {
        if (!_cacheWrapper.TryGetValue(key, out var obj))
        {
            return (CachedValue<T>) (_cacheWrapper[key] = new CachedValue<T>(value));
        }
        
        return (CachedValue<T>) obj;
    }
    
    public List<DeviceInfo> GetDeviceInfos()
    {
        var cached = GetCache<List<DeviceInfo>>("DeviceInfos", []);

        if (DateTime.UtcNow - cached.LastFetch > _cacheDuration)
            _ = FetchDeviceInfos();

        return cached.Value;
    }

    public BatteryState? GetBatteryState(string device)
    {
        var cached = GetCache<BatteryState?>($"BatteryState_{device}", null);

        if (DateTime.UtcNow - cached.LastFetch > _cacheDuration)
            _ = FetchBatteryState(device);

        return cached.Value;
    }
    

    private async Task<List<DeviceInfo>> FetchDeviceInfos()
    {
        var resp = await GetAsync<DeviceListPayload>("/devices/list");
        var fetched = resp?.DeviceInfos.FindAll(d =>
            (d.HasWirelessInterface ||
                d.ConnectionType == "WIRELESS" ||
                d.DisplayConnectionType == "LIGHTSPEED") &&
            d.DeviceType != "CHARGE_PAD") ?? [];
        
        return UpdateCache("DeviceInfos", fetched);
    }

    private async Task<BatteryState?> FetchBatteryState(string device)
    {
        var resp = await GetAsync<BatteryState>($"/battery/{device}/state");
        return UpdateCache($"BatteryState_{device}", resp);
    }

    private async Task<T?> GetAsync<T>(string path) where T : class
    {
        if (_pendingRequests.ContainsKey(path))
            return null;

        var tcs = new TaskCompletionSource<string>();
        _pendingRequests.TryAdd(path, tcs);

        var request = new { path, verb = "GET" };
        var json = JsonConvert.SerializeObject(request);

        _webSocket?.Send(json);

        using var timeoutCts = new CancellationTokenSource(5000);
        await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
        {
            try
            {
                var responseJson = await tcs.Task;
                _pendingRequests.Remove(path, out _);
                var response = JsonConvert.DeserializeObject<WebSocketResponse<T>>(responseJson);
                return response?.Payload;
            }
            catch (TaskCanceledException)
            {
                _pendingRequests.Remove(path, out _);
                return null;
            }
        }
    }

    [WhenFrameworkDisposing]
    public void Dispose()
    {
        _webSocket?.Dispose();
    }
    
    private class CachedValue<T>(T value)
    {
        public T Value { get; set; } = value;
        public DateTime LastFetch { get; set; } = DateTime.MinValue;
    }

    private class WebSocketResponse<T>
    {
        [JsonProperty("verb")] public string Verb { get; set; } = "";
        [JsonProperty("path")] public string Path { get; set; } = "";
        [JsonProperty("msgId")] public string MsgId { get; set; } = "";
        [JsonProperty("payload")] public T? Payload { get; set; }
    }

    private class DeviceListPayload
    {
        [JsonProperty("deviceInfos")] public List<DeviceInfo> DeviceInfos { get; set; } = new();
    }
}
