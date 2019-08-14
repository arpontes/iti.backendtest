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

        private class CategoryKeyIgnoreDiacritics : IEqualityComparer<string>
        {
            private readonly CultureInfo ci;
            public CategoryKeyIgnoreDiacritics(CultureInfo ci) => this.ci = ci;
            public bool Equals(string x, string y) => string.Compare(x, y, ci, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0;
            //Estou retornando sempre zero para forçar a chamada ao Equals, já que as strings com letras acentuadas e sem acento teriam hashs diferentes.
            //Isso impacta na performance do dicionário, porém, considerando que é um dicionário de poucos elementos, e considerando a necessidade
            //de que categorias com palavras acentuadas ou não sejam agrupadas corretamente, a perda na performance acaba compensando.
            //Uma outra possibilidade seria retirar os acentos e obter o GetHashCode da string. Optei, porém, pelo primeiro caso para não aumentar mais ainda o código.
            public int GetHashCode(string obj) => 0;
        }

        private readonly object lockObjIn = new object();
        private readonly object lockObjOut = new object();
        private readonly ConcurrentDictionary<string, decimal> dictCategories;
        private readonly ConcurrentDictionary<byte, decimal> dictMonths = new ConcurrentDictionary<byte, decimal>();
        private decimal totalOut = 0;
        private decimal totalIn = 0;

        private readonly CultureInfo ci;
        private readonly IEntryCollector entryCollector;
        public Consolidator(CultureInfo ci, IEntryCollector entryCollector)
        {
            this.ci = ci;
            this.entryCollector = entryCollector;
            //O dicionário deve ignorar diferenças entre acentos, tratando caracteres acentuados e não acentuados como iguais.
            this.dictCategories = new ConcurrentDictionary<string, decimal>(new CategoryKeyIgnoreDiacritics(ci));
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