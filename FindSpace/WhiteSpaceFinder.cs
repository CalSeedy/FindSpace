using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using SoupSoftware.FindSpace.Results;

namespace SoupSoftware.FindSpace
{
    public class WhiteSpaceFinder
    {
        private readonly Bitmap image;
        private SearchMatrix masks;
        private Rectangle WorkArea;

        public WhitespacerfinderSettings Settings { get; private set; }

        public WhiteSpaceFinder(Bitmap orig) => new WhiteSpaceFinder(orig, new WhitespacerfinderSettings());

        public WhiteSpaceFinder(Bitmap Image, WhitespacerfinderSettings settings)
        {
            using (Bitmap newBmp = new Bitmap(Image))
            {
                image = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            }
            Settings = settings;
            Init(image);
        }

        private void Init(Bitmap image)
        {
            masks = new SearchMatrix(image, this.Settings);
            WorkArea = Settings.Margins.GetWorkArea(masks);
        }

        private Rectangle SelectBestArea(Rectangle ScanArea, FindResults findReturn, FindResults findReturn90)
        {
            Rectangle place2 = new Rectangle(WorkArea.Left, WorkArea.Top, findReturn.StampWidth, findReturn.StampHeight);
            FindResults target = findReturn.MinValue <= findReturn90.MinValue ? findReturn : findReturn90;
            findReturn.FilterMatches(masks, Settings);
            if (findReturn90.ContainsResults)
            {
                findReturn90.FilterMatches(masks, Settings);
                place2 = findReturn.CompareBest(masks, Settings, findReturn90);
            }
            else
            {
                place2 = target.BestMatch;
            }


            place2 = new Rectangle(place2.X + Settings.Padding,
                                    place2.Y + Settings.Padding,
                                    place2.Width - 2 * Settings.Padding,
                                    place2.Height - 2 * Settings.Padding);
#if DEBUG && TRACE
            Trace.WriteLine($"Position found: ({place2.X},{place2.Y}) : W={place2.Width}, H={place2.Height}");
#endif
            return place2;
        }


        private FindResults FindLocations(int stampwidth, int stampheight, SearchMatrix masks, Rectangle ScanArea)
        {
            int deepCheckFail = (stampheight * stampwidth) + 1;
            FindResults findReturn = new FindResults(masks.Mask.GetLength(0), masks.Mask.GetLength(1), ScanArea);
            findReturn.StampWidth = stampwidth;
            findReturn.StampHeight = stampheight;
            findReturn.ContainsResults = true;
            //iterate the 2 matrices, if the top left corners X & Y sums is greater than the sticker dimensions its a potential location, 
            // aswe add the loctions transposing the loction to the top left.
            foreach (Point p in this.Settings.Optimiser.GetOptimisedPoints(ScanArea))
            {
                if (masks.MaskValsX[p.X, p.Y] >= stampwidth && masks.MaskValsY[p.X, p.Y] >= stampheight)
                {

                    findReturn.PossibleMatches[p.X, p.Y] = Settings.SearchAlgorithm.Search(masks,
                        p.X, p.Y, stampwidth, stampheight);



                    if (findReturn.PossibleMatches[p.X, p.Y] == 0)
                    {
                        //if there are no zeros we can use this space, currently the first found place is used. (The algo is pre-optimised for desired location).

                        findReturn.ExactMatches.Add(new Rectangle(
                                                     p.X, p.Y, stampwidth, stampheight
                                                    ));
                        //bail on first find exact macth.
                        //return findReturn;

                        if (findReturn.ExactMatches.Count >= Settings.BailOnExact)
                            return findReturn;
                    }
                }
                else
                {
                    // if the top left corner is not greater than sticker size just skip it..
                    //when it comes to secondary searches we set the number of conflicting spaces to the max value possible.
                    findReturn.PossibleMatches[p.X, p.Y] = deepCheckFail;
                }
            }

            return findReturn;
        }

        public Rectangle FindSpaceAt(Rectangle stamp, Point pt)
        {
            this.Settings.Optimiser = new Optimisers.TargetOptimiser(pt);
            return FindSpaceFor(stamp);
        }

