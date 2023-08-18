using Emgu.CV;
using Emgu.CV.Util;

namespace CameraX.Helpers
{
    public class EmguHelper
    {
        public VectorOfPoint[] SelectContours(VectorOfVectorOfPoint contours)
        {
            VectorOfPoint[] top5Contours = new VectorOfPoint[5];
            for (int i = 0; i < 5; i++)
            {
                top5Contours[i] = contours[i];
            }
            return top5Contours;
        }

        public void SelectTopContour(VectorOfPoint[] contours)
        {
            foreach (var contourVector in contours)
            {
                using (var contour = new VectorOfPoint())
                {
                    var peri = CvInvoke.ArcLength(contourVector, true);
                    CvInvoke.ApproxPolyDP(contourVector, contour, 0.1 * peri, true);
                    if (contour.ToArray().Length == 4 && CvInvoke.IsContourConvex(contour))
                        ContourCoordinates = contour;
                }
            }
        }
        public VectorOfPoint ContourCoordinates { get; private set; } = new VectorOfPoint();
    }
}