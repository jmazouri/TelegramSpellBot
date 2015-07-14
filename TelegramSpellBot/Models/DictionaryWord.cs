using SQLite;

namespace TelegramSpellBot.Models
{
    public class DictionaryWord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Word { get; set; }
    }
}
