using SoupSoftware.FindSpace.Interfaces;
using SoupSoftware.FindSpace.Optimisers;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SoupSoftware.FindSpace
{


   

    public class ManualMargin : iMargin
    {
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

        public Rectangle GetworkArea(searchMatrix masks)
        {
            if (masks.mask.GetUpperBound(0) - (Left + Right) < 0 ||
                masks.mask.GetUpperBound(1) - (Top + Bottom) < 0)
            {
                throw new IndexOutOfRangeException("The margins are larger than the image");
            }

            return new Rectangle(Left, Top, masks.mask.GetUpperBound(0)  - (Left + Right), masks.mask.GetUpperBound(1)  - (Top + Bottom));
        }


        public bool AutoExpand { get; set; } = true;

       

        public int Left { get; set; }

        public int Right { get; set; }

        public int Top { get; set; }

        public int Bottom { get; set; }
    }

  
    public class WhitespacerfinderSettings
    {

        //this value is used to determine if a pixel is empty or not. Future tweak to find average non black pixel and use the color of this
        private int brightness = 10;
        public int Brightness { get { return brightness; } set { brightness = value;recalcMask(); } }

        public int DetectionRange { get { return (int)(brightness) / 2; } }

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

        public int calcLowFilter(int color,int input)
        {
         int colsum=  (color & 0xFF) + ((color & 0xFF00) >> 8) + ((color & 0xFF0000) >> 16);
            int fl;
               
            switch (colsum)
            {
                case var expression when colsum < input:
                    fl = 0;
                    
                        break;
                case var expression when colsum  > 3 * byte.MaxValue - input:
                   
                    fl = 3 * byte.MaxValue - input;
                    break;
                default:
                    fl = (int)(colsum - input / 2);
                   
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
                case var expression when colsum < input:
                    fl = input;

                    break;
                case var expression when colsum > (3 * byte.MaxValue - input):

                    fl = 3 * byte.MaxValue ;
                    break;
                default:
                    fl = (int)(colsum + input / 2);

                    break;

            }
            return fl;
        }

        public int filterLow { get; private set; } = 755;
        public int filterHigh { get; private set; } = 765;


       

        public IDeepSearch SearchAlgorithm { get; set; } = new ExactSearch();



        public int Padding { get; set; } = 2;

        public iMargin Margins { get; set; } = new ManualMargin(10, 10, 10, 10);


        public bool AutoRotate { get; set; }

        public int CutOffVal { get; } = 3 * byte.MaxValue;


        public IOptimiser Optimiser { get; set; } = new BottomRightOptimiser();

    }


    public interface IDeepSearch
    {
        int Search(ISearchMatrix masks, int Left, int Top, int Width, int Height);
    }

}


