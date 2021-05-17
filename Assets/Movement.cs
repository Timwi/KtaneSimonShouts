using System.Linq;

namespace SimonShouts
{
    struct Movement
    {
        public int XDist { get; private set; }
        public int YDist { get; private set; }
        public int DiagramBPosition { get; private set; }

        private static readonly int[][] _xDists = new[]
        {
            new[] { 9 },
            new[] { 4, 10, 15 },
            new[] { 1, 5, 11, 16, 20 },
            new[] { 0, 2, 6, 17, 21, 23 },
            new[] { 3, 7, 12, 18, 22 },
            new[] { 8, 13, 19 },
            new[] { 14 }
        };
        private static readonly int[][] _yDists = new[]
        {
            new[] { 0 },
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6, 7, 8 },
            new[] { 9, 10, 11, 12, 13, 14 },
            new[] { 15, 16, 17, 18, 19 },
            new[] { 20, 21, 22 },
            new[] { 23 }
        };

        public Movement(int diagramBPosition) : this()
        {
            XDist = _xDists.IndexOf(arr => arr.Contains(diagramBPosition)) - 3;
            YDist = _yDists.IndexOf(arr => arr.Contains(diagramBPosition)) - 3;
            DiagramBPosition = diagramBPosition;
        }

        public static int operator +(int pos, Movement mv) { return (pos + 16 + mv.XDist) % 16 + 16 * ((pos / 16 + 16 + mv.YDist) % 16); }
        public static int operator -(int pos, Movement mv) { return (pos + 16 - mv.XDist) % 16 + 16 * ((pos / 16 + 16 - mv.YDist) % 16); }
    }
}