        public Rectangle[] SortStamps(Rectangle[] stamps)
        {
            // sort by area, then by the w/h ratio
            Rectangle[] sorted = stamps.OrderByDescending(gr => (gr.Height * gr.Width)).ThenByDescending(gr => Math.Max(gr.Width, gr.Height) / Math.Min(gr.Width, gr.Height)).ToArray();
            return sorted;
        }

#if DEBUG
        public Rectangle[] FindSpaceFor(Rectangle[] stamps, string filename = "")
        {
            Stopwatch sw = new Stopwatch();
#else
        public Rectangle[] FindSpaceFor(Rectangle[] stamps)
        {
#endif
            stamps = SortStamps(stamps);

            List<Rectangle> results = new List<Rectangle>();

            int count = 0;
            foreach (Rectangle stamp in stamps)
            {
#if DEBUG
                sw.Restart();
                Rectangle res = FindSpaceFor(stamp, filename, count);
#else
                Rectangle res = FindSpaceFor(stamp);
#endif

                masks.AddStampToMask(res);
                results.Add(res);
                count++;
#if DEBUG && TRACE
                Trace.WriteLine($"Stamp #{count} Found... {sw.ElapsedMilliseconds} ms");
#endif
            }


            return results.ToArray();
        }

#if DEBUG
        public Rectangle FindSpaceFor(Rectangle stamp, string filename = "", int count = 0)
        {
#else
        public Rectangle FindSpaceFor(Rectangle stamp)
        {
#endif
            if ((WorkArea.Height - (2 * Settings.Padding + stamp.Height) < 0) ||
              (WorkArea.Width - (2 * Settings.Padding + stamp.Width) < 0)
              )
            {
                throw new Exception("The image is smaller than the stamp + padding + margin");
            }

            int stampwidth = stamp.Width + 2 * Settings.Padding;
            int stampheight = stamp.Height + 2 * Settings.Padding;

            // subtract stamp width and height to keep the search restricted to the top left pxel of the stamp (avoids fits past the bounds of the image)
            Rectangle TopLeftBiasedScanArea = new Rectangle(WorkArea.Left, WorkArea.Top, WorkArea.Width - stampwidth , WorkArea.Height - stampheight);
            masks.UpdateMask(stampwidth, stampheight, WorkArea);            

            if (Settings.Margins is IAutoMargin)
            {
                WorkArea = Settings.Margins.GetWorkArea(masks);
                masks.UpdateMask(stampwidth, stampheight, WorkArea);
                TopLeftBiasedScanArea = new Rectangle(WorkArea.Left, WorkArea.Top, WorkArea.Width - stampwidth, WorkArea.Height - stampheight);
#if DEBUG && TRACE
                Trace.WriteLine($"Margins: L={Settings.Margins.Left}, T={Settings.Margins.Top}, R={Settings.Margins.Right}, B={Settings.Margins.Bottom}");
#endif
            }

            FindResults findReturn = FindLocations(stampwidth, stampheight, masks, TopLeftBiasedScanArea);
            FindResults findReturn90 = new FindResults(image.Width, image.Height, TopLeftBiasedScanArea);
#if DEBUG && POSSIBLES
            findReturn.PossiblesToBitmap($"TestImages\\Possibles\\{count}-findReturn.bmp");
#endif
            if (Settings.AutoRotate && !findReturn.HasExactMatches() && stampheight != stampwidth)
            {
                findReturn90 = FindLocations(stampheight, stampwidth, masks, TopLeftBiasedScanArea);
#if DEBUG && POSSIBLES
            findReturn90.PossiblesToBitmap($"TestImages\\Possibles\\{count}-findReturn90.bmp");
#endif
            }
#if DEBUG
            if (filename.Length > 0)
            {
                string extension = System.IO.Path.GetExtension(filename);
#if (CSVS)
                MaskToCSV(filename.Replace(extension, $"-{count}{extension}"));
#endif
#if (MASKS)
                string dir = System.IO.Path.GetDirectoryName(filename);
                string maskFile = filename.Replace(extension, "-mask"+ count + Settings.Optimiser.GetType().Name + extension);
                maskFile = maskFile.Replace(dir, dir + "\\Masks");
                MaskToBitmap(maskFile);//, true);
#endif
            }
#endif
            return SelectBestArea(TopLeftBiasedScanArea, findReturn, findReturn90);
        }

#region Debug Methods

#if DEBUG
        public void MaskToCSV(string filepath, char delimiter = ',', bool runs = true, bool sums = true)
        {
            string extension = System.IO.Path.GetExtension(filepath);

            void WriteMask2D<T>(StreamWriter sw, T[,] arr)
            {
                lock (arr)
                {
                    for (int y = 0; y < arr.GetLength(1); y++)
                    {
                        for (int x = 0; x < arr.GetLength(0); x++)
                        {
                            sw.Write($"{arr[x, y]}{delimiter}");
                        }
                        sw.Write("\n");
                    }
                }
            }

            void WriteMask<T>(StreamWriter sw, T[] arr)
            {
                lock (arr)
                {
                    for (int x = 0; x < arr.Length; x++)
                    {
                        sw.Write($"{arr[x]}{delimiter}");
                    }
                    sw.Write("\n");
                }
            }

            filepath = filepath.Replace(extension, ".csv");
            extension = ".csv";
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                WriteMask2D(sw, masks.Mask);
            }

            if (runs)
            {
                using (StreamWriter sw = new StreamWriter(filepath.Replace(extension, $"-RowRuns{extension}")))
                {
                    WriteMask2D(sw, masks.MaskValsX);

                }
                using (StreamWriter sw = new StreamWriter(filepath.Replace(extension, $"-ColRuns{extension}")))
                {
                    WriteMask2D(sw, masks.MaskValsY);

                }
            }

            if (sums)
            {
                using (StreamWriter sw = new StreamWriter(filepath.Replace(extension, $"-RowSums{extension}")))
                {
                    WriteMask(sw, masks.RowSums);
                }
                using (StreamWriter sw = new StreamWriter(filepath.Replace(extension, $"-ColSums{extension}")))
                {
                    WriteMask(sw, masks.ColSums);
                }
            }
        }

        public void MaskToBitmap(string filepath, bool runs = false)
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
                else
                    return ColorInterp((percentage - 0.5f) / 0.5f, Start, End);
            }

