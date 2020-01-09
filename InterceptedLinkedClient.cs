using WoongConnector.Tools;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace WoongConnector
{
    public sealed class InterceptedLinkedClient
    {
        private readonly Session _inSession;
        private Session _outSession;

        private bool _encrypted;
        private readonly ushort _port;
        private bool _connected = true;
        public InterceptedLinkedClient(Session inside, string toIP, ushort toPort, int ret)
        {
            _port = toPort;
            this.ret = ret;
            _inSession = inside;
            inside.OnPacketReceived += OnInPacketReceived;
            inside.OnClientDisconnected += OnInClientDisconnected;

            ConnectOut(toIP, toPort);
        }
        private void OnInClientDisconnected(Session session)
        {
            _outSession?.Socket.Shutdown(SocketShutdown.Both);
            _connected = false;
        }
        private void ConnectOut(string ip, int port)
        {
            try
            {
                Socket outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                outSocket.BeginConnect(ip, port, OnOutConnectCallback, outSocket);
            }

            catch { OnOutClientDisconnected(null); }
        }
        private void OnOutConnectCallback(IAsyncResult ar)
        {
            Socket sock = (Socket)ar.AsyncState;

            try
            {
                sock.EndConnect(ar);
            }

            catch (Exception ex)
            {
                _connected = false;
                _inSession.Socket.Shutdown(SocketShutdown.Both);
                MessageBox.Show(ex.ToString());
                return;
            }

            if (_outSession != null)
            {
                _outSession.Socket.Close();
                _outSession.Connected = false;
            }

            Session session = new Session(sock, SessionType.CLIENT_TO_SERVER);
            _outSession = session;
            _outSession.OnInitPacketReceived += OnOutInitPacketReceived;
            _outSession.OnPacketReceived += OnOutPacketReceived;
            _outSession.OnClientDisconnected += OnOutClientDisconnected;
            session.WaitForDataNoEncryption();
        }

        private volatile Mutex _mutex = new Mutex();
        private void OnInPacketReceived(byte[] packet)
        {
            if (!_connected)
                return;

            _mutex.WaitOne();

            try
            {
                _outSession.SendPacket(packet);
            }

            finally
            {
                _mutex.ReleaseMutex();
            }
        }
        private void OnOutClientDisconnected(Session session)
        {
            _inSession.Socket.Shutdown(SocketShutdown.Both);
            _connected = false;
        }

        private volatile Mutex _secondMutex = new Mutex();
        public int ret { get; private set; }
        private void OnOutPacketReceived(byte[] packet)
        {
            if (!_encrypted || !_connected)
            {
                return;
            }
            _secondMutex.WaitOne();
            try
            {
                short opcode = BitConverter.ToInt16(packet, 0);
                Debug.WriteLine($"Got a packet from server: {opcode}");
                _inSession.SendPacket(packet);
            }
            finally
            {
                _secondMutex.ReleaseMutex();
            }
        }
        private void OnOutInitPacketReceived(short version, byte serverIdentifier)
        {
            SendHandShake(version, serverIdentifier);
        }
        private void SendHandShake(short version, byte serverIdentifier)
        {
            PacketWriter writer = new PacketWriter();
            writer.WriteShort(0x19);
            writer.WriteByte(35);
            writer.WriteByte(1);

            ret ^= version & short.MaxValue;
            ret ^= 32768;
            ret ^= (serverIdentifier & byte.MaxValue) << 16;

            writer.WriteMapleString(ret.ToString());
            var numArray1 = new byte[4];
            var numArray2 = new byte[4];

            Random random = new Random();
            var buffer1 = numArray1;
            random.NextBytes(buffer1);

            var buffer2 = numArray2;
            random.NextBytes(buffer2);

            _inSession.RIV = new MapleCrypto(numArray1, version);
            _inSession.SIV = new MapleCrypto(numArray2, version);

            writer.WriteBytes(numArray1);
            writer.WriteBytes(numArray2);
            writer.WriteByte(1);
            writer.WriteByte(0);

            _encrypted = true;
            _inSession.SendRawPacket(writer.ToArray());
        }
    }
}
