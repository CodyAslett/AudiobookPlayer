using AudiobookPlayer_3.Views;

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Essentials;
using Xamarin.Forms;

namespace AudiobookPlayer_3.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public Command LoginCommand { get; }

        static HttpClient client;
        static string BaseUrl = "http://codyaslett.com:3000";
        string usernameEntry = string.Empty;
        public string UsernameEntry
        {
            get { return usernameEntry; }
            set { SetProperty(ref usernameEntry, value); }
        }

        string passwordEntry = string.Empty;
        public string PasswordEntry
        {
            get { return passwordEntry; }
            set { SetProperty(ref passwordEntry, value); }
        }

        public LoginViewModel()
        {
            LoginCommand = new Command(OnLoginClicked);
        }

        private async void OnLoginClicked(object obj)
        {
            try
            {
                // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
                //await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                string url = BaseUrl + "/login?username=" + usernameEntry + "&password=" + passwordEntry;
                client = new HttpClient();
                Uri uri = new Uri(string.Format(url));
                HttpResponseMessage response = await client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (content.Substring(0, 5) != "ERROR" && content.Substring(0, 6) != "DENIED")
                    {
                        Preferences.Set("loginUsername", usernameEntry);
                        Preferences.Set("loginToken", content.Replace("ACCEPTED:", ""));
                        //await Application.Current.MainPage.DisplayAlert("Login success." + content.Substring(0, 6) + " Token : ", content, "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("LOGIN FAILED ", content, "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("error with request ", url, "OK");
                }

                //string serverRespons = await client.GetStringAsync(" ");
                //await Application.Current.MainPage.DisplayAlert("get request returned ", serverRespons, "OK");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception e)
            {
                await Application.Current.MainPage.DisplayAlert("ERROR Login in ", e.StackTrace, "OK");
            }
        }
    }
}
