using SkiaSharp;
using System;

namespace ORS_ER.connections
{
    public class Connection
    {
        private string _id = Guid.NewGuid().ToString();
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string FromComponentId { get; set; }
        public string ToComponentId { get; set; }
        public bool IsSelected { get; set; } = false;
        public Connection(string fromId, string toId, string fromComponentId, string toComponentId)
        {
            FromId = fromId;
            ToId = toId;
            FromComponentId = fromComponentId;
            ToComponentId = toComponentId;
        }

        public bool HitTest(SKPoint p, SKPoint a, SKPoint b, float tolerance)
        {
            var minX = MathF.Min(a.X, b.X) - tolerance;
            var maxX = MathF.Max(a.X, b.X) + tolerance;
            var minY = MathF.Min(a.Y, b.Y) - tolerance;
            var maxY = MathF.Max(a.Y, b.Y) + tolerance;
            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
                IsSelected = false;

            var ab = b - a;
            var ap = p - a;

            var abLenSq = ab.X * ab.X + ab.Y * ab.Y;
            if (abLenSq <= float.Epsilon)
                IsSelected = (p - a).Length <= tolerance;

            var t = (ap.X * ab.X + ap.Y * ab.Y) / abLenSq;
            t = Math.Clamp(t, 0f, 1f);

            var closest = new SKPoint(a.X + ab.X * t, a.Y + ab.Y * t);
            IsSelected = (p - closest).Length <= tolerance;
            return IsSelected;
        }
        public string GetId()
        {
            return _id;
        }
        public void SetId(string id)
        {
            _id = id;
        }
        public string ToJson()
        {
            return $"{{ \n" +
                $"\"id\": \"{_id}\",\n" +
                $"\"fromId\": \"{FromId}\",\n" +
                $"\"toId\": \"{ToId}\",\n" +
                $"\"fromComponentId\": \"{FromComponentId}\",\n" +
                $"\"toComponentId\": \"{ToComponentId}\"\n" +
                $"}}\n";
        }
    }
}
