using System.Collections.Generic;
using System.Linq;
using SQLite;
using TelegramSpellBot.Models;

namespace TelegramSpellBot
{
    public class DatabaseContext
    {
        readonly SQLiteConnection _db;

        public DatabaseContext()
        {
            _db = new SQLiteConnection("telegramspell.db");
            _db.CreateTable<DictionaryWord>();
            _db.CreateTable<HeuristicWord>();
        }

        public void AddWord(string word)
        {
            _db.InsertOrReplace(new DictionaryWord
            {
                Word = word.ToLower()
            });
        }
        
        public DictionaryWord FindWord(string word)
        {
            return _db.Find<DictionaryWord>(d => d.Word == word.ToLower());
        }

        public int GetHeuristicForWord(string word)
        {
            var found = _db.Find<HeuristicWord>(word);

            if (found != null)
            {
                return found.Count;
            }

            _db.Insert(new HeuristicWord {Count = 1, Word = word});
            return 1;
        }

        public int IncreaseHeuristic(string word)
        {
            var found = GetHeuristicForWord(word);
            _db.Update(new HeuristicWord { Count = found + 1, Word = word});
            return found + 1;
        }

        public List<DictionaryWord> GetWords()
        {
            return _db.Table<DictionaryWord>().ToList();
        }

        public int GetWordCount()
        {
            return _db.ExecuteScalar<int>("SELECT Count(*) FROM DictionaryWord");
        }
    }
}
