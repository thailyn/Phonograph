using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phonograph.Model
{
    public class PlaysView
    {
        public int Id { get; set; }
        public string TrackTitle { get; set; }
        public string AlbumTitle { get; set; }
        public string ArtistName { get; set; }
        public string SourceName { get; set; }
        public DateTime Time { get; set; }
    }
}
