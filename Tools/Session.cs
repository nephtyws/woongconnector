/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010 Snow and haha01haha01

 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this. If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Net.Sockets;
using System.Threading;

namespace WoongConnector.Tools
{
    /// <summary>
    /// Class to a network session socket
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Method to handle packets received
        /// </summary>
        public delegate void PacketReceivedHandler(byte[] packet);

        /// <summary>
        /// Packet received event
        /// </summary>
        public event PacketReceivedHandler OnPacketReceived;

        /// <summary>
        /// Method to handle client disconnected
        /// </summary>
        public delegate void ClientDisconnectedHandler(Session session);

        /// <summary>
        /// Client disconnected event
        /// </summary>
        public event ClientDisconnectedHandler OnClientDisconnected;

        public delegate void InitPacketReceived(short version, byte serverIdentifier);
        public event InitPacketReceived OnInitPacketReceived;

        /// <summary>
        /// The received packet crypto manager
        /// </summary>
        public MapleCrypto RIV { get; set; }

        public bool Connected = true;

        /// <summary>
        /// The Sent packet crypto manager
        /// </summary>
        public MapleCrypto SIV { get; set; }

        /// <summary>
        /// The Session's socket
        /// </summary>
        public Socket Socket { get; }

        public SessionType Type { get; }

        #region buffers
        private const int DEFAULT_SIZE = 16000;
        private byte[] _mBuffer = new byte[DEFAULT_SIZE];
        private readonly byte[] _mSharedBuffer = new byte[DEFAULT_SIZE];
        private int _mCursor = 0;
        #endregion

        /// <summary>
        /// Creates a new instance of a Session
        /// </summary>
        /// <param name="socket">Socket connection of the session</param>

        public Session(Socket socket, SessionType type)
        {
            Socket = socket;
            Type = type;
        }

        #region bufferstuff

        #endregion
        /// <summary>
		/// Waits for more data to arrive (encrypted)
		/// </summary>
		public void WaitForData()
        {
            BeginReceive();
        }

        public void WaitForDataNoEncryption()
        {
            if (!Socket.Connected)
            {
                ForceDisconnect();
                return;
            }

            var initBuffer = new byte[16];
            Socket.BeginReceive(initBuffer, 0, 16, SocketFlags.None, OnInitPacketRecv, initBuffer);
        }

        private void BeginReceive()
        {
            if (!Connected || !Socket.Connected)
            {
                ForceDisconnect();
                return;
            }

            Socket.BeginReceive(_mSharedBuffer, 0, DEFAULT_SIZE, SocketFlags.None, EndReceive, Socket);
        }

        private void ForceDisconnect()
        {
            if (!Connected) return;

            OnClientDisconnected?.Invoke(this);
            Connected = false;
        }

        public void Append(byte[] pBuffer) { Append(pBuffer, 0, pBuffer.Length); }
        public void Append(byte[] pBuffer, int pStart, int pLength)
        {
            if (_mBuffer.Length - _mCursor < pLength)
            {
                var newSize = _mBuffer.Length * 2;
                while (newSize < _mCursor + pLength) newSize *= 2;
                Array.Resize<byte>(ref _mBuffer, newSize);
            }

            Buffer.BlockCopy(pBuffer, pStart, _mBuffer, _mCursor, pLength);
            _mCursor += pLength;
        }

