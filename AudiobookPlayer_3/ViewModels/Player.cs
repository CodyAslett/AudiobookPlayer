using AudiobookPlayer_3.Models;
using AudiobookPlayer_3.Services;
using MediaManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AudiobookPlayer_3.ViewModels
{
    static class Player
    {
        public static IDataStore<Item> DataStore => DependencyService.Get<IDataStore<Item>>();

        private static Item currentItem = null;

        private static bool hasInit = false;


        static Player()
        {
            Debug.WriteLine("Player : player booting");
            try
            {
                PermissionCheck();
                CrossMediaManager.Current.Init();
                hasInit = true;
            }
            catch(Exception e)
            {
                Debug.WriteLine("***** PLAYER : ERROR : Failed to Initiate : " + e.Message + "\n" + e.StackTrace);
            }
        }

        public static Item CurrentItem
        {
            get
            {
                if (!hasInit)
                    return null;

                Debug.WriteLine("PLAYER : getting CurrentItem ");
                if (currentItem != null)
                {
                    try
                    {
                        currentItem.Pos = CrossMediaManager.Current.Position.TotalSeconds;
                        return currentItem;
                    }
                    catch
                    {

                    }

                }
                return null;
            }
            set
            {
                if (!hasInit)
                    return;
                Debug.WriteLine("starting Setting Current Item " + value.Path);
                if (currentItem == value)
                {
                    Debug.WriteLine("current item alread = value will return");
                    return;
                }
                currentItem = value;
            }
        }

        public static double Pos
        {
            get
            {
                if (!hasInit)
                {
                    Debug.WriteLine("Player : Pos : has not initiated");
                    return 0;
                }
                Debug.WriteLine("PLAYER : Getting Pos ");
                if (currentItem != null)
                {
                    currentItem.Pos = CrossMediaManager.Current.Position.TotalSeconds;
                    return currentItem.Pos;
                }
                Debug.WriteLine("Player : Pos : no current Item");
                return 1;
            }
            set
            {
                if (!hasInit)
                    return;
                try
                {
                    Debug.WriteLine("PLAYER : Setting Pos : " + value);
                    if (currentItem != null)
                    {
                        Debug.WriteLine("Setting Player Pos to : " + value);
                        currentItem.Pos = value;
                        DataStore.UpdateItemAsync(currentItem);
                        Task.Factory.StartNew(() => SeekTo(TimeSpan.FromSeconds(value), currentItem));
                        return;
                    }
                    else
                    {

                        Task.Factory.StartNew(() => Play(DataStore.GetItemAsync(Preferences.Get("playerPath", null)).Result));
                        Debug.WriteLine("ERROR : in Player Pos - Current Item Not Set");
                    }
                }
                catch
                {
                    Debug.WriteLine("Could Not Update Player Pos");
                }
            }
        }

        public static double Length
        {
            get
            {
                if (!hasInit)
                    return 0;
                Debug.WriteLine("Player : getting Length : " + CrossMediaManager.Current.Duration.TotalSeconds);
                if (CrossMediaManager.Current.State == MediaManager.Player.MediaPlayerState.Playing || CrossMediaManager.Current.State == MediaManager.Player.MediaPlayerState.Paused)
                {
                    return CrossMediaManager.Current.Duration.TotalSeconds;
                }
                return 1;
            }
        }

        public static async void Play()
        {
            if (!hasInit)
                return;
            Debug.WriteLine("trying to paly Play");
            await CrossMediaManager.Current.Play();
        }


        public static async Task<bool> Play(Item item)
        {
            if (!hasInit)
                return false;
            try
            {
                Debug.WriteLine("Player Play : Starting Playing " + item.Path);

                Preferences.Set("playerTitle", item.FileName);
                Preferences.Set("playerPath", item.Path);
                Preferences.Set("playerDiscription", item.Description);
                Preferences.Set("playerPos", item.Description);
                Preferences.Set("playerId", item.PrimaryId.ToString());
                if (CrossMediaManager.IsSupported)
                {
                    if (item == currentItem)
                    {

                        Debug.WriteLine("Player Play : Player Playing stoped becuse Already currentItem " + item.Path + " == " + currentItem.Path);
                        await CrossMediaManager.Current.Play(currentItem.Path);
                        await CrossMediaManager.Current.SeekTo(TimeSpan.FromSeconds(item.Pos));
                        //PlayPause();
                        return true;
                    }

                    Debug.WriteLine("Player Play : Player Starting Playing " + item.Path + " at " + item.Pos);
                    double itemPos = item.Pos;// DataStore.GetItemAsync(item.PrimaryId).Result.Pos;

                    Debug.WriteLine("Trying to play : " + item.Path + " at " + itemPos);
                    try
                    {

                        _ = await CrossMediaManager.Current.Play(item.Path);
                        Debug.WriteLine("Player Play : Playing Will now try to seek to " + itemPos + " : " + CrossMediaManager.Current.State);
                        currentItem = item;
                        await CrossMediaManager.Current.SeekTo(TimeSpan.FromSeconds(itemPos));

                        currentItem = item;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Player Play : Error : Failed To Add item : " + e.Message + ":\n" + e.StackTrace);
                    }
                }
                else
                {
                    Debug.WriteLine("Player Play : Playing Failed becuse Cross Media Manager not supported");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Player Play : ERROR : Player play failed : " + e.Message + "\t" + e.StackTrace);
            }

            return CrossMediaManager.Current.IsPlaying();
        }

        public static async void PlayPause()
        {
            if (!hasInit)
                return;
            Debug.WriteLine("PLAYPAUSE()");
            await CrossMediaManager.Current.PlayPause();
            /*
            try
            {
                if (CrossMediaManager.IsSupported)
                {
                    if (CrossMediaManager.Current.State == MediaManager.Player.MediaPlayerState.Paused || CrossMediaManager.Current.State == MediaManager.Player.MediaPlayerState.Playing)
                    {
                        _ = CrossMediaManager.Current.PlayPause();
                        return true;
                    }
                    else if (CrossMediaManager.Current.State == MediaManager.Player.MediaPlayerState.Stopped && currentItem != null)
                    {
                        Debug.WriteLine(" *************************** Player Stopped will try to paly : " + currentItem.Path + " at " + currentItem.Pos);
                        currentItem.Pos = CrossMediaManager.Current.Position.TotalSeconds;

                        return Play(currentItem).Result;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Preferences.Get("playerPath", string.Empty)))
                        {
                            if (Preferences.Get("playerPath", string.Empty) != "")
                            {
                                Debug.WriteLine(" *************************** No Play Found Will Try To load Preferences " + Preferences.Get("playerPath", string.Empty) + Preferences.Get("playerId", string.Empty) + " : " + Double.Parse(Preferences.Get("playerId", string.Empty)).ToString());
                                string prefPath = Preferences.Get("playerPath", string.Empty);
                                await CrossMediaManager.Current.Stop();
                                await CrossMediaManager.Current.Play(Preferences.Get("playerPath", string.Empty));
                                await CrossMediaManager.Current.Pause();
                                double temp = double.Parse(Preferences.Get("playerPos", string.Empty));
                                await CrossMediaManager.Current.SeekTo(TimeSpan.FromSeconds(temp));
                            }
                        }
                        Debug.WriteLine(" *************************** Player Failed playPause");
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR : Player play pause failed : " + e.Message + "\t" + e.StackTrace);
                return false;
            }
            */
        }

        public static async Task<bool> SeekTo(TimeSpan position, Item backupItem = null)
        {
            if (!hasInit)
                return false;
            Debug.WriteLine("SEEK : Will Try to Seek To " + position);

            if (position.TotalSeconds < 0)
                return false;

            if (currentItem == null)
            {
                Debug.WriteLine("SEEK : current Item is null");
                return false;
            }
            if (position == CrossMediaManager.Current.Position)
            {
                Debug.WriteLine("SEEK : no position needed");
                return true;
            }
            Debug.WriteLine("SEEK : Start Seek To " + position + " from " + CrossMediaManager.Current.Position);
            TimeSpan startSeekToPos = CrossMediaManager.Current.Position;
            try
            {
                if (CrossMediaManager.IsSupported)
                {
                    if (position.TotalSeconds > 1)
                    {
                        DateTime start = DateTime.Now;
                        while (!CrossMediaManager.Current.IsPrepared())
                        {
                            if (start.AddSeconds(10) > DateTime.Now)
                                break;
                        }
                        if (CrossMediaManager.Current.IsPrepared())
                        {
                            await CrossMediaManager.Current.SeekTo(position);
                            Debug.WriteLine("SEEK : Seek Done went to " + position);
                            return true;
                        }
                        else
                        {
                            Debug.WriteLine("SEEK : ERROR : Seek To Pos Failed : Not prepaired : " + CrossMediaManager.Current.State);
                            return false;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("SEEK : ERROR : Seek to Failed Becuse Position is too small : " + position.TotalSeconds);
                        return false;
                    }
                }
                bool test = position == CrossMediaManager.Current.Position;
                Debug.WriteLine("SEEK : Seek completed : " + CrossMediaManager.Current.Position + " should equal " + position + " : " + test);
                return position.TotalSeconds == CrossMediaManager.Current.Position.TotalSeconds;
            }
            catch (Exception e)
            {
                Debug.WriteLine("SEEK : ERROR IN SEEK TO : " + e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        public static Double getLength()
        {
            if (!hasInit)
                return 0;
            Debug.WriteLine("PLAYER : GetLength");
            if (currentItem != null)
            {
                if (CrossMediaManager.Current.IsPrepared())
                {
                    try
                    {
                        TimeSpan length = CrossMediaManager.Current.Duration;

                        return length.TotalSeconds;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("PLAYER : ERROR : Failed to get length : " + e.Message + "\n" + e.StackTrace);
                    }
                }
            }
            return 1;
        }

        public static async Task<bool> Resume()
        {
            if (!hasInit)
                return false;
            Debug.WriteLine("Main Page : Resume");
            /*
            if (CrossMediaManager.Current.IsPlaying())
                return true;
            if (CrossMediaManager.Current.IsPrepared())
                await CrossMediaManager.Current.Play();
            if (!CrossMediaManager.Current.IsPlaying() && !string.IsNullOrEmpty(Preferences.Get("playerPath", String.Empty)))
                CrossMediaManager.Current.Play(Preferences.Get("playerPath", string.Empty));
            */
            return CrossMediaManager.Current.IsPlaying();
            //return PlayPause().Result;
        }

        private static async void PermissionCheck()
        {
            if (!hasInit)
                return;
            Debug.WriteLine("PLAYER : Permission Check");
            var writePermissionStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (writePermissionStatus != PermissionStatus.Granted)
            {
                Debug.WriteLine("   ******************************** Player trying to get file write permission ");
                writePermissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (writePermissionStatus == PermissionStatus.Granted)
                    Debug.WriteLine("PLAYER : Sucess getting write permission");
                else
                    Debug.WriteLine("PLAYER : Failed to get permission");
            }
            else
            {
                Debug.WriteLine("   ******************************** Player has write Permission");
            }
        }
    }
}
