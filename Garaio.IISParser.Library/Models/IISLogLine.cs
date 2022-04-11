using System;

namespace Garaio.IISParser.Library
{
    public class IISLogLine
    {
        public int LineLength { get; set; }
        public DateTime DateTimeEvent { get; set; }
        public string SiteName { get; set; }
        public string ComputerName { get; set; }
        public string ServerIP { get; set; }
        public string Method { get; set; }
        public string UriSteam { get; set; }
        public string UriQuery { get; set; }
        public int? Port { get; set; }
        public string UserName { get; set; }
        public string ClientIP { get; set; }
        public string Version { get; set; }
        public string UserAgent { get; set; }
        public string Cookie { get; set; }
        public string Referer { get; set; }
        public string Host { get; set; }
        public int? Status { get; set; }
        public int? SubStatus { get; set; }
        public long? Win32Status { get; set; }
        public int? ServerResponseBytes { get; set; }
        public int? ClientRequestBytes { get; set; }
        public int? TimeTaken { get; set; }
    }
}
