using AudiobookPlayer_3.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Essentials;

using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.CommunityToolkit.ObjectModel;

namespace AudiobookPlayer_3.Services
{
    public class MockDataStore : ObservableObject, IDataStore<Item>
    {
        private List<Item> items;
        private static SQLiteAsyncConnection dbAsync;

        public List<Item> Items
        {
            get => items;
        }

        public MockDataStore()
        {
            Task tast = Task.WhenAny(Init());
            List<Task<bool>> dataTasks = Enumerable.Range(0, 1).Select(item => Task.Run(() => Init())).ToList();
            // Get an absolute path to the database file
            items = new List<Item>();
        }
        public async Task<bool> Init()
        {
            Debug.WriteLine("Starting Data Base Init");
            try
            {
                string databasePath = Path.Combine(FileSystem.AppDataDirectory, "MyData.db");
                if (dbAsync != null && File.Exists(databasePath))
                {
                    Debug.WriteLine("DB Init not needed");
                    return await Task.FromResult(false);
                }
                Debug.WriteLine("Starting DB Init");
                
                Debug.WriteLine("Database Path : " + databasePath);
                var writePermissionStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (writePermissionStatus == PermissionStatus.Granted)
                {
                    Debug.WriteLine("   ******************************** Data Store Init : Have File Write Permission ");

                }
                else
                {
                    writePermissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }
                Debug.WriteLine("Trying to connect to DB");

                dbAsync = new SQLiteAsyncConnection(databasePath);
                if (dbAsync != null)
                    Debug.WriteLine("Connected to database");
                else 
                {
                    Debug.WriteLine("ERROR : Failed to connect to DB");
                } 
                    
                await dbAsync.CreateTableAsync<Item>();
                Debug.WriteLine("Table Created for Items");
                foreach (string file in System.IO.Directory.GetFiles(FileSystem.AppDataDirectory))
                {
                    Debug.WriteLine("   ********************************  File in " + FileSystem.AppDataDirectory + " : " + file);
                }

                FillListItemFromDB();
                Debug.WriteLine("Returning from Data Store Init");
                return await Task.FromResult(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine("   ******************************** ERROR : Failed To Initiate DB : \nStackTrace : " + e.StackTrace + "\nMessage : " + e.Message + " \nSource: " + e.Source + "\n " + e.Source);
                return await Task.FromResult(false);
            }
        }

        public async Task<bool> AddItemAsync(Item item)
        {
            Debug.WriteLine("AdditemAsync Start " + item.FileName);
            try
            {
                foreach (Item i in items)
                {
                    if (i.FileName.Equals(item.FileName))
                    {
                        Debug.WriteLine("   ******************************** Found Duplicate : Will stop adding item ");
                        return await Task.FromResult(false);
                    }
                }
                await Init();
                int itemResult = await dbAsync.InsertAsync(item);

                items.Add(item);
                Debug.WriteLine("   ******************************** DB Add Item : " + item.FileName);
                return await Task.FromResult(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine("   ******************************** ERROR: In DB Add Item : " + e.Message + " : " + e.StackTrace);
                return await Task.FromResult(false);
            }
        }

        public async Task<bool> UpdateItemAsync(Item item)
        {
            Debug.WriteLine("@@@@@@@@@@@@@@@@@@  Update itemAsync Start " + item.FileName + " at " + item.Pos);
            await Init();
            await dbAsync.UpdateAsync(item);

            var oldItem = items.Where((Item arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> updatePos(string path, double pos)
        {
            Debug.WriteLine("Update pos Async Start " + path + " to " + pos);
            await Init();
            try
            {
                Item oldItem = items.Where((Item arg) => arg.Path == path).FirstOrDefault();
                oldItem.Pos = pos;
                await UpdateItemAsync(oldItem);
            }
            catch (Exception)
            {
                return await Task.FromResult(false);
            }
            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            Debug.WriteLine("delete itemAsync Start " + id);
            await Init();

            Item oldItem = items.Where((Item arg) => arg.PrimaryId == id).FirstOrDefault();

            await dbAsync.DeleteAsync<Item>(oldItem.PrimaryId);

            items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<Item> GetItemAsync(int id)
        {
            Debug.WriteLine("Get item Async Start ID " + id);
            await Init();
            items.Clear();
            items = dbAsync.Table<Item>().ToListAsync().Result;
            Item foundItem = await dbAsync.Table<Item>().Where(s => s.PrimaryId.Equals(id)).FirstAsync();
            Debug.WriteLine("################# Get Item Async found : " + foundItem.FileName + " at " + foundItem.Pos);

            return foundItem;
            //return Task.FromResult(items.FirstOrDefault(s => s.PrimaryId == id)).Result;
            //return query;
        }

        public async Task<Item> GetItemAsync(string path)
        {
            Debug.WriteLine("##################  Get item Async Path Start :" + path);
            await Init();
            items.Clear();
            items = await dbAsync.Table<Item>().ToListAsync();
            Debug.WriteLine("Get Item Async Init done");
            Item foundItem = await dbAsync.Table<Item>().Where(s => s.Path.Equals(path)).FirstAsync();
            Debug.WriteLine("################# Get Item Async Got " + foundItem.FileName + foundItem.Pos);
            return foundItem;
            //return query;
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(bool forceRefresh = false)
        {
            Debug.WriteLine("GET items Async Start ");
            await Init();
            items.Clear();
            items = await dbAsync.Table<Item>().ToListAsync();
            //return await db.Table<Item>().ToListAsync();
            return await Task.FromResult(items);
            //return await Task.FromResult(items);
        }

        public async void FillListItemFromDB()
        {
            try
            {
                await Init();
                int itemsCount = await dbAsync.Table<Item>().CountAsync();


                List<Item> temp = await dbAsync.Table<Item>().ToListAsync();

                if (temp != null)
                {
                    if (temp.Count() > 0)
                    {
                        foreach (Item row in temp)
                        {
                            if (row != null)
                            {
                                items.Add(row);
                            }
                            else
                            {
                                Debug.WriteLine(" *************** ERROR in fill list : Found NULL row ");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("************* Failed to convert task list to list result was empty");
                    }
                }
                else
                {
                    Debug.WriteLine("************* Failed to convert task list to list result was null");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("******** ERROR : Failed to fill items from DB : " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}