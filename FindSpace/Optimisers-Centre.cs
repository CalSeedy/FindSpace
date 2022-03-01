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
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => cntrResolver; }

        protected override ICoordinateSorter YAxisResolver { get => Lboundresolver; }

        public override Rectangle GetFocusArea(Rectangle rect)
        {
            return new Rectangle(rect.X + rect.Width / 10, rect.Y, 4 * rect.Width / 5, 4 * rect.Height / 5);
        }
    }

    public class MiddleCentreOptimiser : IOptimiser
    {
        public Rectangle GetFocusArea(Rectangle rect)
        {
            int x = (rect.Right - rect.Left) / 2;
            int y = (rect.Bottom - rect.Top) / 2;

            Optimisers.TargetOptimiser c = new TargetOptimiser(new Point(x, y));
            return c.GetFocusArea(rect);
        }

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
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => cntrResolver; }

        protected override ICoordinateSorter YAxisResolver { get => Uboundresolver; }
        public override Rectangle GetFocusArea(Rectangle rect)
        {
            int y = rect.Y + rect.Height / 3;
            return new Rectangle(rect.X + rect.Width / 10, y, 4 * rect.Width / 5, 4 * rect.Height / 5);
        }
    }


}
