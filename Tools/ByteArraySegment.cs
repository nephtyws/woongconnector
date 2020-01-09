namespace WoongConnector.Tools
{
    internal sealed class ByteArraySegment
    {
        public ByteArraySegment(byte[] pBuffer)
        {
            Buffer = pBuffer;
            Length = Buffer.Length;
        }
        public ByteArraySegment(byte[] pBuffer, int pStart, int pLength)
        {
            Buffer = pBuffer;
            Start = pStart;
            Length = pLength;
        }

        public byte[] Buffer { get; } = null;
        public int Start { get; private set; } = 0;
        public int Length { get; private set; } = 0;
        public bool Advance(int pLength)
        {
            Start += pLength;
            Length -= pLength;

            return Length <= 0;
        }
    }
}
