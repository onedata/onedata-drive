using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public struct SseEvent
    {
        public string SSEventId;
        public string eventType;
        public string data;

        public override string ToString()
        {
            return $"Id: {SSEventId}, Event: {eventType}, Data: {data}";
        }
    }

    public class SseReader
    {
        public async static IAsyncEnumerable<SseEvent> Read(Stream stream, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string? line;
                string? id = null;
                string? eventType = null;
                StringBuilder data = new StringBuilder();

                while ((line = await reader.ReadLineAsync(cancelToken)) is not null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        if (data.Length > 0)
                        {
                            yield return new SseEvent
                            {
                                SSEventId = id ?? "",
                                eventType = eventType ?? "",
                                data = data.ToString().TrimEnd('\n')
                            };
                            id = null;
                            eventType = null;
                            data.Clear();
                        }
                    }
                    else if (line.StartsWith(":"))
                    {
                        // Comment line, ignore
                        Debug.Print($"SSE READER - IT IS ALIVE - comment: {line}");
                        continue;
                    }
                    else if (line.StartsWith("id:"))
                    {
                        id = line.Substring(3).Trim();
                    }
                    else if (line.StartsWith("event:"))
                    {
                        eventType = line.Substring(6).Trim();
                    }
                    else if (line.StartsWith("data:"))
                    {
                        data.Append(line.Substring(5).Trim());
                        data.Append('\n');
                    }
                    else
                    {
                        throw new FormatException($"Invalid SSE line: {line}");
                    }
                }
            }
        }
    }
}
