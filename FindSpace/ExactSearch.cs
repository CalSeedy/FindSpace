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
        public int Search(ISearchMatrix masks, int Left, int Top, int Width, int Height)
        {

            {

                //counts how many zeros in a given sub array.

                int res = 0;
                try
                {
                    for (int a = Left; a <= Left + Width; a++)
                    {
                        if (masks.maskvalsy[a, Top] < Height)
                        {
                            for (int b = Top; b <= Top + Height; b++)
                            {
                                if (masks.mask[a, b] == 0)
                                {
                                    res++;
                                }
                            }

                        }
                    }
                    /*
                    for (int a = Top; a <= Top + Height; a++)
                    {
                        if (masks.maskvalsx[Left, a] < Width)
                        {
                            for (int b = Left; b <= Left+ Width; b++)
                            {
                                if (masks.mask[b, a] == 0)
                                {
                                    res++;
                                }
                            }

                        }
                    }
                    */
                }
                catch (Exception)
                {


                }


                return res;
            }
        }
    }

}
