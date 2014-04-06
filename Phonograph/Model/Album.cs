using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Phonograph.Model
{
    [Table("albums")]
    public class Album
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("sort_title")]
        public string SortTitle { get; set; }

        [Column("album_artist_id")]
        public int AlbumArtistId { get; set; }
    }
}
