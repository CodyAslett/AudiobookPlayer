using AudiobookPlayer_3.Models;
using AudiobookPlayer_3.Views;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using MediaManager;

using Xamarin.Forms;
using Xamarin.Essentials;
using AudiobookPlayer_3.Services;
using System.Collections.Generic;

namespace AudiobookPlayer_3.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        public Command LoadItemsCommand { get; }
        public Command AddItemCommand { get; }
        public Command<Item> ItemTapped { get; }
        public Command<Item> ItemSwipped { get; }
        public ObservableCollection<Item> Items { get; }

        private Item _selectedItem;

        public ObservableCollection<ItemsGroup> GroupedItems { get; private set; }


        async void OnItemSelected(Item item)
        {
            try
            {
                if (item.OnDivice)
                {
                    Debug.WriteLine("ITEMS : on Item selected");
                    IsBusy = true;

                    if (item == null)
                    {
                        Debug.WriteLine("ITEMS : on item selected : item is null");
                        return;
                    }
                    Debug.WriteLine("ITEMS : on item selected : item is not null");
                    await CrossMediaManager.Current.Pause();
                    BookTittle = item.FileName;
                    try
                    {
                        //Player.Play(item);
                        Debug.WriteLine("On Item Selected : try and play");
                        await Player.Play(item);
                        Debug.WriteLine("Item selected : File " + item.FileName + " will seek to " + item.Pos);
                        await Player.SeekTo(TimeSpan.FromSeconds(item.Pos), item);
                        Pos = item.Pos;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ERROR : FAILED TO PLAY Track " + e.Message + "\n" + e.StackTrace);
                    }

                    IsBusy = false;
                    await Shell.Current.Navigation.PopToRootAsync();
                }
                else // not on divice
                {

                    bool userWantsToDownload = await Application.Current.MainPage.DisplayAlert("Download", $"Do you Want to download {item.FileName}", "Yes", "No");
                    if (userWantsToDownload)
                    {
                        if (Server.DownloadFile(item.ServerID))
                        {
                            // setup torrent
                            try
                            {
                                Torrent t = new Torrent();
                                string torrentPath = await t.CreateTorrent(FileSystem.AppDataDirectory + item.FileName + ".torrent");
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"ERROR : Selected Item : On Add Torrent : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Selected Item Error : failed to download torrent file");
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ITEMS : item selected : ERROR : selecting item failed : " + e.Message + "\n" + e.StackTrace);
            }

        }

        async void OnItemSwipped(Item item)
        {
            Debug.WriteLine("ITEMS : On Item Swipped starting");
            IsBusy = false;
            if (item.OnDivice)
            {
                bool userWantsToDelete = false;
                userWantsToDelete = await Application.Current.MainPage.DisplayAlert("Caution : Deleting " + item.BookName, "Are you Sure you want to delete File : " + item.FileName, "Yes Delete IT", "NO!!! STOP");
                if (userWantsToDelete == true)
                {
                    await DataStore.DeleteItemAsync(item.PrimaryId);

                }
                IsBusy = true;
                IsBusy = false;
            }
            // TODO : add deleting from server
        }

        string itemTitle = String.Empty;
        public string ItemTitle
        {
            get
            {
                Debug.WriteLine("ITEMS : getting item Title");
                return itemTitle;
            }
            set
            {
                Debug.WriteLine("ITEMS : setting item Title");
                if (itemTitle == value)
                {
                    return;
                }

                itemTitle = value;
                OnPropertyChanged(nameof(ItemTitle));
            }
        }


        public ItemsViewModel()
        {
            Debug.WriteLine("Items : View Model Starting");
            Title = "Library";

            PermissionCheck();

            Items = new ObservableCollection<Item>();

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            ItemTapped = new Command<Item>(OnItemSelected);
            ItemSwipped = new Command<Item>(OnItemSwipped);
            AddItemCommand = new Command(OnAddItem);

            GroupedItems = new ObservableCollection<ItemsGroup>();
        }


        async Task ExecuteLoadItemsCommand()
        {
            Debug.WriteLine("ITEMS : Execute Load Items Command");
            IsBusy = true;

            try
            {
                List<Item> tempItems = new List<Item>();
                List<Item> tempServerItems = new List<Item>();

                Items.Clear();
                GroupedItems.Clear();
                IEnumerable<Item> items = await DataStore.GetItemsAsync(true);
                foreach (Item item in items)
                {
                    Items.Add(item);
                    tempItems.Add(item);
                }
                IEnumerable<Item> serverItems = await DataStore.GetServerItemsAsync(true);
                
                foreach (Item si in serverItems)
                {
                    tempServerItems.Add(si);
                }
                GroupedItems.Add(new ItemsGroup("On Divice", tempItems));
                GroupedItems.Add(new ItemsGroup("On Server", tempServerItems));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }


        public void OnAppearing()
        {
            IsBusy = true;
            Debug.WriteLine("ITEMS : Appearing");
            PermissionCheck();

            OnPropertyChanged(nameof(Items));


            SelectedItem = null;
            IsBusy = false;
        }


        public Item SelectedItem
        {
            get => _selectedItem;
            set
            {
                Debug.WriteLine("ITEMS : Item Selected " + value);
                IsBusy = true;
                if (value != null)
                {
                    try
                    {
                        Debug.WriteLine("Item Selected " + value.FileName + " at " + value.Pos);
                        Item oldItem = Player.CurrentItem;
                        if (oldItem != null)
                            DataStore.UpdateItemAsync(oldItem);
                        Player.Play(value);
                        Player.SeekTo(TimeSpan.FromSeconds(value.Pos));
                        ItemTitle = value.FileName;
                        SetProperty(ref _selectedItem, value);

                        Preferences.Set("playerPath", value.Path);
                        Preferences.Set("playerTitle", value.FileName);
                        Preferences.Set("playerDiscription", value.Description);
                        //CurrentItem = value;
                        Debug.WriteLine("selecteditme trying to play");
                        Task.Factory.StartNew(() => Player.Play(value));
                        //Pos = Player.Pos;

                        OnPropertyChanged(nameof(TrackLength));
                        OnPropertyChanged(nameof(Items));

                        IsBusy = false;
                        Debug.WriteLine("Done Selecting Item");
                        Task.Factory.StartNew(() => Shell.Current.GoToAsync(".."));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ITEMS : Selecting Items Failed : " + e.Message + "\n" + e.StackTrace);
                    }
                    Task.Factory.StartNew(() => Shell.Current.GoToAsync(".."));
                }
            }
        }


        private async void OnAddItem(object obj)
        {
            Debug.WriteLine("Starting Adding Item");
            IsBusy = true;
            PermissionCheck();
            FileResult pickResult = await FilePicker.PickAsync();
            try
            {
                if (pickResult != null)
                {
                    Debug.WriteLine("**********[My Debugging] : File Picked : " + pickResult.FullPath.ToString() + " : " + pickResult.FileName);
                    string path = pickResult.FullPath;
                    Debug.WriteLine($"ON ADD ITEM : {path} File Picked - {Path.GetFileName(path)}");
                    string name = Path.GetFileNameWithoutExtension(path);
                    string type = pickResult.ContentType.ToString();
                    int hash = pickResult.GetHashCode();
                    // move file to app directory 
                    try
                    {
                        string destinationPath = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(pickResult.FullPath));
                        Debug.WriteLine($"ON ADD ITEM : copying {pickResult.FullPath} to {destinationPath}");

                        CopyFileInterface copyDependencyResult = DependencyService.Get<CopyFileInterface>();
                        if (copyDependencyResult == null)
                        {
                            Debug.WriteLine($"ON ADD ITEM : Dependency copy returned null");
                        }
                        else
                        {
                            bool copyResult = copyDependencyResult.Copy(pickResult);
                            if (copyResult)
                            {
                                string shouldBeNewFile = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(path));
                                Debug.WriteLine($"ON ADD ITEM : Copied to : {shouldBeNewFile}");
                                path = shouldBeNewFile;
                            }
                            else
                            {
                                Debug.WriteLine($"ON ADD ITEM : Copied Failed");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"ON ADD ITEM : ERROR failed to copy file {e.Message}\nSTACK TRACE : {e.StackTrace}");
                    }
                    Debug.WriteLine($"ON ADD ITEM : current File path size is {new FileInfo(path).Length} bytes");
                    if (File.Exists(path))
                    {
                        // setup torrent
                        try
                        {
                            Torrent t = new Torrent();
                            string torrentPath = await t.CreateTorrent(path);
                            Server.UploadFile(path + ".torrent");
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"ERROR : On Add Torrent : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"ON ADD ITEM : {path} NOT FOUND");
                    }
                    Debug.WriteLine($"ON ADD ITEM : Path = {path}");
                    Item newItem = new Item()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ServerID = 0,
                        FileName = name,
                        Path = path,
                        Size = string.Empty,
                        Description = path,
                        UserName = string.Empty,
                        DiviceID = string.Empty,
                        MagnentUrl = string.Empty,
                        Hash = hash.ToString(),
                        BookName = string.Empty,
                        SeriesName = string.Empty,
                        Auther = string.Empty,
                        Reader = string.Empty,
                        Length = string.Empty,
                        OnDivice = true,
                        Pos = 0
                    };

                    bool addedItem = await DataStore.AddItemAsync(newItem);

                    Debug.WriteLine("Add item trying to play");

                    Player.Play(newItem);

                    //CurrentItem = newItem;


                    OnPropertyChanged(nameof(TrackLength));

                    Shell.Current.GoToAsync("..");
                }
                else
                {
                    Debug.WriteLine("**********[My Debugging] : Error: File Picker didn't load");
                }
                IsBusy = false;

                Debug.WriteLine("**********[My Debugging] : Done file add ");
                IsBusy = false;
                //await Shell.Current.Navigation.PushAsync(new AboutPage(), true);
                //await Application.Current.MainPage.DisplayAlert(newItem.Text, newItem.Description, "OK");
            }
            catch (Exception e)
            {
                IsBusy = false;
                Debug.WriteLine("ADD NEW ITEM : ERROR : Message : " + e.Message + "\nStackTrace : " + e.StackTrace);
            }
        }

        private async void PermissionCheck()
        {

            var writePermissionStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (writePermissionStatus != PermissionStatus.Granted)
            {
                Debug.WriteLine("Items : trying to get file write permission ");
                writePermissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
            }
            else
            {
                Debug.WriteLine("Items : Items View Model Have File Write Permission ");
            }
        }


    }
}