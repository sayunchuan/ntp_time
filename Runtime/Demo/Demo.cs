using System.Collections;
using NtpTime;
using UnityEngine;

namespace NtpTime.Demo
{
    public class Demo : MonoBehaviour
    {
        private INtpTime _ntpTime;

        private void Awake()
        {
            _ntpTime = new MSTime();
            _ntpTime.Init();
            StartCoroutine(_Tick());
        }

        IEnumerator _Tick()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                Debug.Log($"local:{System.DateTime.Now}, ntp:{_ntpTime.NetworkLocalTime}");
            }
        }
    }
}