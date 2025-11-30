using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace ShopeeServer
{

    class Program
    {
        private static readonly Regex LocationRegex = new Regex(@"^\[(?<Shelf>\d+)N(?<Level>\d+)(?:-(?<Box>\d+))?\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static Dictionary<string, string> GetItemLocation(string input)
        {
            var resultData = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(input)) return resultData;

            var match = LocationRegex.Match(input);
            if (match.Success)
            {
                resultData["Shelf"] = match.Groups["Shelf"].Value;
                resultData["Level"] = match.Groups["Level"].Value;
                if (match.Groups["Box"].Success) resultData["Box"] = match.Groups["Box"].Value;
            }

            return resultData;
        }

        private static List<Order> _dbOrders = new List<Order>();
        private static object _lock = new object();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== SHOPEE WMS SERVER ===");

            // 1. CHECK AUTH
            if (string.IsNullOrEmpty(ShopeeApiHelper.AccessToken))
            {
                Console.WriteLine("\n[WARN] Chưa có Token. Vui lòng Login:");
                Console.WriteLine(ShopeeApiHelper.GetAuthUrl());
                Console.Write("Callback URL: ");
                string cb = Console.ReadLine()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(cb))
                {
                    try
                    {
                        var q = QueryHelpers.ParseQuery(new Uri(cb).Query);
                        long sid = long.Parse(q["shop_id"]!);
                        if (await ShopeeApiHelper.ExchangeCodeForToken(sid, q["code"]!))
                            Console.WriteLine("[OK] Đã lưu Token.");
                        else return;
                    }
                    catch { Console.WriteLine("Lỗi Login."); return; }
                }
                else return;
            }

            // 2. START SYNC
            _ = Task.Run(async () => {
                while (true)
                {
                    try { await CoreEngineSync(); } catch (Exception ex) { Console.WriteLine($"[SyncErr] {ex.Message}"); }
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            });

            // 3. START SERVER
            StartServer();
        }

        static async Task CoreEngineSync()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm}] Syncing...");
            long to = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), from = DateTimeOffset.UtcNow.AddDays(-15).ToUnixTimeSeconds();

            string json = await ShopeeApiHelper.GetOrderList(from, to);
            if (!json.Contains("\"response\""))
            {
                if (await ShopeeApiHelper.RefreshTokenNow()) json = await ShopeeApiHelper.GetOrderList(from, to);
                else return;
            }

            List<string> liveIds = new List<string>();
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.TryGetProperty("response", out var r) && r.TryGetProperty("order_list", out var l))
                    foreach (var i in l.EnumerateArray()) liveIds.Add(i.GetProperty("order_sn").GetString()!);
            }

            lock (_lock) { _dbOrders.RemoveAll(o => o.Status == 0 && !liveIds.Contains(o.OrderId)); }

            List<string> newIds;
            lock (_lock) { newIds = liveIds.Where(id => !_dbOrders.Any(o => o.OrderId == id)).ToList(); }

            if (newIds.Count > 0)
            {
                Console.WriteLine($"Phát hiện {newIds.Count} đơn mới.");
                for (int i = 0; i < newIds.Count; i += 50)
                {
                    string snStr = string.Join(",", newIds.Skip(i).Take(50));
                    string detailJson = await ShopeeApiHelper.GetOrderDetails(snStr);
                    using (JsonDocument dDoc = JsonDocument.Parse(detailJson))
                    {
                        if (dDoc.RootElement.TryGetProperty("response", out var dr) && dr.TryGetProperty("order_list", out var dl))
                        {
                            lock (_lock)
                            {
                                foreach (var o in dl.EnumerateArray())
                                {
                                    var ord = new Order { OrderId = o.GetProperty("order_sn").GetString()!, CreatedAt = o.GetProperty("create_time").GetInt64(), Status = 0 };
                                    foreach (var it in o.GetProperty("item_list").EnumerateArray())
                                    {
                                        string name = it.GetProperty("model_name").GetString()!;
                                        Dictionary<string, string> itemLocation = GetItemLocation(name);
                                        ord.Items.Add(new OrderItem
                                        {
                                            ItemId = it.GetProperty("item_id").GetInt64(),
                                            ProductName = it.GetProperty("item_name").GetString()!,
                                            ModelName = name,
                                            ImageUrl = it.GetProperty("image_info").GetProperty("image_url").GetString()!,
                                            Quantity = it.GetProperty("model_quantity_purchased").GetInt32(),
                                            SKU = it.GetProperty("model_sku").GetString() ?? "",
                                            Shelf = itemLocation.ContainsKey("Shelf") ? $"Kệ {itemLocation["Shelf"]}" : null,
                                            Level = itemLocation.ContainsKey("Level") ? $" - Ngăn {itemLocation["Level"]}" : null,
                                            Box = itemLocation.ContainsKey("Box") ? $" - Thùng {itemLocation["Box"]}" : null
                                        });
                                    }
                                    _dbOrders.Add(ord);
                                }
                            }
                        }
                    }
                }
            }
        }

        static void StartServer()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/");
            try
            {
                listener.Start();
                Console.WriteLine("Web: http://localhost:8080");
                Process.Start(new ProcessStartInfo("http://localhost:8080") { UseShellExecute = true });
            }
            catch { Console.WriteLine("Lỗi 8080. Chạy Admin!"); return; }

            while (true)
            {
                try
                {
                    var ctx = listener.GetContext();
                    var req = ctx.Request;
                    var resp = ctx.Response;
                    string url = req.Url.AbsolutePath;

                    if (url == "/")
                    {
                        byte[] b = Encoding.UTF8.GetBytes(HtmlTemplates.Index);
                        resp.ContentType = "text/html; charset=utf-8"; resp.OutputStream.Write(b, 0, b.Length);
                    }
                    else if (url == "/api/data")
                    {
                        string j; lock (_lock) { j = JsonSerializer.Serialize(_dbOrders); }
                        byte[] b = Encoding.UTF8.GetBytes(j);
                        resp.ContentType = "application/json"; resp.OutputStream.Write(b, 0, b.Length);
                    }
                    else if (url == "/api/assign")
                    {
                        string id = req.QueryString["id"], u = req.QueryString["user"];
                        lock (_lock) { var o = _dbOrders.FirstOrDefault(x => x.OrderId == id); if (o != null) o.AssignedTo = u; }
                        resp.StatusCode = 200;
                    }
                    else if (url == "/api/ship")
                    {
                        string id = req.QueryString["id"];
                        lock (_lock) { var o = _dbOrders.FirstOrDefault(x => x.OrderId == id); if (o != null) o.Status = 1; }
                        resp.StatusCode = 200;
                    }
                    // API LẤY CHI TIẾT SẢN PHẨM (ĐÃ FIX LOGIC LẤY STOCK)
                    else if (url == "/api/product")
                    {
                        string sid = req.QueryString["id"];
                        if (long.TryParse(sid, out long itemId))
                        {
                            string rawJson = ShopeeApiHelper.GetItemBaseInfo(itemId).Result;
                            var result = new { success = false, name = "", variations = new List<object>() };

                            using (JsonDocument doc = JsonDocument.Parse(rawJson))
                            {
                                if (doc.RootElement.TryGetProperty("response", out var r) && r.TryGetProperty("item_list", out var l) && l.GetArrayLength() > 0)
                                {
                                    var item = l[0];
                                    string iName = item.GetProperty("item_name").GetString()!;
                                    string defImg = item.GetProperty("image").GetProperty("image_url_list")[0].GetString()!;

                                    var vars = new List<object>();
                                    if (item.TryGetProperty("model_list", out var ms))
                                    {
                                        foreach (var m in ms.EnumerateArray())
                                        {
                                            int stock = 0;
                                            // Logic lấy Stock V2: stock_info_v2 -> summary_info -> total_available_stock
                                            if (m.TryGetProperty("stock_info_v2", out var si) && si.TryGetProperty("summary_info", out var sum))
                                                stock = sum.GetProperty("total_available_stock").GetInt32();
                                            // Fallback V1
                                            else if (m.TryGetProperty("stock_info", out var oldSi) && oldSi.GetArrayLength() > 0)
                                                stock = oldSi[0].GetProperty("normal_stock").GetInt32();

                                            vars.Add(new { name = m.GetProperty("model_name").GetString(), stock = stock, img = defImg });
                                        }
                                    }
                                    else
                                    {
                                        int stock = 0;
                                        if (item.TryGetProperty("stock_info_v2", out var si) && si.TryGetProperty("summary_info", out var sum))
                                            stock = sum.GetProperty("total_available_stock").GetInt32();
                                        vars.Add(new { name = "Mặc định", stock = stock, img = defImg });
                                    }
                                    result = new { success = true, name = iName, variations = vars };
                                }
                            }
                            byte[] b = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
                            resp.ContentType = "application/json"; resp.OutputStream.Write(b, 0, b.Length);
                        }
                    }
                    resp.Close();
                }
                catch { }
            }
        }
    }
}