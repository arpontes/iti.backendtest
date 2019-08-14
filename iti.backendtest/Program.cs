using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace iti.backendtest
{
    public class Program
    {
        private static readonly CultureInfo ci = new CultureInfo("pt-br");


        static void Main(string[] args)
        {
            var logFilePath = args[0];
            var isLogFileEstimatedAsBig = args.Length > 1 && args[1] == "1";

            //Se o arquivo for muito grande, vale a pena utilizar o sistema de arquivos para armazenar a lista das movimentações.
            //O uso de memória (e consequentemente a pressão sobre o GC) é muito menor. Utilizando um arquivo de 1.5 milhão de linhas, a execução
            //diretamente na memória RAM consumiu 367Mb, e levou 3 minutos. Na mesma máquina, a execução utilizando o disco (SSD) consumiu apenas 
            //95Mb de RAM e levou 3:04 minutos. Houve muito menos execuções do GC, o que provavelmente equilibrou a velocidade em relação ao acesso ao disco, que é mais lento.
            //Se o arquivo for pequeno, a diferença no uso de memória não compensa. Seria preciso realizar benchmarkings para identificar o ponto ótimo
            //no qual passamos a ver vantagem em utilizar o sistema de arquivos e assim deixar o sistema escolher automaticamente baseando-se no tamanho do arquivo, por exemplo.
            //Em relação aos JSONs, utilizei o Newtonsoft, que é utilizado pelo próprio .Net Core até a versão 2.2. Isso significa que um arquivo json muito grande levará a um consumo grande de memória RAM.
            //Na versão 3, haveria a possibilidade de utilizar o novo serializador, que consegue fazer a leitura do JSON de forma iterativa, o que levaria a uma redução no uso de memória.

            using (var entryCollector = isLogFileEstimatedAsBig ? (IEntryCollector)new EntryCollectorFile() : new EntryCollectorMemory())
            using (var obj = new Consolidator(ci, entryCollector))
            {
                //As três chamadas (arquivo de log, e os endpoints de pagamentos e recebimentos) não tem dependências entre si, logo, podem ser paralelizadas.
                //A única consequência foi a necessidade de deixar o Consolidador thread-safe.
                //Caso alguma das chamadas apresente erro, estou assumindo que o resultado do log não será confiável, logo, as demais chamadas serão canceladas pelo CancellationToken e o erro será apresentado.
                using (var cancelToken = new CancellationTokenSource())
                {
                    async Task callThreadExec(Func<Task> fn)
                    {
                        try
                        {
                            await fn();
                        }
                        catch
                        {
                            cancelToken.Cancel();
                            throw;
                        }
                    }

                    //No .Net Core 3 haverá a classe IAsyncEnumerable, que eliminaria a necessidade de utilizar Task.Run no método ReadLog.
                    Task threadFile(string filePath) => callThreadExec(() => Task.Run(() => QueryData.ReadLog(File.ReadLines(filePath), obj, cancelToken.Token)));
                    Task threadJson(string url) => callThreadExec(async () =>
                    {
                        try
                        {
                            var ret = await readJsonAsync(url, cancelToken.Token);
                            if (!ret.Success)
                                throw new Exception(ret.Result);
                            QueryData.ReadJson(ret.Result, obj, cancelToken.Token);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Erro na chamada a {url}: {ex.Message}");
                        }
                    });

                    obj.Prepare();
                    try
                    {
                        Task.WaitAll(
                            threadFile(logFilePath),
                            threadJson("https://my-json-server.typicode.com/cairano/backend-test/pagamentos"),
                            threadJson("https://my-json-server.typicode.com/cairano/backend-test/recebimentos")
                        );
                        obj.End();
                        writeSuccess(obj);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("erro: " + ex.Message);
                    }
                }
            }
            Console.ReadLine();
        }

        private const string SUMMARYMODEL = @"Sumário:
Em qual categoria o cliente gastou mais? {0}
Em qual mês o cliente gastou mais? {1}
Qual o total de gastos do cliente? {2}
Qual o total de recebimentos do cliente? {3}
Saldo total de movimentações do cliente: {4}
";
        private static void writeSuccess(Consolidator obj)
        {
            Console.WriteLine("Lista de todas as movimentações:");
            foreach (var item in obj.GetOrderedEntries())
                Console.WriteLine(item);

            Console.WriteLine("\n----------\n");

            Console.WriteLine("Lista dos gastos por categoria:");
            foreach (var item in obj.GetSpentByCategory())
                Console.WriteLine($"{item.Category}: {item.Total.ToString("C", ci)}");

            Console.WriteLine("\n----------\n");

            Console.WriteLine(string.Format(SUMMARYMODEL, obj.GetLowestValueByCategory(), obj.GetLowestValueByMonth(), obj.GetTotalOut().ToString("C", ci), obj.GetTotalIn().ToString("C", ci), obj.GetBalance().ToString("C", ci)));
        }
        private static async Task<(bool Success, string Result)> readJsonAsync(string url, CancellationToken cancelToken)
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url, cancelToken))
                if (response.StatusCode == HttpStatusCode.OK)
                    return (true, await response.Content.ReadAsStringAsync());
                else
                    return (false, $"Erro lendo endpoint {url}: ({response.StatusCode})");
        }

    }
}
