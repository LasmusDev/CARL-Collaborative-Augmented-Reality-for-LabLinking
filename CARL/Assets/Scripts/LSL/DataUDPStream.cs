using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;


/**
    * Class to represent an UDP data Stream which sends bytes to an open UDP port.
    * The IP Adress of the server can be set with the ipString and port string fields of the DataUDPStream.
    * The main method is createSendAction, which returns a Callback in which bytes 
    * are sent to the specified IP Adress and port in an async fashion.
    */
class DataUDPStream
{

    /**
        * Minimal allowed port
        */
    private static int MIN_PORT = 0;
    /**
        * Maximal allowed port. 
        */
    private static int MAX_PORT = 65535;

    /**
        *  Field for the IP adress of the UDP server to which the bytes will be sent.
        */
    public String ipString = "192.168.178.108";
    /**
        * Port of the UDP server to which the bytes will be sent.
        */
    public String port = "8081";

    /**
        *  UdpClient class used for the UDP conneciton under the hood.
        *  Is initiated in the sendAction method.
        */
    private UdpClient udpClient = null;
    /**
        *  IPEndPoint defined by the IP Adress and port fields;
        */
    private IPEndPoint ipEndPoint { get; set; } = null;

    /**
        * A boolean 
        */
    private bool connected { get; set; } = false;


    public DataUDPStream()
    {

    }

    /**
        * Checks whether the private ipString field is a valid IP string.
        * Returns true if this is the case, false otherwise.
        */
    public bool isValidIPAdress()
    {
        if (String.IsNullOrWhiteSpace(ipString))
        {
            return false;
        }

        string[] splitValues = ipString.Split('.');
        if (splitValues.Length != 4)
        {
            return false;
        }

        byte tempForParsing;

        return splitValues.All(r => byte.TryParse(r, out tempForParsing));
    }


    /**
        * Returns whether the port field is a valid port.
        */
    public bool isValidPort()
    {
        int portNum = parsePort();
        return MIN_PORT <= portNum && portNum <= MAX_PORT;
    }

    /**
        * Tries to parse the internal port field from a string to an int.
        * Returns -1 incase the port string could not be parsed.
        */
    private int parsePort()
    {
        try
        {
            return Int32.Parse(port);
        }
        catch (FormatException)
        {
            return -1;
        }
    }


    /**
        * Disconnects from the UDP Server and disposes the UDP client.
        */
    public void disconnect()
    {
        if (this.connected)
        {
            this.udpClient.Close();
            this.udpClient.Dispose();
            this.connected = false;
        }

    }

    /**
        * Creates an Action which sends Bytes over UDP to the server.
        */
    public Action<Byte[]> createSendAction()
    {
        if (this.udpClient == null)
        {
            this.udpClient = new UdpClient();
        }
        else
        {
            this.disconnect();
            this.udpClient = new UdpClient();

        }
        Debug.WriteLine("Creating new IP Endpoint");

        if (ipEndPoint == null)
        {
            this.ipEndPoint = new IPEndPoint(IPAddress.Parse(this.ipString), parsePort());
        }
        else
        {
            var ipAddrNew = IPAddress.Parse(this.ipString);
            var portNew = parsePort();
            if (!this.ipEndPoint.Address.Equals(ipAddrNew) || !this.ipEndPoint.Port.Equals(portNew))
            {
                Debug.WriteLine("Overwriting IP Endpoint");

                this.ipEndPoint = new IPEndPoint(IPAddress.Parse(this.ipString), parsePort());
            }
            else
            {
                Debug.WriteLine("Will not change IP Endpoint since adress and port are the same.");
            }
        }
        Debug.WriteLine("IP Endpoint created -- Trying to connect");
        this.udpClient.Connect(ipEndPoint);
        Debug.WriteLine("Connection has been done");
        this.connected = true;
        Action<Byte[]> sendAction = (bytes) =>
        {
            if (this.udpClient != null && bytes != null && bytes.Length > 0)
            {
                int byteLen = bytes.Length;
                try
                {
                    _ = this.udpClient.SendAsync(bytes, byteLen);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException || ex is SocketException)
                {
                    Debug.WriteLine("Got an exception when trying to send UDP paket");
                    Debug.WriteLine(ex);
                }

            }
        };
        return sendAction;
    }
}