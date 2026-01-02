using Codenizer.HttpClient.Testable.SentryServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.SentryServer
{
    public class EnvelopeParser
    {
        public static List<object> Parse(string content)
        {
            var results = new List<object>();
            var lines = content.Split('\n');
            
            // Expected Format (NDJSON):
            // Header JSON
            // Item Header JSON
            // Item Payload (JSON or bytes, but we assume JSON for simulation)
            // Item Header JSON...
            // Item Payload...
            
            // Line 0: Header
            if (lines.Length < 3) return results; // Minimum: Header, ItemHeader, Payload? Or sometimes payload is separate line?
            
            // Envelope format allows for:
            // Header \n ItemHeader \n Payload \n ItemHeader \n Payload
            
            int i = 0;
            JObject? header = null;
            try 
            {
               header = JsonConvert.DeserializeObject<JObject>(lines[i++]);
            } 
            catch { /* Invalid header */ }

            if (header == null) return results;

            while(i < lines.Length)
            {
                var line = lines[i++];
                if(string.IsNullOrWhiteSpace(line)) continue;

                JObject? itemHeader = null;
                try
                {
                    itemHeader = JsonConvert.DeserializeObject<JObject>(line);
                }
                catch { break; }
                
                if (itemHeader == null) break;

                var type = itemHeader.Value<string>("type");
                
                // Payload is next line
                if (i >= lines.Length) break;
                
                var payloadLine = lines[i++];
                
                if (type == "event" || type == "transaction")
                {
                     var evt = JsonConvert.DeserializeObject<SentryEvent>(payloadLine);
                     if(evt != null) results.Add(evt);
                }
                else if (type == "user_report")
                {
                    var report = JsonConvert.DeserializeObject<SentryUserReport>(payloadLine);
                    if (report != null) results.Add(report);
                }
                // Can handle other types like 'attachment', etc in future
            }
            
            return results;
        }
    }
}
