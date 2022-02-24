
using SoupSoftware.FindSpace.Interfaces;
using System.Drawing;

namespace SoupSoftware.FindSpace.Optimisers
{

    public class BottomRightOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Uboundresolver = new UboundLinearSorter();
        private readonly IPointGenerator pointgenerator = new DiagonalPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => Uboundresolver; }
        protected override ICoordinateSorter YAxisResolver { get => Uboundresolver; }

        public override Rectangle GetFocusArea(Rectangle rect)
        {
            return new Rectangle(rect.X + rect.Width/3, rect.Y + rect.Height / 3, 2 * rect.Width / 3, 2 * rect.Height / 3);
        }
    }

    public class MiddleRightOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Uboundresolver = new UboundLinearSorter();
        private readonly ICoordinateSorter cntrResolver = new CentreLinearSorter();
        private readonly IPointGenerator pointgenerator = new HorizontalThenVerticalSweepPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => Uboundresolver; }
        protected override ICoordinateSorter YAxisResolver { get => cntrResolver; }

        public override Rectangle GetFocusArea(Rectangle rect)
        {
            return new Rectangle(rect.X + rect.Width / 3, rect.Y + rect.Height / 6, 2 * rect.Width / 3, 2 * rect.Height / 3);
        }
    }

    public class TopRightOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly ICoordinateSorter Uboundresolver = new UboundLinearSorter();
        private readonly IPointGenerator pointgenerator = new DiagonalPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => Uboundresolver; }
        protected override ICoordinateSorter YAxisResolver { get => Lboundresolver; }

        public override Rectangle GetFocusArea(Rectangle rect)
        {
            return new Rectangle(rect.X + rect.Width / 3, rect.Y, 2 * rect.Width / 3, 2 * rect.Height / 3);
        }
    }

}
