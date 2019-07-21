using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace iti.backendtest
{
    class Program
    {
        static void Main(string[] args)
        {
            var obj = new Consolidator();

            //A leitura das linhas do arquivo de log deverá ser feita por meio de um iterator, de modo a manter um consumo
            //menor de memória do que a leitura completa do arquivo, já que eu não sei o tamanho dos logs e preciso apenas de acesso
            //sequencial às linhas.
            var lines = File.ReadLines(args[0]);

            //Pular a primeira linha, que contém os títulos das colunas.
            lines.GetEnumerator().MoveNext();

            foreach (var line in lines)
            {
                var lineData = readLine(line);
                obj.ProcessEntry(lineData.Month, lineData.Value, lineData.Category);
            }
            Console.WriteLine(obj.GetSummary());
            Console.ReadLine();
        }

        private static CultureInfo ci = new CultureInfo("pt-br");
        private static (string Month, decimal Value, string Category) readLine(string line)
        {
            //Eu poderia ter utilizado um split, mas ele acaba alocando mais recursos do que o necessário.
            //Em testes com milhões de registros, mesmo com split, o número de execuções do GC ficou bastante baixo,
            //mas a execução sem ele ficou por volta de 20% mais rápida. Neste caso, mesmo tornando o código mais difícil de ler
            //do que um split, considero que não é tão difícil assim para deixar de lado essa diminuição de alocações.

            //Vou ignorar a primeira coluna, pois o valor que quero dela está fixo entre os índices 3 e 6.
            //Primeiro tab, divisão entre a primeira e segunda colunas, posso ignorar também, pois não busco nada na segunda coluna.
            var secondTab = line.IndexOf('\t', line.IndexOf('\t', 0) + 1);

            var lastTab = line.IndexOf('\t', secondTab + 1);
            //O valor está entre o segundo e terceiro tabs.
            var val = line.Substring(secondTab + 1, lastTab - secondTab - 1);
            //A categoria está após o último tab.
            var categ = line.Substring(lastTab + 1);

            return (
                line.Substring(3, 3), //considerando que o padrão desta coluna sempre será DD-MMM
                decimal.Parse(val, ci),
                categ.Trim()
            );
        }
    }
}
