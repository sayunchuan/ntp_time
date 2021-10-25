using System;

namespace NtpTime
{
    public interface INtpTime
    {
        /// <summary>
        /// UTC时间
        /// </summary>
        DateTime NetworkUtcTime { get; }

        /// <summary>
        /// 本时区时间
        /// </summary>
        DateTime NetworkLocalTime { get; }

        void Init();
    }
}