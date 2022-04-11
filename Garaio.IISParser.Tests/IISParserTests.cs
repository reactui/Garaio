using Garaio.IISParser.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections;

namespace Garaio.IISParser.Tests
{
    [TestClass]
    public class IISParserTests
    {
        const string IIS_LOG_LINE = "{\"sc-win32-status\":\"995\",\"c-ip\":\"212.120.32.82\",\"time-taken\":\"40478\",\"cs-uri-stem\":\"/\",\"s-port\":\"443\",\"sc-substatus\":\"0\",\"cs-uri-query\":null,\"time\":\"09:30:24\",\"date\":\"2016-02-15\",\"cs(User-Agent)\":\"Mozilla/5.0+(Macintosh;+Intel+Mac+OS+X+10_11_3)+AppleWebKit/601.4.4+(KHTML,+like+Gecko)\",\"cs(Referer)\":null,\"cs-username\":null,\"cs-method\":\"GET\",\"sc-status\":\"200\",\"s-ip\":\"10.10.2.18\"}";
        
        private IISParserCore _parser;

        [TestInitialize]
        public void Initialize()
        {
            _parser = new IISParserCore("IISLog.log");

        }

        [TestCleanup]
        public void CleanUp()
        {
            _parser?.Dispose();
        }


        [TestMethod]
        public void LogLineParsingToHashtableTest()
        {
            Hashtable logLinesHashtable = JsonConvert.DeserializeObject<Hashtable>(IIS_LOG_LINE);

            IISLogLine logline = _parser.LogLine(logLinesHashtable, IIS_LOG_LINE.Length);

            Assert.AreEqual(Convert.ToString(logline.ClientIP), "212.120.32.82");
            Assert.AreEqual(Convert.ToString(logline.Status), "200");
            Assert.AreEqual(Convert.ToString(logline.Method), "GET");
        }
    }
}
