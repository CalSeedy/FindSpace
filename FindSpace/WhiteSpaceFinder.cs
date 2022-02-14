using SoupSoftware.FindSpace;
using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using WhitSpace.Results;

namespace SoupSoftware.FindSpace
{


  
   

    public class WhiteSpaceFinder
    {

        private readonly Bitmap image;
        private searchMatrix masks;

        private Rectangle WorkArea;
        private bool AutoMarginResized = false;

        private void init(Bitmap image)
        {
            masks = new searchMatrix(image, this.Settings);
            WorkArea = Settings.Margins.GetworkArea(masks);

        }

        public WhiteSpaceFinder(Bitmap orig)
        {


            using (Bitmap newBmp = new Bitmap(orig))
            {
                image = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            }

            Settings = new WhitespacerfinderSettings();
            init(image);
        }
        public WhiteSpaceFinder(Bitmap Image, WhitespacerfinderSettings settings)
        {
            using (Bitmap newBmp = new Bitmap(Image))
            {
                image = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            }
            Settings = settings;
            init(image);
        }
        public WhitespacerfinderSettings Settings { get; private set; }
        private int forgiveness;

        public Rectangle? FindSpaceAt(Rectangle stamp, Point pt)
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

        public Rectangle?[] FindSpaceFor(Rectangle[] stamps)
        {
            stamps = SortStamps(stamps);

            List<Rectangle?> results = new List<Rectangle?>();
            foreach (Rectangle stamp in stamps)
            {
                Rectangle? res = FindSpaceFor(stamp);
                
                masks.AddStampToMask(res);

                results.Add(res);
            }


            return results.ToArray();
        }

        public Rectangle? FindSpaceFor(Rectangle stamp)
        {
            if ((WorkArea.Height - (2 * Settings.Padding + stamp.Height) < 0) ||
              (WorkArea.Width - (2 * Settings.Padding + stamp.Width) < 0)
              )
            {
                throw new Exception("The image is smaller than the stamp + padding + margin");
            }

            int stampwidth = stamp.Width + 2 * Settings.Padding;
            int stampheight = stamp.Height + 2 * Settings.Padding;

            Rectangle TopLeftBiasedScanArea = new Rectangle(WorkArea.Left, WorkArea.Top, WorkArea.Width - stampwidth, WorkArea.Height - stampheight);
            masks.CalculateMask(stampwidth, stampheight, WorkArea);
            
            FindResults findReturn;
            FindResults findReturn90 = new FindResults(image.Width, image.Height, TopLeftBiasedScanArea);

            if (Settings.Margins.AutoExpand)
            {
                //this keeps the allocated space for the placement close to the contents.
                TopLeftBiasedScanArea = RefineScanArea(masks,  TopLeftBiasedScanArea);
            }

            if (Settings.Margins is AutomaticMargin)
            {
                if (!AutoMarginResized)
                {
                    (Settings.Margins as AutomaticMargin).Resize(masks);
                    WorkArea = Settings.Margins.GetworkArea(masks);
                    masks.UpdateMask(stampwidth, stampheight, WorkArea);
                }
                AutoMarginResized = true;
            }
            findReturn = FindLocations(stampwidth, stampheight, masks, WorkArea);

            if (Settings.AutoRotate && !findReturn.hasExactMatches() && stampheight != stampwidth)
            {
                findReturn90 = FindLocations(stampheight, stampwidth, masks, WorkArea);

            }
            return SelectBestArea(stampwidth, stampheight, WorkArea, findReturn, findReturn90);
        }

        private static Rectangle RefineScanArea(searchMatrix searchMatrix, Rectangle ScanArea)
        {
            //optimise the size of the grid/

            int newLeft = ScanArea.Left;
            for (int x = ScanArea.Left; x <= ScanArea.Right; x++)
            {
                if (searchMatrix.colSums[x] != 0)
                {
                    newLeft = Math.Max(ScanArea.Left, x);
                    break;
                }
            }

            int newTop = ScanArea.Top;
            for (int y = ScanArea.Top; y <= ScanArea.Bottom; y++)
            {
                if (searchMatrix.rowSums[y] != 0)
                {
                    newTop = Math.Max(ScanArea.Top, y);
                    break;
                }
            }


            int newRight = ScanArea.Right;
            for (int x = ScanArea.Right; x >= ScanArea.Left; x--)
            {
                if (searchMatrix.colSums[x] != 0)
                {
                    newRight = Math.Min(ScanArea.Right, x);
                    break;
                }
            }

            int newBottom = ScanArea.Bottom;
            for (int y = ScanArea.Bottom; y >= ScanArea.Top; y--)
            {
                if (searchMatrix.rowSums[y] != 0)
                {
                    newBottom = Math.Min(ScanArea.Bottom, y);
                    break;
                }
            }
            ScanArea = new Rectangle(newLeft, newTop, newRight - newLeft, newBottom - newTop); ;
            return ScanArea;
        }

