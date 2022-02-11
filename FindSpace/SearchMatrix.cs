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
            mask = new byte[image.Width, image.Height];
            maskvalsx = new int[image.Width, image.Height];
            maskvalsy = new int[image.Width, image.Height];
            colSums = new int[image.Height];
            rowSums = new int[image.Width];
            Image = image;
            Width = Image.Width;
            Height = Image.Height;
            Settings = settings;

        }
        public byte[,] mask { get; private set; }
        public int[,] maskvalsx { get; private set; }
        public int[,] maskvalsy { get; private set; }
        public int[,] deepCheck { get; private set; }
        public int[] colSums { get; private set; }
        public int[] rowSums { get; private set; }
        public bool maskCalculated { get; private set; } = false;
        public void CalculateMask(int stampwidth, int stampheight, Rectangle WorkArea)
        {
            if (Settings.backGroundColor == Color.Empty)
            {

                Settings.backGroundColor = GetModalColor();


            }

            UpdateMask(stampwidth, stampheight, WorkArea);

            maskCalculated = true;
        }



        private Bitmap Image;
        public int Width;
        public int Height;

        WhitespacerfinderSettings Settings;

        private Color backColor { get; set; }

        public void UpdateMask(int stampwidth, int stampheight, Rectangle WorkArea)
        {
            CalculateXVectors(stampwidth, WorkArea);
            CalculateYVectors(stampheight, WorkArea);

            //ApplyEdits();
        }

        private Color GetModalColor()
        {
            const ulong sumMask = ulong.MaxValue - UInt32.MaxValue;
            const ulong colorMask = UInt32.MaxValue;
            const ulong coarseFilterMask = 0xF7F7F7;
            int depth;
            byte[] buffer;
            GetBitmapData(out depth, out buffer);
            int len = buffer.Length / depth;
            ulong[] RoundCol = new ulong[len];

            Parallel.For(0, len, (i) => {
                //todo: write new function below does not work.
                RoundCol[i] = GetbitvalColorlong(buffer, i * depth);

            });



            IEnumerable<IGrouping<ulong, ulong>> colorGroups = RoundCol.GroupBy(d => d & colorMask); //group based on color as int
            ulong modalColor = colorGroups.OrderBy(g => g.Count()).Last().First(); //most occouring Color

            int maskheight = (int)((modalColor & sumMask) >> 32);
            //cutoff filter (fine based on sum of components)
            ulong highColRange = ((ulong)Settings.calcHighFilter((int)modalColor, Settings.DetectionRange)) << 32;
            ulong lowcolRange = ((ulong)Settings.calcLowFilter((int)modalColor, Settings.DetectionRange)) << 32;

            //if ((((modalColor & sumMask) >> 32) > (ulong)(765 - Settings.Brightness)))
            //    return Color.White;


            //the below filters colors which have close sum of RGBs to the modal color (could be a completly diff color but very close sum)
            ulong[] colorGroupsRefined = colorGroups.Where(g =>
            (highColRange >= (g.First() & sumMask)) &&
            (lowcolRange <= (g.First() & sumMask))
            ).Select(h => h.First()).ToArray();

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
            int width = Image.Width;

            int depth;
            byte[] buffer;
            GetBitmapData(out depth, out buffer);

            Parallel.For(WorkArea.Top, WorkArea.Bottom, (int i) =>
            {
                rowSums[i] = CalculateRowSum(i, buffer, depth, width, Settings);
                CalculateRowRuns(i, stampwidth);
            });

        }

        private void GetBitmapData(out int depth, out byte[] buffer)
        {
            BitmapData data = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, Image.PixelFormat);
            IntPtr ptr = data.Scan0;
            depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
            buffer = new byte[data.Stride * Image.Height];
            System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, buffer.Length);
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
                (uint)((buffer[offset + 0])| //red
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
                int saveval = (runval < stampwidth) ? 0 : runval;
                maskvalsx[x, y] = saveval;
            }
        }

        private int CalculateRowSum(int y, byte[] buffer, int depth, int width, Rectangle WorkArea, WhitespacerfinderSettings Settings)
        {
            int rowSum = 0;
            for (int x = WorkArea.Right; x >= WorkArea.Left; x--)
            {
                uint col = Getbitval(buffer, (y * width + x) * depth);
                byte val;
                if (!maskCalculated)
                {
                    val = (Settings.filterHigh >= col && col >= Settings.filterLow) ? (byte)1 : (byte)0;
                    mask[x, y] = val;
                }
                else
                {
                    val = mask[x, y];
                }
                rowSum += (1 - val);
            }
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
                int saveval = (runval < stampheight) ? 0 : runval;
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
