using System;
using System.Net.Sockets;
using System.Threading;

namespace WoongConnector
{
    public sealed class LinkClient
    {
        private readonly Socket _inSocket;
        private readonly Socket _outSocket;
        private const int MAX_BUFFER = 16000;

        private readonly byte[] _outBuffer = new byte[MAX_BUFFER];
        private readonly byte[] _inBuffer = new byte[MAX_BUFFER];
        private bool _connected = true;

        private readonly string _host;
        private readonly ushort _port;
        public LinkClient(Socket sock, string host, ushort port)
        {
            try
            {
                _host = host;
                _port = port;
                _inSocket = sock;
                _outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _outSocket.BeginConnect(_host, _port, OnOutConnect, _outSocket);
            }

            catch
            {
                throw new Exception("Client failed to connect to the target host!");
            }
        }
        private void OnOutConnect(IAsyncResult ar)
        {
            try
            {
                _outSocket.EndConnect(ar);
            }

            catch
            {
                _inSocket.Shutdown(SocketShutdown.Both);
                return;
            }

            if (!_outSocket.Connected)
            {
                _inSocket.Shutdown(SocketShutdown.Both);
                return;
            }

            try
            {
                _inSocket.BeginReceive(_inBuffer, 0, MAX_BUFFER, SocketFlags.None, OnInPacket, _inSocket);
                _outSocket.BeginReceive(_outBuffer, 0, MAX_BUFFER, SocketFlags.None, OnOutPacket, _outSocket);
            }

            catch
            {
                throw new Exception("Socket has been failed to receive packets!");
            }
        }
        private void SendToIn(byte[] data)
        {
            if (!_connected) return;
            BeginInSend(data);
        }

        private void SendToOut(byte[] data)
        {
            if (!_connected) return;
            BeginOutSend(data);
        }
        private void BeginOutSend(byte[] data)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            _outSocket.SendAsync(args);
        }

        private void BeginInSend(byte[] data)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            _inSocket.SendAsync(args);
        }

        private readonly Mutex _mutex = new Mutex();
        private void OnOutPacket(IAsyncResult ar)
        {
            if (!_connected) return;

            _mutex.WaitOne();

            try
            {
                var len = _outSocket.EndReceive(ar);

                if (len <= 0 || !_connected)
                {
                    _connected = false;
                    _outSocket.Shutdown(SocketShutdown.Both);
                    return;
                }

                var toSend = new byte[len];
                Buffer.BlockCopy(_outBuffer, 0, toSend, 0, len);
                SendToIn(toSend);
                _outSocket.BeginReceive(_outBuffer, 0, MAX_BUFFER, SocketFlags.None, OnOutPacket, _outSocket);
            }

            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private readonly Mutex _secondMutex = new Mutex();
        private void OnInPacket(IAsyncResult ar)
        {
            _secondMutex.WaitOne();

            try
            {
                var len = _inSocket.EndReceive(ar);

                if (len <= 0 || !_connected)
                {
                    _connected = false;
                    _inSocket.Shutdown(SocketShutdown.Both);
                    return;
                }

                var toSend = new byte[len];
                Buffer.BlockCopy(_inBuffer, 0, toSend, 0, len);
                SendToOut(toSend);
                _inSocket.BeginReceive(_inBuffer, 0, MAX_BUFFER, SocketFlags.None, OnInPacket, _inSocket);
            }

            finally
            {
                _secondMutex.ReleaseMutex();
            }
        }
    }
}
