using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using TelegramSpellBot.Models;

namespace TelegramSpellBot
{
    public class DatabaseContext
    {
        SQLiteConnection _db;

        public DatabaseContext()
        {
            _db = new SQLiteConnection("telegramspell.db");
            _db.CreateTable<DictionaryWord>();
        }

        public void AddWord(DictionaryWord word)
        {
            _db.InsertOrReplace(new DictionaryWord
            {
                Word = word.Word.ToLower()
            });
        }
        
        public DictionaryWord FindWord(string word)
        {
            return _db.Find<DictionaryWord>(d => d.Word == word.ToLower());
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
