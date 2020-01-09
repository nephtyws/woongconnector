using System;
using System.Net;
using System.Net.Sockets;

namespace WoongConnector.Tools
{
    public class Listener
    {
        /// <summary>
        /// The listener socket
        /// </summary>
        private readonly Socket _listener;
        private ushort _port;

        /// <summary>
        /// Method called when a client is connected
        /// </summary>
        public delegate void ClientConnectedHandler(Session session, ushort port);

        /// <summary>
        /// Client connected event
        /// </summary>
        public event ClientConnectedHandler OnClientConnected;

        /// <summary>
        /// A List contains all the sessions connected to the listener.
        /// </summary>
        public bool Running => _listener.IsBound;

        /// <summary>
        /// Creates a new instance of Acceptor
        /// </summary>
        public Listener()
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Starts listening and accepting connections
        /// </summary>
        /// <param name="port">Port to listen to</param>
        public void Listen(ushort port)
        {
            _port = port;
            _listener.Bind(new IPEndPoint(IPAddress.Any, port));
            _listener.Listen(15);
            _listener.BeginAccept(OnClientConnect, null);
        }

        /// <summary>
        /// Client connected handler
        /// </summary>
        /// <param name="async">The IAsyncResult</param>
        private void OnClientConnect(IAsyncResult async)
        {
            Socket socket = _listener.EndAccept(async);
            Session session = new Session(socket, SessionType.SERVER_TO_CLIENT);
            OnClientConnected?.Invoke(session, _port);

            session.WaitForData();

            _listener.BeginAccept(OnClientConnect, null);
        }

        /// <summary>
        /// Releases a session.
        /// </summary>
        /// <param name="session">The Session to kick.</param>
        public void Release(Session session)
        {
            session.Socket.Close();
        }

        /// <summary>
        /// Stops listening.
        /// </summary>
        public void Close()
        {
            _listener.Close();
        }
    }
}
