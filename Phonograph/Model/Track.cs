using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Phonograph.Model
{
    [Table("tracks")]
    public class Track
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("artist_id")]
        public int ArtistId { get; set; }

        [Column("album_id")]
        public int AlbumId { get; set; }
    }
}
