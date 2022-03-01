using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace SoupSoftware.FindSpace.Results
{
    public class FindResults
    {
        private Rectangle ScanArea;
        private int minvalue = System.Int32.MaxValue;

        public bool ContainsResults { get; set; } = false;
        public int StampWidth { get; set; } = 0;
        public int StampHeight { get; set; } = 0;
        public Rectangle? BestMatch { get; private set; }
        public List<Rectangle> ExactMatches { get; private set; } = new List<Rectangle>();
        public int[,] PossibleMatches;
        public int[,] PossibleMatchesRotated;
        public bool IncludeRotated { get; set; } = false;
        public int MinValue
        {
            get
            {
                if (minvalue == System.Int32.MaxValue)
                {
                    int minNormal = SquareIterator(PossibleMatches, ScanArea).Min();

                    if (IncludeRotated)
                    {
                        int minRotated = SquareIterator(PossibleMatchesRotated, ScanArea).Min();

                        minvalue = Math.Min(minRotated, minNormal);
                    }
                    else
                    {
                        minvalue = minNormal;
                    }
                }
                return minvalue;
            }
        }

        public FindResults(int width, int height, Rectangle scanArea)
        {
            PossibleMatches = new int[width, height];
            PossibleMatchesRotated = new int[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    PossibleMatches[i, j] = System.Int32.MaxValue;
                    PossibleMatchesRotated[i, j] = System.Int32.MaxValue;
                }
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


        private Rectangle? ChooseBest(Rectangle[] points, SearchMatrix masks)
        {
            int N = points.Length;
            if (N == 0)
                return null;
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            Point optimal = masks.Settings.Optimiser.GetOptimalPoint(ScanArea);

            // if we have any stamps, get avg stamp position
            double distanceToOthers = 0;
            int C = masks.Stamps.Count;
            Point? group = null;
            if (C > 0)
            {
                int avgX = 0;
                int avgY = 0;

                for (int k = 0; k < C; k++)
                {
                    avgX += masks.Stamps[k].X;
                    avgY += masks.Stamps[k].Y;
                }
                avgX /= C;
                avgY /= C;

                group = new Point(avgX, avgY);
            }

            double[] values = new double[N];
            for (int i = 0; i < N; ++i)
            {
                Rectangle match = points[i];
                double distanceToOpt = optimal.DistanceTo(match.Location);

                if (group != null)
                {
                    distanceToOthers = match.Location.DistanceTo(group.Value);
                }
                // filter results / stop overlap
                if (masks.Stamps.Any(r => r.IntersectsWith(match)))
                {
                    values[i] = Int64.MaxValue;
                }
                else
                {
                    double overlapPcnt = (StampWidth != match.Width ? PossibleMatchesRotated[match.Location.X, match.Location.Y] :
                                    PossibleMatches[match.Location.X, match.Location.Y]);
                    if (overlapPcnt == 0.0f)
                    {
                        values[i] = Math.Pow(masks.Settings.DistanceWeight, 2) * distanceToOpt
                                        + Math.Pow(masks.Settings.GroupingWeight, 2) * distanceToOthers;
                    }
                    else
                    {
                        values[i] = overlapPcnt + overlapPcnt * (masks.Settings.DistanceWeight * distanceToOpt
                                                            + masks.Settings.GroupingWeight * distanceToOthers);
                    }
                }
            }

            KeyValuePair<Point, double>[] pairings = new KeyValuePair<Point, double>[N];
            for (int i = 0; i < N; i++)
            {
                pairings[i] = new KeyValuePair<Point, double>(points[i].Location, values[i]);
            }

            Rectangle? output = points.Where(p => p.Location == pairings.OrderBy(x => x.Value).Select(x => x.Key).First())?.First();

            //sw.Stop();
            //Trace.WriteLine($"ChooseBest: {sw.ElapsedMilliseconds}ms");
            return output;
        }

        internal void FilterMatches(SearchMatrix masks)
        {
            /*
            lock (PossibleMatches)
            {
                Stopwatch sw2 = new Stopwatch();
                sw2.Start();
                Point[] possibles = GetMinimumPoints(masks);
                ConcurrentQueue<Point> queue = new ConcurrentQueue<Point>();
                Parallel.ForEach(possibles, (p) =>
                {
                    if (PossibleMatches[p.X, p.Y] <= minvalue + masks.Settings.Forgiveness * 100f)
                        queue.Enqueue(new Point(p.X, p.Y));                 
                });
                sw2.Stop();
                Trace.WriteLine($"ForEach: {sw2.ElapsedMilliseconds} ms");
                
                matches = queue.ToList();
                Trace.WriteLine("Potentials: " + matches.Count);
            }
            */
            Rectangle focus = masks.Settings.Optimiser.GetFocusArea(ScanArea);
            IEnumerable<Point> optimals = masks.Settings.Optimiser.GetOptimisedPoints(focus);
            List<Rectangle> matches = new List<Rectangle>();
            foreach (var a in ExactMatches)
                matches.Add(a);

            int compareVal = masks.Settings.PercentageOverlap;
            ConcurrentQueue<Rectangle> queue = new ConcurrentQueue<Rectangle>();
            Parallel.ForEach(optimals, (p) =>
            {
                if (PossibleMatches[p.X, p.Y] <= compareVal)
                    queue.Enqueue(new Rectangle(p, new Size(StampWidth, StampHeight)));

                if (IncludeRotated)
                {
                    if (PossibleMatchesRotated[p.X, p.Y] <= compareVal)
                        queue.Enqueue(new Rectangle(p, new Size(StampHeight, StampWidth)));
                }    
            });

            matches.AddRange(queue.ToArray());

            BestMatch = ChooseBest(matches.Distinct().ToArray(), masks);

        }

        #region DEBUG
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
                        long val = PossibleMatches[i, j];
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
        #endregion
    }
}