        private void EndReceive(IAsyncResult ar)
        {
            if (!Connected) return;

            var recvLen = 0;

            try
            {
                recvLen = Socket.EndReceive(ar);
            }

            catch
            {
                ForceDisconnect();
                return;
            }

            if (recvLen <= 0)
            {
                ForceDisconnect();
                return;
            }

            Append(_mSharedBuffer, 0, recvLen);

            while (true)
            {
                if (_mCursor < 4) break;

                var packetSize = MapleCrypto.getPacketLength(_mBuffer, 0);

                if (_mCursor < packetSize + 4) break;

                var packetBuffer = new byte[packetSize];
                Buffer.BlockCopy(_mBuffer, 4, packetBuffer, 0, packetSize);
                RIV.Transform(packetBuffer);
                _mCursor -= (packetSize + 4);

                if (_mCursor > 0)
                    Buffer.BlockCopy(_mBuffer, packetSize + 4, _mBuffer, 0, _mCursor);

                if (OnPacketReceived != null)
                {
                    if (!Connected) return;
                    OnPacketReceived(packetBuffer);
                }
            }

            BeginReceive();
        }
        private void OnInitPacketRecv(IAsyncResult ar)
        {
            if (!Connected) return;

            var data = (byte[])ar.AsyncState;
            var len = Socket.EndReceive(ar);

            if (len < 15)
            {
                OnClientDisconnected?.Invoke(this);
                Connected = false;
                return;
            }

            PacketReader reader = new PacketReader(data);
            reader.ReadShort();
            reader.ReadShort();
            reader.ReadMapleString();

            SIV = new MapleCrypto(reader.ReadBytes(4), 227); // kms
            RIV = new MapleCrypto(reader.ReadBytes(4), 227); // kms
            // byte serverType = reader.ReadByte();

            if (Type == SessionType.CLIENT_TO_SERVER)
                OnInitPacketReceived(227, 1);

            WaitForData();
        }

        /// <summary>
        /// Encrypts the packet then send it to the client.
        /// </summary>
        /// <param name="packet">The PacketWrtier object to be sent.</param>
        public void SendPacket(PacketWriter packet)
        {
            if (!Connected) return;
            SendPacket(packet.ToArray());
        }

        /// <summary>
        /// Encrypts the packet then send it to the client.
        /// </summary>
        /// <param name="input">The byte array to be sent.</param>
        public void SendPacket(byte[] input)
        {
            if (!Connected || !Socket.Connected)
            {
                return;
            }
            var cryptData = input;
            var sendData = new byte[cryptData.Length + 4];
            var header = Type == SessionType.SERVER_TO_CLIENT ? SIV.getHeaderToClient(cryptData.Length) : SIV.getHeaderToServer(cryptData.Length);

            // SIV.Encrypt(cryptData);
            SIV.Transform(cryptData);

            System.Buffer.BlockCopy(header, 0, sendData, 0, 4);
            System.Buffer.BlockCopy(cryptData, 0, sendData, 4, cryptData.Length);

            SendRawPacket(sendData);
        }

        private volatile LockFreeQueue<ByteArraySegment> _mSendSegments = new LockFreeQueue<ByteArraySegment>();
        private int _mSending = 0;

        /// <summary>
        /// Sends a raw buffer to the client.
        /// </summary>
        /// <param name="buffer">The buffer to be sent.</param>
        public void SendRawPacket(byte[] buffer)
        {
            if (!Connected)
                return;

            _mSendSegments.Enqueue(new ByteArraySegment(buffer));

            if (Interlocked.CompareExchange(ref _mSending, 1, 0) == 0)
                BeginInSend(buffer);
        }
        private void BeginInSend()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += (s, a) => EndInSend(a);
            ByteArraySegment segment = _mSendSegments.Next;
            args.SetBuffer(segment.Buffer, segment.Start, segment.Length);

            if (!Socket.SendAsync(args))
                EndInSend(args);
        }
        private void BeginInSend(byte[] data)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            Socket.SendAsync(args);
            EndInSend(args);
        }
        private void EndInSend(SocketAsyncEventArgs pArguments)
        {
            if (!Connected)
                return;

            if (pArguments.BytesTransferred <= 0)
            {
                Connected = false;
                return;
            }

            if (_mSendSegments.Next.Advance(pArguments.BytesTransferred))
                _mSendSegments.Dequeue();

            if (_mSendSegments.Next != null)
                BeginInSend();

            else
                _mSending = 0;
        }
    }
}
