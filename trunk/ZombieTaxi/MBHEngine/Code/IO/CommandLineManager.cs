using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace MBHEngine.IO
{
    // Singleton class to handle the data that is passed into the program
    // via the command line.
    //
    public class CommandLineManager
    {
        // The static instance of the class making this a singleton.
        //
        private static CommandLineManager mInstance;

        // Stores a copy of the command line arguments for parsing.
        //
        private StringDictionary mArgs;

        // This is private so that no ones creates and instance but us.
        //
        private CommandLineManager()
        {
            mArgs = new StringDictionary();
        }

        // Accessor for our singleton.
        //
        public static CommandLineManager pInstance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new CommandLineManager();
                }

                return mInstance;
            }
        }

        // This needs to be called before any command line arguments can per parsed.
        //
        public string[] pArgs
        {
            set 
            {
                // The regular expression used to tokenize our arguments.  Keep in mind it has already
                // been broken up based on spaces.
                Regex spliter = new Regex(@"=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                // This gets used to store the tokenized arguments.
                string[] parts;

                // Loop through each argument in the array.
                foreach (string arg in value)
                {
                    // Split this argument based on the regular expression defined above.
                    parts = spliter.Split(arg, 2);

                    // How many pieces were there?  We only support 1 or 2 at them moment.
                    switch (parts.Length)
                    {
                        case 1:
                            {
                                // Make sure this wasn't already entered.
                                if (!mArgs.ContainsKey(parts[0]))
                                {
                                    // Since this didn't have a value assotiated with it, it means
                                    // this argument just needs to exist.
                                    mArgs.Add(parts[0], "true");
                                }
                                break;
                            }

                        case 2:
                            {
                                // Make sure this wasn't already entered.
                                if (!mArgs.ContainsKey(parts[0]))
                                {
                                    // Store the value keyed by the name.
                                    mArgs.Add(parts[0], parts[1]);
                                }
                                break;
                            }
                    }
                }
            }
        }

        // Overload the [] operator for our look up.
        public string this[string argument]
        {
            get
            {
                return (mArgs[argument]);
            }
        }
    }
}
