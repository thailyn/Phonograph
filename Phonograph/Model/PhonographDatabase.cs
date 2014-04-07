using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Phonograph.Model
{
    public class PhonographDatabase : SQLiteConnection
    {
        public PhonographDatabase(string path)
            : base(path)
        {
            // The database file is created by the base class, so it always
            // exists by this point.  It is unclear if "creating" the tables
            // here overwrites any existing data.

            CreateTable<Album>();
            CreateTable<Artist>();
            CreateTable<Play>();
            CreateTable<Source>();
            CreateTable<Track>();
        }
    }
}
