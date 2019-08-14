using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace iti.backendtest
{
    public interface IEntryCollector
    {
        void Prepare();
        void AddEntry(Consolidator.Entry entry, CultureInfo ci);
        void Close();
        IEnumerable<string> GetOrderedEntries(CultureInfo ci);
    }

    public class EntryCollectorMemory : IEntryCollector
    {
        private readonly ConcurrentBag<Consolidator.Entry> allEntries = new ConcurrentBag<Consolidator.Entry>();

        public void Prepare() { }
        public void AddEntry(Consolidator.Entry entry, CultureInfo ci) => allEntries.Add(entry);
        public void Close() { }
        public IEnumerable<string> GetOrderedEntries(CultureInfo ci) => from x in allEntries orderby x.Month, x.Day select x.ToString(ci);
    }
    public class EntryCollectorFile : IEntryCollector
    {
        private string tempFilePath;
        private Stream fileAllEntries;
        public void Prepare()
        {
            tempFilePath = Path.GetTempFileName();
            fileAllEntries = new FileStream(tempFilePath, FileMode.Open);
        }

        private int lastWriteByte;
        private readonly List<(byte Month, byte Day, int fileStart, int fileEnd)> allEntries = new List<(byte Month, byte Day, int fileStart, int fileEnd)>();
        private readonly object lockFile = new object();
        public void AddEntry(Consolidator.Entry entry, CultureInfo ci)
        {
            lock (lockFile)
            {
                var bt = Encoding.UTF8.GetBytes(entry.ToString(ci));
                fileAllEntries.Write(bt, 0, bt.Length);
                allEntries.Add((entry.Month, entry.Day, lastWriteByte, lastWriteByte + bt.Length));
                lastWriteByte += bt.Length;
            }
        }
        public void Close()
        {
            if (fileAllEntries != null)
            {
                fileAllEntries.Close();
                fileAllEntries.Dispose();
                fileAllEntries = null;
            }
        }

        public IEnumerable<string> GetOrderedEntries(CultureInfo ci)
        {
            using (var file = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                var lst = from x in allEntries orderby x.Month, x.Day select x;
                foreach (var item in lst)
                {
                    var bt = new byte[item.fileEnd - item.fileStart];
                    file.Seek(item.fileStart, SeekOrigin.Begin);
                    file.Read(bt, 0, bt.Length);
                    yield return Encoding.UTF8.GetString(bt);
                }
                file.Close();
            }
        }
    }
}