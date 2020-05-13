namespace buip
{
    public struct PortProtocol
    {
        public int WaitForAnswer;
        public int BoudRate;
        public byte Address;

        public static PortProtocol Default
        {
            get
            {
                return new PortProtocol
                {
                    WaitForAnswer = 200,
                    BoudRate = 19200,
                    Address = 0
                };
            }
        }
    }
}
