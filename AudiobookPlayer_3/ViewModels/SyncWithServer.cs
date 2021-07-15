using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Xamarin.Essentials;
using Xamarin.Forms;

namespace AudiobookPlayer_3.ViewModels
{
    public class SyncWithServer
    {
        private string userName = string.Empty;
        private string token = string.Empty;
        private Dictionary<long, string> filesList;

        private const string ServerAddress = "http://codyaslett.com:3000/";

        public string Username
        {
            get => userName;
            set
            {
                if (value == userName)
                {
                    return;
                }
                userName = value;
            }
        }
        public string Token
        {
            get => token;
            set
            {
                if (value == token)
                {
                    return;
                }
                token = value;
            }
        }

        public SyncWithServer()
        {
            userName = Preferences.Get("loginUsername", string.Empty);
            token = Preferences.Get("serverToken", string.Empty);

            filesList = new Dictionary<long, string>();
        }

        public async Task<bool> Login(string username, string password)
        {
            try
            {
                string loginRequest = ServerAddress + "login?username=" + username + "&password=" + password;
                HttpClient httpClient = new HttpClient();
                Debug.WriteLine($"Server : about to request : {loginRequest}");
                string serverResponse = await httpClient.GetStringAsync(loginRequest);
                Debug.WriteLine($"Server : server response : {serverResponse}");
                if (serverResponse.Substring(0, 8) == "ACCEPTED")
                {
                    Debug.WriteLine($"Server : Login succeeded");
                    userName = username;
                    token = serverResponse.Split(':')[1].Remove(0, 1);

                    Preferences.Set("loginUsername", username);
                    Preferences.Set("serverToken", token);

                    Debug.WriteLine($"Server : From Server Recived token : {token}, username : {userName}");
                    return await Task.FromResult(true);
                }
                else
                {
                    Debug.WriteLine($"Server : Login failed");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Server : Login ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
            }
            return await Task.FromResult(false);
        }

        public Dictionary<long, string> GetFileList()
        {
            try
            {
                string serverResponse = GetRequestToServer(ServerAddress + "getfiles?username=" + userName + "&token=" + token);
                Debug.WriteLine($"Server : Get File list request returned : {serverResponse}");

                if (!string.IsNullOrEmpty(serverResponse))
                {
                    if (serverResponse.Substring(0, 8) == "ACCEPTED")
                    {
                        string getFilesJson = serverResponse.Remove(0, 11);
                        Debug.WriteLine($"Server : File List Json : '{getFilesJson}'");
                        serverGetFilesJsonResponse resultGetFiles = JsonConvert.DeserializeObject<serverGetFilesJsonResponse>(getFilesJson);
                        Debug.WriteLine($"Server : Found {resultGetFiles.FileCount} files");
                        
                        foreach (ServerTorrentFile file in resultGetFiles.Files)
                        {
                            Debug.WriteLine($"Server : Adding {file.Name} to availible file list");
                            filesList.Add(file.Id, file.Name);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Server : Get Files Failed got : {serverResponse.Substring(0, 8)}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Server : Error getting file list : {e.Message}\nSTACK TRACE : {e.StackTrace}");
            }
            return filesList;
        }

        public bool DownloadFile(long id)
        {
            try
            {
                if (filesList.Count == 0)
                {
                    _ = GetFileList();
                }

                string filename = string.Empty;
                if (filesList.TryGetValue(id, out filename))
                {
                    string getFileRequest = ServerAddress + "getfile?username=" + userName + "&token=" + token + "&id=" + id;
                    string torrentFilename = Path.Combine(FileSystem.AppDataDirectory, filename + ".torrent");
                    Debug.WriteLine($"Server : Download File : Requesting : '{getFileRequest}' and putting it in '{torrentFilename}'");

                    //string serverResponse = GetRequestToServer(getFileRequest);
                    //Debug.WriteLine($"Server : Download File : server Returned : {serverResponse}");
                    //File.WriteAllText(torrentFilename, serverResponse);

                    GetFileFromServer(getFileRequest, torrentFilename);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Sync With Server : Download File : Error : {e.Message}\nSTACK TRACE : {e.StackTrace}");
            }
            return false;
        }

        public void UploadFile(string filePath)
        {
            if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(token))
            {
                string postFileRequest = ServerAddress + "addfile?username=" + userName + "&token=" + token;
                Debug.WriteLine($"Server : Upload File : requesting : {postFileRequest} to upload'{filePath}");
                PostFileToServer(postFileRequest, filePath);
            }
        }

        private string GetRequestToServer(string serverRequest)
        {
            if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(token))
            {
                Debug.WriteLine($"Server : Requesting from server : {serverRequest}");
                HttpClient httpClient = new HttpClient();
                string serverResponse = httpClient.GetStringAsync(serverRequest).Result;
                Debug.WriteLine($"Server : Server Responded with : {serverResponse}");
                return serverResponse;
            }
            Debug.WriteLine($"Server : Request Failed becuse no username token combo found username:{userName}, token:{token}");
            return string.Empty;
        }

        private bool GetFileFromServer(string request, string name)
        {
            try
            {
                using (HttpClient webClient = new HttpClient())
                {
                    Debug.WriteLine($"Server : Get File From Server : requesting : '{request}' and saving to '{name}'");

                    FileStream fileStream = File.Create(name);
                    webClient.GetStreamAsync(request).Result.CopyTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
                if (File.Exists(FileSystem.AppDataDirectory + name))
                {
                    return true;
                }
                Debug.WriteLine($"Server : Get File From Server : Error : file not found");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Server : Get File From Server : Error : {e.Message}\nSTACK TRACE : {e.StackTrace}");
            }
            return false;
        }

        private bool PostFileToServer(string request, string filePath)
        {
            Debug.WriteLine($"Server : Post File to server requesting {request} and uploading file '{filePath}'");
            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(new StreamContent(fileStream), "torrent", filePath);
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = httpClient.PostAsync(request, content).Result;
                Debug.WriteLine($"Server : Post File To Server response : '{response.Content} with code {response.StatusCode}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Server : Post File : Error : {e.Message}\nSTACK TRACE {e.StackTrace}");
            }

            return false;
        }
    }
        
    public class serverGetFilesJsonResponse
    {
        [JsonProperty("fileCount")]
        public long FileCount { get; set; }
        [JsonProperty("files")]
        public List<ServerTorrentFile> Files { get; set; }
    }

    public class ServerTorrentFile
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
