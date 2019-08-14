using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;

namespace iti.backendtest.Tests
{
    [TestClass]
    public class ConsolidatorUnitTest
    {
        private static readonly CultureInfo ci = new CultureInfo("pt-br");

        [TestMethod]
        public void SaldoZerado()
        {
            var obj = new Consolidator(ci, new EntryCollectorMemory());
            Assert.AreEqual(0, obj.GetBalance());
        }
        private void buildConsolidator(Action<Consolidator> fn)
        {
            using (var obj = new Consolidator(ci, new EntryCollectorMemory()))
            {
                obj.Prepare();
                obj.ProcessEntry(new Consolidator.Entry { Month = 1, Day = 1, Value = 10, Category = "transporte", Desc = "abc" });
                obj.ProcessEntry(new Consolidator.Entry { Month = 2, Day = 2, Value = -12, Category = "transporte", Desc = "abc" });
                obj.ProcessEntry(new Consolidator.Entry { Month = 5, Day = 5, Value = -74.9m, Category = "transporte", Desc = "abc" });
                obj.ProcessEntry(new Consolidator.Entry { Month = 5, Day = 4, Value = -13.78m, Category = "alimentacao", Desc = "abc" });
                obj.ProcessEntry(new Consolidator.Entry { Month = 5, Day = 1, Value = 35, Category = "transporte", Desc = "abc" });
                obj.ProcessEntry(new Consolidator.Entry { Month = 6, Day = 20, Value = -12, Category = "alimentacao", Desc = "abc" });
                obj.ProcessEntry(new Consolidator.Entry { Month = 6, Day = 1, Value = 6.09m, Category = "transporte", Desc = "abc" });
                obj.End();
                fn(obj);
            }
        }
        [TestMethod]
        public void SaldoCorreto() => buildConsolidator(x => Assert.AreEqual(-61.59m, x.GetBalance()));
        [TestMethod]
        public void MesEmQueGastouMais() => buildConsolidator(x => Assert.AreEqual("maio", x.GetLowestValueByMonth()));
        [TestMethod]
        public void CategoriaEmQueGastouMais() => buildConsolidator(x => Assert.AreEqual("transporte", x.GetLowestValueByCategory()));
        [TestMethod]
        public void TotalRecebimentos() => buildConsolidator(x => Assert.AreEqual(51.09m, x.GetTotalIn()));
        [TestMethod]
        public void TotalPagamentos() => buildConsolidator(x => Assert.AreEqual(112.68m, x.GetTotalOut()));
        [TestMethod]
        public void Categorias()
            => buildConsolidator(x =>
            {
                var resultList = new List<(string Cat, decimal Tot)> { ("transporte", 86.9m), ("alimentacao", 25.78m) };
                var i = 0;
                foreach (var item in x.GetSpentByCategory())
                {
                    Assert.AreEqual(item.Category, resultList[i].Cat);
                    Assert.AreEqual(item.Total, resultList[i].Tot);
                    i++;
                }
            });
        [TestMethod]
        public void MovimentacoesOrdenadas()
            => buildConsolidator(x =>
            {
                var resultList = new List<string> { "01/jan", "02/fev", "01/mai", "04/mai", "05/mai", "01/jun", "20/jun" };
                var i = 0;
                foreach (var item in x.GetOrderedEntries())
                {
                    Assert.IsTrue(item.StartsWith(resultList[i]));
                    i++;
                }
            });
    }
}