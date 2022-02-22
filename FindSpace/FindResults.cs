using SoupSoftware.FindSpace;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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

        public Rectangle BestMatch { get; private set; }
        public List<Rectangle> exactMatches { get; private set; } = new List<Rectangle>();
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
#if DEBUG
        // For debug only vvv
        public void PossiblesToBitmap(string filepath)
        {
            int LinearInterp(int start, int end, double percentage) => start + (int)Math.Round(percentage * (end - start));
            Color ColorInterp(float percentage, Color start, Color end) =>
                Color.FromArgb(LinearInterp(start.A, end.A, percentage),
                               LinearInterp(start.R, end.R, percentage),
                               LinearInterp(start.G, end.G, percentage),
                               LinearInterp(start.B, end.B, percentage));
            Color GradientPick(float percentage, Color Start, Color End)
            {
                if (percentage < 0.5)
                    return ColorInterp(percentage / 0.5f, Start, End);
                else if (percentage > 1.0f)
                    return Color.White;
                else
                    return ColorInterp((percentage - 0.5f) / 0.5f, Start, End);
            }

            int w = possibleMatches.GetLength(0);
            int h = possibleMatches.GetLength(1);
            int bpp = Bitmap.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;

            Bitmap maskBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            BitmapData data = maskBitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            IntPtr intPtr = data.Scan0;

            IEnumerable<int> ints = possibleMatches.Cast<int>();
            float max = StampWidth * StampHeight;
            float min = ints.Cast<int>().Min();
            Color red = Color.FromArgb(255, 255, 0, 0);
            Color green = Color.FromArgb(255, 0, 255, 0);

            lock (possibleMatches)
            {
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        Color col;
                        int val = possibleMatches[i, j];
                        if (val != Int32.MaxValue)
                            col = GradientPick(((val - min) / (max - min)), green, red);
                        else
                            col = Color.Black;
                        System.Runtime.InteropServices.Marshal.WriteByte(intPtr, (j * data.Stride) + (i * bpp) + 0, col.B);
                        System.Runtime.InteropServices.Marshal.WriteByte(intPtr, (j * data.Stride) + (i * bpp) + 1, col.G);
                        System.Runtime.InteropServices.Marshal.WriteByte(intPtr, (j * data.Stride) + (i * bpp) + 2, col.R);
                    }
                }
            }

            //RGB[] f = sRGB.Deserialize<RGB[]>(buffer)
            maskBitmap.UnlockBits(data);

            maskBitmap.Save(filepath);
        }
#endif

        private Rectangle ChooseBest(Point[] points, searchMatrix masks, WhitespacerfinderSettings settings)
        {
            Point optimal = settings.Optimiser.GetOptimalPoint(scanarea);

            int N = points.Length;
            double[] values = new double[N];
            for (int i = 0; i < N; ++i)
            {
                Point match = points[i];
                double distanceToOpt = GeoLibrary.DistanceTo(match, optimal);

                // if we have a stamp, get avg stamp position
                double distanceToOthers = 0;
                int C = masks.Stamps.Count;
                if (C > 0)
                {
                    int avgX = 0;
                    int avgY = 0;

                    for (int k = 0; k < C; ++k)
                    {
                        avgX += masks.Stamps[k].X;
                        avgY += masks.Stamps[k].Y;
                    }
                    avgX /= C;
                    avgY /= C;
                    distanceToOthers = GeoLibrary.DistanceTo(new Point(avgX, avgY), optimal);
                }

                values[i] = settings.DistanceWeight * distanceToOpt + settings.GroupingWeight * distanceToOthers;
            }

            Point[] copy = points.ToArray();
            var idxs = values.Select((x, n) => new KeyValuePair<double, int>(x, n))
                             .OrderBy(x => x.Key).ToList();

            return new Rectangle(idxs.Select(x => copy[x.Value]).First(), new Size(StampWidth, StampHeight));
        }

        internal void FilterMatches(searchMatrix masks, WhitespacerfinderSettings settings)
        {
            //int N = exactMatches.Count;
            //if (N < 2)
            //    return;

            // weighted filter based on distance to desired point
            //                        + distance to other stamps

            
            //Point[] possibles = possibleMatches.Cast<int>().Select((x, k) => new {val = x, idx = k })
            //                                               .Where(x => x.val == minValue)
            //                                               .Select(p => new Point(p.idx % possibleMatches.GetLength(0), p.idx / possibleMatches.GetLength(1)))
            //                                               .ToArray();

            List<Point> matches = new List<Point>();


            for (int j = 0; j < possibleMatches.GetLength(1); ++j)
            {
                for (int i = 0; i < possibleMatches.GetLength(0); ++i)
                {
                    if (possibleMatches[i, j] == minvalue)
                        matches.Add(new Point(i + 1, j + 1));
                }
            }

            int nExact = exactMatches.Count;
            int nPossible = matches.Count;
            


            if (nExact > 0)
            {
                matches.AddRange(exactMatches.Select(x => x.Location));
            }

            // filter results
            matches = matches.Where(x => !masks.Stamps.Any(r => r.IntersectsWith(new Rectangle(x.X, x.Y, StampWidth, StampHeight)))).ToList();         // stop overlap
            //matches = matches.Where(x => x.X + StampWidth <= scanarea.Right && x.X >= scanarea.Left && x.Y + StampHeight <= scanarea.Bottom && x.Y >= scanarea.Top) // stop oob
            //                 .ToList();
            
            BestMatch = ChooseBest(matches.Distinct().ToArray(), masks, settings);
        }

        internal Rectangle CompareBest(searchMatrix masks, WhitespacerfinderSettings settings, FindResults other)
        {
            if (BestMatch.IsEmpty || other.BestMatch.IsEmpty)
                throw new InvalidOperationException("Cannot compare. BestMatch is empty");

            Point[] ps = new Point[2];
            ps[0] = this.BestMatch.Location;
            ps[1] = other.BestMatch.Location;
            return ChooseBest(ps, masks, settings);
        }
    }
}
