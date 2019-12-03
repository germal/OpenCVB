using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Linq;

namespace Emgu_Classes
{
    public static class DrawSubdivision
    {
        public static void CreateSubdivision(int rows, int cols, int pointCount, out Triangle2DF[] delaunayTriangles, out VoronoiFacet[] voronoiFacets)
        {
            var rand = new Random();
            var pts = Enumerable.Range(0, pointCount).Select(_=>new PointF(rand.Next(0, cols), rand.Next(0, rows))).ToArray();

            using (Subdiv2D subdivision = new Subdiv2D(pts))
            {
                delaunayTriangles = subdivision.GetDelaunayTriangles();
                voronoiFacets = subdivision.GetVoronoiFacets();
            }
        }

        /// <returns>An image representing the planar subvidision of the points</returns>
        public static Mat Draw(int rows, int cols, int pointCount)
        {
            Triangle2DF[] delaunayTriangles;
            VoronoiFacet[] voronoiFacets;
            Random r = new Random((int)(DateTime.Now.Ticks & 0x0000ffff));

            CreateSubdivision(rows, cols, pointCount, out delaunayTriangles, out voronoiFacets);

            Mat img = new Mat(rows, cols, DepthType.Cv8U, 3);

            //Draw the voronoi Facets
            foreach (VoronoiFacet facet in voronoiFacets)
            {
                Point[] polyline = Array.ConvertAll<PointF, Point>(facet.Vertices, Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(polyline))
                using (VectorOfVectorOfPoint vvp = new VectorOfVectorOfPoint(vp))
                {
                    //Draw the facet in color
                    CvInvoke.FillPoly(img, vvp, new Bgr(r.NextDouble() * 120, r.NextDouble() * 120, r.NextDouble() * 120).MCvScalar);

                    //highlight the edge of the facet in black
                    CvInvoke.Polylines(img, vp, true, new Bgr(0, 0, 0).MCvScalar, 2);
                }
                //draw the points associated with each facet in red
                CvInvoke.Circle(img, Point.Round(facet.Point), 5, new Bgr(0, 0, 255).MCvScalar, -1);
            }

            //Draw the Delaunay triangulation
            foreach (Triangle2DF triangle in delaunayTriangles)
            {
                Point[] vertices = Array.ConvertAll<PointF, Point>(triangle.GetVertices(), Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(vertices))
                {
                    CvInvoke.Polylines(img, vp, true, new Bgr(255, 255, 255).MCvScalar);
                }
            }

            CvInvoke.Imshow("Emgu Mat format of img", img);
            return img;
        }
    }
}
