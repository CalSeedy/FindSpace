using SoupSoftware.FindSpace.Interfaces;
using SoupSoftware.FindSpace.Optimisers;
using System;
using System.Drawing;
using System.Linq;

namespace SoupSoftware.FindSpace
{
    public class AutomaticMargin : IAutoMargin
    {
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }
        public bool Resized { get; private set; }
        public bool AutoExpand { get; set; } = false;

        public AutomaticMargin()
        {
            Left = 0;
            Right = 0;
            Top = 0;
            Bottom = 0;
        }

        public AutomaticMargin(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public AutomaticMargin(int size)
        {
            Left = size;
            Right = size;
            Top = size;
            Bottom = size;
        }

        public Rectangle GetWorkArea(SearchMatrix masks)
        {
            if (masks.Mask.GetUpperBound(0) - (Left + Right) < 0 ||
                masks.Mask.GetUpperBound(1) - (Top + Bottom) < 0)
            {
                throw new IndexOutOfRangeException("The margins are larger than the image");
            }

            if (!Resized)
            {
                Resize(masks);
                masks.MarkMask(new Rectangle(Left, Top, masks.Width - (Left + Right), masks.Height - (Top + Bottom)));
            }
            return new Rectangle(Left, Top, masks.Width - (Left + Right), masks.Height - (Top + Bottom));
        }

        // Would be preferred to be private - but no private interface methods
        // This should only be called in this class
        public void Resize(SearchMatrix mask)
        {

            bool sumsArentZeros = mask.RowSums.Any(x => x > 0) && mask.ColSums.Any(x => x > 0);
            if (!sumsArentZeros)                // false == just an array of zeros, use original margins
            {
                Resized = true; 
                return;
            }

            // filter = 10% of the Difference between Max and Min row/col sums
            float filter = (Math.Max(mask.RowSums.Max(), mask.ColSums.Max()) - 
                            Math.Min(mask.RowSums.Min(), mask.ColSums.Min())
                            ) * 0.025f;
                       

            var ygroup = mask.ColSums.Select((x, n) => new { Sum = x, idx = n })
                                .Where(s => s.Sum >= filter)
                                .OrderBy(g => g.idx);
            int xmin = ygroup.First().idx;
            int xmax = ygroup.Last().idx;

            var xgroup = mask.RowSums.Select((y, n) => new { Sum = y, idx = n })
                                .Where(s => s.Sum >= filter)
                                .OrderBy(g => g.idx);
            int ymin = xgroup.First().idx;
            int ymax = xgroup.Last().idx;

            Left = xmin;
            Right = mask.Width - xmax;
            Top = ymin;
            Bottom = mask.Height - ymax;

            Resized = true;
        }

        public void FromRect(Rectangle rect)
        {
            Left = rect.Left;
            Right = rect.Right;
            Top = rect.Top;
            Bottom = rect.Bottom;
        }

        
    }

    public class ManualMargin : IMargin
    {
        public bool AutoExpand { get; set; } = true;
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }

        public ManualMargin(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public ManualMargin(int size)
        {
            Left = size;
            Right = size;
            Top = size;
            Bottom = size;
        }

        public Rectangle GetWorkArea(SearchMatrix masks)
        {
            if (masks.Mask.GetUpperBound(0) - (Left + Right) < 0 ||
                masks.Mask.GetUpperBound(1) - (Top + Bottom) < 0)
            {
                throw new IndexOutOfRangeException("The margins are larger than the image");
            }

            return new Rectangle(Left, Top, masks.Mask.GetUpperBound(0) - (Left + Right), masks.Mask.GetUpperBound(1) - (Top + Bottom));
        }

        public void FromRect(Rectangle rect)
        {
            Left = rect.Left;
            Right = rect.Right;
            Top = rect.Top;
            Bottom = rect.Bottom;
        }
    }
    
  
    public class WhitespaceFinderSettings
    {

        //this value is used to determine if a pixel is empty or not. Future tweak to find average non black pixel and use the color of this
        private int brightness = 10;
        public int Brightness { get { return brightness; } set { brightness = value;recalcMask(); } }

        public int DetectionRange { get { return brightness / 2; } }

        public Color backgroundcolor = Color.Empty;
        public Color backGroundColor { 
            get { return backgroundcolor; }
            set {
                backgroundcolor = value;
                if (backgroundcolor != Color.Empty)
                {
                    recalcMask();
                }
            
            } } 

        private void recalcMask()
        {
            filterLow = calcLowFilter(backgroundcolor.ToArgb(), brightness);
            filterHigh = calcHighFilter(backgroundcolor.ToArgb(), brightness);
        }

        public int calcLowFilter(int color, int input)
        {
         int colsum = (color & 0xFF) + ((color & 0xFF00) >> 8) + ((color & 0xFF0000) >> 16);
            int fl;
               
            switch (colsum)
            {
                case var _ when colsum < input:
                    fl = 0;
                    break;

                case var _ when colsum  > 3 * byte.MaxValue - input:
                    fl = 3 * byte.MaxValue - input;
                    break;

                default:
                    fl = colsum - input / 2;
                    break;

            }
            return fl;
        }

        public int calcHighFilter(int colorRGB, int input)
        {
            
            int colsum = (colorRGB & 0xFF) + ((colorRGB & 0xFF00) >> 8) + ((colorRGB & 0xFF0000) >> 16);
            int fl;

            switch (colsum)
            {
                case var _ when colsum < input:
                    fl = input;
                    break;

                case var _ when colsum > (3 * byte.MaxValue - input):
                    fl = 3 * byte.MaxValue ;
                    break;

                default:
                    fl = colsum + input / 2;
                    break;

            }
            return fl;
        }

        public int filterLow { get; private set; } = 755;
        public int filterHigh { get; private set; } = 765;


        public byte PercentageToScan { get; set; } = 25;

        public IDeepSearch SearchAlgorithm { get; set; } = new ExactSearch();

        public int Padding { get; set; } = 2;

        public IMargin Margins { get; set; } = new ManualMargin(10, 10, 10, 10);

        public bool AutoRotate { get; set; } = true;

        public int CutOffVal { get; } = 3 * byte.MaxValue;

        // Sum of weights should be 1.0f
        public float GroupingWeight { get; set; } = 0.5f;
        public float DistanceWeight { get; set; } = 0.5f;

        public byte PercentageOverlap { get; set; } = 0;

        public ushort BailOnExact { get; set; } = 3;

        public IOptimiser Optimiser { get; set; } = new BottomRightOptimiser();

    }


    public interface IDeepSearch
    {
        int Search(SearchMatrix masks, int Left, int Top, int Width, int Height);
    }

}


