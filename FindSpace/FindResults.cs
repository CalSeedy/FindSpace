﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WhitSpace.Results
{
    public class FindResults
    {
        private Rectangle scanarea;
        public bool containsResults { get; set; } = false;
        public FindResults(int width, int height, Rectangle scanArea)
        {
            possibleMatches = new int[width, height];
            scanarea = scanArea;
        }

        public List<Rectangle> exactMatches { get; } = new List<Rectangle>();
        public int[,] possibleMatches;

        public bool hasExactMatches()
        {
            return containsResults && exactMatches.Count > 0;
        }
        private int minvalue = -1;
        public int minValue
        {
            get
            {
                if (minvalue == -1)
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
