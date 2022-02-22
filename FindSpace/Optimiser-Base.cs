using SoupSoftware.FindSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SoupSoftware.FindSpace.Optimisers
{

    public abstract class LinearPointOptimiser : IOptimiser
    {

        protected abstract ICoordinateSorter XAxisResolver { get; }

        protected abstract ICoordinateSorter YAxisResolver { get; }

        public abstract IPointGenerator PointGenerator { get; }

        public IEnumerable<Point> GetOptimisedPoints(Rectangle rect)
        {
            foreach (Point p in PointGenerator.GetOptimisedPoints(
                 XAxisResolver.GetOptimisedPositions(rect.Left, rect.Right),
                  YAxisResolver.GetOptimisedPositions(rect.Top, rect.Bottom)))
            {
                yield return p;
            }
        }

        public Point GetOptimalPoint(Rectangle rect)
        {
            return PointGenerator.GetOptimisedPoints(XAxisResolver.GetOptimisedPositions(rect.Left, rect.Right),
                  YAxisResolver.GetOptimisedPositions(rect.Top, rect.Bottom)).First();
        }

    }

    public class TargetOptimiser : IOptimiser

    {
        private System.Drawing.Point target;

        private IPointGenerator pointgenerator;
        public TargetOptimiser(System.Drawing.Point Target)
        {
            target = Target;
            pointgenerator = new CircularPointGenerator(target);
        }

        public Point GetOptimalPoint(Rectangle rect)
        {
            return target;
        }

        public IEnumerable<Point> GetOptimisedPoints(Rectangle rect)
        {
            return pointgenerator.GetOptimisedPoints(Enumerable.Range(rect.Left,
                rect.Right), Enumerable.Range(rect.Top, rect.Bottom));
        }
    }



    public class VerticalThenHorizontalSweepPointGenerator : IPointGenerator
    {
        public IEnumerable<Point> GetOptimisedPoints(IEnumerable<int> xcoords, IEnumerable<int> ycoords)
        {

            foreach (int x in xcoords)
            {
                foreach (int y in ycoords)
                {
                    yield return new Point(x, y);
                }
            }

        }
    }


    public class DiagonalPointGenerator : IPointGenerator
    {
        public IEnumerable<Point> GetOptimisedPoints(IEnumerable<int> xcoords, IEnumerable<int> ycoords)
        {

            int maxX = xcoords.Max();
            int maxY = ycoords.Max();
            int minX = xcoords.Min();
            int minY = ycoords.Min();
            int xStep = xcoords.Take(2).Last() - xcoords.First();
            int yStep = ycoords.Take(2).Last() - ycoords.First();

            int xctr = xcoords.First();
            int yctr = ycoords.First();




            do
            {

                int y = yctr;
                int x = xctr;
                do
                {

                    // Trace.TraceInformation(string.Format("{0},{1}", new string[] { x.ToString(), y.ToString() }));
                    yield return new Point(x, y);
                    x = x - xStep;
                    y = y + yStep;
                }
                while (x <= maxX && x >= minX && y <= maxY && y >= minY);
                xctr = xctr + xStep;
                yctr = ycoords.First();
            } while (xctr <= maxX && xctr >= minX);
            xctr = xcoords.Last();
            yctr = ycoords.Take(2).Last();

            do
            {
                int y = yctr;
                int x = xctr;

                do
                {


                    yield return new Point(x, y);
                    x = x - xStep;
                    y = y + yStep;
                }
                while (x <= maxX && x >= minX && y <= maxY && y >= minY);
                yctr = yctr + yStep;
                xctr = xcoords.Last();
            } while (yctr <= maxY && yctr >= minY);

        }
    }


    public class HorizontalThenVerticalSweepPointGenerator : IPointGenerator
    {
        public IEnumerable<Point> GetOptimisedPoints(IEnumerable<int> xcoords, IEnumerable<int> ycoords)
        {

            foreach (int y in ycoords)
            {
                foreach (int x in xcoords)
                {
                    yield return new Point(x, y);
                }
            }

        }

    }


    public class CircularPointGenerator : IPointGenerator
    {


        Point Target;
        public CircularPointGenerator(Point target)
        {
            Target = target;
        }

        public IEnumerable<Point> GetOptimisedPoints(IEnumerable<int> xcoords, IEnumerable<int> ycoords)
        {
            var pts = xcoords.SelectMany(x => ycoords.Select(y => new Point(x, y))).Select(r => new Tuple<double, double, Point>(r.DistanceTo(Target), r.AbsAngle(Target), r)).AsQueryable();

            foreach (Tuple<Double, Double, Point> pt in pts.OrderBy(q => q.Item1).ThenBy(r => r.Item2))
            {
                yield return pt.Item3;
            }

        }

    }

}

public static class GeoLibrary
{
    public static double DistanceTo(this Point pt1, Point pt2)
    {
        return (Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
    }


    public static double AbsAngle(this Point to, Point from)
    {

        double dx = to.X - from.X;
        double dy = to.Y - from.Y;

        return Math.Atan2(dy, dx);
    }



}



