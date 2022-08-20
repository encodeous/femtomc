using System.Data;
using System.Xml;
using System.Xml.Linq;
using Serilog;
using Serilog.Events;

namespace femtomc;

public class Log4jParser
{
    public static async Task RedirectUntilEnd(StreamReader output, string redactSession)
    {
        string log4j = "http://jakarta.apache.org/log4j";
        XmlReaderSettings settings = new XmlReaderSettings()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            Async = true
        };
        NameTable nt = new NameTable();
        XmlNamespaceManager mgr = new XmlNamespaceManager(nt);
        mgr.AddNamespace("log4j", log4j);

        XmlParserContext pc = new XmlParserContext(nt, mgr, "", XmlSpace.Default);

        XNamespace log4jNs = log4j;

        using (XmlReader xr = XmlReader.Create(output, settings, pc))
        {
            while (await xr.ReadAsync())
            {
                if (xr.NodeType == XmlNodeType.Element && xr.LocalName.ToLower() == "event")
                {
                    using (XmlReader eventReader = xr.ReadSubtree())
                    {
                        await eventReader.ReadAsync();
                        
                        XElement eventEl = XNode.ReadFrom(eventReader) as XElement;
                        var (logger, timestamp, level, thread, message) =
                            (eventEl.Attribute("logger"), eventEl.Attribute("timestamp"), eventEl.Attribute("level"),
                                eventEl.Attribute("thread"), eventEl.Element(log4jNs + "Message"));
                        LogEventLevel? slLevel = level.Value switch
                        {
                            "DEBUG" => LogEventLevel.Debug,
                            "INFO" => LogEventLevel.Information,
                            "WARN" => LogEventLevel.Warning,
                            "ERROR" => LogEventLevel.Error,
                            "FATAL" => LogEventLevel.Fatal,
                            "OFF" => LogEventLevel.Fatal,
                            "TRACE" => LogEventLevel.Debug,
                            _ => null
                        };
                        var formattedMessage = $"[{thread.Value}] {message.Value}";
                        formattedMessage = formattedMessage.Replace(redactSession, " <redacted> ");
                        if (slLevel is not null)
                        {
                            Log.Write(slLevel.Value, formattedMessage);
                        }
                        else
                        {
                            Log.Write(LogEventLevel.Information, formattedMessage);
                        }
                        eventReader.Close();
                    }
                }
            }
            xr.Close();
        }
    }
}