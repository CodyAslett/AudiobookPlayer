using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudiobookPlayer_3.Services
{
    public interface IDataStore<T>
    {
        Task<bool> AddItemAsync(T item);
        Task<bool> UpdateItemAsync(T item);
        Task<bool> DeleteItemAsync(int id);
        Task<T> GetItemAsync(string path);
        Task<T> GetItemAsync(int id);
        Task<T> GetServerItemAsync(int id);
        Task<IEnumerable<T>> GetItemsAsync(bool forceRefresh = false);
        Task<IEnumerable<T>> GetServerItemsAsync(bool forceRefresh = false);
    }
}
