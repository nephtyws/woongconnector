using System;
using System.Net;
using System.Net.Sockets;

namespace WoongConnector
{
    public sealed class LinkServer
    {
        private readonly Socket _listener;
        private readonly string _host;
        private readonly ushort _port;
        public LinkServer(string host, ushort port)
        {
            _host = host;
            _port = port;

            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(new IPEndPoint(IPAddress.Any, port));
                _listener.Listen(10);
                _listener.BeginAccept(OnConnect, _listener);
            }

            catch (Exception)
            {
                throw new Exception("Failed to create LinkServer between target host!");
            }
        }

        private void OnConnect(IAsyncResult ar)
        {
            Socket client = _listener.EndAccept(ar);
            LinkClient lClient = new LinkClient(client, _host, _port);
            _listener.BeginAccept(new AsyncCallback(OnConnect), _listener);
        }
    }
}
