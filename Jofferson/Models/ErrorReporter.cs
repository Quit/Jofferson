using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jofferson.Models;

namespace Jofferson
{
    class Error
    {
        public string Description { get; private set; }
        public Resource Resource { get; private set; }

        public Error(Resource resource, string description)
        {
            this.Resource = resource;
            this.Description = description;
        }
    }

    class ErrorReporter
    {
        public static List<Error> GlobalErrors = new List<Error>();

        public static void AddError(Mod mod, Resource resource, string format, params object[] args)
        {
            if (mod == null)
                GlobalErrors.Add(new Error(null, string.Format(format, args)));
            else
                mod.Errors.Add(new Error(resource, string.Format(format, args)));
        }
    }
}
