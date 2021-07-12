using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Logging;
using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;

using Xamarin;
using Xamarin.Forms;
using Xamarin.Essentials;


namespace AudiobookPlayer_3.ViewModels
{
    public class Torrent
    {
        public ClientEngine Engine { get; private set; }
        public List<MonoTorrent.Torrent> torrents;
        public string DefaultTracker { get; } = "http://codyaslett.com:8000/announce";

        string rootPath = FileSystem.AppDataDirectory;
        public Torrent()
        {
            Debug.WriteLine("TORRENT : starting in " + rootPath);
            SetupEngine();
            string testTorrent = "Ah-MeinKampf-MurphyVersionAudioBook.mp3.torrent";
            string testTorrentPath = Path.Combine(rootPath, testTorrent);
            if (File.Exists(testTorrentPath))
            {
                Debug.WriteLine("TORRENT : Found test torrent");
                bool result = Task.Run(async () => await AddTorrent(testTorrentPath)).Result;
                if (result)
                {
                    StartEngine();
                }
            }
            else
            {
                Debug.WriteLine("TORRENT : Can't find test torrent");
            }

            // SetupEngine();
        }

        public async Task<bool> AddTorrent(string torrentPath)
        {
            Debug.WriteLine("TORRENT : Adding Torrent : " + torrentPath);
            try
            {
                MonoTorrent.Torrent torrent = await MonoTorrent.Torrent.LoadAsync(torrentPath);
                Debug.WriteLine($"TORRENT : torrent created : {torrent.Name}");

                TorrentManager manager = await Engine.AddAsync(torrent, rootPath);
                manager.PeersFound += ManagerPeersFound;

                Debug.WriteLine($"TORRENT : Engine Count : {Engine.Torrents.Count}");

                return await Task.FromResult(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"TORRENT : Add Torrent ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                return await Task.FromResult(false);
            }
        }

        public async void StartEngine()
        {
            Debug.WriteLine($"TORRENT : StartEngine : starting");
            if (Engine.Torrents.Count == 0)
            {
                Debug.WriteLine($"TORRENT : StartEngine : No Torrents found {Engine.Torrents.Count}");
                return;
            }
            foreach(TorrentManager manager in Engine.Torrents)
            {
                manager.PeerConnected += (o, e) =>
                {
                    Debug.WriteLine($"TORRENT : {e.TorrentManager.Torrent.Name} connected to peers {e.Peer.Uri}");
                };
                manager.ConnectionAttemptFailed += (o, e) =>
                {
                    Debug.WriteLine($"TORRENT : Connection failed for {e.TorrentManager.Torrent.Name} : {e.Peer.ConnectionUri} - {e.Reason}");
                };
                manager.PieceHashed += delegate (object o,PieceHashedEventArgs e)
                {
                    Debug.WriteLine($"TORRENT : Piece Hashed for {e.TorrentManager.Torrent.Name} : {e.PieceIndex} - {(e.HashPassed ? "Pas" : "Fail")} - Progress : {e.Progress * 100}%");
                };
                manager.TorrentStateChanged += delegate (object o, TorrentStateChangedEventArgs e)
                {
                    Debug.WriteLine($"TORRENT : Torrent State Changed for {e.TorrentManager.Torrent.Name} : Oldstate: {e.OldState} NewState: {e.NewState}");
                    TorrentState test = e.NewState;
                    if (e.TorrentManager.Complete)
                    {
                        Debug.WriteLine($"TORRENT : {e.TorrentManager.Torrent.Name} is done");
                        // Task r = e.TorrentManager.StopAsync();
                        //e.TorrentManager.StartAsync();
                    }
                };
                manager.TrackerManager.AnnounceComplete += (sender, e) =>
                {
                    Debug.WriteLine($"TORRENT : Announce Completed : {e.Successful}: {e.Tracker}");
                };

                await manager.StartAsync();
            }
            while (Engine.IsRunning)
            {
                foreach(TorrentManager manager in Engine.Torrents)
                {
                    Debug.WriteLine($"TORRENT : {(manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name)} - {manager.State} {manager.Progress:0.00}%");
                }
                await Task.Delay(10000);
            }

        }

        private void SetupEngine()
        {
            //EngineSettings settings = new EngineSettings();
            Debug.WriteLine($"TORRENT : Setup Engine");
            EngineSettingsBuilder settingsBuilder = new EngineSettingsBuilder
            {
                AllowPortForwarding = true,
                CacheDirectory = rootPath,
                DiskCacheBytes = Int32.MaxValue,
            };
            Debug.WriteLine($"TORRENT : setup engine cache size : {settingsBuilder.DiskCacheBytes} Bytes");

            Engine = new ClientEngine(settingsBuilder.ToSettings());
        }

        void ManagerPeersFound (object sender, PeersAddedEventArgs e)
        {
            Debug.WriteLine($"TORRENT : Found {e.NewPeers} new peers and {e.ExistingPeers} existing peers");
        }

        public async void CreateTorrent(string filePath)
        {
            TorrentCreator c = new TorrentCreator();
            List<string> trackersList = new List<string>();
            trackersList.Add(DefaultTracker);
            c.Announces.Add(trackersList);
            BEncodedDictionary metadata = await c.CreateAsync(new TorrentFileSource(rootPath));
        }
    }
}
