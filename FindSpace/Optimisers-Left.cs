using SoupSoftware.FindSpace.Interfaces;

namespace SoupSoftware.FindSpace.Optimisers
{

    public class TopLeftOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly IPointGenerator pointgenerator = new DiagonalPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }

        protected override ICoordinateSorter XAxisResolver { get => Lboundresolver; }

        protected override ICoordinateSorter YAxisResolver { get => Lboundresolver; }
    }

    public class MiddleLeftOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly ICoordinateSorter cntrResolver = new CentreLinearSorter();
        private readonly IPointGenerator pointgenerator = new VerticalThenHorizontalSweepPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => Lboundresolver; }

        protected override ICoordinateSorter YAxisResolver { get => cntrResolver; }

    }

    public class BottomLeftOptimiser : LinearPointOptimiser
    {
        private readonly ICoordinateSorter Lboundresolver = new LboundLinearSorter();
        private readonly ICoordinateSorter Uboundresolver = new UboundLinearSorter();
        private readonly IPointGenerator pointgenerator = new DiagonalPointGenerator();
        public override IPointGenerator PointGenerator { get => pointgenerator; }
        protected override ICoordinateSorter XAxisResolver { get => Lboundresolver; }

        protected override ICoordinateSorter YAxisResolver { get => Uboundresolver; }
    }


}
