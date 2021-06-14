using System;
using SQLite;

namespace AudiobookPlayer_3.Models
{
    public class Item
    {
        [PrimaryKey, AutoIncrement]
        public int PrimaryId { get; set; }
        public string Id { get; set; }
        public string ServerID { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public string Size { get; set; }
        public string Description { get; set; }
        public string UserName { get; set; }
        public string DiviceID { get; set; }
        public string MagnentUrl { get; set; }
        public string Hash { get; set; }
        public string BookName { get; set; }
        public string SeriesName { get; set; }
        public string Auther { get; set; }
        public string Reader { get; set; }
        public string Length { get; set; }
        public bool OnDivice { get; set; }
        public double Pos { get; set; }
    }
}