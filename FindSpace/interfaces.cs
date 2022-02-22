using System.Collections.Generic;
using System.Drawing;

namespace SoupSoftware.FindSpace.Interfaces
{

    public interface IOptimiser
    {

        IEnumerable<Point> GetOptimisedPoints(Rectangle rect);

        Point GetOptimalPoint(Rectangle rect);
    }

    public interface ICoordinateSorter
    {
        IEnumerable<int> GetOptimisedPositions(int lower, int upper);

    }

    public interface IPointGenerator
    {
        IEnumerable<Point> GetOptimisedPoints(IEnumerable<int> xcoords, IEnumerable<int> ycoords);

    }

    public interface IMargin
    {
        bool AutoExpand { get; }
        int Left { get; }
        int Right { get; }
        int Top { get; }
        int Bottom { get; }
        Rectangle GetWorkArea(SearchMatrix masks);
        void FromRect(Rectangle rect);
    }

    public interface IAutoMargin : IMargin
    {
        bool Resized { get; }

        void Resize(SearchMatrix masks);
    }

    public interface ISearchMatrix {

        byte[,] Mask { get;  }
        int[,] MaskValsX { get;  }
        int[,] MaskValsY { get;  }
        int[,] DeepCheck { get; }
        int[] ColSums { get; }
        int[] RowSums { get;  }
        void CalculateMask();
        void UpdateMask(int stampwidth, int stampheight, Rectangle WorkArea);

    }
}

