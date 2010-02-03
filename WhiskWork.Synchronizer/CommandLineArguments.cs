using System.Collections.Generic;

namespace WhiskWork.Synchronizer
{
    internal class CommandLineArguments
    {
        private readonly string[] _args;
        private readonly Dictionary<string, string> _optionalArguments;

        public CommandLineArguments(string[] args)
        {
            _args = args;
            _optionalArguments = ParseOptionalParameters();
        }

        public int Count
        {
            get
            {
                return _args.Length;
            }
        }

        public string this[int index]
        {
            get
            {
                return _args[index];
            }
        }

        public string this[string argName]
        {
            get
            {
                return _optionalArguments[argName];
            }
        }

        public IEnumerable<string> GetSafeValues(string argName)
        {
            if(!ContainsArg(argName))
            {
                return new string[0];
            }

            return _optionalArguments[argName].Split(',');
        }

        public bool ContainsArg(string name)
        {
            return _optionalArguments.ContainsKey(name);
        }


        private Dictionary<string, string> ParseOptionalParameters()
        {
            var optionalParameters = new Dictionary<string, string>();

            foreach (var arg in _args)
            {
                if (!arg.StartsWith("-"))
                {
                    continue;
                }

                var keyValue = arg.Split(':');
                if (keyValue.Length == 2)
                {
                    optionalParameters.Add(keyValue[0], keyValue[1]);
                }
                else
                {
                    optionalParameters.Add(keyValue[0], null);
                }
            }

            return optionalParameters;
        }
    }
}