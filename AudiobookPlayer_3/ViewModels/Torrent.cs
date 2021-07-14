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
        }

        public async void LoadAllTorrents()
        {
            SetupEngine();
            DirectoryInfo d = new DirectoryInfo(rootPath);
            foreach(var file in d.GetFiles("*.torrent"))
            {
                Debug.WriteLine($"TORRENT : Load All Torrents found : {file.FullName}");
                AddTorrent(file.FullName);
            }
            await StartEngine();
        }

        public bool AddTorrent(string torrentPath)
        {
            Debug.WriteLine("TORRENT : Adding Torrent : " + torrentPath);
            try
            {
                MonoTorrent.Torrent torrent = Task.Run(async () => await MonoTorrent.Torrent.LoadAsync(torrentPath)).Result;
                Debug.WriteLine($"TORRENT : torrent created : {torrent.Name}");

                TorrentManager manager = Engine.AddAsync(torrent, rootPath).Result;
                manager.PeersFound += ManagerPeersFound;

                Debug.WriteLine($"TORRENT : Engine Count : {Engine.Torrents.Count}");

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"TORRENT : Add Torrent ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                return false;
            }
        }

        public async Task<bool> StartEngine()
        {
            try
            {
                Debug.WriteLine($"TORRENT : StartEngine : starting");
                if (Engine.Torrents.Count == 0)
                {
                    Debug.WriteLine($"TORRENT : StartEngine : No Torrents found {Engine.Torrents.Count}");
                    return await Task.FromResult(false);
                }
                foreach (TorrentManager manager in Engine.Torrents)
                {
                    manager.PeerConnected += (o, e) =>
                    {
                        Debug.WriteLine($"TORRENT : {e.TorrentManager.Torrent.Name} connected to peers {e.Peer.Uri}");
                    };
                    manager.ConnectionAttemptFailed += (o, e) =>
                    {
                        Debug.WriteLine($"TORRENT : Connection failed for {e.TorrentManager.Torrent.Name} : {e.Peer.ConnectionUri} - {e.Reason}");
                    };
                    manager.PieceHashed += delegate (object o, PieceHashedEventArgs e)
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
                    foreach (TorrentManager manager in Engine.Torrents)
                    {
                        Debug.WriteLine($"TORRENT : {(manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name)} - {manager.State} {manager.Progress:0.00}%");
                    }
                    await Task.Delay(10000);
                }
                return await Task.FromResult(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"TORRENT : Start Engine ERROR : {e.Message}\nStack Trace : {e.StackTrace}");
                return await Task.FromResult(false);
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

        public async Task<String> CreateTorrent(string filePath)
        {
            Debug.WriteLine($"TORRENT : Creating Torrent from {filePath}");
            string fileDestination = Path.Combine(filePath + ".torrent");
            Debug.WriteLine($"TORRENT : crated Torrent should be {fileDestination}");
            TorrentCreator c = new TorrentCreator();
            List<string> trackersList = new List<string>();
            trackersList.Add(DefaultTracker);
            c.Announces.Add(trackersList);
            Debug.WriteLine($"TORRENT : about to try and create torrent from {filePath} to {fileDestination}");
            //c.Create(new TorrentFileSource(filePath), fileDestination); // seems to hang
            await c.CreateAsync(new TorrentFileSource(filePath), fileDestination);
            Debug.WriteLine($"TORRENT : create torrent created : {fileDestination}");

            if(File.Exists(fileDestination))
            {
                SetupEngine();
                AddTorrent(fileDestination);
                StartEngine();
                return await Task.FromResult(fileDestination);
            }
            else
            {
                Debug.WriteLine($"TORRENT : create torrent torrent dosent exist : {fileDestination}");
                return await Task.FromResult(string.Empty);
            }
            

        }
    }
}
