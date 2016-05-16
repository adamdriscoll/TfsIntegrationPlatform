// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Base class for the CommandLine

#region Using directives

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace Microsoft.TeamFoundation.Converters.Utility
{
    /// <summary>
    /// Commandline parser class
    /// </summary>
    internal sealed class CommandLineParser
    {

        #region  Constructor

        /// <summary>
        /// This private constr is to eliminate the fxcop error that says to add a private constr to 
        /// prevent the compiler from added a default public constr
        /// </summary>
        private CommandLineParser()
        {
        }

        #endregion

        #region Static methods


        /// <summary>
        /// This method can be used to dispaly the copyright message.
        /// </summary>
        /// <param name="name"></param>
        internal static void DisplayCopyRightMessage()
        {
            Console.WriteLine(CommonResource.CopyRightMessage);
        }


        /// <summary>
        /// Overloaded method for adding the argumets.
        /// </summary>
        /// <param name="list"></param>
        internal static void AddArgumentDetails(ArgumentDetails[] list)
        {
            Debug.Assert(list != null, "Argumentdetails list cannot be null");
            m_validatorList.AddRange(list);
        }

        /// <summary>
        /// Parses the arguments and calls the corresponding validator functions.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static bool Parse(String[] args)
        {
            return Parse(args, "/", ':');   //Default escape character and separator;
        }

        /// <summary>
        /// Parses the arguments with given separator and escape sequence and calls the corresponding validator functions.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="escape"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        internal static bool Parse(String[] args, string escape, char separator)
        {
            Debug.Assert(args != null, "Args is null");
            Debug.Assert(escape != null, "Escape string is null");

            string escapeSequence;      // Initial string while giving the parameter
            char separatingCharacter;   // Separating character 
            Regex argumentStructure;    // Regular expression for parsing the input arguments into key and value;

            escapeSequence = escape;
            separatingCharacter = separator;
            argumentStructure = new Regex(string.Format(CultureInfo.InvariantCulture,
                "^{0}(?<key>[^{1}]+)({1}(?<value>.+))?", escapeSequence, separatingCharacter));
            if (args.Length == 0)
            {
                DisplayHelp();
                return false;
            }

            foreach (String cmd in args)
            {
                // Try to match the cmd
                Match matcher = argumentStructure.Match(cmd);
                if (!matcher.Success)
                {
                    // Regular expression doesn't match.. invalid input
                    UtilityMethods.DisplayError(UtilityMethods.Format(
                        CommonResource.InvalidArgument, cmd));
                    DisplayHelp();
                    return false;
                }

                // Call the corresponding validating function if argument is
                // in the list of valid arguments else return error
                string key = matcher.Groups["key"].Value;
                string value = matcher.Groups["value"].Value;
                bool present = false;
                foreach (ArgumentDetails arg in m_validatorList)
                {
                    // Do a invariant culture, case-insensitive compare
                    // Shortname can be null. So, a check is required. Where as the full name can never be null.
                    if ((arg.ShortName != null && TFStringComparer.CommandLineOptionName.Equals (arg.ShortName, key))
                        || TFStringComparer.CommandLineOptionName.Equals(arg.FullName, key))
                    {
                        // If the argument cannot have a null value but null is passed, print an error;
                        // Here the value wont be null, rather it will be an empty string. coz value is set after matching.
                        if (!arg.IsNullable && value.Length == 0)
                        {
                            UtilityMethods.DisplayError(UtilityMethods.Format(
                                CommonResource.ValueCannotBeNull, cmd));
                            DisplayHelp();
                            return false;
                        }
                        // Argument is there in the list of valid arguments.
                        // Call validator function.
                        if (arg.Function(value))
                        {
                            // A valid value. Remove the argument from the list
                            m_validatorList.Remove(arg);
                            present = true;
                            break;
                        }
                        else
                        {   // There is something wrong with the input value
                            // the proper error message should be printed by the delgate; so, just return
                            return false;
                        }
                    }
                }

                // The given arguement is not in the list of valid arguemts.
                // Print an error message and usage.
                if (!present)
                {
                    UtilityMethods.DisplayError(UtilityMethods.Format(
                        CommonResource.DuplicatedArgument, cmd));
                    DisplayHelp();
                    return false;
                }
            }

            // Ensure all mandatory arguments are provided.
            foreach (ArgumentDetails arg in m_validatorList)
            {
                if (arg.IsMandatory == true)
                {
                    UtilityMethods.DisplayError(UtilityMethods.Format(
                        CommonResource.MissingArgument, escapeSequence + arg.ShortName));
                    DisplayHelp();
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Print the commandline help message
        /// </summary>
        static void DisplayHelp()
        {
            foreach (ArgumentDetails arg in m_validatorList)
            {
                if (TFStringComparer.CommandLineOptionName.Equals(arg.FullName, "help") ||
                    TFStringComparer.CommandLineOptionName.Equals (arg.FullName, "?"))
                {
                    arg.Function("?");
                    break;
                }
            }
        }
        #endregion

        #region Private Variables
        private static ArrayList m_validatorList = new ArrayList();            // List containing the validators
        #endregion
    }

    /// <remarks>
    /// This class represents a unit comprising of a valid argument,
    /// the validator function and if the argument is mandatory or not.
    /// </remarks>
    internal class ArgumentDetails
    {
        internal delegate bool Validator(string argumentValue);

        #region Properties
        /// <summary>
        /// Properties to get and set the values
        /// </summary>
        /// <value></value>
        internal string FullName
        {
            get { return m_fullName; }
        }
        internal string ShortName
        {
            get { return m_shortName; }
        }
        internal bool IsMandatory
        {
            get { return m_mandatory; }
        }
        internal Validator Function
        {
            get { return m_functionName; }
        }
        internal bool IsNullable
        {
            get { return m_nullable; }
        }
        #endregion

        #region Constrctors
        /// <summary>
        /// This constructor takes all the arguments
        ///	FullName and FunctionName can never be null (mandatory).
        /// ShortName and rest of the arguments can be null (optional).
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="shortName"></param>
        /// <param name="funcName"></param>
        /// <param name="mandatory"></param>
        /// <param name="nullable"></param>
        internal ArgumentDetails(string fullName, string shorterName, Validator functionName, bool mandatory, bool nullable)
        {
            Debug.Assert(fullName != null, "Full name of the Argument cannot be null");
            Debug.Assert(functionName != null, "Argument validator function is null");

            m_fullName = fullName;
            m_shortName = shorterName;
            m_mandatory = mandatory;
            m_nullable = nullable;
            m_functionName = functionName;
        }


        /// <summary>
        /// This constructor can be used when a default value of false need to be used for nullable field 
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="shorterName"></param>
        /// <param name="functionName"></param>
        /// <param name="mandatory"></param>
        internal ArgumentDetails(string fullName, string shorterName, Validator functionName, bool mandatory)
            :
            this(fullName, shorterName, functionName, mandatory, false)
        {
        }
        #endregion

        #region Private Variables

        private string m_shortName;
        private string m_fullName;
        private bool m_mandatory;
        private bool m_nullable;
        private Validator m_functionName;

        #endregion
    }
}
