using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sfintegration.infrastructure.Helper
{
    public class JsonBuilder<T> where T : class
    {
        public string JsonifyWithLineFeed(IEnumerable<T> objList)
        {
            var buf = new StringBuilder();
            var count = objList.Count();
            var counter = 1;

            buf.AppendLine("[");

            foreach (var obj in objList)
            {
                buf.Append(JsonConvert.SerializeObject(obj));
                if (counter < count)
                {
                    buf.AppendLine(",");
                }
                else
                {
                    buf.AppendLine();
                }
                counter++;
            }

            buf.AppendLine("]");

            return buf.ToString();
        }
    }
}
