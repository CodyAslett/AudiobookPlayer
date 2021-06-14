using AudiobookPlayer_3.Models;
using AudiobookPlayer_3.Views;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;

using MediaManager;
using MediaManager.Playback;

using Xamarin.Forms;
using Xamarin.Essentials;
using System.Threading.Tasks;
using System.Diagnostics;
namespace AudiobookPlayer_3.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        // Commands
        public Command NavigateToLoginCommand { get; }
        public Command PlayButtonCommand { get; }
        public Command PlayerPathSource { get; }
        public Command NavigateToLibraryCommand { get; }
        public Command dragCompletedCommand { get; private set; }
        public Command LoadItemsCommand { get; }


        public Command ListenTestCommand { get; }
        public Command SendTestCommand { get; }

        public string name = string.Empty;


        public AboutViewModel()
        {
            PlayButtonCommand = new Command(PlayButton);
            NavigateToLoginCommand = new Command(NavigateToLogin);
            NavigateToLibraryCommand = new Command(NavigateToLibrary);
        }
        private async void NavigateToLogin()
        {
            try
            {
                //await Application.Current.MainPage.DisplayAlert("Placeholder", "just here for show", "OK");
                await Shell.Current.Navigation.PushAsync(new LoginPage(), true);
            }
            catch (Exception e)
            {
                await Application.Current.MainPage.DisplayAlert("Error: Navigating to Login", e.Message + "\n" + e.ToString() + "\n" + e.StackTrace, "OK");
            }
        }

        private async void NavigateToLibrary()
        {
            try
            {
                await Shell.Current.Navigation.PushAsync(new ItemsPage(), true);
            }
            catch (Exception e)
            {
                await Application.Current.MainPage.DisplayAlert("Error: Navigating to Library", e.ToString(), "OK");
            }
        }

        private void PlayButton()
        {
            try
            {
                Debug.WriteLine("Play Button starting : " + Preferences.Get("playerPath", String.Empty) + " : " + Preferences.Get("playerId", String.Empty));
                Item currentItem = Player.CurrentItem;
                if (currentItem != null)
                {
                    Player.PlayPause();
                }
                else
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("!!!!!! ERROR : Play Button Failed " + e.Message + "\n" + e.StackTrace);
            }
        }


        public void OnSliderChange()
        {
            try
            {
                Debug.WriteLine("Slider Changed to " + SliderPos);
                if (SliderPos < 1)
                    SliderPos = 1;
                Pos = SliderPos;
            }
            catch
            {
                Debug.WriteLine("Failed to Update Slider Info");
            }
        }

    }
}