using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace SoupSoftware.FindSpace.Results
{
    public class FindResults
    {
        private Rectangle ScanArea;
        private int minvalue = System.Int32.MaxValue;

        public bool ContainsResults { get; set; } = false;
        public int StampWidth { get; set; } = 0;
        public int StampHeight { get; set; } = 0;
        public Rectangle BestMatch { get; private set; }
        public List<Rectangle> ExactMatches { get; private set; } = new List<Rectangle>();
        public int[,] PossibleMatches;
        public int MinValue
        {
            get
            {
                if (minvalue == System.Int32.MaxValue)
                {
                    minvalue = SquareIterator(PossibleMatches, ScanArea).Min();
                }
                return minvalue;
            }
        }

        public FindResults(int width, int height, Rectangle scanArea)
        {
            PossibleMatches = new int[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    PossibleMatches[i, j] = System.Int32.MaxValue;
            ScanArea = scanArea;
        }

        public bool HasExactMatches()
        {
            return ContainsResults && ExactMatches.Count > 0;
        }

        private static IEnumerable<int> SquareIterator(int[,] array, Rectangle wa)
        {
            for (int x = wa.Left; x < wa.Right; x++)
            {
                for (int y = wa.Bottom; y > wa.Top; y--)
                {
                    yield return array[x, y];
                }
            }
        }

        private Rectangle ChooseBest(Point[] points, SearchMatrix masks, WhitespaceFinderSettings settings)
        {
            Point optimal = settings.Optimiser.GetOptimalPoint(ScanArea);

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

        internal void FilterMatches(SearchMatrix masks, WhitespaceFinderSettings settings)
        {

            List<Point> matches = new List<Point>();


            for (int j = 0; j < PossibleMatches.GetLength(1); ++j)
            {
                for (int i = 0; i < PossibleMatches.GetLength(0); ++i)
                {
                    if (PossibleMatches[i, j] == minvalue)
                        matches.Add(new Point(i + 1, j + 1));
                }
            }

            int nExact = ExactMatches.Count;
            int nPossible = matches.Count;

            if (nExact > 0)
            {
                matches.AddRange(ExactMatches.Select(x => x.Location));
            }

            // filter results / stop overlap
            matches = matches.Where(x => !masks.Stamps.Any(r => r.IntersectsWith(new Rectangle(x.X, x.Y, StampWidth, StampHeight)))).ToList();

            BestMatch = ChooseBest(matches.Distinct().ToArray(), masks, settings);
        }

        internal Rectangle CompareBest(SearchMatrix masks, WhitespaceFinderSettings settings, FindResults other)
        {
            if (BestMatch.IsEmpty || other.BestMatch.IsEmpty)
                throw new InvalidOperationException("Cannot compare. BestMatch is empty");

            Point[] ps = new Point[2];
            ps[0] = this.BestMatch.Location;
            ps[1] = other.BestMatch.Location;
            return ChooseBest(ps, masks, settings);
        }

#if DEBUG
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

            int w = PossibleMatches.GetLength(0);
            int h = PossibleMatches.GetLength(1);
            int bpp = Bitmap.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;

            Bitmap maskBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            BitmapData data = maskBitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            IntPtr intPtr = data.Scan0;

            IEnumerable<int> ints = PossibleMatches.Cast<int>();
            float max = StampWidth * StampHeight;
            float min = ints.Cast<int>().Min();
            Color red = Color.FromArgb(255, 255, 0, 0);
            Color green = Color.FromArgb(255, 0, 255, 0);

            lock (PossibleMatches)
            {
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        Color col;
                        int val = PossibleMatches[i, j];
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
    }
}
