using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoupSoftware.FindSpace
{
    public class OptimisedSearch : IDeepSearch
    {
        public int Search(SearchMatrix masks, int Left, int Top, int Width, int Height)
        {

            //counts how many zeros in a given sub array.

            int res = 0;
            try
            {
                for (int a = Left; a <= Left + Width; a++)
                {
                    if (masks.MaskValsY[a, Top] < Height) { res++; }
                }
            }
            catch (Exception)
            {


            }


            return res;
        }


    }
}
