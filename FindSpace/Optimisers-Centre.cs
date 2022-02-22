using SoupSoftware.FindSpace.Interfaces;
using System.Collections.Generic;
using System.Drawing;

namespace SoupSoftware.FindSpace.Optimisers
{




    public class TopCentreOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly ICoordinateSorter cntrResolver = new CentreLinearSorter();
        private readonly IPointGenerator pointgenerator = new HorizontalThenVerticalSweepPointGenerator();
        public override IPointGenerator pointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => cntrResolver; }

        protected override ICoordinateSorter YAxisResolver { get => Lboundresolver; }
    }

    public class MiddleCentreOptimiser : IOptimiser
    {
        public Point GetOptimalPoint(Rectangle rect)
        {
            int x = (rect.Right - rect.Left) / 2;
            int y = (rect.Bottom - rect.Top) / 2;
            return new Point(x, y);
        }

        public IEnumerable<Point> GetOptimisedPoints(Rectangle rect)
        {
            int x = (rect.Right - rect.Left) / 2;
            int y = (rect.Bottom - rect.Top) / 2;

            Optimisers.TargetOptimiser c = new TargetOptimiser(new Point(x, y));
            return c.GetOptimisedPoints(rect);

        }
    }

    public class BottomCentreOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Uboundresolver = new UboundLinearSorter();
        private readonly ICoordinateSorter cntrResolver = new CentreLinearSorter();
        private readonly IPointGenerator pointgenerator = new VerticalThenHorizontalSweepPointGenerator();
        public override IPointGenerator pointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => cntrResolver; }

        protected override ICoordinateSorter YAxisResolver { get => Uboundresolver; }
    }


}
