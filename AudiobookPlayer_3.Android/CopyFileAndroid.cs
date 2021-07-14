using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AudiobookPlayer_3.Droid;
using AudiobookPlayer_3.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Xamarin;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(CopyFile))]
namespace AudiobookPlayer_3.Droid
{
    public class CopyFile : CopyFileInterface
    {
        public bool Copy(string sourcePath)
        {
            try
            {
                string destination = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(sourcePath));
                System.Diagnostics.Debug.WriteLine($"COPY : ANDROID : Copying from {sourcePath} to {destination}");
                File.Copy(sourcePath, destination, true);
                if (File.Exists(destination))
                {
                    System.Diagnostics.Debug.WriteLine($"COPY : ANDROID : Copying should have worked");
                    return true;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"COPY : ANDROID : ERROR : {e.Message}\nSTACK TRACE : {e.StackTrace}");
                return false;
            }
            System.Diagnostics.Debug.WriteLine($"COPY : ANDROID : Copying failed");
            return false;
        }

        public bool Copy(FileResult pickerResult)
        {
            return Copy(pickerResult.FullPath);
        }
    }
}