using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;
using HvZ.Common;

namespace HvZClient {
    /// <summary>
    /// Having a little fun here. Feel free to add as many quotes as you like.
    /// 
    /// Format for forming plurals:
    ///     format 1: "{plain}" - an s is appended to the end
    ///     format 2: "{0}", "{1}" - 1 is appended on to 0
    ///     format 3: "{0}", "{1}", "{2}" - the end of 0 is replace with 1 and 2 is appended onto the end
    ///     
    /// </summary>
    static class QuoteGenerator {
        // Variable keys:
        private enum QKey {
            q,  // = quote
            c,  // = conjunction
            p,  // = prefix
            s,  // = source
            n,  // = name
            o,  // = object
            r,  // = random positive integer (0 - 900)
            R,  // = random positive double (0 - 1)
            d,  // = directive i.e "I", "You", "we"

            rR, // = the sum of r and R
            os, // = source - forced plural
            ss  // = object - forced plural
        }
        // Variable keys can be used from anywhere but must be used sparingly.
        //  Keys only work in sets other than the one they refer to, to prevent circular references
        //  and keys refering to one of it parent's sets will also not be applied.

        private static object[]
        sources = new object[] {
            new string[] { "The Doctor", "", "" },
            new string[] { "spiderman", "en", "" },
            new string[] { "Yoda", "", " the padawan master" },
            "doctor",
            "news stand",
            "hulk",
            "officer",
            new string[] { "policeman", "en", "" },
            new string[] { "fireman", "en", "" },
            "scientist",
            "artist",
            "expert",
            "Drug addict",
            new string[] { "celebrity", "i", "es" },
            "space {o}",
            "zombie",
            "human",
            "Mr Willy Wonka",
            "specialist",
            "school teacher",
            "lunatic",
            "Individual",
            new string[] { "person", "ople" , ""},
            new string[] { "elderly", "i", "es" },
            new string[] { "family", "i", "es" },
            "Ape"
        },
        objects = new object[] {
            new string[] { "force", "arce", "" },
            "bannana",
            new string[] { "galaxy", "i", "es" },
            "fiber",
            "fiberoptic",
            "grandma",
            "alien",
            "carrot",
            "vegetable",
            "badingadink",
            new string[] { "people", "", "" },
            new string[] {"snow", "" },
            "piano",
            "ice-cream",
            "bomb",
            "brain"
        },
        directives = new object[] {
            new object[] {
                new string[] { "you", "I" }, // singular { other | self }
                new string[] { "us", "we" } }, // plural { other | self }
            new object[] {
                new string[] { "he", "she" }, // singular { male | female }
                "they" }, // plural { same for both }
        };

        private static string[]
            //test line
            //"q:{q} c:{c} p:{p} s:{s} n:{n} r:{r} R:{R} d:{d} rR:{rR} os:{os} ss:{ss}"
        quotes = new string[] {
            "Don't eat the yellow {o}",
            "A zombie apocalypse is very unlikely",
            "\"There is no such thing as zombies.\" was what I said",
            "Uuugh",
            "Smash!!",
            "{d} just lost THE GAME",
            "Eating {rR} kilos of {o} is good for zombie bites",
            "Zombies love {o}",
            "Eveyone must read the hichhikers guide to {o}",
            "Resupply points are the best place to get food",
            "Becoming a zombie was the best desition I've ever made",
            "You must not think about THE GAME",
            "The {os} are not to be trusted",
            "The only way to survive is to die",
            "If you can't beat em...",
            "They are the master race",
            "Zombies, just zombies",
            "Unicorns, deal with it.",
            "Wonka bars are made from <sensored> and <sensored> from monkeys <sensored>, but only after they've been washed.",
            "I remember what it was like to be human",
            "Brains taste alot like {o}",
            "You'll know what it means when you need it",
            "Use the {o} Luke",
            "Zombie are the main cause of zombies",
            "Zombies are your friends!",
            "Terrible! astounding! Fantastic!",
            "Lookout for the {os}!"
        },
        conjunc = new string[] {
            "say", "reveal", "tell", "believe", "estimate", "report", "state", "unimpressed", "sweaty", "scream"
        },
        prefixes = new string[] {
            "screaming",
            "world renound",
            "celebrated",
            "young",
            "fresh",
            "famous",
            "artistic",
            "autistic",
            "fantastic",
            "malevolent",
            "respected",
            "beloved",
            "local",
            "crazed",
            "babbling",
            "doomed",
            "elderly",
            "confused"
        },
        names = new string[] {
            "Shabalala",
            "Jimmy Hendriks",
            "Jack Frost",
            "Arnold Schartzenigger",
            "Snoop Dogg",
            "Rowaldo",
            "Chuck Norris",
            "Bitler",
            "Steamy Herring",
            "Obama Sinladin",
            "Obama",
            "Zumer",
            "George"
        },
        formats = new string[] {
            //The formats used to compile quotes:
            "{q} ~{p} {s}",
            "\"{q}\", {c} {s}",
            "{s}: {q}",
            "Some friendly advice: \"{q}\", brought to you by your friendly neighbourhood {s}",
            "Breaking News! {q}",
            "Warning: {c} {s}"
        };

