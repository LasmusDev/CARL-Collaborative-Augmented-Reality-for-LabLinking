using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LSLNetwork
{
    public class BridgeConnectionManager : MonoBehaviour
    {
        public static BridgeConnectionManager Instance;
        public const string EventChannelKey = "event_stream";
        public const string HLTrackingChannelKey = "HL_tracking_stream";
        private class UnpackException : Exception
        {
            public UnpackException(string message)
                : base(message)
            {
            }

            public UnpackException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public struct Package
        {
            public string ChannelName;

            public byte PkgType;

            // timestamp in system byte order
            public double Timestamp;

            // payload in system byte order
            public byte[] Payload;
        }

        //[SerializeField] private string _hostName;
        //[SerializeField] private string _port;
        public string _hostName;                    //DERVIS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public int _port;                        //DERVIS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // List of input channels from which we want to receive data
        [SerializeField] private List<string> _expectedChannels;
        

        private System.Net.Sockets.TcpClient _socket;
        private string _serverHost;

        
        private Task _listeningTask;

        //private NtpSync _ntpSync;
        private Stream _streamOut;
        private Stream _streamIn;

        private Dictionary<string, ConcurrentQueue<Package>> _channelBuffers;
        readonly string UNKOWN_CHANNEL = "UNKNOWN";

        readonly long TICKS_OF_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        public const byte BYTE_TYPE = 0x00;
        public const byte CHAR_TYPE = 0x01;
        public const byte SHORT_TYPE = 0x02;
        public const byte INT_TYPE = 0x03;
        public const byte LONG_TYPE = 0x04; // type long is currently bugged in lsl so please avoid using it 
        public const byte FLOAT_TYPE = 0x05;
        public const byte DOUBLE_TYPE = 0x06;
        public const byte EMPTY = 0x07;

        // all package sizes are in bytes
        const int TIMESTAMP_SIZE = 8;
        const int TYPE_SIZE = 1;
        const int SIZE_OF_NAME_SIZE = 4;
        const int SIZE_OF_PAYLOAD_SIZE = 4;

        const int BYTE_SIZE_SHORT = 2;
        const int BYTE_SIZE_INT = 4;
        const int BYTE_SIZE_FLOAT = 4;
        const int BYTE_SIZE_DOUBLE = 8;

        public bool _noConnection;

        readonly Package EMPTY_PKG = new Package
        { ChannelName = "", PkgType = 0x07, Timestamp = -1, Payload = new byte[0] };

        private void Awake()
        {           
            _channelBuffers = new Dictionary<string, ConcurrentQueue<Package>>();
        }

        // Use this for initialization
        private void Start()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("2 connection managers at the same time!");
                Destroy(this);
            }
            //_ntpSync = GetComponent<NtpSync>();
            foreach (var channelName in _expectedChannels)
            {
                _channelBuffers.Add(channelName, new ConcurrentQueue<Package>());
            }

            _channelBuffers.Add(UNKOWN_CHANNEL, new ConcurrentQueue<Package>());
            _noConnection = true;
        }


        public async void Connect()
        {             
            Debug.Log("HostName : " + _hostName);
            Debug.Log("Port : " + _port);
            if (_socket != null && _noConnection)
            {
                try
                {
                    _socket.Close();
                    _listeningTask = null;
                    _socket.Dispose();
                    _socket = null;
                    _streamIn = null;
                    _streamOut = null;
                }
                catch { }
             }
            _socket = new System.Net.Sockets.TcpClient();
            await _socket.ConnectAsync(_hostName, _port);
            _streamOut = _socket.GetStream();
            _streamIn = _socket.GetStream();
            _noConnection = !_socket.Connected;
            if (_streamIn != null && _streamOut != null && !_noConnection)
            {
                StartListening();
            }            
        }

        private void StartListening()
        {
            if (_listeningTask != null)
            {
                StopListening();
            }
            _listeningTask = Task.Run(() => RunListening());
        }

        public IEnumerator ConnectionPoll()
        {
            while (true)
            {
                _noConnection = _socket == null ? true : !_socket.Connected;
                if (_noConnection)
                {
                    Connect();
                    yield return new WaitForSeconds(5.0f);
                }
                yield return new WaitForSeconds(1.0f);
            }
        }

        public static string PackTransformAsString(Transform t)
        {
            return t.name + ";" + t.position.ToString() + ";" + t.rotation.ToString() + ";\n";
        }

        public static string PackTransformsAsString(IEnumerable<Transform> ts)
        {
            string result = "";
            foreach(Transform t in ts)
            {
                result += PackTransformAsString(t);
            }
            return result;
        }

        private byte[] PackBigEndianAsciiString(string value)
        {
            // Already in big endian order...
            var packedString = Encoding.ASCII.GetBytes(value);

            return packedString;
        }

        static byte[] ConvertByteOrderWithPackageTypeCheck(byte[] payload, byte packageType, byte expectedType)
        {
            if (packageType != expectedType)
            {
                throw new UnpackException(
                    $"Package type does not match the return type! Package type {packageType} is not {expectedType} return type.");
            }

            return payload;
        }

        #region Unpacking

        public static string UnpackString(Package pkg)
        {
            var payload = ConvertByteOrderWithPackageTypeCheck(pkg.Payload, pkg.PkgType, CHAR_TYPE);

            return Encoding.ASCII.GetString(payload);
        }

        public static byte[] UnpackByte(Package pkg)
        {
            var payload = ConvertByteOrderWithPackageTypeCheck(pkg.Payload, pkg.PkgType, BYTE_TYPE);

            return payload;
        }

        public static short[] UnpackShort(Package pkg)
        {
            var payload = ConvertByteOrderWithPackageTypeCheck(pkg.Payload, pkg.PkgType, SHORT_TYPE);

            var valueCount = payload.Length / BYTE_SIZE_SHORT;
            var values = new short[valueCount];
            for (var i = 0; i < valueCount; i++)
            {
                var valueInByte = payload.Skip(i * BYTE_SIZE_SHORT).Take(BYTE_SIZE_SHORT).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(valueInByte);
                }

                values[i] = BitConverter.ToInt16(valueInByte, 0);
            }

            return values;
        }

        public static int[] UnpackInt(Package pkg)
        {
            var payload = ConvertByteOrderWithPackageTypeCheck(pkg.Payload, pkg.PkgType, INT_TYPE);

            var valueCount = payload.Length / BYTE_SIZE_INT;
            var values = new int[valueCount];
            for (var i = 0; i < valueCount; i++)
            {
                var valueInByte = payload.Skip(i * BYTE_SIZE_INT).Take(BYTE_SIZE_INT).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(valueInByte);
                }

                values[i] = BitConverter.ToInt32(valueInByte, 0);
            }

            return values;
        }

        public static float[] UnpackFloat(Package pkg)
        {
            var payload = ConvertByteOrderWithPackageTypeCheck(pkg.Payload, pkg.PkgType, FLOAT_TYPE);

            var valueCount = payload.Length / BYTE_SIZE_FLOAT;
            var values = new float[valueCount];
            for (var i = 0; i < valueCount; i++)
            {
                var valueInByte = payload.Skip(i * BYTE_SIZE_FLOAT).Take(BYTE_SIZE_FLOAT).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(valueInByte);
                }

                values[i] = BitConverter.ToSingle(valueInByte, 0);
            }

            return values;
        }

        public static double[] UnpackDouble(Package pkg)
        {
            var payload = ConvertByteOrderWithPackageTypeCheck(pkg.Payload, pkg.PkgType, DOUBLE_TYPE);

            var valueCount = payload.Length / BYTE_SIZE_DOUBLE;
            var values = new double[valueCount];
            for (var i = 0; i < valueCount; i++)
            {
                var valueInByte = payload.Skip(i * BYTE_SIZE_DOUBLE).Take(BYTE_SIZE_DOUBLE).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(valueInByte);
                }

                values[i] = BitConverter.ToDouble(valueInByte, 0);
            }

            return values;
        }

        #endregion

        #region Unpacking

        public void Send(string message, string channelName)
        {
            //Attach client Metadata before every message
            var messageInByte = PackBigEndianAsciiString("Client:" + NetworkManager.Singleton.LocalClientId + ";" + MetaDataHolder.Instance.deviceType.ToString() +";\n" + message);
            var channelNameInByte = PackBigEndianAsciiString(channelName);
            SendPackage(messageInByte, channelNameInByte, CHAR_TYPE);
        }

        public void Send(byte message, string channelName)
        {
            var messageInByte = new[] { message };
            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, BYTE_TYPE);
        }

        public void Send(byte[] message, string channelName)
        {
            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(message, channelNameInByte, BYTE_TYPE);
        }

        public void Send(Int16 message, string channelName)
        {
            var messageInByte = BitConverter.GetBytes(message);
            if (BitConverter.IsLittleEndian) Array.Reverse(messageInByte);

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, SHORT_TYPE);
        }

        public void Send(Int16[] message, string channelName)
        {
            var messageInByte = new byte[message.Length * BYTE_SIZE_SHORT];
            for (var i = 0; i < message.Length; i++)
            {
                var value = BitConverter.GetBytes(message[i]);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value);
                }

                value.CopyTo(messageInByte, i * BYTE_SIZE_SHORT);
            }

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, SHORT_TYPE);
        }

        public void Send(Int32 message, string channelName)
        {
            var messageInByte = BitConverter.GetBytes(message);
            if (BitConverter.IsLittleEndian) Array.Reverse(messageInByte);

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, INT_TYPE);
        }

        public void Send(Int32[] message, string channelName)
        {
            var messageInByte = new byte[message.Length * BYTE_SIZE_INT];
            for (var i = 0; i < message.Length; i++)
            {
                var value = BitConverter.GetBytes(message[i]);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value);
                }

                value.CopyTo(messageInByte, i * BYTE_SIZE_INT);
            }

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, INT_TYPE);
        }

        public void Send(float message, string channelName)
        {
            var messageInByte = BitConverter.GetBytes(message);
            if (BitConverter.IsLittleEndian) Array.Reverse(messageInByte);

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, FLOAT_TYPE);
        }

        public void Send(float[] message, string channelName)
        {
            var messageInByte = new byte[message.Length * BYTE_SIZE_FLOAT];
            for (var i = 0; i < message.Length; i++)
            {
                var value = BitConverter.GetBytes(message[i]);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value);
                }

                value.CopyTo(messageInByte, i * BYTE_SIZE_FLOAT);
            }

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, FLOAT_TYPE);
        }

        public void Send(double message, string channelName)
        {
            var messageInByte = BitConverter.GetBytes(message);
            if (BitConverter.IsLittleEndian) Array.Reverse(messageInByte);

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, DOUBLE_TYPE);
        }

        public void Send(double[] message, string channelName)
        {
            var messageInByte = new byte[message.Length * BYTE_SIZE_DOUBLE];
            for (var i = 0; i < message.Length; i++)
            {
                var value = BitConverter.GetBytes(message[i]);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value);
                }

                value.CopyTo(messageInByte, i * BYTE_SIZE_DOUBLE);
            }

            var channelNameInByte = PackBigEndianAsciiString(channelName);

            SendPackage(messageInByte, channelNameInByte, DOUBLE_TYPE);
        }

