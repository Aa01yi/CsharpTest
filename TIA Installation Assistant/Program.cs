using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;

namespace TIA_Installation_Assistant
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string productName = GetWindowsProductName();
            bool isPro = !string.IsNullOrEmpty(productName) && productName.IndexOf("Pro", StringComparison.OrdinalIgnoreCase) >= 0;
            bool net35 = IsNet35Installed();
            var pending = GetPendingFileRenameOperations();

            Console.WriteLine($"Windows ProductName: {productName}");
            Console.WriteLine(isPro ? "系统：Windows 专业版" : "系统：不是 Windows 专业版");
            Console.WriteLine(net35 ? ".NET 3.5 已安装" : ".NET 3.5 未安装");
            Console.WriteLine(pending.Length == 0 ? "PendingFileRenameOperations：无" : $"PendingFileRenameOperations：存在 {pending.Length} 项");

            Console.WriteLine();
            Console.WriteLine("按 Y 开始持续监视 PendingFileRenameOperations 并在发现时删除；按 ESC 退出。");
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) return;
                if (key.Key == ConsoleKey.Y)
                {
                    Console.WriteLine("开始持续监视（按 ESC 停止）...");
                    while (true)
                    {
                        var p = GetPendingFileRenameOperations();
                        if (p.Length == 0)
                        {
                            Console.WriteLine($"[{DateTime.Now}] 未发现 PendingFileRenameOperations。");
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now}] 发现 PendingFileRenameOperations 共 {p.Length} 项，尝试删除：");
                            foreach (var s in p.Take(50)) Console.WriteLine("  " + s);
                            if (p.Length > 50) Console.WriteLine("  ...");

                            bool deleted = DeletePendingFileRenameOperations();
                            Console.WriteLine(deleted ? "已删除 PendingFileRenameOperations 键值。" : "尝试删除失败（可能权限不足或注册表访问错误）。");
                        }

                        // 等待 60 秒，同时监听 ESC 键
                        bool exit = false;
                        int iterations = 600; // 600 * 100ms = 60000ms
                        for (int i = 0; i < iterations; i++)
                        {
                            Thread.Sleep(100);
                            if (Console.KeyAvailable)
                            {
                                var k2 = Console.ReadKey(true);
                                if (k2.Key == ConsoleKey.Escape)
                                {
                                    exit = true;
                                    break;
                                }
                            }
                        }
                        if (exit)
                        {
                            Console.WriteLine("检测停止，程序退出。按任意键关闭...");
                            Console.ReadKey(true);
                            return;
                        }
                    }
                }
            }
        }

        static string GetWindowsProductName()
        {
            try
            {
                string sub = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion";
                string name = ReadRegistryValue(RegistryHive.LocalMachine, sub, "ProductName");
                return name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        static bool IsNet35Installed()
        {
            try
            {
                string sub = "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v3.5";
                string val = ReadRegistryValue(RegistryHive.LocalMachine, sub, "Install");
                if (int.TryParse(val, out int v)) return v == 1;
                return false;
            }
            catch
            {
                return false;
            }
        }

        static string[] GetPendingFileRenameOperations()
        {
            try
            {
                using (var key = OpenLocalMachineKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager"))
                {
                    if (key == null) return new string[0];
                    object val = key.GetValue("PendingFileRenameOperations");
                    if (val == null) return new string[0];
                    if (val is string[] sa) return sa.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    if (val is object[] oa) return oa.Select(o => o?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    return new string[0];
                }
            }
            catch
            {
                return new string[0];
            }
        }

        static RegistryKey OpenLocalMachineKey(string subKey)
        {
            try
            {
                var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(subKey);
                if (k != null) return k;
            }
            catch { }
            try
            {
                var k2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subKey);
                return k2;
            }
            catch { }
            return null;
        }

        static bool DeletePendingFileRenameOperations()
        {
            string subKey = "SYSTEM\\CurrentControlSet\\Control\\Session Manager";
            bool deletedAny = false;
            try
            {
                // Only delete the single value named "PendingFileRenameOperations" under the exact subKey path.
                // Do not enumerate or delete any other values or keys elsewhere.
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                        using (var key = baseKey.OpenSubKey(subKey, true))
                        {
                            if (key == null) continue;
                            var names = key.GetValueNames();
                            if (names != null && Array.IndexOf(names, "PendingFileRenameOperations") >= 0)
                            {
                                // Delete only this named value
                                key.DeleteValue("PendingFileRenameOperations", false);
                                deletedAny = true;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return deletedAny;
        }

        static string ReadRegistryValue(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using (var key = baseKey.OpenSubKey(subKey))
                {
                    if (key != null)
                    {
                        var v = key.GetValue(valueName);
                        return v?.ToString();
                    }
                }
            }
            catch { }
            try
            {
                var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32);
                using (var key = baseKey.OpenSubKey(subKey))
                {
                    if (key != null)
                    {
                        var v = key.GetValue(valueName);
                        return v?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}