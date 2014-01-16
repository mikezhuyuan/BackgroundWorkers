using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BackgroundWorkers
{
    public interface IMessageFormatter
    {
        string Serialize(object @object, bool preserveTypeInformation = true);
        object Deserialize(string item, bool preserveTypeInformation = true);
    }

    public class MessageFormatter : IMessageFormatter
    {
        public string Serialize(object @object, bool preserveTypeInformation = true)
        {
            var s = BuildSerializer(preserveTypeInformation); 

            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            s.Serialize(writer, @object);

            return sb.ToString();
        }

        public object Deserialize(string item, bool preserveTypeInformation = true)
        {            
            return BuildSerializer(preserveTypeInformation).Deserialize(new JsonTextReader(new StringReader(item)));
        }

        static JsonSerializer BuildSerializer(bool preserveTypeInformation)
        {
            // It's important to preserve CLR type in the serialized payload
            // because it helps us to resolve the correct message handler to 
            // process the message. See WorkItemDispatcher.Process method for
            // more information.
            return new JsonSerializer { TypeNameHandling =  preserveTypeInformation ? TypeNameHandling.All : TypeNameHandling.None };
        }
    }
}