        private static bool plural = false;

        private static string NextQuote() {
            plural = Utils.rand.Next(1) == 0;
            string quote = formattedString(formats.PickOne(), new List<QKey>());
            return Char.ToUpper(quote[0]) + quote.Substring(1, quote.Length - 1);
        }

        private static string formattedString(string s, List<QKey> exclude) {
            foreach (QKey key in Enum.GetValues(typeof(QKey))) {
                if (s.Contains("{" + key + "}")) {
                    if (!exclude.Contains(key)) {
                        s = s.Replace("{" + key.ToString() + "}", formattedString(getComponent(key), exclude.chain(key)));
                    } else {
                        s = s.Replace("{" + key.ToString() + "}", "<censored>");
                    }
                }
            }
            return s;
        }

        private static string getComponent(QKey c) {
            switch (c) {
                case QKey.q: return quotes.PickNext();
                case QKey.c: return getPlural(conjunc, !plural);
                case QKey.p: return prefixes.PickOne();
                case QKey.s: return getPlural(sources, plural);
                case QKey.n: return names.PickOne();
                case QKey.o: return getPlural(objects, Utils.rand.Next(5) == 0);
                case QKey.os: return getPlural(objects, true);
                case QKey.ss: return getPlural(sources, true);
                case QKey.r: return Utils.rand.Next(0, 901).ToString();
                case QKey.R: return Utils.rand.NextDouble().ToString();
                case QKey.rR: return (((double)Utils.rand.Next(0, 901)) + Utils.rand.NextDouble()).ToString();
                case QKey.d: return getDirective(Utils.rand.Next(20) == 0);
            }

            throw new NotSupportedException("Edit me so I know what to do with " + c.ToString() + " ...PLEASE!!!");
        }

        private static string getDirective(bool plural) {
            object item = directives.PickOne();

            if (item is string) {
                return (string)item;
            } else {
                object pick = ((object[])item)[plural ? 0 : 1];

                if (pick is string) {
                    return (string)pick;
                } else {
                    return ((string[])pick).PickOne();
                }
            }
        }

        private static List<T> chain<T>(this List<T> list, T link) {
            List<T> newList = new List<T>();
            newList.AddRange(list);
            if (!newList.Contains(link)) {
                newList.Add(link);
            }
            return newList;
        }

        private static string getPlural(object[] collection, bool plur) {
            object item = collection.PickOne();
            string result = item is string ? (string)item : (string)((object[])item)[0];

            if (plur) {
                string app = "s";
                if (item is string[]) {
                    string[] items = (string[])item;
                    if (items.Length > 1) {
                        app = items[1];
                        if (items.Length > 2) {
                            app = items[2];
                            result = result.Substring(0, result.Length - items[1].Length) + items[1];
                        }
                    }
                }

                result += app;
            }

            return result;
        }

        private static List<Label> contexts = new List<Label>();
        private static DispatcherTimer timer = new DispatcherTimer() {
            Interval = randomTime()
        };

        static QuoteGenerator() {
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private static TimeSpan randomTime() {
            return TimeSpan.FromSeconds(Utils.rand.Next(30, 101));
        }

        private static void timer_Tick(object sender, EventArgs e) {
            timer.Interval = randomTime();
            foreach (Label i in contexts) {
                i.Content = NextQuote();
            }
        }

        public static void RegisterQuoteListener(Label context) {
            if (!contexts.Contains(context)) {
                contexts.Add(context);
                context.Content = NextQuote();
            }
        }
    }
}
