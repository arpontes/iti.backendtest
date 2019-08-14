using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Threading;

namespace iti.backendtest.Tests
{
    [TestClass]
    public class QueryDataUnitTest
    {
        private static readonly CultureInfo ci = new CultureInfo("pt-br");

        private Consolidator buildConsolidatorFromLog(List<string> lst, CancellationToken token)
        {
            var obj = new Consolidator(ci, new EntryCollectorMemory());
            QueryData.ReadLog(lst, obj, token);
            return obj;
        }

        [TestMethod]
        public void LinhaCorreta()
        {
            var obj = buildConsolidatorFromLog(new List<string> { "ignorar", "29-May	Hirota	-13	alimentacao", "30-May	Hirota	-13	alimentacao" }, CancellationToken.None);
            Assert.AreEqual(-26, obj.GetBalance());
        }
        [TestMethod]
        public void ErroDeFormato()
        {
            try
            {
                buildConsolidatorFromLog(new List<string> { "ignorar", "1-" }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Erro no formato da linha"));
            }
        }
        [TestMethod]
        public void UmTokenCanceladoDevePararNaPrimeiraLinha()
        {
            var obj = buildConsolidatorFromLog(new List<string> { "ignorar", "29-May	Hirota	-13	alimentacao", "30-May	Hirota	-13	alimentacao" }, new CancellationToken(true));
            Assert.AreEqual(-13, obj.GetBalance());
        }


        private Consolidator buildConsolidatorFromJson(string json, CancellationToken token)
        {
            var obj = new Consolidator(ci, new EntryCollectorMemory());
            QueryData.ReadJson(json, obj, token);
            return obj;
        }

        [TestMethod]
        public void JsonCorreto()
        {
            var obj = buildConsolidatorFromJson("[{data: '11/jul',descricao: 'x',valor: '-13,00', categoria: 'x' }, {data: '11/jul',descricao: 'x',valor: '-13,00', categoria: 'x' }]", CancellationToken.None);
            Assert.AreEqual(-26, obj.GetBalance());
        }
        [TestMethod]
        public void ErroDeFormatoJson()
        {
            try
            {
                buildConsolidatorFromJson("[{ x: 1 }]", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Erro no formato do elemento na posição"));
            }
        }
        [TestMethod]
        public void ErroDeJsonInvalido()
        {
            try
            {
                buildConsolidatorFromJson("{", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Erro no formato do JSON"));
            }
        }
        [TestMethod]
        public void UmTokenCanceladoDevePararNaPrimeiraLinhaJson()
        {
            var obj = buildConsolidatorFromJson("[{data: '11/jul',descricao: 'x',valor: '-13,00', categoria: 'x' }, {data: '11/jul',descricao: 'x',valor: '-13,00', categoria: 'x' }]", new CancellationToken(true));
            Assert.AreEqual(-13, obj.GetBalance());
        }
    }
}