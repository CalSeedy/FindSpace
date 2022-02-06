using SoupSoftware.FindSpace;
using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace SoupSoftware.WhiteSpace
{
    public class searchMatrix: ISearchMatrix
    {

        public searchMatrix(Bitmap image, WhitespacerfinderSettings settings)
        {
            mask = new byte[image.Width, image.Height];
            maskvalsx = new int[image.Width, image.Height];
            maskvalsy = new int[image.Width, image.Height];
            colSums = new int[image.Height];
            rowSums = new int[image.Width];
            Image = image;
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
            GetBits colorEvaluation;
            if (backColor == Color.Empty)
            {

                backColor = GetModalColor();


            }
            else
            {
                backColor = Settings.backGroundColor;
            }

            if (backColor == Color.White)
            {
                colorEvaluation = GetbitvalWhite;
            }
            else
            {
                colorEvaluation = GetbitvalColor;
            }


            CalculateXVectors(stampwidth, WorkArea, colorEvaluation);
            CalculateYVectors(stampheight, WorkArea);
        }



        private Bitmap Image;
        WhitespacerfinderSettings Settings;
        private byte redMask;
        private byte greenMask;
        private byte blueMask;
        private const byte autoDetectColorMask = 0b11111000;


        private Color backColor { get; set; }

        //public void UpdateMask(int stampwidth, int stampheight, Rectangle ClearArea)
        //{


        //    CalculateXVectors(stampwidth, WorkArea);
        //    CalculateYVectors(stampheight, WorkArea);
        //}


        private Color GetModalColor()
        {
        
            
            int depth;
            byte[] buffer;
            GetBitmapData(Image, out depth, out buffer);
            int len = buffer.Length / depth;
            int[] RoundCol = new int[len];
            Parallel.For(0,len, (i) => { 
                //todo: write new function below does not work.
                 RoundCol[i]=GetbitColor(buffer, i*depth);
            });

            int foo = RoundCol.GroupBy(d => d).OrderBy(g => g.Count()).Last().Key;
            Color c = Color.FromArgb(foo);
            return c;
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


        private void CalculateXVectors(int stampwidth, Rectangle WorkArea, GetBits colorEvaluation)
        {

            int rowSum;


            rowSums = new int[mask.GetLength(1)];
            //cycle through the pixels. Set the Mask Matrix to 1 or 0.  
            int width = Image.Width;

            int depth;
            byte[] buffer;
            GetBitmapData(Image, out depth, out buffer);

            Parallel.For(WorkArea.Top, WorkArea.Bottom, (int i) =>
            {
                
                rowSum = CalculateRowSum(stampwidth, WorkArea, i, buffer, depth, width, Settings, colorEvaluation);
                rowSums[i] = rowSum;
            });

        }

        internal static void GetBitmapData(Bitmap image, out int depth, out byte[] buffer)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
            IntPtr ptr = data.Scan0;
            depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
            buffer = new byte[data.Stride * image.Height];
            System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, buffer.Length);
            image.UnlockBits(data);
        }

        delegate int  GetBits(byte[] buffer, int x, int y, int width, int depth);

        int GetbitvalWhite(byte[] buffer, int x, int y, int width, int depth)
        {
            var offset = ((y * width) + x) * depth;
            int a = buffer[offset + 0] + buffer[offset + 1] + buffer[offset + 2];
            return a;
        }
        int GetbitvalColor(byte[] buffer, int x, int y, int width, int depth)
        {
            var offset = ((y * width) + x) * depth;
            int a = buffer[offset + 0] + buffer[offset + 1] + buffer[offset + 2];
            return a;
        }

        int GetbitColor(byte[] buffer, int offset )
        {
            int ColorMask = (buffer[offset + 0] & autoDetectColorMask)<<16 | (buffer[offset + 1] & autoDetectColorMask)<<8 | (buffer[offset + 2] & autoDetectColorMask);
            return ColorMask;
        }


        private int CalculateRowSum(int stampwidth, Rectangle WorkArea, int y, byte[] buffer, int depth, int width, WhitespacerfinderSettings Settings, GetBits colorEvaluation)
        {
            int rowSum = 0;
            int runval = 0;
            int filterVal = Settings.CutOffVal - Settings.Brightness;
            for (int x = WorkArea.Right; x >= WorkArea.Left; x--)
            {
                byte val;
                if (!maskCalculated)
                {
                  val = (colorEvaluation.Invoke(buffer,x,y,width,depth) > filterVal) ? (byte)1 : (byte)0;

                    mask[x, y] = val;
                }
                else
                {
                    val = mask[x, y];
                }
                //also whilst we are iterating sum the number of non 0 cells in a row store in 'x' matrix
                runval = val == 0 ? 0 : val + runval;
                int saveval = (runval < stampwidth) ? 0 : runval;
                rowSum = (1 - val) + rowSum;
                maskvalsx[x, y] = saveval;
            }


            return rowSum;
        }

        private void CalculateYVectors(int stampheight, Rectangle WorkArea)
        {

            colSums = new int[mask.GetLength(0)];
            //now reiterare as sum the number of non 0 cells in a column store in 'y' matrix, we sum from bottom up.

            int colSum;
            Parallel.For(WorkArea.Left, WorkArea.Right, (x) =>
            {
                

                colSum = CalculateColSum(stampheight, WorkArea, x);
                colSums[x] = colSum;
            });



        }

        private int CalculateColSum(int stampheight, Rectangle WorkArea, int x)
        {
            int colSum = 0;
            int runval = 0;
            for (int y = WorkArea.Bottom; y >= WorkArea.Top; y--)
            {
                int val = mask[x, y];
                runval = val == 0 ? 0 : val + runval;
                colSum = (1 - val) + colSum;
                int saveval = (runval < stampheight) ? 0 : runval;
                maskvalsy[x, y] = saveval;
            }
            runval = 0;
            return colSum;
        }


    }
}
