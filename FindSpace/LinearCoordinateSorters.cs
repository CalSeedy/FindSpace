using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoupSoftware.FindSpace
{

    public class UboundLinearSorter : ICoordinateSorter
    {
        public IEnumerable<int> GetOptimisedPositions(int lower, int upper)
        {
            if ((upper - lower) < 1)
            {
                throw new IndexOutOfRangeException();
            }
            return Enumerable.Range(lower, upper - lower + 1).Reverse().ToArray();

        }
    }

    public class LboundLinearSorter : ICoordinateSorter
    {
        public IEnumerable<int> GetOptimisedPositions(int lower, int upper)
        {
            if ((upper - lower) < 1)
            {
                throw new IndexOutOfRangeException();
            }
            return Enumerable.Range(lower, upper - lower + 1).ToArray();

        }
    }

    public class CentreLinearSorter : ICoordinateSorter
    {


        public IEnumerable<int> GetOptimisedPositions(int lower, int upper)
        {
            if ((upper - lower) < 1)
            {
                throw new IndexOutOfRangeException();
            }

            int ArrayLength = (upper - lower + 1);
            int[] retval = new int[ArrayLength];
            int startArrayIndex;
            int lowerval;
            int upperval;
            if (((upper - lower) % 2) == 0)
            {
                startArrayIndex = 1;
                retval[0] = (upper + lower) / 2;
                lowerval = retval[0] - 1;
                upperval = retval[0] + 1;
            }
            else
            {
                startArrayIndex = 0;
                upperval = (int)Math.Round((decimal)(upper - lower) / 2, 0, MidpointRounding.AwayFromZero);
                lowerval = upperval - 1;
            }

            do
            {
                retval[startArrayIndex] = upperval;
                startArrayIndex++;
                retval[startArrayIndex] = lowerval;
                startArrayIndex++;
                lowerval--;
                upperval++;

            } while (lowerval >= lower);
            return retval;

        }
    }

    public class Targetresolver : ICoordinateSorter
    {

        private readonly int Target;


        public Targetresolver(int target)
        {
            this.Target = target;
        }

        public IEnumerable<int> GetOptimisedPositions(int lower, int upper)
        {
            if ((upper - lower) < 1)
            {
                throw new IndexOutOfRangeException();
            }

            int ArrayLength = (upper - lower + 1);
            int[] retval = new int[ArrayLength];
            int startArrayIndex;
            int lowerval;
            int upperval;

            startArrayIndex = 1;
            retval[0] = Target;
            lowerval = Target - 1;
            upperval = Target + 1;

            int loopsMax = Math.Max(Target - lower, upper - Target);

            do
            {
                if (upperval <= upper) { { retval[startArrayIndex] = upperval; ; upperval++; startArrayIndex++; } }

                if (lowerval >= lower) { retval[startArrayIndex] = lowerval; ; lowerval--; startArrayIndex++; }


                loopsMax--;
            } while (loopsMax > -1);
            return retval;

        }
    }



}
