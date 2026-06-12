using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;

namespace UTE_Parking_Project
{
    public class ParkingRecord
    {
        public string Zone    { get; set; }
        public string Slot    { get; set; }   // Text hiển thị, vd: "A1"
        public string SlotId  { get; set; }   // ID nội bộ,      vd: "A-1-1"
        public string Vehicle { get; set; }
        public string BienSo  { get; set; }
        public string Mssv    { get; set; }   // MSSV sinh viên sở hữu vé
        public DateTime InTime  { get; set; }
        public DateTime OutTime { get; set; }
        public int Fee { get; set; }
    }

    // ỐNG DẪN TIN NHẮN
    public class SyncMessage : ValueChangedMessage<string>
    {
        public SyncMessage(string val) : base(val) { }
    }

    // KHO DỮ LIỆU DÙNG CHUNG CỦA TOÀN APP
    public static class SharedState
    {
        public static HashSet<string> TakenSlots = new();
        public static Dictionary<string, bool> LockedZones = new() 
        { 
            { "A", false }, { "B", false }, { "C", false }, { "D", true } 
        };
        public static List<ParkingRecord> History = new();

        // XE ĐANG GỬI HIỆN TẠI (chưa lấy ra) — Guard tìm kiếm sẽ thấy
        // Key = BienSo (uppercase), Value = bản ghi xe đang gửi
        public static Dictionary<string, ParkingRecord> ActiveParking = new();
        
        // Giá vé dùng chung toàn hệ thống
        public static int FeeXeDap = 3000;
        public static int FeeXeMay = 5000;
        public static int FeePhat = 10000;

        // Biến mô phỏng doanh thu tự nhảy
        public static int DailyRevenue = 3450000;
        public static int DailyCount = 690;

        private static bool _isInit = false;

        public static void InitFakeData()
        {
            if (_isInit) return;
            for (int c = 1; c <= 10; c++) TakenSlots.Add($"A-1-{c}");
            for (int c = 1; c <= 10; c++) TakenSlots.Add($"A-2-{c}");
            for (int r = 1; r <= 4; r++)
                for (int c = 1; c <= 10; c++) TakenSlots.Add($"B-{r}-{c}");
            for (int c = 1; c <= 6; c++) TakenSlots.Add($"B-5-{c}");
            for (int r = 1; r <= 5; r++)
                for (int c = 1; c <= 10; c++) TakenSlots.Add($"C-{r}-{c}");
            for (int c = 1; c <= 8; c++) TakenSlots.Add($"C-6-{c}");
            for (int r = 1; r <= 3; r++)
                for (int c = 1; c <= 10; c++) TakenSlots.Add($"D-{r}-{c}");
            for (int c = 1; c <= 7; c++) TakenSlots.Add($"D-4-{c}");
            _isInit = true;
        }

        // HÀM KIỂM TRA GIỜ (6:00 đến 23:00)
        public static bool IsParkingOpen()
        {
            int h = DateTime.Now.Hour;
            return h >= 6 && h < 23;
        }
    }
}