using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHunspell;
using TelegramBotSharp;
using TelegramBotSharp.Types;

namespace TelegramSpellBot
{
    class Program
    {
        private static TelegramBot bot;
        private static Hunspell hunspell;
        private static DatabaseContext database;
        
        private static readonly List<string> nameFilter = new List<string> {"ikagara"}; 

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Bot...");
            bot = new TelegramBot(File.ReadAllText("apikey.txt"));
            Console.WriteLine("Bot initialized.");

            Console.WriteLine("Initializing Hunspell...");
            hunspell = new Hunspell("en_us.aff", "en_us.dic");
            Console.WriteLine("Hunspell initialized. {0} words.", File.ReadAllLines("en_us.dic").Count());

            Console.WriteLine("Connecting to SQLite Database...");
            database = new DatabaseContext();
            Console.WriteLine("Database connected. {0} words.", database.GetWordCount());

            Console.WriteLine("Hi, i'm {0}! ID: {1}", bot.Me.FirstName, bot.Me.Id);

            new Task(PollMessages).Start();

            Console.ReadLine();
        }

        static void AddWordToDictionary(string word)
        {
            if (word == null) { return; }
            database.AddWord(word.Trim());
        }

        static async void PollMessages()
        {
            while (true)
            {
                var result = await bot.GetMessages();
                foreach (Message m in result
                    .Where(m => m.Date.ToLocalTime() >= DateTime.Now.AddSeconds(-10))
                    .Where(m => m.Text != null && !m.Text.Contains("'") && !m.Text.Contains("-")))
                {
                    if (m.Text.StartsWith("/addtodictionary") && m.Text.Contains(" "))  
                    {
                        if (nameFilter.Contains(m.From.Username.ToLower()))
                        {
                            bot.SendMessage((m.Chat ?? (MessageTarget)m.From), $"Sorry, {m.From.Username}, but you're on the blacklist.");
                            continue;
                        }

                        string newWord = m.Text.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];

                        AddWordToDictionary(newWord);
                        bot.SendMessage((m.Chat ?? (MessageTarget)m.From), $"{newWord} added to dictionary.");
                        return;
                    }

                    if (!nameFilter.Contains(m.From.Username)) { continue; }

                    var foundCorrections = new Dictionary<string, List<string>>();

                    foreach (string word in m.Text.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(d=>d.ToLower()))
                    {
                        bool correct = hunspell.Spell(word);
                        var corrections = hunspell.Suggest(word);

                        if (correct || !corrections.Any()) { continue; }

                        if (database.FindWord(word) == null)
                        {
                            foundCorrections.Add(word, corrections.Take(3).ToList());
                        }
                    }

                    if (!foundCorrections.Any()) { continue; }

                    var build = new StringBuilder();
                    foreach (var word in foundCorrections)
                    {
                        build.Append(word.Key);

                        if (!nameFilter.Contains(m.From.Username.ToLower()))
                        {
                            int heuristic = database.IncreaseHeuristic(word.Key);

                            if (heuristic >= 3)
                            {
                                database.AddWord(word.Key);
                            }
                        }

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

                    bot.SendMessage((m.Chat ?? (MessageTarget)m.From), $"Did you mean...\n{build}", true, m);
                }
            }
        }
    }
}
