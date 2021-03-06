﻿using System;

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
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            TableLayout playsTable = FindViewById<TableLayout>(Resource.Id.PlaysTable);

            string applicationDirectory = System.IO.Path.Combine("/storage", "emulated", "legacy", "Phonograph");
            if (!System.IO.Directory.Exists(applicationDirectory))
            {
                System.IO.Directory.CreateDirectory(applicationDirectory);
            }

            string databasePath = System.IO.Path.Combine(applicationDirectory, "PlaysDatabase.sqlite3");
            var pdb = new Phonograph.Model.PhonographDatabase(databasePath);

            var plays = pdb.Query<PlaysView>(
@"select p.id as ""Id"", t.title as ""TrackTitle"", a.title as ""AlbumTitle"",
    ar.name as ""ArtistName"", s.name as ""SourceName"", p.time as ""Time""
from plays p
inner join tracks t on p.track_id = t.id
inner join albums a on t.album_id = a.id
inner join artists ar on t.artist_id = ar.id
inner join sources s on p.source_id = s.id
order by p.time desc
limit 200");

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

            StartService(new Intent(this, typeof(PhonographService)));
        }
    }
}

