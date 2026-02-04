using SkiaSharp;
using System;

namespace ORS_ER.connections
{
    public class Connection
    {
        private string Id = Guid.NewGuid().ToString();
        public string fromId { get; set; }
        public string toId { get; set; }
        public string fromComponentId { get; set; }
        public string toComponentId { get; set; }
        public bool selected { get; set; } = false;
        public Connection(string fromId, string toId, string fromComponentId, string toComponentId)
        {
            this.fromId = fromId;
            this.toId = toId;
            this.fromComponentId = fromComponentId;
            this.toComponentId = toComponentId;
        }

        public bool HitTest(SKPoint p, SKPoint a, SKPoint b, float tolerance)
        {
            var minX = MathF.Min(a.X, b.X) - tolerance;
            var maxX = MathF.Max(a.X, b.X) + tolerance;
            var minY = MathF.Min(a.Y, b.Y) - tolerance;
            var maxY = MathF.Max(a.Y, b.Y) + tolerance;
            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
                selected = false;

            var ab = b - a;
            var ap = p - a;

            var abLenSq = ab.X * ab.X + ab.Y * ab.Y;
            if (abLenSq <= float.Epsilon)
                selected = (p - a).Length <= tolerance;

            var t = (ap.X * ab.X + ap.Y * ab.Y) / abLenSq;
            t = Math.Clamp(t, 0f, 1f);

            var closest = new SKPoint(a.X + ab.X * t, a.Y + ab.Y * t);
            selected = (p - closest).Length <= tolerance;
            return selected;
        }
        public string GetId()
        {
            return Id;
        }
        public void SetId(string id)
        {
            Id = id;
        }
        public string ToJson()
        {
            return $"{{ \n" +
                $"\"id\": \"{Id}\",\n" +
                $"\"fromId\": \"{fromId}\",\n" +
                $"\"toId\": \"{toId}\",\n" +
                $"\"fromComponentId\": \"{fromComponentId}\",\n" +
                $"\"toComponentId\": \"{toComponentId}\"\n" +
                $"}}\n";
        }
    }
}
