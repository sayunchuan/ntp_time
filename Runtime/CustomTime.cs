using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace NtpTime
{
    public class CustomTime : INtpTime
    {
        //Server IP addresses from   
        //http://www.boulder.nist.gov/timefreq/service/time-servers.html  
        private static string[] Servers =
        {
            "129.6.15.28",
            "129.6.15.29",
            "132.163.4.101",
            "132.163.4.102",
            "132.163.4.103",
            "128.138.140.44",
            "192.43.244.18",
            "131.107.1.10",
            "66.243.43.21",
            "216.200.93.8",
            "208.184.49.9",
            "207.126.98.204",
            "205.188.185.33"
            //65.55.21.15time.windows.com微软时间同步服务器
        };

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

        /// <summary>
        /// 网络UTC时间
        /// </summary>
        public DateTime NetworkUtcTime => _inited
            ? _utcStampBegin.AddMilliseconds(_networkUtcStamp).AddMilliseconds(_syncLocalGameTime)
            : DateTime.UtcNow;

        /// <summary>
        /// 网络当前时区时间
        /// </summary>
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
            DateTime serverUtcTime = await _GetNTPTime();
            _networkUtcStamp = (ulong)(serverUtcTime - _utcStampBegin).TotalMilliseconds;
            _syncLocalGameTime = (ulong)(Time.realtimeSinceStartup * 1000);
            _inited = true;
            Debug.Log($"Custom NTP time is {NetworkUtcTime}(UTC), {NetworkLocalTime}(local)");
        }

        private async Task<DateTime> _GetNTPTime()
        {
            return await Task<DateTime>.Run(GetTime);
        }

        public DateTime GetTime()
        {
            //Returns UTC/GMT using an NIST server if possible,   
            // degrading to simply returning the system clock  

            //If we are successful in getting NIST time, then  
            // LastHost indicates which server was used and  
            // LastSysTime contains the system time of the call  
            // If LastSysTime is not within 15 seconds of NIST time,  
            //  the system clock may need to be reset  
            // If LastHost is "", time is equal to system clock  

            string host = null;
            DateTime result = default(DateTime);

            string lastHost = "";
            foreach (string host_loopVariable in Servers)
            {
                host = host_loopVariable;
                try
                {
                    result = GetNISTTime(host);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    result = DateTime.MinValue;
                }

                if (result > DateTime.MinValue)
                {
                    lastHost = host;
                    break; // TODO: might not be correct. Was : Exit For  
                }
            }

            if (string.IsNullOrEmpty(lastHost))
            {
                //No server in list was successful so use system time  
                result = DateTime.UtcNow;
            }

            return result;
        }

        private DateTime GetNISTTime(string host)
        {
            //Returns DateTime.MinValue if host unreachable or does not produce time  
            DateTime result = default(DateTime);
            string timeStr;

            try
            {
                StreamReader reader = new StreamReader(new TcpClient(host, 13).GetStream());
                timeStr = reader.ReadToEnd();
                reader.Close();
            }
            catch (SocketException ex)
            {
                //Couldn't connect to server, transmission error  
                Debug.LogError("Socket Exception [" + host + "]");
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                //Some other error, such as Stream under/overflow  
                return DateTime.MinValue;
            }

            //Parse timeStr  
            if (timeStr.Length < 47 || (timeStr.Substring(38, 9) != "UTC(NIST)"))
            {
                //This signature should be there  
                return DateTime.MinValue;
            }

            if (timeStr.Length < 31 || (timeStr.Substring(30, 1) != "0"))
            {
                //Server reports non-optimum status, time off by as much as 5 seconds  
                return DateTime.MinValue;
                //Try a different server  
            }

            int jd = int.Parse(timeStr.Substring(1, 5));
            int yr = int.Parse(timeStr.Substring(7, 2));
            int mo = int.Parse(timeStr.Substring(10, 2));
            int dy = int.Parse(timeStr.Substring(13, 2));
            int hr = int.Parse(timeStr.Substring(16, 2));
            int mm = int.Parse(timeStr.Substring(19, 2));
            int sc = int.Parse(timeStr.Substring(22, 2));

            if ((jd < 15020))
            {
                //Date is before 1900  
                return DateTime.MinValue;
            }

            if ((jd > 51544))
                yr += 2000;
            else
                yr += 1900;

            return new DateTime(yr, mo, dy, hr, mm, sc);
        }
    }
}