        private Rectangle? SelectBestArea(int stampwidth, int stampheight, Rectangle ScanArea, FindResults findReturn, FindResults findReturn90)
        {
            Rectangle place2 = new Rectangle(0, 0, stampwidth, stampheight);
            if (findReturn.hasExactMatches())
            {
                place2 = findReturn.exactMatches.First();
            }
            else if (findReturn90.hasExactMatches())
            {
                place2 = findReturn90.exactMatches.First();
            }
            else
            {
                FindResults target = findReturn.minValue <= findReturn90.minValue ? findReturn : findReturn90;
                foreach (Point p in this.Settings.Optimiser.GetOptimisedPoints(ScanArea))
                {
                    if (target.possibleMatches[p.X, p.Y] == target.minValue)
                    {
                        place2 = new Rectangle(p.X, p.Y, stampwidth, stampheight);
                        break;
                    }
                }
            }

            // account for stamps on the edge where (X, Y) + (sw, sh) > (w, h)
            int? newX = null;
            int? newY = null;
            if (place2.X + stampwidth > image.Width)
                newX = place2.X - stampwidth;

            if (place2.Y + stampheight > image.Height)
                newY = place2.Y - stampheight;

            place2 = new Rectangle((newX is null ? place2.X  : newX.Value) + Settings.Padding, (newY is null ? place2.Y : newY.Value) + Settings.Padding, stampwidth - 2 * Settings.Padding, stampheight - 2 * Settings.Padding);
            return place2;
        }

        

        

        

        private FindResults FindLocations(int stampwidth, int stampheight, searchMatrix masks, Rectangle ScanArea)
        {

            

           


            int deepCheckFail = (stampheight * stampwidth) + 1;
            FindResults findReturn = new FindResults(masks.mask.GetLength(0), masks.mask.GetLength(1), ScanArea);
            findReturn.containsResults = true;
            //iterate the 2 matrices, if the top left corners X & Y sums is greater than the sticker dimensions its a potential location, 
            // aswe add the loctions transposing the loction to the top left.
            foreach (Point p in this.Settings.Optimiser.GetOptimisedPoints(ScanArea))


            {
                if (masks.maskvalsx[p.X, p.Y] > stampwidth && masks.maskvalsy[p.X, p.Y] > stampheight)
                {

                    findReturn.possibleMatches[p.X, p.Y] = Settings.SearchAlgorithm.Search(masks,
                        p.X, p.Y, stampwidth, stampheight);



                    if (findReturn.possibleMatches[p.X, p.Y] == 0)
                    {
                        //if there are no zeros we can use this space, currently the first found place is used. (The algo is pre-optimised for desired location).

                        findReturn.exactMatches.Add(new Rectangle(
                                                     p.X, p.Y, stampwidth, stampheight
                                                    ));
                        //bail on first find exact macth.
                        return findReturn;
                    }
                }
                else
                {
                    // if the top left corner is not greater than sticker size just skip it..
                    //when it comes to secondary searches we set the number of conflicting spaces to the max value possible.
                    findReturn.possibleMatches[p.X, p.Y] = deepCheckFail;
                }
            }

            return findReturn;
        }

        public void MaskToBitmap(string filepath)
        {
            int w = image.Width;
            int h = image.Height;
            int bpp = 3; //rgb


            Bitmap maskBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            BitmapData data = maskBitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            IntPtr ptr = data.Scan0;

            lock (masks.mask)
            {
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        bool maskFilter = masks.mask[i, j] == 0;
                        System.Runtime.InteropServices.Marshal.WriteByte(ptr, (j * w * bpp) + (i * bpp) + 0, 0);
                        System.Runtime.InteropServices.Marshal.WriteByte(ptr, (j * w * bpp) + (i * bpp) + 1, maskFilter ? (byte)0 : (byte)255);
                        System.Runtime.InteropServices.Marshal.WriteByte(ptr, (j * w * bpp) + (i * bpp) + 2, maskFilter ? (byte)255 : (byte)0);
                    }
                }
            }
            //RGB[] f = sRGB.Deserialize<RGB[]>(buffer)
            maskBitmap.UnlockBits(data);

            maskBitmap.Save(filepath);
        }


    }


}


