﻿using AudiobookPlayer_3.Models;
using AudiobookPlayer_3.Services;
using MediaManager;
using MediaManager.Media;
using MediaManager.Playback;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AudiobookPlayer_3.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public IDataStore<Item> DataStore => DependencyService.Get<IDataStore<Item>>();


        public BaseViewModel()
        {
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        // Local Variables
        // Variables
        private double pos = 1;
        private double playerDuration = 1;
        private string playButtonText = "Play >";
        private string displayPos = string.Empty;
        private int secDelayOnPosUpdate = 3;


        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                SetProperty(ref isBusy, value);
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(BookTittle));
            }
        }

        string username = "";
        public String Username
        {
            get
            {
                username = Preferences.Get("loginUsername", "");
                return username;
            }
            set
            {
                username = value;
                Preferences.Set("loginUsername", value);
            }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        string bookTittle = "No Book Found";
        public string BookTittle
        {
            get
            {
                Debug.WriteLine("BOOK TITTLE : getting tittle");
                try
                {
                    Item currentItem = Player.CurrentItem;
                    if (currentItem != null)
                    {
                        /*
                        if (!string.IsNullOrEmpty(currentItem.BookName))
                            return currentItem.BookName;
                        */
                        if (!string.IsNullOrEmpty(currentItem.FileName))
                            return currentItem.FileName;
                        if (!string.IsNullOrEmpty(currentItem.Path))
                            return currentItem.Path;
                    }
                }
                catch
                {
                    return "Error Loading book";
                }
                Debug.WriteLine("BOOK TITTLE : returning tittle : " + bookTittle);
                return bookTittle;
            }
            set
            {
                IsBusy = true;

                Debug.WriteLine("BOOK TITTLE : Setting " + BookTittle + " to " + value);
                Preferences.Set("playerTitle", value);
                bookTittle = value;

                IsBusy = false;
                OnPropertyChanged(nameof(BookTittle));
            }
        }

        public string PlayButtonText
        {
            get => playButtonText;
            set
            {
                if (playButtonText == value)
                    return;
                playButtonText = value;
                OnPropertyChanged(nameof(PlayButtonText));
            }
        }

        public double Pos
        {
            get
            {
                //pos = Convert.ToDouble(Preferences.Get("playerPos", pos));
                if (Player.Pos > 0)
                    return Player.Pos;
                return -1;
            }
            set
            {
                try
                {
                    Debug.WriteLine("POS: Setting Pos to " + value);
                    if (pos == value)
                        return;
                    pos = value;
                    Debug.WriteLine("POS : pos set to " + pos);

                    if (sliderPos != value)
                        sliderPos = value;
                    Debug.WriteLine("POS : slider pos set to " + sliderPos);
                    OnPropertyChanged(nameof(Pos));
                    if (Player.CurrentItem == null)
                        return;
                    if (value > 1 && Player.Pos != value)
                    {
                        Debug.WriteLine("POS : seekting to " + value);
                        Player.SeekTo(TimeSpan.FromSeconds(value));
                    }
                    Debug.WriteLine("Pos SET to : " + value);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ERROR : Failed to set Pos " + e.Message + "\n\n" + e.StackTrace);
                }
            }
        }

        public double PlayerDuration
        {
            get => playerDuration;
            set
            {
                if (PlayerDuration == value)
                    return;
                playerDuration = value;

                OnPropertyChanged(nameof(PlayerDuration));
            }
        }

        private double sliderPos;
        public double SliderPos
        {
            get
            {
                return sliderPos;
            }
            set
            {
                sliderPos = value;
                if (sliderPos > 1 && playerDuration > 2)
                    progress = PlayerDuration / sliderPos;
                OnPropertyChanged(nameof(DisplayPos));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(SliderPos));
            }
        }

        public string DisplayPos
        {
            get
            {
                Debug.WriteLine("Getting dispaly Pos");
                TimeSpan time = TimeSpan.FromSeconds(SliderPos);
                displayPos = time.ToString(@"hh\:mm\:ss");

                return displayPos;
            }
        }

        double progress = 0;
        public double Progress
        {
            get => progress;
        }

        public double TrackLength
        {
            get => Player.Length;
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}