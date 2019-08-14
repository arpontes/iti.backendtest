using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace iti.backendtest.Tests
{
    [TestClass]
    public class EntryCollectorUnitTest
    {
        private static readonly CultureInfo ci = new CultureInfo("pt-br");

        private void correta(IEntryCollector obj)
        {
            var lst = new[] {
                new Consolidator.Entry { Category = "x", Day = 1, Month = 2, Desc = "xx", Value = -13 },
                new Consolidator.Entry { Category = "x", Day = 10, Month = 2, Desc = "xx", Value = -13 },
                new Consolidator.Entry { Category = "x", Day = 2, Month = 2, Desc = "xx", Value = -13 }
            };

            obj.Prepare();

            Parallel.ForEach(lst, x => obj.AddEntry(x, ci));

            obj.Close();

            var resultList = new List<string> { "01/fev", "02/fev", "10/fev" };
            var i = 0;
            foreach (var item in obj.GetOrderedEntries(ci))
            {
                Assert.IsTrue(item.StartsWith(resultList[i]));
                i++;
            }
            obj.Dispose();
        }

        [TestMethod]
        public void ExecucaoCorretaMemoria() => correta(new EntryCollectorMemory());
        [TestMethod]
        public void ExecucaoCorretaArquivo() => correta(new EntryCollectorFile());

        [TestMethod]
        public void ExecucaoArquivoSemPrepare()
        {
            using (var obj = new EntryCollectorFile())
                Assert.ThrowsException<NullReferenceException>(() => obj.AddEntry(new Consolidator.Entry { Category = "x", Day = 1, Month = 2, Desc = "xx", Value = -13 }, ci));
        }

        [TestMethod]
        public void ExecucaoArquivoSemClose()
        {
            var obj = new EntryCollectorFile();
            obj.Prepare();
            obj.AddEntry(new Consolidator.Entry { Category = "x", Day = 1, Month = 2, Desc = "xx", Value = -13 }, ci);
            Assert.ThrowsException<IOException>(() => obj.GetOrderedEntries(ci).Count());
        }
    }
}
