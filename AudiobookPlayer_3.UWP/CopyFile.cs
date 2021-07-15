using AudiobookPlayer_3.Services;
using AudiobookPlayer_3.UWP;

using PCLStorage;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin;
using Xamarin.Forms;
using Xamarin.Essentials;

using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Pickers;

[assembly: Xamarin.Forms.Dependency(typeof(CopyFile))]
namespace AudiobookPlayer_3.UWP
{
    public class CopyFile : CopyFileInterface
    {
        public bool Copy(string sourcePath)
        {
            try
            {
                string destination = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, Path.GetFileName(sourcePath));
                System.Diagnostics.Debug.WriteLine($"COPY : UWP : trying default Copying from {sourcePath} to {destination}");
                File.Copy(sourcePath, destination, true);
                if (File.Exists(destination))
                {
                    Debug.WriteLine($"COPY : UWP : Copying using the default method should have worked");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"COPY : UWP : ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
            }

            

            try
            {
                string destination = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, Path.GetFileName(sourcePath));
                Debug.WriteLine($"COPY : UWP : coping {sourcePath} to {destination}");

                // StorageFile source = StorageFile.GetFileFromPathAsync(sourcePath).GetResults().CopyAsync(StorageFolder.GetFolderFromPathAsync(FileSystem.AppDataDirectory).GetResults()).GetResults();

                bool copyAsyncResult = copyAsync(sourcePath).Result;

                if (File.Exists(destination))
                {
                    Debug.WriteLine($"COPY : UWP : copy should have worked");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"COPY : UWP : ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                return false;
            }
            Debug.WriteLine($"COPY : UWP : copy Failed");
            return false;
        }

        private async Task<bool> copyAsync(string sourcePath)
        {
            Debug.WriteLine($"COPY : Copy Async starting copy from : {sourcePath}");
            if (File.Exists(sourcePath))
            {

                Debug.WriteLine($"COPY : Copy Async Found Source File");
                try
                {
                    string name = Path.GetFileName(sourcePath);

                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    Debug.WriteLine($"COPY : Copy Async - Local Folder : {localFolder.Path}");
                    Uri uri = new Uri(sourcePath);
                    Debug.WriteLine($"COPY : Copy Async - URI : {uri.AbsolutePath}");
                    StorageFile file = StorageFile.GetFileFromApplicationUriAsync(uri).GetResults();

                    Debug.WriteLine($"COPY : Copy Async - file : {file.Name}");
                    StorageFile copyResult = await file.CopyAsync(localFolder, name, Windows.Storage.NameCollisionOption.ReplaceExisting);
                    Debug.WriteLine($"COPY : Copy Async - Result : {copyResult.Path}");
                    return await Task.FromResult(true);
                }
                catch
                {
                    return await Task.FromResult(false);
                }
            }
            else
            {
                Debug.WriteLine($"COPY : Copy Async Failed to find source file : {sourcePath}");
                return await Task.FromResult(false);
            }
            
        }


        public bool Copy(FileResult pickerResult)
        {
            string destination = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, Path.GetFileName(pickerResult.FileName));
            Debug.WriteLine($"COPY : coping from {pickerResult.FullPath} to {destination}");
            try
            {
                Stream stream = pickerResult.OpenReadAsync().Result;
                long length = stream.Length;
                string name = pickerResult.FileName;
                string path = pickerResult.FullPath;
                Debug.WriteLine($"COPY : extrated basc info");

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Debug.WriteLine($"COPY : Storage folder {storageFolder.Path}");
                FileStream test = File.OpenWrite(destination);
                Debug.WriteLine($"COPY : Test File Stream created at size {test.Length} bytes");
                stream.CopyTo(test);
                Debug.WriteLine($"COPY : Test File STream size is {test.Length} bytes");
                test.Flush();
                test.Close();
                Debug.WriteLine($"COPY : file is {new FileInfo(destination).Length} bytes");

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"COPY : UWP : ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                Stream stream = pickerResult.OpenReadAsync().Result;

                if (new FileInfo(destination).Length == stream.Length)
                {
                    Debug.WriteLine($"COPY : returning true with size {new FileInfo(destination).Length} : {stream.Length}");
                    return true;
                }
                Debug.WriteLine($"COPY : returning false");
                return false;
            }
        }

        public async Task<StorageFile> CreateFile(StorageFolder storageFolder, string name)
        {
            StorageFile newFile = await storageFolder.CreateFileAsync(name);
            Debug.WriteLine($"COPY : new File {newFile.Path}");
            return await Task.FromResult(newFile);
        }
        public async Task<StorageFile> GetFile(string path)
        {
            StorageFile oldFile = await StorageFile.GetFileFromPathAsync(path);
            return await Task.FromResult(oldFile);
        }


        private async Task<bool> PickerResultCopyAsync(string source, string destination)
        {
            try
            {
                Windows.Storage.Streams.IBuffer buffer = await PathIO.ReadBufferAsync(source);
                Debug.WriteLine($"COPY : buffer : {buffer.Length}");

                await PathIO.WriteBufferAsync(destination, buffer);
                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }
    }
}
