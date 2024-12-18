using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Xml.Serialization;

namespace FBDApp.Models
{
    [Serializable]
    public class DiagramConfiguration
    {
        public ObservableCollection<SeceModule> Modules { get; set; } = new();
        public ObservableCollection<ConnectionInfo> Connections { get; set; } = new();
    }

    [Serializable]
    public class ConnectionInfo
    {
        [JsonPropertyName("sourceModuleId")]
        public string? SourceModuleId { get; set; }

        [JsonPropertyName("targetModuleId")]
        public string? TargetModuleId { get; set; }

        [JsonPropertyName("sourceConnectorName")]
        public string? SourceConnectorName { get; set; }

        [JsonPropertyName("targetConnectorName")]
        public string? TargetConnectorName { get; set; }

        [JsonPropertyName("startPoint")]
        public SerializablePoint? StartPoint { get; set; }

        [JsonPropertyName("endPoint")]
        public SerializablePoint? EndPoint { get; set; }
    }

    [Serializable]
    public class SerializablePoint
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        public SerializablePoint() { }

        public SerializablePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static SerializablePoint FromWindowsPoint(Point point)
        {
            return new SerializablePoint(point.X, point.Y);
        }

        public Point ToWindowsPoint()
        {
            return new Point(X, Y);
        }
    }
}
