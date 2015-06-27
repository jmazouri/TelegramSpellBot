using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBotSharp;
using TelegramBotSharp.Types;
using NHunspell;
using System.Text.RegularExpressions;

namespace TelegramSpellBot
{
    class Program
    {
        private static TelegramBot bot;
        private static Hunspell hunspell;
        private static DatabaseContext database;

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Bot...");
            bot = new TelegramBot(System.IO.File.ReadAllText("apikey.txt"));
            Console.WriteLine("Bot initialized.");

            Console.WriteLine("Initializing Hunspell...");
            hunspell = new Hunspell("en_us.aff", "en_us.dic");
            Console.WriteLine("Hunspell initialized. {0} words.", System.IO.File.ReadAllLines("en_us.dic").Count());

            Console.WriteLine("Connecting to SQLite Database...");
            database = new DatabaseContext();
            Console.WriteLine("Database connected. {0} words.", database.GetWordCount());

            Console.WriteLine("Hi, i'm {0}! ID: {1}", bot.Me.FirstName, bot.Me.Id);

            new Task(() => PollMessages()).Start();

            Console.ReadLine();
        }

        static void AddWordToDictionary(string word)
        {
            if (word == null) { return; }
            database.AddWord(new Models.DictionaryWord
            {
                Word = word.Trim()
            });
        }

        static async void PollMessages()
        {
            while (true)
            {
                var result = await bot.GetMessages();
                foreach (Message m in result)
                {
                    //Skip prior messages
                    if (m.Date.ToLocalTime() < DateTime.Now.AddSeconds(-10)) { continue; }

                    if (m.Text.StartsWith("/addtodictionary") && m.Text.Contains(" "))
                    {
                        string newWord = m.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];

                        AddWordToDictionary(newWord);
                        bot.SendMessage((m.Chat.Title == null ? (MessageTarget)m.From : m.Chat), String.Format("{0} added to dictionary.", newWord));
                        return;
                    }

                    if (m.Text != null && !m.Text.Contains("'") && !m.Text.Contains("-"))
                    {
                        Dictionary<string, List<string>> foundCorrections = new Dictionary<string, List<string>>();

                        foreach (string word in m.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            bool correct = hunspell.Spell(word);
                            var corrections = hunspell.Suggest(word);

                            if (!correct && corrections.Any())
                            {
                                if (database.FindWord(word) == null)
                                {
                                    foundCorrections.Add(word, corrections.Take(3).ToList());
                                }
                            }
                        }

                        if (foundCorrections.Any())
                        {
                            StringBuilder build = new StringBuilder();
                            foreach (var word in foundCorrections)
                            {
                                build.Append(word.Key);
                                build.Append(": ");
                                foreach (var correction in word.Value)
                                {
                                    build.Append(correction);
                                    if (correction != word.Value.Last())
                                    {
                                        build.Append(", ");
                                    }
                                }
                                build.AppendLine();
                                build.Append("No? /addtodictionary ");
                                build.Append(word.Key);
                                build.AppendLine();
                            }

                            bot.SendMessage((m.Chat.Title == null ? (MessageTarget)m.From : m.Chat), String.Format("Did you mean...\n{0}", build.ToString()));
                        }
                    }
                }
            }
        }
    }
}
