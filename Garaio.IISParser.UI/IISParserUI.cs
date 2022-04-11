using Garaio.IISParser.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Garaio.IISParser.UI
{
    class IISParser
    {
        private const string DEFAULT_FILE_NAME = "IISLog.log";
        static void Main(string[] args)
        {            
            var logs = new List<IISLogLine>();
            // use default log file name if no command line parameters are given, otherwise use the first parameter as the log filename
            string fileName = args.Length > 0 ? args[0] : DEFAULT_FILE_NAME;

            
            using (IISParserCore parser = new IISParserCore(Path.Combine(Environment.CurrentDirectory, fileName)))
            {
                while (parser.ReadingActive)
                {                    
                    logs.AddRange(parser.ParseLog().ToList());
                    int percentageEstimate = parser.GetProcessedPercentageEstimate(logs);

                    if (!parser.IsProcessingAtOnce)
                    {
                        Console.WriteLine($"Percentage of logs processed: {percentageEstimate}%");
                    }
                    
                }

                Console.WriteLine($"Percentage of logs processed: 100%");
            }

            Console.WriteLine("=====================================================================================================");
            Console.WriteLine("   Done. Results by IP, Count in brackets and resolved IP to FQDN (is resolving is possible follow.  ");
            Console.WriteLine("=====================================================================================================");
            Console.WriteLine();

            // use LINQ to group log entries by IP
            var groupedLogs = logs.GroupBy(l => l.ClientIP).ToList();

            foreach (var log in groupedLogs)
            {
                string requestIP = log.FirstOrDefault().ClientIP;
                long requestCount = log.Count();
               
                try
                {
                    Console.WriteLine($"{requestIP} ({requestCount}) : {Dns.GetHostEntry(requestIP).HostName}");
                }
                catch (Exception)
                {
                    Console.WriteLine($"{requestIP} ({requestCount})  : Cannot resolve host");                        
                }
            }

            Console.WriteLine();
            Console.WriteLine("=================");
            Console.WriteLine("Press any key to quit.");
            Console.WriteLine("=================");
            
            Console.ReadKey();
        }


    }
}
