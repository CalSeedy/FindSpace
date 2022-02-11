using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoupSoftware.FindSpace.Interfaces;
using SoupSoftware.FindSpace.Optimisers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace FindSpaceTests
{
    [TestClass]
    public class FindSapceTests
    {

        [DataTestMethod]
        [DataRow(typeof(VerticalThenHorizontalSweepPointGenerator), new int[] { 10, 10 })]
        [DataRow(typeof(VerticalThenHorizontalSweepPointGenerator), new int[] { 16, 10 })]
        [DataRow(typeof(VerticalThenHorizontalSweepPointGenerator), new int[] { 10, 16 })]

        [DataRow(typeof(HorizontalThenVerticalSweepPointGenerator), new int[] { 10, 10 })]
        [DataRow(typeof(HorizontalThenVerticalSweepPointGenerator), new int[] { 16, 10 })]
        [DataRow(typeof(HorizontalThenVerticalSweepPointGenerator), new int[] { 10, 16 })]

        [DataRow(typeof(DiagonalPointGenerator), new int[] { 10, 10 })]
        [DataRow(typeof(DiagonalPointGenerator), new int[] { 16, 10 })]
        [DataRow(typeof(DiagonalPointGenerator), new int[] { 10, 16 })]
        public void PointGeneratorTests(Type t, int[] dims)
        {
            List<Point> p = new List<Point>();
            IPointGenerator gen;
            if ((t.GetConstructors().Where(c => c.GetParameters().Length == 0).ToArray().Length) == 1)
                gen = (IPointGenerator)Activator.CreateInstance(t);
            else
                gen = (IPointGenerator)Activator.CreateInstance(t, new Point(dims[0] / 2, dims[1] / 2));

            int[] r = Enumerable.Range(0, dims[0]).ToArray();
            int[] r2 = Enumerable.Range(0, dims[1]).ToArray();
            p.AddRange(gen.GetOptimisedPoints(r, r2));

            Assert.AreEqual(dims[0] * dims[1], p.Distinct().Count());
        }

        [DataTestMethod]
        [DataRow(typeof(circularPointGenerator), new int[] { 10, 10 })]
        [DataRow(typeof(circularPointGenerator), new int[] { 16, 10 })]
        [DataRow(typeof(circularPointGenerator), new int[] { 10, 16 })]
        public void CircularPointGeneratorTests(Type t, int[] dims)
        {
            List<Point> p = new List<Point>();
            IPointGenerator genCentre = (IPointGenerator)Activator.CreateInstance(t, new Point(dims[0] / 2, dims[1] / 2));
            IPointGenerator genOffCentre = (IPointGenerator)Activator.CreateInstance(t, new Point(3 * dims[0] / 4, dims[1] / 4));

            int[] r = Enumerable.Range(0, dims[0]).ToArray();
            int[] r2 = Enumerable.Range(0, dims[1]).ToArray();
            p.AddRange(genCentre.GetOptimisedPoints(r, r2));

            Assert.AreEqual(dims[0] * dims[1], p.Distinct().Count());

            p.Clear();
            r = Enumerable.Range(0, dims[0]).ToArray();
            r2 = Enumerable.Range(0, dims[1]).ToArray();
            p.AddRange(genOffCentre.GetOptimisedPoints(r, r2));

            Assert.AreEqual(dims[0] * dims[1], p.Distinct().Count());
        }



    }
}
