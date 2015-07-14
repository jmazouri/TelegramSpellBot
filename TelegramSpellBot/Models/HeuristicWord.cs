using SQLite;

namespace TelegramSpellBot.Models
{
    public class HeuristicWord
    {
        [PrimaryKey]
        public string Word { get; set; }

        public int Count { get; set; }
    }
}
