using SoupSoftware.FindSpace.Interfaces;

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
    }

    public class TopOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly ICoordinateSorter centreResolver = new CentreLinearSorter();
        protected override ICoordinateSorter XAxisResolver { get => centreResolver; }
        private readonly IPointGenerator pointgenerator = new HorizontalThenVerticalSweepPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }

        protected override ICoordinateSorter YAxisResolver { get => Lboundresolver; }
    }
}
