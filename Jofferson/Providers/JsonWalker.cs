using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Jofferson.Providers
{
    class JsonWalker
    {
        private Queue<JToken> Queue;

        /// <summary>
        /// Called whenever a token is found.
        /// </summary>
        public event Action<JToken> OnToken;

        /// <summary>
        /// Called to each key of a property. This will still call OnToken later for the value.
        /// </summary>
        public event Action<JProperty> OnKey;

        public JsonWalker(JToken root)
        {
            Queue = new Queue<JToken>();
            Queue.Enqueue(root);
        }

        /// <summary>
        /// Walks through the tree and calls the callbacks
        /// </summary>
        public void Work()
        {
            while (Queue.Count > 0)
            {
                JToken current = Queue.Dequeue();

                switch (current.Type)
                {
                    case JTokenType.Array:
                        foreach (JToken value in current)
                            Queue.Enqueue(value);
                        break;
                    case JTokenType.Object:
                        foreach (JProperty value in current)
                        {
                            if (OnKey != null)
                                OnKey(value);

                            Queue.Enqueue(value.Value);
                        }
                        break;
                    default:
                        if (OnToken != null)
                            OnToken(current);
                        break;
                }
            }
        }
    }
}
