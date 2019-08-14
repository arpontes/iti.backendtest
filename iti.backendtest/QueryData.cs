using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iti.backendtest
{
    public class QueryData
    {
        private static Dictionary<string, byte> buildMonthNamesDict(CultureInfo c)
            => c.DateTimeFormat.AbbreviatedMonthNames.Take(12).Select((name, idx) => (name, idx)).ToDictionary(x => x.name.ToLower(), x => Convert.ToByte(x.idx + 1));
        private static readonly CultureInfo ci = new CultureInfo("pt-br");
        private static readonly Dictionary<string, byte> dictPtBrMonthNames = buildMonthNamesDict(ci);
        private static readonly Dictionary<string, byte> dictEnglishMonthNames = buildMonthNamesDict(CultureInfo.InvariantCulture);

        /// <summary>
        /// Lê, linha a linha, o enumerador recebido como parâmetro e insere os elementos no Consolidador.
        /// O formato do log para o mês do ano é abreviado em inglês, e o campo "valor" no formato pt-br (vírgula como separador de decimais).
        /// </summary>
        /// <param name="lines">Enumerador com as linhas, considerando que a primeira linha deverá ser ignorada, pois deve conter os títulos das colunas no arquivo de log.</param>
        /// <param name="consolidator">Objeto responsável por realizar a consolidação das movimentações.</param>
        /// <param name="cancelToken">Token de cancelamento, para parar a execução prematuramente caso o sistema tenha encontrado algum erro nas demais threads.</param>
        public static void ReadLog(IEnumerable<string> lines, Consolidator consolidator, CancellationToken cancelToken)
        {
            var en = lines.GetEnumerator();
            //Pular a primeira linha, que contém os títulos das colunas.
            en.MoveNext();

            var i = 0;
            while (en.MoveNext())
            {
                var line = en.Current;
                consolidator.ProcessEntry(readLogLine(line));
                if (i++ % 100 == 0 && cancelToken.IsCancellationRequested)
                    break;
            }

            Consolidator.Entry readLogLine(string line)
            {
                try
                {
                    //Eu poderia ter utilizado um split, mas ele acaba alocando mais recursos do que o necessário.
                    //Em testes com milhões de registros, mesmo com split, o número de execuções do GC ficou bastante baixo,
                    //mas a execução sem ele ficou por volta de 20% mais rápida. Neste caso, mesmo tornando o código mais difícil de ler
                    //do que um split, considero que não é tão difícil assim para deixar de lado essa diminuição de alocações.

                    //Vou ignorar a primeira coluna, pois o valor que quero dela está fixo entre os índices 0 e 1 / 3 e 6, considerando que o padrão desta coluna será DD-MM.
                    var firstTab = line.IndexOf('\t') + 1;
                    var secondTab = line.IndexOf('\t', firstTab);
                    var desc = line.Substring(firstTab, secondTab - firstTab);

                    var lastTab = line.IndexOf('\t', secondTab + 1);
                    //O valor está entre o segundo e terceiro tabs.
                    var val = line.Substring(secondTab + 1, lastTab - secondTab - 1);
                    //A categoria está após o último tab.
                    var categ = line.Substring(lastTab + 1);

                    var month = line.Substring(3, 3).ToLower();
                    if (!dictEnglishMonthNames.ContainsKey(month))
                        throw new Exception($"Mês {month} não encontrado na lista dos meses possíveis!");

                    return new Consolidator.Entry
                    {
                        Day = byte.Parse(line.Substring(0, 2)),
                        Month = dictEnglishMonthNames[month],
                        Desc = desc,
                        Value = decimal.Parse(val, ci.NumberFormat),
                        Category = categ.Trim()
                    };
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro no formato da linha {line}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Desserializa o json recebido e insere os elementos no Consolidador.
        /// O JSON está retornando os meses do ano abreviados em português, e o campo "valor" no formato pt-br (vírgula como separador de decimais).</param>
        /// </summary>
        /// <param name="json">JSON no formato de um array cujos elementos têm os seguintes atributos: data, descricao, valor, categoria. O JSON também trará o atributo "moeda", mas este elemento não será utilizado.</param>
        /// <param name="consolidator">Objeto responsável por realizar a consolidação das movimentações.</param>
        /// <param name="cancelToken">Token de cancelamento, para parar a execução prematuramente caso o sistema tenha encontrado algum erro nas demais threads.</param>
        public static void ReadJson(string json, Consolidator consolidator, CancellationToken cancelToken)
        {
            var entries = new[] { new { data = string.Empty, descricao = string.Empty, valor = string.Empty, categoria = string.Empty } };
            try
            {
                entries = JsonConvert.DeserializeAnonymousType(json, entries);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro no formato do JSON: {ex.Message}");
            }

            for (var i = 0; i < entries.Length; i++)
            {
                consolidator.ProcessEntry(readJsonElement(i));
                if (i % 100 == 0 && cancelToken.IsCancellationRequested)
                    break;
            }

            Consolidator.Entry readJsonElement(int idx)
            {
                var item = entries[idx];
                try
                {
                    //Há dois formatos observados para a data: "DD/MMM" e "DD / MMM". Assim, para obter o mês, vou pegar os três últimos caracteres.
                    var month = item.data.Substring(item.data.Length - 3, 3).ToLower();
                    if (!dictPtBrMonthNames.ContainsKey(month))
                        throw new Exception($"Mês {month} não encontrado na lista dos meses possíveis!");

                    return new Consolidator.Entry
                    {
                        Day = byte.Parse(item.data.Substring(0, 2)),
                        Month = dictPtBrMonthNames[month],
                        Desc = item.descricao,
                        Value = decimal.Parse(item.valor.Replace(" ", ""), ci.NumberFormat),
                        Category = item.categoria?.Trim()
                    };
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro no formato do elemento na posição {idx}: {ex.Message}");
                }
            }
        }
    }
}