using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Common;

namespace HvZClient {
    /// <summary>
    /// Having a little fun here. Feel free to add as many quotes as you like.
    /// 
    /// Format for forming plurals:
    ///     format 1: "{plain}" //an s is appended to the end
    ///     format 2: "{0}", "{1}" // 1 is appended on to 0
    ///     format 3: "{0}", "{1}", "{2}" // the end of 0 is replace with 1 and 2 is appended onto the end
    ///     
    /// 
    /// Variable keys:
    ///     0 = quote
    ///     1 = conjunction
    ///     2 = prefix
    ///     3 = source
    ///     4 = name
    ///     5 = object
    ///     
    /// Names and objects can be reffered to from quotes and sources.
    ///     
    ///Only usable in quotes and sources:
    ///     5s = forced plural object
    /// </summary>
    static class QuoteGenerator {
        private static object[]
        sources = new object[] {
            "news stand",
            "hulk",
            "scientist",
            "expert",
            "Drug addict",
            new string[] { "celebrity", "i", "es" },
            "space {5}",
            "zombie",
            "school teacher",
            "lunatic",
            "Individual",
            "person",
            new string[] { "family", "i", "es" },
            "Ape"},
        objects = new object[] {
            "bannana",
            "grandma",
            "alien",
            "carrot",
            "badingadink",
            new string[] { "people", "" },
            new string[] {"snow", "" },
            "piano"
        };
        //I had many more but they are gone :(
        private static string[]
        quotes = new string[] {
            "Don't eat the yellow {5}",
            "Uuugh",
            "Smash!!",
            "You must not think about THE GAME",
            "The {5s} are not to be trusted",
            "I remember what it was like to be human",
            "Brains taste alot like {5}",
            "You'll know what it means when you need it",
            "Zombie are the main cause of zombies",
            "Zombies are your friends!"
        },
        conjunc = new string[] {
            "say", "reveal", "tell", "believe", "estimate", "report"
        },
        prefixes = new string[] {
            "world renound",
            "celebrated",
            "famous",
            "respected",
            "beloved",
            "local",
            "crazed",
            "babbling",
            "doomed",
            "elderly"
        },
        names = new string[] {
            "Shabalala",
            "Obama"
        },
        //The format used to compile quotes:
        formats = new string[] {
            "{0} ~{2} {3}",
            "\"{0}\", {1} {3}",
            "{3}: {0}",
            "Some friendly advice: \"{0}\", brought to you by your friendly neighbourhood {3}"
        };

        private static bool plural = false;

        public static string NextQuote() {
            plural = Utils.rand.Next(1) == 0;
            string quote = String.Format(formats.PickNext(),
                getQuote(),
                getConjunc(),
                prefixes.PickOne(),
                getSource(),
                names.PickOne(),
                getObject());
            return Char.ToUpper(quote[0]) + quote.Substring(1, quote.Length - 1);
        }

        private static string formattedString(string s) {
            return s
                .Replace("{4}", names.PickOne())
                .Replace("{5}", getObject())
                .Replace("{5s}", getPlural(objects, true));
        }

        private static string getObject() {
            return getPlural(objects, Utils.rand.Next(5) == 0);
        }

        private static string getConjunc() {
            return formattedString(getPlural(conjunc, !plural));
        }

        private static string getSource() {
            return formattedString(getPlural(sources, plural));
        }

        private static string getQuote() {
            return formattedString(quotes.PickNext());
        }

        private static string getPlural(object[] collection, bool plur) {
            object item = collection.PickOne();
            string result = item is string ? (string)item : (string)((object[])item)[0];

            if (plur) {
                if (item is string) {
                    result += "s";
                } else {
                    string[] items = (string[])item;

                    if (items.Length > 1) {
                        string app = items[1];

                        if (items.Length > 2) {
                            app = items[2];
                            string repl = items[1];

                            result = result.Substring(0, result.Length - repl.Length) + repl;
                        }

                        result += app;
                    }
                }
            }

            return result;
        }
    }
}
