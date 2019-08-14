using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace iti.backendtest
{
    public class Consolidator : IDisposable
    {
        public class Entry
        {
            public byte Month { get; set; }
            public byte Day { get; set; }
            public string Desc { get; set; }
            public decimal Value { get; set; }
            public string Category { get; set; }

            public string ToString(CultureInfo ci)
                => $"{this.Day.ToString("00")}/{ci.DateTimeFormat.GetAbbreviatedMonthName(this.Month)}\t{this.Desc}\t{this.Value.ToString("N2", ci.NumberFormat)}\t{this.Category}";
        }

        private readonly object lockObjIn = new object();
        private readonly object lockObjOut = new object();
        private readonly ConcurrentDictionary<string, decimal> dictCategories = new ConcurrentDictionary<string, decimal>(StringComparer.InvariantCulture);
        private readonly ConcurrentDictionary<byte, decimal> dictMonths = new ConcurrentDictionary<byte, decimal>();
        private decimal totalOut = 0;
        private decimal totalIn = 0;

        private readonly CultureInfo ci;
        private readonly IEntryCollector entryCollector;
        public Consolidator(CultureInfo ci, IEntryCollector entryCollector)
        {
            this.ci = ci;
            this.entryCollector = entryCollector;
        }

        /// <summary>
        /// Método que deve ser chamado antes de iniciar o ProcessEntry.
        /// A idéia é que o Consolidador prepare as estruturas de dados necessárias para o processamento.
        /// </summary>
        public void Prepare() => entryCollector.Prepare();
        /// <summary>
        /// Processamento da movimentação que foi lida ou do log ou do json.
        /// </summary>
        /// <param name="entry">Dados da movimentação</param>
        public void ProcessEntry(Entry entry)
        {
            var value = entry.Value;
            void addToDict<T>(ConcurrentDictionary<T, decimal> dict, T key)
                => dict.AddOrUpdate(key, value, (_, v) => v + value);

            entryCollector.AddEntry(entry, ci);

            if (value > 0)
                lock (lockObjIn)
                    totalIn += value;
            else if (value < 0)
            {
                lock (lockObjOut)
                    totalOut += value;
                addToDict(dictMonths, entry.Month);
                addToDict(dictCategories, entry.Category.ToLowerInvariant());
            }
        }
        /// <summary>
        /// Método que deve ser chamado após todas as movimentações terem sido executadas.
        /// </summary>
        public void End() => entryCollector.Close();

        private T getLowestValueKey<T>(ConcurrentDictionary<T, decimal> dict)
            => dict.OrderBy(x => x.Value).Select(x => x.Key).FirstOrDefault();

        public string GetLowestValueByCategory() => getLowestValueKey(dictCategories) ?? "Não houve";
        public string GetLowestValueByMonth()
        {
            var month = getLowestValueKey(dictMonths);
            return month == 0 ? "Não houve" : ci.DateTimeFormat.GetMonthName(month);
        }

        public decimal GetTotalIn() => totalIn;
        public decimal GetTotalOut() => Math.Abs(totalOut);
        public decimal GetBalance() => totalIn + totalOut;
        public IEnumerable<(string Category, decimal Total)> GetSpentByCategory()
            => from x in dictCategories
               orderby x.Value
               let cat = string.IsNullOrEmpty(x.Key) ? "[sem categoria]" : x.Key
               select (cat, Math.Abs(x.Value));

        public IEnumerable<string> GetOrderedEntries() => entryCollector.GetOrderedEntries(ci);

        public void Dispose() => End();
    }
}