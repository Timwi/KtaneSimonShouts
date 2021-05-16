namespace SimonShouts
{
    struct Movement
    {
        public int XDist { get; private set; }
        public int YDist { get; private set; }
        public int Letter { get; private set; }

        private static readonly string[] _xDists = new[]
        {
            "K",
            "FLQ",
            "BGMRW",
            "ACHSXZ",
            "DINUY",
            "JOV",
            "P"
        };
        private static readonly string[] _yDists = new[]
        {
            "A",
            "BCD",
            "FGHIJ",
            "KLMNOP",
            "QRSUV",
            "WXY",
            "Z"
        };

        public Movement(int letter) : this()
        {
            var ch = ((char) ('A' + letter)).ToString();
            XDist = _xDists.IndexOf(str => str.Contains(ch)) - 3;
            YDist = _yDists.IndexOf(str => str.Contains(ch)) - 3;
            Letter = letter;
        }

        public static int operator +(int pos, Movement mv) { return (pos + 16 + mv.XDist) % 16 + 16 * ((pos / 16 + 16 + mv.YDist) % 16); }
        public static int operator -(int pos, Movement mv) { return (pos + 16 - mv.XDist) % 16 + 16 * ((pos / 16 + 16 - mv.YDist) % 16); }
    }
}