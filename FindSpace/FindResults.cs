using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WhitSpace.Results
{
    public class FindResults
    {
        private Rectangle scanarea;
        public bool containsResults { get; set; } = false;

        public int StampWidth { get; set; } = 0;
        public int StampHeight { get; set; } = 0;

        public FindResults(int width, int height, Rectangle scanArea)
        {
            possibleMatches = new int[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    possibleMatches[i, j] = System.Int32.MaxValue;
            scanarea = scanArea;
        }

        public List<Rectangle> exactMatches { get; } = new List<Rectangle>();
        public int[,] possibleMatches;

        public bool hasExactMatches()
        {
            return containsResults && exactMatches.Count > 0;
        }
        private int minvalue = System.Int32.MaxValue;
        public int minValue
        {
            get
            {
                if (minvalue == System.Int32.MaxValue)
                {
                    minvalue = squareIterator(possibleMatches, scanarea).Min();
                }
                return minvalue;
            }
        }

        private static IEnumerable<int> squareIterator(int[,] array, Rectangle wa)
        {
            for (int x = wa.Left; x < wa.Right; x++)
            {
                for (int y = wa.Bottom; y > wa.Top; y--)
                {
                    yield return array[x, y];
                }
            }
        }

    }
}
