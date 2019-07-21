using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace iti.backendtest
{
    public class Consolidator
    {
        private Dictionary<string, decimal> dictCategories = new Dictionary<string, decimal>();
        private Dictionary<string, decimal> dictMonths = new Dictionary<string, decimal>();
        private decimal totalOut = 0;
        private decimal totalIn = 0;

        public void ProcessEntry(string month, decimal value, string category)
        {
            void addToDict(Dictionary<string, decimal> dict, string key)
            {
                if (dict.ContainsKey(key))
                    dict[key] += value;
                else
                    dict.Add(key, value);
            }

            if (value > 0)
                totalIn += value;
            else if (value < 0)
            {
                totalOut += value;
                addToDict(dictMonths, month);
                addToDict(dictCategories, category.ToLower());
            }
        }

        private string getLowestValueKey(Dictionary<string, decimal> dict)
            => dict.OrderBy(x => x.Value).Select(x => x.Key).FirstOrDefault();

        public string GetLowestValueByCategory() => getLowestValueKey(dictCategories) ?? "Não houve";
        public string GetLowestValueByMonth() => getLowestValueKey(dictMonths) ?? "Não houve";
        public decimal GetTotalIn() => totalIn;
        public decimal GetTotalOut() => totalOut;
        public decimal GetBalance() => totalIn + totalOut;

        private const string SUMMARYMODEL = @"Resultado:
Em qual categoria o cliente gastou mais? {0}
Em qual mês o cliente gastou mais? {1}
Qual o total de gastos do cliente? {2}
Qual o total de recebimentos do cliente? {3}
Saldo total de movimentações do cliente: {4}
";
        public string GetSummary()
            => string.Format(SUMMARYMODEL, GetLowestValueByCategory(), GetLowestValueByMonth(), GetTotalOut(), GetTotalIn(), GetBalance());
    }
}