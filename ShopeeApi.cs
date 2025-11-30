using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace ShopeeServer
{
    public static class ShopeeApiHelper
    {
        // === CẤU HÌNH ===
        public static long PartnerId { get; private set; }
        public static string PartnerKey { get; private set; } = "";
        public static string BaseUrl { get; private set; } = "";
        public static string CallbackUrl { get; private set; } = "";

        // === DỮ LIỆU ĐỘNG ===
        public static long ShopId { get; private set; }
        public static string AccessToken { get; private set; } = "";
        public static string RefreshToken { get; private set; } = "";
        //public static string AppPassword { get; private set; } = "";

        static ShopeeApiHelper() { LoadConfig(); }

        private static void LoadConfig()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                var config = builder.Build().GetSection("ShopeeSettings");

                PartnerId = long.Parse(config["PartnerId"] ?? "0");
                PartnerKey = config["PartnerKey"] ?? "";
                BaseUrl = config["BaseUrl"] ?? "https://partner.shopeemobile.com";
                CallbackUrl = config["CallbackUrl"] ?? "";
                //AppPassword = config["AppPassword"] ?? "";

                long.TryParse(config["SavedShopId"], out long sid); ShopId = sid;
                AccessToken = config["SavedAccessToken"] ?? "";
                RefreshToken = config["SavedRefreshToken"] ?? "";
            }
            catch { }
        }

        public static void SaveTokenToConfig(long shopId, string accessToken, string refreshToken)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                // Tạo file mới nếu chưa có
                if (!File.Exists(filePath))
                {
                    var defaultJson = new { ShopeeSettings = new { PartnerId = PartnerId, PartnerKey = PartnerKey, BaseUrl = BaseUrl, CallbackUrl = CallbackUrl, SavedShopId = 0, SavedAccessToken = "", SavedRefreshToken = "" } };
                    File.WriteAllText(filePath, JsonSerializer.Serialize(defaultJson));
                }

                string jsonString = File.ReadAllText(filePath);
                var jsonNode = JsonNode.Parse(jsonString);

                if (jsonNode != null)
                {
                    jsonNode["ShopeeSettings"]!["SavedShopId"] = shopId;
                    jsonNode["ShopeeSettings"]!["SavedAccessToken"] = accessToken;
                    jsonNode["ShopeeSettings"]!["SavedRefreshToken"] = refreshToken;

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(filePath, jsonNode.ToJsonString(options));

                    ShopId = shopId; AccessToken = accessToken; RefreshToken = refreshToken;
                    Console.WriteLine("[Config] Đã lưu Token mới.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Save Error] {ex.Message}");
                SaveTokenToConfig(ShopId, "","");
            }
        }

        public static string GenerateSignature(string path, long timeStamp, bool isAuth = false)
        {
            string baseString = $"{PartnerId}{path}{timeStamp}";
            if (!isAuth) baseString += $"{AccessToken}{ShopId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(PartnerKey));
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString))).Replace("-", "").ToLowerInvariant();
        }

        public static string GetAuthUrl()
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string sign = GenerateSignature("/api/v2/shop/auth_partner", ts, true);
            return $"{BaseUrl}/api/v2/shop/auth_partner?partner_id={PartnerId}&timestamp={ts}&sign={sign}&redirect={CallbackUrl}";
        }

        public static async Task<bool> ExchangeCodeForToken(long shopId, string code)
        {
            return await PostAuth("/api/v2/auth/token/get", new { partner_id = PartnerId, shop_id = shopId, code = code });
        }

        public static async Task<bool> RefreshTokenNow()
        {
            Console.WriteLine("[Auto-Refresh] Đang gia hạn Token...");
            return await PostAuth("/api/v2/auth/access_token/get", new { partner_id = PartnerId, shop_id = ShopId, refresh_token = RefreshToken });
        }

        private static async Task<bool> PostAuth(string path, object body)
        {
            try
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string sign = GenerateSignature(path, ts, true);
                using var client = new HttpClient();
                var resp = await client.PostAsync($"{BaseUrl}{path}?partner_id={PartnerId}&timestamp={ts}&sign={sign}",
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("access_token", out var t))
                {
                    long sid = ShopId;
                    if (doc.RootElement.TryGetProperty("shop_id", out var s)) sid = s.GetInt64();
                    SaveTokenToConfig(sid, t.GetString()!, doc.RootElement.GetProperty("refresh_token").GetString()!);
                    return true;
                }
                Console.WriteLine($"[Auth Fail] {json}");
            }
            catch (Exception ex) { Console.WriteLine($"[Auth Ex] {ex.Message}"); }
            return false;
        }

        public static async Task<string> CallApi(string path, Dictionary<string, string> p)
        {
            if (string.IsNullOrEmpty(AccessToken)) return "{\"error\":\"no_token\"}";
            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string sign = GenerateSignature(path, ts);

            var q = new Dictionary<string, string?> {
                { "partner_id", PartnerId.ToString() }, { "timestamp", ts.ToString() },
                { "access_token", AccessToken }, { "shop_id", ShopId.ToString() }, { "sign", sign }
            };
            foreach (var k in p) q[k.Key] = k.Value;

            using var client = new HttpClient();
            try { return await client.GetStringAsync(QueryHelpers.AddQueryString(BaseUrl + path, q)); }
            catch (Exception ex) { return "{\"error\":\"network_error\"}"; }
        }

        // --- API WRAPPERS ---
        public static async Task<string> GetOrderList(long fromDate, long toDate)
        {
            var p = new Dictionary<string, string> {
                { "time_range_field", "create_time" }, { "time_from", fromDate.ToString() },
                { "time_to", toDate.ToString() }, { "page_size", "100" }, { "order_status", "READY_TO_SHIP" }
            };
            return await CallApi("/api/v2/order/get_order_list", p);
        }

        public static async Task<string> GetOrderDetails(string sns)
        {
            return await CallApi("/api/v2/order/get_order_detail", new Dictionary<string, string> {
                { "order_sn_list", sns }, { "response_optional_fields", "item_list" }
            });
        }

        public static async Task<string> GetItemBaseInfo(long itemId)
        {
            return await CallApi("/api/v2/product/get_item_base_info", new Dictionary<string, string> {
                { "item_id_list", itemId.ToString() }, { "response_optional_fields", "model_list,image,stock_info_v2" }
            });
        }
    }
}