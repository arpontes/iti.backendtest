using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace iti.backendtest.Tests
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void SaldoZerado()
        {
            var obj = new Consolidator();
            Assert.AreEqual(0, obj.GetBalance());
        }
        private Consolidator buildConsolidator()
        {
            var obj = new Consolidator();
            obj.ProcessEntry("Jan", 10, "transporte");
            obj.ProcessEntry("Feb", -12, "transporte");
            obj.ProcessEntry("May", -74.9m, "transporte");
            obj.ProcessEntry("May", -13.78m, "alimentacao");
            obj.ProcessEntry("May", 35, "transporte");
            obj.ProcessEntry("Jun", -12, "alimentacao");
            obj.ProcessEntry("Jun", 6.09m, "transporte");
            return obj;
        }
        [TestMethod]
        public void SaldoCorreto() => Assert.AreEqual(-61.59m, buildConsolidator().GetBalance());
        [TestMethod]
        public void MesEmQueGastouMais() => Assert.AreEqual("May", buildConsolidator().GetLowestValueByMonth());
        [TestMethod]
        public void CategoriaEmQueGastouMais() => Assert.AreEqual("transporte", buildConsolidator().GetLowestValueByCategory());
        [TestMethod]
        public void TotalRecebimentos() => Assert.AreEqual(51.09m, buildConsolidator().GetTotalIn());
    }
}
