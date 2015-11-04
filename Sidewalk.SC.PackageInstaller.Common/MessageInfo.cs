using System;
using System.Xml.Serialization;

namespace Sidewalk.SC.PackageInstaller.Common
{
    [Serializable]
    public class MessageInfo
    {
        [XmlElement]
        public string Level { get; set; }
        [XmlElement]
        public string Message { get; set; }
        [XmlElement]
        public DateTime Date { get; set; }
        [XmlElement]
        public MessageInfoException Exception { get; set; }
        [XmlElement]
        public MessageInfoProgress Progress { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}{2} - {3}{4}",
                Date.ToString("dd/MM/yyyy HH:mm:ss"),
                Progress == null ? string.Empty : $"({Progress.Processed}/{Progress.TotalToProcess} - {Progress.Percentage}%) - ",
                Level,
                Message,
                Exception == null ? string.Empty : $"<newline>{Exception.ErrorMessage} - {Exception.Source}<newline>{Exception.StackTrace}"
                );
        }
    }

    public class MessageInfoException
    {
        [XmlElement]
        public string ErrorMessage { get; set; }
        [XmlElement]
        public string Source { get; set; }
        [XmlElement]
        public string StackTrace { get; set; }
    }

    public class MessageInfoProgress
    {
        [XmlElement]
        public double Percentage { get; set; }
        [XmlElement]
        public int Processed { get; set; }
        [XmlElement]
        public int TotalToProcess { get; set; }
    }
}
