using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace NtpTime
{
    /// <summary>
    /// Get time from microsoft ntp server
    /// time.windows.com
    /// </summary>
    public class MSTime : INtpTime
    {
        private static DateTime _utcStampBegin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 网络当前时间的UTC时间戳，毫秒级
        /// </summary>
        private ulong _networkUtcStamp;

        /// <summary>
        /// 同步时间完毕时，本地unity的时间（使用Time.realtimeSinceStartup进行记录，毫秒级）
        /// </summary>
        private ulong _syncLocalGameTime;

        private bool _inited = false;

        public DateTime NetworkUtcTime => _inited
            ? _utcStampBegin.AddMilliseconds(_networkUtcStamp).AddMilliseconds(_syncLocalGameTime)
            : DateTime.UtcNow;

        public DateTime NetworkLocalTime => _inited
            ? TimeZone.CurrentTimeZone.ToLocalTime(NetworkUtcTime)
            : DateTime.Now;

        public void Init()
        {
            _AsyncInit();
        }

        private async void _AsyncInit()
        {
            _inited = false;
            DateTime serverUtcTime = await _GetMSTime();
            _networkUtcStamp = (ulong)(serverUtcTime - _utcStampBegin).TotalMilliseconds;
            _syncLocalGameTime = (ulong)(Time.realtimeSinceStartup * 1000);
            _inited = true;
            Debug.Log($"Microsoft NTP time is {NetworkUtcTime}(UTC), {NetworkLocalTime}(local)");
        }

        private async Task<DateTime> _GetMSTime()
        {
            return await Task<DateTime>.Run(() => _GetMSTime("time.windows.com"));
        }

        private DateTime _GetMSTime(string ntpServer)
        {
            // 解析地址
            IPAddress[] address = Dns.GetHostEntry(ntpServer).AddressList;

            if (address == null || address.Length == 0)
            {
                Debug.LogError($"Could not resolve ip address from '{ntpServer}'.");
                return DateTime.UtcNow;
            }

            IPEndPoint ep = new IPEndPoint(address[0], 123);

            try
            {
                return GetNetworkTime(ep);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when GetNetworkTime\n{e}");
            }

            // 异常情况下，均返回本地的UTC当前时间
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the current DateTime form <paramref name="ep"/> IPEndPoint.
        /// </summary>
        /// <param name="ep">The IPEndPoint to connect to.</param>
        /// <returns>A DateTime containing the current time.</returns>
        public DateTime GetNetworkTime(IPEndPoint ep)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            s.Connect(ep);

            byte[] ntpData = new byte[48]; // RFC 2030 
            ntpData[0] = 0x1B;
            for (int i = 1; i < 48; i++)
                ntpData[i] = 0;

            s.Send(ntpData);
            s.Receive(ntpData);

            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;

            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
            s.Close();

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);

            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            // 返回UTC时间
            return dateTime;

            // 打开注释获取本地时间
            // TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
            // DateTime networkDateTime = (dateTime + offsetAmount);
            //
            // return networkDateTime;
        }
    }
}