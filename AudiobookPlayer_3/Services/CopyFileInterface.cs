using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Essentials;

namespace AudiobookPlayer_3.Services
{
    public interface CopyFileInterface
    {
        bool Copy(string SourcePath);

        bool Copy(FileResult pickerResult);
    }
}