#pragma warning disable AsyncFixer01 // Unnecessary async/await usage
        async void SendPackage(byte[] payload, byte[] channelName, byte type)
        {
            if (_noConnection) return;

            // Get timestamp from the 1st of January 1970
            var timestamp = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - TICKS_OF_EPOCH).TotalSeconds;
            var networkTimestamp = BitConverter.GetBytes(timestamp);
            var payloadSize = BitConverter.GetBytes((uint)payload.Length);
            var channelNameSize = BitConverter.GetBytes((uint)channelName.Length);


            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(networkTimestamp);
                Array.Reverse(payloadSize);
                Array.Reverse(channelNameSize);
            }

            var completePackageSize = TIMESTAMP_SIZE + TYPE_SIZE + SIZE_OF_NAME_SIZE + channelName.Length +
                                      SIZE_OF_PAYLOAD_SIZE + payload.Length;

            /**
            Protocol:
         
            64 Bit time stamp, 
            8 Bit lsl format, 
            32 Bit channel name size,
            n Bit channel name,
            32 Bit payload size,
            n Bit payload 
            */
            var package = new byte[completePackageSize];

            // Copy data to the package 
            var i = 0;
            networkTimestamp.CopyTo(package, i);
            i += TIMESTAMP_SIZE;

            package[i] = type;
            i += TYPE_SIZE;

            channelNameSize.CopyTo(package, i);
            i += SIZE_OF_NAME_SIZE;

            channelName.CopyTo(package, i);
            i += channelName.Length;

            payloadSize.CopyTo(package, i);
            i += SIZE_OF_PAYLOAD_SIZE;

            payload.CopyTo(package, i);

            if (_streamOut != null) 
            {
                await _streamOut.WriteAsync(package, 0, package.Length);
                await _streamOut.FlushAsync();
            } else {
                Debug.Log("_streamOut ist null");
            }
        }
