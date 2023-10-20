using Newtonsoft.Json.Linq;

namespace SlowUdpPipe.MauiClient.Data;

internal record NotificationTapEvent(long Timestamp, int NotificationId, bool IsDismissed, JToken? Data);
