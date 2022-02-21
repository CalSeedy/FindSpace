using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoupSoftware.FindSpace
{
    public class ExactSearch : IDeepSearch
    {
        public int Search(searchMatrix masks, int Left, int Top, int Width, int Height)
        {

            //counts how many zeros in a given sub array.

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
                    if (masks.maskvalsy[a, Top] < Height)
                    {
                        for (int b = Top; b <= Top + Height; b++)
                        {
                            if (masks.mask[a, b] == 1)
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
