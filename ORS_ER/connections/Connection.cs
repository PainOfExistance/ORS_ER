using SkiaSharp;
using System;

namespace ORS_ER.connections
{
    public class Connection
    {
        private string _id = Guid.NewGuid().ToString();
        public string FromIOId { get; set; }
        public string ToIOId { get; set; }
        public string FromComponentId { get; set; }
        public string ToComponentId { get; set; }
        public bool IsSelected { get; set; } = false;
        public Connection(string fromId, string toId, string fromComponentId, string toComponentId)
        {
            FromIOId = fromId;
            ToIOId = toId;
            FromComponentId = fromComponentId;
            ToComponentId = toComponentId;
        }

        public bool HitTest(SKPoint testPoint, SKPoint lineStart, SKPoint lineEnd, float tolerance)
        {
            var minX = MathF.Min(lineStart.X, lineEnd.X) - tolerance;
            var maxX = MathF.Max(lineStart.X, lineEnd.X) + tolerance;
            var minY = MathF.Min(lineStart.Y, lineEnd.Y) - tolerance;
            var maxY = MathF.Max(lineStart.Y, lineEnd.Y) + tolerance;
            if (testPoint.X < minX || testPoint.X > maxX || testPoint.Y < minY || testPoint.Y > maxY)
                IsSelected = false;

            var lineVector = lineEnd - lineStart;
            var pointVector = testPoint - lineStart;

            var lineLengthSquared = lineVector.X * lineVector.X + lineVector.Y * lineVector.Y;
            if (lineLengthSquared <= float.Epsilon)
                IsSelected = (testPoint - lineStart).Length <= tolerance;

            var projection = (pointVector.X * lineVector.X + pointVector.Y * lineVector.Y) / lineLengthSquared;
            projection = Math.Clamp(projection, 0f, 1f);

            var closestPoint = new SKPoint(lineStart.X + lineVector.X * projection, lineStart.Y + lineVector.Y * projection);
            IsSelected = (testPoint - closestPoint).Length <= tolerance;
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
                $"\"fromId\": \"{FromIOId}\",\n" +
                $"\"toId\": \"{ToIOId}\",\n" +
                $"\"fromComponentId\": \"{FromComponentId}\",\n" +
                $"\"toComponentId\": \"{ToComponentId}\"\n" +
                $"}}\n";
        }
    }
}
