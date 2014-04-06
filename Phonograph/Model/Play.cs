using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Phonograph.Model
{
    [Table("plays")]
    public class Play
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("track_id")]
        public int TrackId { get; set; }

        [Column("source_id")]
        public int SourceId { get; set; }

        [Column("time")]
        public DateTime Time { get; set; }
    }
}
