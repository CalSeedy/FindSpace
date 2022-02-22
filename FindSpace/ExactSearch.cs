using System;
using System.Linq;

namespace SoupSoftware.FindSpace
{
    public class ExactSearch : IDeepSearch
    {
        public int Search(SearchMatrix masks, int Left, int Top, int Width, int Height)
        {

            //counts how many ones in a given sub array.

            int res = 0;
            try
            {
                // stop any overlaps
                if (masks.Stamps.Any(x => x.IntersectsWith(new System.Drawing.Rectangle(Left, Top, Width, Height))))
                {
                    return System.Int32.MaxValue;
                }

                for (int a = Left; a <= Left + Width; a++)
                {
                    if (masks.MaskValsY[a, Top] < Height)
                    {
                        for (int b = Top; b <= Top + Height; b++)
                        {
                            if (masks.Mask[a, b] == 1)
                            {
                                res++;
                            }
                        }

                    }
                }

            }
            catch (Exception)
            {


            }


            return res;

        }
    }

}
