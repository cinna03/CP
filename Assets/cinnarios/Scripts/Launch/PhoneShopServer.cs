using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Cinnarios.Launch
{
    /// <summary>
    /// Serves the shop from the phone. The pages are the website files copied into StreamingAssets,
    /// and the accounts, cart, and orders stay in a file on the phone. The laptop is not involved.
    /// </summary>
    public static class PhoneShopServer
    {
        private const int FirstPort = 3010;

        private static HttpListener _listener;
        private static string _origin;
        private static PhoneShopBook _book;
        private static string _pages;

        public static string EnsureStarted()
        {
            if (_origin != null)
            {
                return _origin;
            }

            _pages = Path.Combine(Application.streamingAssetsPath, "shop");
            _book = new PhoneShopBook(_pages, Path.Combine(Application.persistentDataPath, "phone-shop.json"));

            for (int port = FirstPort; port < FirstPort + 20; port++)
            {
                var listener = new HttpListener();
                string prefix = $"http://127.0.0.1:{port}/";
                listener.Prefixes.Add(prefix);
                try
                {
                    listener.Start();
                    _listener = listener;
                    _origin = $"http://127.0.0.1:{port}";
                    var thread = new Thread(Listen) { IsBackground = true, Name = "Phone shop" };
                    thread.Start();
                    Debug.Log($"[Shop] The shop is on this phone at {_origin}");
                    return _origin;
                }
                catch (Exception exception)
                {
                    listener.Close();
                    Debug.LogWarning($"[Shop] Port {port} is busy ({exception.Message})");
                }
            }

            throw new InvalidOperationException("The shop could not listen on this phone.");
        }

        private static void Listen()
        {
            while (_listener != null && _listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.GetContext();
                }
                catch (Exception)
                {
                    return;
                }

                try
                {
                    Handle(context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    WriteJson(context.Response, 500, "{\"message\":\"The shop on this phone failed.\"}");
                }
            }
        }

        private static void Handle(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            string path = request.Url.AbsolutePath;
            string method = request.HttpMethod;

            if (path == "/lan")
            {
                WriteJson(context.Response, 200, "{\"address\":\"127.0.0.1\"}");
                return;
            }

            if (path.StartsWith("/wall-tries/", StringComparison.Ordinal))
            {
                ServeWallTry(context.Response, path);
                return;
            }

            if (!path.StartsWith("/v1/", StringComparison.Ordinal))
            {
                ServePage(context.Response, path);
                return;
            }

            string route = path.Substring("/v1".Length);
            Dictionary<string, object> body = ReadBody(request);
            string token = Bearer(request);
            try
            {
                Route(context.Response, method, route, body, token);
            }
            catch (PhoneShopException exception)
            {
                WriteJson(context.Response, exception.Status, PhoneShopJson.Error(exception.Code, exception.Message));
            }
        }

        private static void Route(HttpListenerResponse response, string method, string route, Dictionary<string, object> body, string token)
        {
            if (method == "GET" && route == "/catalog/artworks")
            {
                WriteJson(response, 200, _book.CatalogList());
                return;
            }

            if (method == "GET" && route.StartsWith("/catalog/artworks/", StringComparison.Ordinal))
            {
                WriteJson(response, 200, _book.CatalogOne(route.Substring("/catalog/artworks/".Length)));
                return;
            }

            if (method == "GET" && route.StartsWith("/integrations/ar/artworks/", StringComparison.Ordinal))
            {
                WriteJson(response, 200, _book.CatalogOne(route.Substring("/integrations/ar/artworks/".Length)));
                return;
            }

            if (method == "GET" && route == "/auth/providers")
            {
                WriteJson(response, 200, "{\"googleClientId\":\"\",\"appleClientId\":\"\"}");
                return;
            }

            if (method == "POST" && route == "/auth/register")
            {
                WriteJson(response, 201, _book.Register(Text(body, "email"), Text(body, "password")));
                return;
            }

            if (method == "POST" && route == "/auth/login")
            {
                WriteJson(response, 200, _book.Login(Text(body, "email"), Text(body, "password")));
                return;
            }

            if (method == "POST" && route == "/auth/refresh")
            {
                WriteJson(response, 200, _book.Refresh(token));
                return;
            }

            if (method == "POST" && route == "/auth/logout")
            {
                _book.Logout(token);
                WriteJson(response, 200, "{}");
                return;
            }

            if (method == "POST" && route == "/auth/password/forgot")
            {
                WriteJson(response, 200, "{\"message\":\"This phone keeps the password on the device. Choose it again by registering the same email, which will say the account already exists.\"}");
                return;
            }

            PhoneUser user = _book.Require(token);

            if (method == "GET" && route == "/me")
            {
                WriteJson(response, 200, _book.Me(user));
                return;
            }

            if (method == "GET" && route == "/me/collector")
            {
                WriteJson(response, 200, _book.Collector(user));
                return;
            }

            if (method == "PATCH" && route == "/me/collector")
            {
                WriteJson(response, 200, _book.UpdateCollector(user, Text(body, "displayName")));
                return;
            }

            if (method == "GET" && route == "/me/artist")
            {
                WriteJson(response, 200, _book.Artist(user));
                return;
            }

            if (method == "POST" && route == "/me/artist")
            {
                WriteJson(response, 201, _book.SaveArtist(user, body, true));
                return;
            }

            if (method == "PATCH" && route == "/me/artist")
            {
                WriteJson(response, 200, _book.SaveArtist(user, body, false));
                return;
            }

            if (method == "GET" && route == "/me/confidence")
            {
                WriteJson(response, 200, _book.Confidence(user));
                return;
            }

            if (method == "POST" && route == "/me/journey")
            {
                WriteJson(response, 201, _book.Journey(user, Text(body, "kind"), Text(body, "sourceId"), Text(body, "sessionId")));
                return;
            }

            if (method == "GET" && route == "/me/addresses")
            {
                WriteJson(response, 200, _book.Addresses(user));
                return;
            }

            if (method == "POST" && route == "/me/addresses")
            {
                WriteJson(response, 201, _book.AddAddress(user, body));
                return;
            }

            if (method == "GET" && route == "/cart")
            {
                WriteJson(response, 200, _book.Cart(user));
                return;
            }

            if (method == "POST" && route == "/cart/items")
            {
                int quantity = (int)Number(body, "quantity", 1);
                WriteJson(response, 201, _book.AddCart(user, Text(body, "sourceId"), quantity));
                return;
            }

            if (route.StartsWith("/cart/items/", StringComparison.Ordinal))
            {
                string itemId = route.Substring("/cart/items/".Length);
                if (method == "PATCH")
                {
                    WriteJson(response, 200, _book.UpdateCart(user, itemId, (int)Number(body, "quantity", 1)));
                    return;
                }

                if (method == "DELETE")
                {
                    WriteJson(response, 200, _book.RemoveCart(user, itemId));
                    return;
                }
            }

            if (method == "POST" && route == "/checkout")
            {
                WriteJson(response, 201, _book.Checkout(user, Text(body, "addressId"), Text(body, "sessionId")));
                return;
            }

            if (method == "GET" && route == "/orders")
            {
                WriteJson(response, 200, _book.Orders(user));
                return;
            }

            throw new PhoneShopException(404, "NOT_FOUND", "That page is not on this phone.");
        }

        private static void ServeWallTry(HttpListenerResponse response, string path)
        {
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name) || name.Contains("..", StringComparison.Ordinal))
            {
                WriteJson(response, 404, "{\"message\":\"Not found.\"}");
                return;
            }

            string file = Path.Combine(Application.persistentDataPath, "wall-tries", name);
            if (!File.Exists(file))
            {
                WriteJson(response, 404, "{\"message\":\"Not found.\"}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(file);
            response.StatusCode = 200;
            response.ContentType = "image/png";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }

        private static void ServePage(HttpListenerResponse response, string path)
        {
            if (path == "/")
            {
                path = "/index.html";
            }

            string name = path.TrimStart('/').Replace('\\', '/');
            if (name.Contains("..", StringComparison.Ordinal))
            {
                WriteJson(response, 404, "{\"message\":\"Not found.\"}");
                return;
            }

            string file = Path.Combine(_pages, name);
            if (!File.Exists(file))
            {
                WriteJson(response, 404, "{\"message\":\"Not found.\"}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(file);
            response.StatusCode = 200;
            response.ContentType = TypeFor(name);
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }

        private static string TypeFor(string name)
        {
            if (name.EndsWith(".js", StringComparison.Ordinal)) return "text/javascript; charset=utf-8";
            if (name.EndsWith(".css", StringComparison.Ordinal)) return "text/css; charset=utf-8";
            if (name.EndsWith(".html", StringComparison.Ordinal)) return "text/html; charset=utf-8";
            if (name.EndsWith(".json", StringComparison.Ordinal)) return "application/json; charset=utf-8";
            return "application/octet-stream";
        }

        private static Dictionary<string, object> ReadBody(HttpListenerRequest request)
        {
            if (request.ContentLength64 <= 0)
            {
                return new Dictionary<string, object>();
            }

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            string text = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(text) || text == "{}")
            {
                return new Dictionary<string, object>();
            }

            if (PhoneShopJson.Parse(text) is Dictionary<string, object> body)
            {
                return body;
            }

            return new Dictionary<string, object>();
        }

        private static string Bearer(HttpListenerRequest request)
        {
            string header = request.Headers["Authorization"];
            if (header != null && header.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                return header.Substring("Bearer ".Length).Trim();
            }

            return null;
        }

        private static string Text(Dictionary<string, object> body, string key)
        {
            if (body != null && body.TryGetValue(key, out object value) && value != null)
            {
                return Convert.ToString(value);
            }

            return "";
        }

        private static double Number(Dictionary<string, object> body, string key, double fallback)
        {
            if (body != null && body.TryGetValue(key, out object value) && value is double number)
            {
                return number;
            }

            return fallback;
        }

        private static void WriteJson(HttpListenerResponse response, int status, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json ?? "{}");
            response.StatusCode = status;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }
    }
}
