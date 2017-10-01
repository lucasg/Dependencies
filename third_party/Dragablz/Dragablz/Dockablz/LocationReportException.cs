using System;

namespace Dragablz.Dockablz
{
    /// <summary>
    /// 
    /// </summary>
    public class LocationReportException : Exception
    {
        public LocationReportException()
        {
        }

        public LocationReportException(string message) : base(message)
        {
        }

        public LocationReportException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}