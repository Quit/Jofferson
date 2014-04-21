using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jofferson.Models
{
    class JsonResource : Resource
    {
        public JToken OriginalToken { get; private set; }

        /// <summary>
        /// Backing field for the semi-final token.
        /// </summary>
        private JToken _token = null;

        /// <summary>
        /// The final token, with mixintos, without resolution
        /// </summary>
        public JToken Token
        {
            get
            {
                if (_token == null)
                    _token = createToken();

                return _token;
            }
        }


        /// <summary>
        /// Backing field for the final token
        /// </summary>
        private JToken _finalToken = null;

        public JToken FinalToken
        {
            get 
            {
                if (_finalToken == null)
                    _finalToken = createFinalToken();
                return _finalToken; 
            }
        }
        

        public JsonResource(string location, Mod mod, bool exists)
            : base(location, mod, exists)
        {
            if (exists)
            {
                OriginalToken = ResourceManager.LoadJson(this);
                if (OriginalToken == null)
                    Invalidate();
            }
        }

        //public override void AddReferredBy(Reference referee)
        //{
        //    if (referee is Mixinto)
        //    {
        //        // Make sure that... this is valid? or something?
        //        JsonResource mixinto = referee.Origin as JsonResource;

        //        if (mixinto == null)
        //            ErrorReporter.AddError(referee.Definition.Mod, referee.Definition, "Invalid mixinto {0}: Origin is not a json file", referee);
        //        else
        //            mergeTokens(this, referee.Origin, this.Token, mixinto.Token);

        //        //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Token, Newtonsoft.Json.Formatting.Indented));
        //    }

        //    base.AddReferredBy(referee);
        //}

        private JToken createToken()
        {
            if (OriginalToken == null)
                return null;

            JToken root = OriginalToken.DeepClone();

            foreach (Reference referee in ReferredBy)
            {
                JsonResource js = referee.Origin as JsonResource;

                if (js == null)
                    ErrorReporter.AddError(referee.Definition.Mod, referee.Definition, "Invalid mixinto {0}: Origin is not a json file", referee);
                else if (!(js is Manifest))
                    mergeTokens(this, referee.Origin, root, js.Token);
            }

            return root;
        }

        private JToken createFinalToken()
        {
            if (OriginalToken == null)
                return null;

            // First: Resolve us. This will create a deep copy already.
            JToken root = ResourceManager.ResolveJson(this);

            foreach (Reference referee in ReferredBy)
            {
                JsonResource js = referee.Origin as JsonResource;

                if (js == null)
                    ErrorReporter.AddError(referee.Definition.Mod, referee.Definition, "Invalid mixinto {0}: Origin is not a json file", referee);
                else if (!(js is Manifest))
                    mergeTokens(this, referee.Origin, root, js.FinalToken);
            }

            return root;
        }

        /// <summary>
        /// Mixintos stuff.
        /// </summary>
        /// <param name="mixintoResource"></param>
        /// <param name="origin"></param>
        /// <param name="mixinto"></param>
        private static JToken mergeTokens(Resource targetResource, Resource mixintoResource, JToken origin, JToken mixinto)
        {
            // Compare types.
            Debug.Assert(origin != null);

            if (origin == null)
            {
                ErrorReporter.AddError(mixintoResource.Mod, mixintoResource, "Cannot mixinto {0} into {1}: Target has invalid JSON", mixintoResource.Location, targetResource.Location);
                return null;
            }

            if (mixinto == null)
            {
                ErrorReporter.AddError(targetResource.Mod, targetResource, "Cannot mixinto {0} into {1}: Mixinto has invalid JSON", mixintoResource.Location, targetResource.Location);
                return null;
            }

            JTokenType originType = origin.Type, mixintoType = mixinto.Type;

            if (originType == mixintoType)
            {
                switch (originType)
                {
                    case JTokenType.Object:
                        JObject originObj = (JObject)origin, mixintoObj = (JObject)mixinto;
                        foreach (var property in mixintoObj)
                        {
                            if (originObj[property.Key] == null)
                                originObj[property.Key] = property.Value;
                            else
                                originObj[property.Key] = mergeTokens(targetResource, mixintoResource, (JToken)originObj[property.Key], property.Value);
                        }

                        return originObj;
                    case JTokenType.Array:
                        JArray originArray = (JArray)origin, mixintoArray = (JArray)mixinto;
                        foreach (var item in mixinto.Reverse())
                            originArray.AddFirst(item);
                        return originArray;
                    case JTokenType.Integer:
                        return mixinto.Value<int>();
                    case JTokenType.String:
                        origin = mixinto.Value<string>();
                        return mixinto.Value<string>();
                    case JTokenType.Float:
                        return mixinto.Value<float>();
                    default:
                        ErrorReporter.AddError(mixintoResource.Mod, mixintoResource, "Invalid mixinto type {0} at {1}", mixinto.Type, mixinto.Path);
                        return null;
                }
            }
            else if (IncompatibleTypes(originType, mixintoType))
            {
                ErrorReporter.AddError(mixintoResource.Mod, mixintoResource, "Cannot mixinto {3}: Types for {2} differ (original: {0}; mixinto: {1})", originType, mixintoType, origin.Path, mixintoResource.Location);
                mixintoResource.Invalidate();
                return null;
            }

            return null;
        }

        private static bool IncompatibleTypes(JTokenType a, JTokenType b)
        {
            // Exception: Numbers.
            if (a == JTokenType.Float && b == JTokenType.Integer || a == JTokenType.Integer && b == JTokenType.Float)
                return false;
            // Setting things to or from null is also allowed I guess.
            if (a == JTokenType.Null || b == JTokenType.Null)
                return false;

            return true;
            //if (a == JTokenType.Array || b == JTokenType.Array || a == JTokenType.Object || b == JTokenType.Object)
        }
        public string OriginalJson { get { return JsonConvert.SerializeObject(OriginalToken, Formatting.Indented); } }
        public string FinalJson { get { return JsonConvert.SerializeObject(Token, Formatting.Indented); } }
        public string FinalResolvedJson { get { return JsonConvert.SerializeObject(FinalToken, Formatting.Indented); } }
    }

}
