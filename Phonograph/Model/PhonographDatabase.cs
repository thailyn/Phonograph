using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Phonograph.Model
{
    class PhonographDatabase : SQLiteConnection
    {
        public PhonographDatabase(string path)
            : base(path)
        {
            if (!System.IO.File.Exists(path))
            {
                CreateTable<Album>();
                CreateTable<Artist>();
                CreateTable<Play>();
                CreateTable<Source>();
                CreateTable<Track>();
            }
        }
    }
}
