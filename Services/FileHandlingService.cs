using System;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace UTE_Parking_Project.Services;

// Kết quả đăng nhập trả về cho LoginPage
public class LoginResult
{
    public string Username { get; set; } = "";
    public string Role     { get; set; } = ""; // "Student" hoặc "Guard"
    public string FullName { get; set; } = "";
    public string Email    { get; set; } = "";
}

public class FileHandlingService
{
    // ── Đường dẫn file .xls ──────────────────────────────────────────────
    // File đặt trong thư mục Data/ cùng cấp với file .exe
    private static readonly string FilePath = @"C:\Users\LEGION\UTE_Parking_Project\Data\UTE_Parking_Accounts_and_Data.xls";
    

    // ── Xác thực tài khoản ───────────────────────────────────────────────
    // Trả về LoginResult nếu đúng, null nếu sai tài khoản / mật khẩu
    public LoginResult? VerifyAccount(string username, string password)
    {
        try
        {
            // Bắt buộc để ExcelDataReader đọc được tiếng Việt
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!File.Exists(FilePath))
                return null;

            using var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            bool isHeader = true;
            while (reader.Read())
            {
                // Bỏ qua dòng tiêu đề đầu tiên
                if (isHeader) { isHeader = false; continue; }

                string fileUser = reader.GetValue(0)?.ToString()?.Trim() ?? "";
                string filePass = reader.GetValue(1)?.ToString()?.Trim() ?? "";
                string fileRole = reader.GetValue(2)?.ToString()?.Trim() ?? "";
                string fileName = reader.GetValue(3)?.ToString()?.Trim() ?? fileUser;
                string fileMail = reader.GetValue(4)?.ToString()?.Trim() ?? "";

                if (fileUser.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase)
                    && filePass == password.Trim())
                {
                    return new LoginResult
                    {
                        Username = fileUser,
                        Role     = fileRole,
                        FullName = fileName,
                        Email    = fileMail
                    };
                }
            }
        }
        catch (Exception ex)
        {
            // Không crash app — ghi log nếu cần debug
            System.Diagnostics.Debug.WriteLine($"[FileHandlingService] Lỗi: {ex.Message}");
        }

        return null; // Sai tài khoản hoặc mật khẩu
    }
}