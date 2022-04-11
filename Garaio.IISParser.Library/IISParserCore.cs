using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Garaio.IISParser.Library
{
    public class IISParserCore : IDisposable
    {
        // How many megabytes can we read for the "fast" ReadAllText method approach, if file is bigger than that, read line by line since we have no memory to
        // load everything at once
        private const int USE_LINE_BY_LINE_READING_THRESHOLD_IN_MB = 100; // defaults to 100 MB        

        // How many lines should we read at one chunk when line by line processing is used
        public const int MAX_LINES_TO_READ_AT_ONCE = 1000;

        // Precalculated in constructor - used to determine if ReadAllText or line by line should be used
        private readonly long _fileSizeInMBs;
        // Precalculated in constructor - used for percentage completed calculations
        private readonly long _fileSizeInBytes;
        // The names of the fields in the log files, specified in the IIS log itself.
        // Real-Life Example: #Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
        private string[] _headerFields;
        // the path to the IIS log, relative to the location of the .exe file
        private string _filePath;
        // If the end of IIS Log has been reached. To be used by library customers to check if processing should be stopped.

        // The file stream used when line by line processing is used
        private FileStream _fileStream;
        // the stream reader (from file) used when line by line processing is used
        private StreamReader _streamReader;

        // Used by customers of this library to see if the log file is still being processed
        public bool ReadingActive { get; private set; } = true;

        public IISParserCore(string filePath)
        {
            if (File.Exists(filePath))
            {
                _filePath = filePath;
                var fileInfo = new FileInfo(filePath);
                _fileSizeInMBs = fileInfo.Length / 1024 / 1024;
                _fileSizeInBytes = fileInfo.Length;
            }
            else
            {
                throw new Exception($"Could not find File {filePath}");
            }
        }

        // if the file size is less than the threshold (defaults to 100MB) use the approach to read all text at once using ReadToEnd
        // otherwise we assume file cannot fit into memory and process via StreamReader line by line
        public IEnumerable<IISLogLine> ParseLog()
        {
            if (IsProcessingAtOnce)
            {
                return ProcessLogAtOnce();
            }

            if (_fileStream == null)
            {
                _fileStream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _streamReader = new StreamReader(_fileStream);
            }

            return ProcessLogLineByLine();

        }

        private IEnumerable<IISLogLine> ProcessLogAtOnce()
        {
            List<IISLogLine> logLines = new List<IISLogLine>();

            var lines = File.ReadAllLines(_filePath);
            foreach (string line in lines)
            {
                ProcessLine(line, logLines);
            }

            ReadingActive = false;

            logLines.RemoveAll(logLine => !logLine.ClientIP.Contains("."));
            return logLines;
        }

        private IEnumerable<IISLogLine> ProcessLogLineByLine()
        {
            List<IISLogLine> logLines = new List<IISLogLine>();
            ReadingActive = false;

            while (_streamReader.Peek() > -1)
            {
                ProcessLine(_streamReader.ReadLine(), logLines);
                if (logLines?.Count > 0 && logLines?.Count % MAX_LINES_TO_READ_AT_ONCE == 0)
                {
                    ReadingActive = true;
                    break;
                }
            }

            logLines.RemoveAll(logLine => !logLine.ClientIP.Contains("."));
            return logLines;
        }

        private void ProcessLine(string line, List<IISLogLine> logLines)
        {
            // Here we get the fields that follow with their IIS specific names
            // Real-Life Example: #Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
            if (line.StartsWith("#Fields:"))
            {
                _headerFields = line.Replace("#Fields: ", string.Empty).Split(' ');
            }

            // if the line is not a comment and header fields are populated correctly process the line - it is a valid log access entry
            if (!line.StartsWith("#") && _headerFields != null)
            {
                string[] fieldsData = line.Split(' ');
                Hashtable logData = GetLogData(fieldsData, _headerFields);
                logLines?.Add(LogLine(logData, line.Length));
            }
        }

        public IISLogLine LogLine(Hashtable logData, int lineLegth)
        {            
            return new IISLogLine
            {
                LineLength = lineLegth,
                DateTimeEvent = GetEventDateTime(logData),
                SiteName = logData["s-sitename"]?.ToString(),
                ComputerName = logData["s-computername"]?.ToString(),
                ServerIP = logData["s-ip"]?.ToString(),
                Method = logData["cs-method"]?.ToString(),
                UriSteam = logData["cs-uri-stem"]?.ToString(),
                UriQuery = logData["cs-uri-query"]?.ToString(),
                Port = logData["s-port"] != null ? int.Parse(logData["s-port"]?.ToString()) : (int?)null,
                UserName = logData["cs-username"]?.ToString(),
                ClientIP = logData["c-ip"]?.ToString(),
                Version = logData["cs-version"]?.ToString(),
                UserAgent = logData["cs(User-Agent)"]?.ToString(),
                Cookie = logData["cs(Cookie)"]?.ToString(),
                Referer = logData["cs(Referer)"]?.ToString(),
                Host = logData["cs-host"]?.ToString(),
                Status = logData["sc-status"] != null ? int.Parse(logData["sc-status"]?.ToString()) : (int?)null,
                SubStatus = logData["sc-substatus"] != null ? int.Parse(logData["sc-substatus"]?.ToString()) : (int?)null,
                Win32Status = logData["sc-win32-status"] != null ? long.Parse(logData["sc-win32-status"]?.ToString()) : (long?)null,
                ServerResponseBytes = logData["sc-bytes"] != null ? int.Parse(logData["sc-bytes"]?.ToString()) : (int?)null,
                ClientRequestBytes = logData["cs-bytes"] != null ? int.Parse(logData["cs-bytes"]?.ToString()) : (int?)null,
                TimeTaken = logData["time-taken"] != null ? int.Parse(logData["time-taken"]?.ToString()) : (int?)null
            };
        }

        private DateTime GetEventDateTime(Hashtable logData)
        {
            DateTime finalDate = DateTime.Parse($"{logData["date"]} {logData["time"]}");
            return finalDate;
        }

        private Hashtable GetLogData(string[] fieldsData, string[] header)
        {
            Hashtable logData = new Hashtable();

            for (int i = 0; i < header.Length; i++)
            {
                logData.Add(header[i], fieldsData[i] == "-" ? null : fieldsData[i]);
            }

            return logData;
        }

        public int GetProcessedPercentageEstimate(List<IISLogLine> logLines)
        {
            int currentProcessedByteSize = 0;
            foreach (var line in logLines)
            {
                currentProcessedByteSize += line.LineLength;
            }

            int percentage = Convert.ToInt32(currentProcessedByteSize / (_fileSizeInBytes / 100));

            return percentage;
        }

        public bool IsProcessingAtOnce
        { 
            get
            {
                return _fileSizeInMBs < USE_LINE_BY_LINE_READING_THRESHOLD_IN_MB;
            }
        }

        public void Dispose() 
        {
            _fileStream?.Dispose();
            _streamReader?.Dispose();
        }
    }
}