#pragma warning restore AsyncFixer01 // Unnecessary async/await usage

#endregion

#region Receiving

        public Package ReadChannel(string channelName)
        {
            var channelQueue = _channelBuffers[channelName];

            Package package;
            if (channelQueue.Count > 0 && channelQueue.TryDequeue(out package))
            {
                return package;
            }

            return EMPTY_PKG;
        }

        private void RunListening()
        {
            while (!_noConnection)
            {
                StoreInput();
            }
        }

        private void StoreInput()
        {
            if (_noConnection)
            {
                return;
            }

            var timestamp = new byte[TIMESTAMP_SIZE];
            var byteCount = _streamIn.Read(timestamp, 0, TIMESTAMP_SIZE);
            if (byteCount != TIMESTAMP_SIZE)
            {
                return;
            }

            var type = new byte[TYPE_SIZE];
            _streamIn.Read(type, 0, TYPE_SIZE);

            var sizeOfNameSize = new byte[SIZE_OF_NAME_SIZE];
            _streamIn.Read(sizeOfNameSize, 0, SIZE_OF_NAME_SIZE);

            if (BitConverter.IsLittleEndian) Array.Reverse(sizeOfNameSize);
            var nameSize = (int)BitConverter.ToUInt32(sizeOfNameSize, 0);
            var pkgName = new byte[nameSize];
            _streamIn.Read(pkgName, 0, nameSize);

            var sizeOfPayloadSize = new byte[SIZE_OF_PAYLOAD_SIZE];
            _streamIn.Read(sizeOfPayloadSize, 0, SIZE_OF_PAYLOAD_SIZE);

            if (BitConverter.IsLittleEndian) Array.Reverse(sizeOfPayloadSize);
            var size = (int)BitConverter.ToUInt32(sizeOfPayloadSize, 0);
            var payload = new byte[size];
            _streamIn.Read(payload, 0, size);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestamp);
            }

            var pkg = new Package
            {
                Timestamp = BitConverter.ToDouble(timestamp, 0),
                PkgType = type[0],
                ChannelName = Encoding.ASCII.GetString(pkgName),
                // still in big endian order but in correct array order
                Payload = payload
            };

            _channelBuffers[pkg.ChannelName].Enqueue(pkg);
        }

        private void StopListening()
        {
            if (_listeningTask == null) return;
            _listeningTask.Wait();
            _listeningTask = null;
        }

        public void StopStream()
        {
            if (_noConnection) return;

            _noConnection = true;
            _socket.Close();
            StopListening();
            _socket.Dispose();
            _socket = null;
            _streamIn = null;
            _streamOut = null;
        }

#endregion

        public void SetInputs(string host, int port)                     //DERVIS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            _hostName = host;                                               //DERVIS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            _port = port;                                                   //DERVIS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        public void OnDestroy()
        {
            StopStream();
        }
    }
}