            int w = image.Width;
            int h = image.Height;
            int bpp = Image.GetPixelFormatSize(image.PixelFormat) / 8;

            void WriteInts(string path, int[,] arr)
            {
                Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);

                BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                IntPtr ptr = bitmapData.Scan0;

                lock (arr)
                {
                    IEnumerable<int> ints = arr.Cast<int>();
                    float max = ints.Max();
                    float min = ints.Min();
                    Color red = Color.FromArgb(255, 255, 0, 0);
                    Color green = Color.FromArgb(255, 0, 255, 0);
                    for (int j = 0; j < h; j++)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            Color col = GradientPick((arr[i, j] - min) / max, red, green);
                            System.Runtime.InteropServices.Marshal.WriteByte(ptr, (j * bitmapData.Stride) + (i * bpp) + 0, col.B);
                            System.Runtime.InteropServices.Marshal.WriteByte(ptr, (j * bitmapData.Stride) + (i * bpp) + 1, col.G);
                            System.Runtime.InteropServices.Marshal.WriteByte(ptr, (j * bitmapData.Stride) + (i * bpp) + 2, col.R);
                        }
                    }
                }

                bmp.UnlockBits(bitmapData);

                bmp.Save(path);
            }

            Bitmap maskBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            BitmapData data = maskBitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            IntPtr intPtr = data.Scan0;


            lock (masks.Mask)
            {
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        bool maskFilter = masks.Mask[i, j] == 0;
                        System.Runtime.InteropServices.Marshal.WriteByte(intPtr, (j * data.Stride) + (i * bpp) + 0, 0);
                        System.Runtime.InteropServices.Marshal.WriteByte(intPtr, (j * data.Stride) + (i * bpp) + 1, maskFilter ? (byte)0 : (byte)255);
                        System.Runtime.InteropServices.Marshal.WriteByte(intPtr, (j * data.Stride) + (i * bpp) + 2, maskFilter ? (byte)255 : (byte)0);
                    }
                }
            }

            //RGB[] f = sRGB.Deserialize<RGB[]>(buffer)
            maskBitmap.UnlockBits(data);

            maskBitmap.Save(filepath);

            if (runs)
            {
                WriteInts(filepath.Replace(".bmp", "-ColRuns.bmp"), masks.MaskValsY);

                WriteInts(filepath.Replace(".bmp", "-RowRuns.bmp"), masks.MaskValsX);
            }
        }
#endif

#endregion

    }


}


