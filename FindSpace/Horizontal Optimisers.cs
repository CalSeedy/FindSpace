using SoupSoftware.FindSpace.Interfaces;
using System.Drawing;

namespace SoupSoftware.FindSpace.Optimisers
{
    public class BottomOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Uboundresolver = new UboundLinearSorter();
        private readonly ICoordinateSorter centreResolver = new CentreLinearSorter();
        protected override ICoordinateSorter XAxisResolver { get => centreResolver; }
        private readonly IPointGenerator pointgenerator = new HorizontalThenVerticalSweepPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }

        protected override ICoordinateSorter YAxisResolver { get => Uboundresolver; }

        public override Rectangle GetFocusArea(Rectangle rect)
        {
            return new Rectangle(rect.X, rect.Y + (2 * rect.Height / 3), rect.Width, rect.Height / 3);
        }
    }

    public class TopOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly ICoordinateSorter centreResolver = new CentreLinearSorter();
        protected override ICoordinateSorter XAxisResolver { get => centreResolver; }
        private readonly IPointGenerator pointgenerator = new HorizontalThenVerticalSweepPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }

        protected override ICoordinateSorter YAxisResolver { get => Lboundresolver; }

        public override Rectangle GetFocusArea(Rectangle rect)
        {
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 3);
        }
    }
}
