using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

/**
 * A class which queries an Network Time Protocol (NTP) server for the current network time.
 * Based on the returned time, estimates the offset between the local time and the network time.
 * This offset is used to sync the timestamps between the HL2 and the Mini PC;
 */ 
public class NTPClient
{

    // The offset between local and network time;
    public double NtpTimeOffset { set; get; } = 0.0;

    // Default windows server
    // The host of the NTP server
    public string ntpServerhost  = "time.windows.com";

    // Default port for NTP servers
    public int ntpPort  = 123;

    // Whether the received 
    public bool verbose = true;

    // Whether we want to use an IP Adress for the ntpServerHost
    // If not, we use DNS to look up the IP adress, otherwise we use the IP adress directly
    private bool useIPAdressAsHost { set; get; } = false;

    // Whether there was an error while getting the time (e.g. no internet)
    public bool ntpError { set; get; } = false;

    // After how many miliseconds the request should timeout
    private int timeOutMs { set; get; } = 3000;

    // The network time we receive from the NTP request - in UTC
    private System.DateTime ntpNetworkTimeUTC { set; get; }

    // The local time we get just right after we get the NTP response - in UTC
    private System.DateTime localTimeUTC { set; get; }

    // Empty constructor - NTPClient can be configured via the Unity editor or by the caller if desired;
    public NTPClient()
    {
    
    }

    /**
     * Tries to call the NTP server and get the network time;
     * Gets the local time after the response and estimates a time offset in seconds;
     * The code is derived from: https://stackoverflow.com/a/12150289
     */
    public void getNTPTimeAndCalcOffset()
    {
        Debug.Log("getNTPTimeAndCalcOffset called");
        // NTP message size - 16 bytes of the digest (RFC 2030)
        var ntpData = new byte[48];
        DateTime localTime;
        try
        {

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            IPEndPoint ipEndPoint;
            if (!useIPAdressAsHost)
            {
                var addresses = Dns.GetHostEntry(ntpServerhost).AddressList;
                //The UDP port number assigned to NTP is 123
                ipEndPoint = new IPEndPoint(addresses[0], ntpPort);
            }
            else
            {
                var ipAddrNew = IPAddress.Parse(ntpServerhost);
                ipEndPoint = new IPEndPoint(ipAddrNew, ntpPort);
            }

            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = timeOutMs;
                localTime = DateTime.UtcNow;
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException || ex is SocketException)
        {
            Debug.Log("Got an exception when getting NTP data..returning");
            ntpError = true;
            return;
        }


        //Offset to get to the "Transmit Timestamp" field (time at which the reply 
        //departed the server for the client, in 64-bit timestamp format."
        const byte serverReplyTime = 40;

        //Get the seconds part
        ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

        //Get the seconds fraction
        ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

        //Convert From big-endian to little-endian
        intPart = SwapEndianness(intPart);
        fractPart = SwapEndianness(fractPart);

        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

        // Get the current time;
        localTimeUTC = localTime;
        //**UTC** time
        ntpNetworkTimeUTC = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

        // Calculate the offset
        NtpTimeOffset = TimeSpan.FromTicks(localTimeUTC.Ticks - ntpNetworkTimeUTC.Ticks).TotalSeconds;

        ntpError = false;

        if (verbose)
        {
            Debug.Log("NTP Network Time UTC:" + ntpNetworkTimeUTC);
            Debug.Log("Local Time UTC:" + localTimeUTC);
            Debug.Log("Offset (s)" + NtpTimeOffset);
        }

    }

    // stackoverflow.com/a/3294698/162671
    static uint SwapEndianness(ulong x)
    {
        return (uint) (((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }
}
