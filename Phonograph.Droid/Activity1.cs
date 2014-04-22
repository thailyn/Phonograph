using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Phonograph.Model;

namespace Phonograph.Droid
{
    [Activity(Label = "Phonograph.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.RefreshButton);
            TableLayout playsTable = FindViewById<TableLayout>(Resource.Id.PlaysTable);

            string databasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Personal), "database.db");
            var pdb = new Phonograph.Model.PhonographDatabase(databasePath);
            /*
            var plays = from p in pdb.Table<Play>()
                        join t in pdb.Table<Track>() on p.TrackId equals t.Id
                        join a in pdb.Table<Album>() on t.AlbumId equals a.Id
                        join s in pdb.Table<Source>() on p.SourceId equals s.Id
                        select new
                        {
                            p.Id,
                            TrackTitle = t.Title,
                            AlbumTitle = a.Title,
                            SourceName = s.Name,
                            p.Time
                        };
             * */
            var plays = pdb.Query<PlaysView>(
@"select p.id as ""Id"", t.title as ""TrackTitle"", a.title as ""AlbumTitle"",
    ar.name as ""ArtistName"", s.name as ""SourceName"", p.time as ""Time""
from plays p
inner join tracks t on p.track_id = t.id
inner join albums a on t.album_id = a.id
inner join artists ar on t.artist_id = ar.id
inner join sources s on p.source_id = s.id");

            foreach(var p in plays)
            {
                TableRow newRow = new TableRow(this);
                TextView tvTrack = new TextView(this);
                tvTrack.SetText(p.TrackTitle, TextView.BufferType.Normal);
                TextView tvArtist = new TextView(this);
                tvArtist.SetText(p.ArtistName, TextView.BufferType.Normal);
                TextView tvAlbum = new TextView(this);
                tvAlbum.SetText(p.AlbumTitle, TextView.BufferType.Normal);
                TextView tvSource = new TextView(this);
                tvSource.SetText(p.SourceName, TextView.BufferType.Normal);
                TextView tvTime = new TextView(this);
                tvTime.SetText(p.Time.ToString(), TextView.BufferType.Normal);

                newRow.AddView(tvTrack);
                newRow.AddView(tvArtist);
                newRow.AddView(tvAlbum);
                newRow.AddView(tvSource);
                newRow.AddView(tvTime);

                playsTable.AddView(newRow);
            }

            button.Click += delegate { button.Text = string.Format("{0} clicks!", count++); };

            StartService(new Intent(this, typeof(PhonographService)));
        }
    }
}

