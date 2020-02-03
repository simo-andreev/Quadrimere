using System.Numerics;
using Emotion.Primitives;

namespace Solution12.Graphics.Data
{
    public class TileQuadrilateral : Transform
    {
        public Vector2 Vertex0;
        public Vector2 Vertex1;
        public Vector2 Vertex2;
        public Vector2 Vertex3;
        
        public byte Height;

        public TileQuadrilateral(float x, float y, byte z, Vector2 size)
        {
            Vertex0 = new Vector2(x, y);
            Vertex1 = new Vector2(x + size.X, y);
            Vertex2 = new Vector2(x + size.X, y + size.Y);
            Vertex3 = new Vector2(x, y + size.Y);

            Height = z;

            Bounds = Rectangle.BoundsFromPolygonPoints(new[] {Vertex0, Vertex1, Vertex2, Vertex3});
        }
    }
}