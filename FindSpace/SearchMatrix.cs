using SoupSoftware.FindSpace;
using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SoupSoftware.FindSpace
{

    [StructLayout(LayoutKind.Explicit)]
    public struct sRGB
    {

        public static byte[] Serialize<T>(T data)
    where T : struct
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, data);
            return stream.ToArray();
        }
        public static T Deserialize<T>(byte[] array)
            where T : struct
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }

        [FieldOffset(0)]
        public Int64 Value;
        [FieldOffset(0)]
        public Int32 Sum;
        [FieldOffset(4)]
        public Int32 asRGB;
        [FieldOffset(5)]
        public byte R;
        [FieldOffset(6)]
        public byte G;
        [FieldOffset(7)]
        public byte B;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RGB
    {

        public static implicit operator RGB(int val)
        {
            return new RGB() { Value = val };
        }

        [FieldOffset(0)]
        public int Value;
        [FieldOffset(1)]
        public byte R;
        [FieldOffset(2)]
        public byte G;
        [FieldOffset(3)]
        public byte B;
    }




    public class searchMatrix : ISearchMatrix
    {

        public searchMatrix(Bitmap image, WhitespacerfinderSettings settings)
        {
            Image = image;
            Settings = settings;
            Width = Image.Width;
            Height = Image.Height;
            mask = new byte[Width + 1, Height + 1];
            maskvalsx = new int[Width + 1, Height + 1];
            maskvalsy = new int[Width + 1, Height + 1];
            colSums = new int[Height + 1];
            rowSums = new int[Width + 1];

            CalculateMask();
        }
        public byte[,] mask { get; private set; }
        public int[,] maskvalsx { get; private set; }
        public int[,] maskvalsy { get; private set; }
        public int[,] deepCheck { get; private set; }
        public int[] colSums { get; private set; }
        public int[] rowSums { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void CalculateMask()
        {
            if (Settings.backGroundColor == Color.Empty)
            {
                Settings.backGroundColor = GetModalColor();
            }

            // Generate mask values based on pixel values
            GetBitmapData(out int depth, out byte[] buffer);
            int stride = Width * depth;

            Parallel.For(0, Width, x =>
            {
                for (int y = 0; y < Height; y++)
                {
                    uint col = Getbitval(buffer, (y * stride) + (x * depth));

                    mask[x, y] = (Settings.filterHigh >= col && col >= Settings.filterLow) ? (byte)1 : (byte)0;

                }
            });            

        }


        private readonly Bitmap Image;
        

        readonly WhitespacerfinderSettings Settings;

        // Update mask to mark everything outside of workarea as occupied
        public void MarkMask(Rectangle WorkArea)
        {
            // Set value for columns past the bounds
            Parallel.For(0, Height, y =>
            {
                // Left of image to Left of WorkArea
                for (int x = 0; x < WorkArea.Left; x++)
                {
                    mask[x, y] = 0;
                }

                // Right of WorkArea to Right of image
                for (int x = WorkArea.Right; x < Width; x++)
                {
                    mask[x, y] = 0;
                }
            });

            // set value for rows between the bounds and previously set columns
            Parallel.For(WorkArea.Left, WorkArea.Right, x =>
            {
                // Top of image to Top of WorkArea
                for (int y = 0; y < WorkArea.Top; y++)
                {
                    mask[x, y] = 0;
                }

                // Bottom of WorkArea to Bottom of image
                for (int y = WorkArea.Bottom; y < Height; y++)
                {
                    mask[x, y] = 0;
                }
            });
        }

        public List<Rectangle> Stamps = new List<Rectangle>();

        public void AddStampToMask(Rectangle area)
        {
            Stamps.Add(area);

            // if any part of the stamp is outside of image borders
            if (area.Bottom > Height || area.Right > Width || area.Left < 0 || area.Top < 0)
            {
                throw new ArgumentException($"Rectangle (X={area.X},Y={area.Y}, W={area.Width}, H={area.Height})" +
                    $" outside of image bounds ({Image.Width},{Image.Height})");
            }

            for (int x = area.Left - 1; x < area.Right; x++)
            {
                for (int y = area.Top - 1; y < area.Bottom; y++)
                {
                    mask[x, y] = 0;
                }
            }
        }

        public void UpdateMask(int stampwidth, int stampheight, Rectangle WorkArea)
        {
            CalculateXVectors(stampwidth, WorkArea);
            CalculateYVectors(stampheight, WorkArea);
        }

        private Color GetModalColor()
        {
            const ulong sumMask = UInt64.MaxValue - UInt32.MaxValue;
            const ulong colorMask = UInt32.MaxValue;
            const ulong coarseFilterMask = 0xF7F7F7;                // TODO: adaptive coarseFilter based on max brightness

            GetBitmapData(out int depth, out byte[] buffer);
            int len = Width * Height;
            ulong[] RoundCol = new ulong[len];

            Parallel.For(0, len, (i) => {
                // get colour sum and value in the form XXXXXXXX00YYYYYY
                // where Xs are sum bytes, and Ys are colour value bytes (expected just RGB)
                RoundCol[i] = GetbitvalColorlong(buffer, i * depth);

            });

            IEnumerable<IGrouping<ulong, ulong>> colorGroups = RoundCol.GroupBy(d => d & colorMask); //group based on color as int
            ulong modalColor = colorGroups.OrderBy(g => g.Count()).Last().First(); //most occouring Color

            //cutoff filter (fine based on sum of components)
            ulong highColRange = ((ulong)Settings.calcHighFilter((int)(modalColor & colorMask), Settings.DetectionRange)) << 32;
            ulong lowcolRange = ((ulong)Settings.calcLowFilter((int)(modalColor & colorMask), Settings.DetectionRange)) << 32;

            if ((((modalColor & sumMask) >> 32) > (ulong)(765 - Settings.Brightness)))
                return Color.White;


            //the below filters colors which have close sum of RGBs to the modal color (could be a completly diff color but very close sum)
            ulong[] colorGroupsRefined = colorGroups.Where(g =>
                (highColRange >= (g.First() & sumMask)) &&          // check if sum is between upper and lower limits
                (lowcolRange <= (g.First() & sumMask)))
                                                    .Select(h => h.First()).ToArray();

            //the below filters colors which have close RGBs i.e. only similar colors.
            ulong[] cols = colorGroupsRefined.Where(x => {
                ulong coly = (coarseFilterMask & x);
                return coly == (coarseFilterMask & modalColor);
            }).ToArray();

            int meanCol = (int)cols.Select(x => (int)(x & colorMask)).Average();

            Color modalCol = Color.FromArgb(meanCol);
            return modalCol;
        }

        private static IEnumerable<int> RangeIterator(int start, int stop, int step)
        {
            int x = start;

            do
            {
                yield return x;
                x += step;
                if (step < 0 && x <= stop || 0 < step && stop <= x)
                    break;
            }
            while (true);
        }

        private void CalculateXVectors(int stampwidth, Rectangle WorkArea)
        {

            rowSums = new int[mask.GetLength(1)];
            //cycle through the pixels. Set the Mask Matrix to 1 or 0.  

            Parallel.For(WorkArea.Top, WorkArea.Bottom, (int i) =>
            {
                rowSums[i] = CalculateRowSum(i, WorkArea);
                CalculateRowRuns(i, stampwidth);
            });

        }

        private void GetBitmapData(out int depth, out byte[] buffer)
        {
            BitmapData data = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            IntPtr ptr = data.Scan0;
            depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
            buffer = new byte[data.Width * data.Height * depth];
            for (int y = 0; y < data.Height; y++)
            {
                IntPtr mem = ptr + y * data.Stride;                                     // set scanline ptr to current row
                Marshal.Copy(mem, buffer, y * data.Width * depth, data.Width * depth);  // copy bytes we want, ignoring any buffer bytes
            }
            //System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, buffer.Length);
            //RGB[] f =  sRGB.Deserialize<RGB[]>(buffer)
            Image.UnlockBits(data);
        }


        uint Getbitval(byte[] buffer, int offset)
        {
            uint a = (uint)(buffer[offset + 0] + buffer[offset + 1] + buffer[offset + 2]);
            return a;
        }
        ulong GetbitvalColorlong(byte[] buffer, int offset)
        {
            //pads a long most significant int(32), is a sum of the colors
            //least significant int(32) is the color as int(32)
            ulong a = ((ulong)(buffer[offset + 0] + buffer[offset + 1] + buffer[offset + 2]) << 32) | //sums
              GetbitvalColor(buffer, offset);
            return a;
        }

        uint GetbitvalColor(byte[] buffer, int offset)
        {
            //gets a color as an int (alpha stripped)
            uint a = //sums
                (uint)((buffer[offset + 0]) | //red
                (buffer[offset + 1]) << 8 | //green
                (buffer[offset + 2]) << 16);//blue
            return a;
        }

        private void CalculateRowRuns(int y, int stampwidth)
        {
            int runval = 0;
            for (int x = Width - 1; x >= 0; x--)
            {
                int val = mask[x, y];
                //sum the number of non 0 cells in a row store in 'x' matrix
                runval = val == 0 ? 0 : val + runval;
                int saveval = runval;//(runval < stampwidth) ? 0 : runval;
                maskvalsx[x, y] = saveval;
            }
        }

        private int CalculateRowSum(int y, Rectangle WorkArea)
        {
            int rowSum = 0;
            for (int x = WorkArea.Right - 1; x >= WorkArea.Left; x--)
                rowSum += (1 - mask[x, y]);

            return rowSum;
        }

        private void CalculateYVectors(int stampheight, Rectangle WorkArea)
        {

            colSums = new int[mask.GetLength(0)];
            //now reiterare as sum the number of non 0 cells in a column store in 'y' matrix, we sum from bottom up.

            Parallel.For(WorkArea.Left, WorkArea.Right, (x) => {
                colSums[x] = CalculateColSum(x, WorkArea);
                CalculateColRuns(x, stampheight);
            });


        }

        private void CalculateColRuns(int x, int stampheight)
        {

            int runval = 0;
            for (int y = Height - 1; y >= 0; y--)
            {
                int val = mask[x, y];
                runval = val == 0 ? 0 : val + runval;
                int saveval = runval;//(runval < stampheight) ? 0 : runval;
                maskvalsy[x, y] = saveval;
            }

        }

        private int CalculateColSum(int x, Rectangle WorkArea)
        {
            int sum = 0;
            for (int y = WorkArea.Bottom; y >= WorkArea.Top; y--)
                sum += (1 - mask[x, y]);

            return sum;
        }


    }
}
