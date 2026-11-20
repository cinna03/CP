using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Cinnarios.Launch
{
    /// <summary>
    /// The shop's records on the phone: the museum works, and the accounts, cart, and
    /// orders created on this device. Passwords are stored as a hash. This is not the laptop database.
    /// </summary>
    public sealed class PhoneShopBook
    {
        private readonly object _gate = new object();
        private readonly string _storePath;
        private readonly CatalogWork[] _works;
        private readonly string _listJson;
        private readonly Dictionary<string, string> _raw = new Dictionary<string, string>();
        private PhoneShopFile _file;

        public PhoneShopBook(string pagesDirectory, string storePath)
        {
            _storePath = storePath;
            string wrapped = File.ReadAllText(Path.Combine(pagesDirectory, "catalog.json"));
            _works = JsonUtility.FromJson<CatalogFile>(wrapped).artworks ?? Array.Empty<CatalogWork>();
            _listJson = File.ReadAllText(Path.Combine(pagesDirectory, "catalog-list.json"));
            foreach (CatalogWork work in _works)
            {
                _raw[work.sourceId] = Slice(work.sourceId);
            }

            _file = File.Exists(storePath)
                ? JsonUtility.FromJson<PhoneShopFile>(File.ReadAllText(storePath)) ?? new PhoneShopFile()
                : new PhoneShopFile();
            _file.users ??= new List<PhoneUser>();
            _file.items ??= new List<PhoneCartItem>();
            _file.addresses ??= new List<PhoneAddress>();
            _file.orders ??= new List<PhoneOrder>();
            _file.events ??= new List<PhoneEvent>();
        }

        public string CatalogList() => _listJson;

        public string CatalogOne(string sourceId)
        {
            if (_raw.TryGetValue(Uri.UnescapeDataString(sourceId), out string json))
            {
                return json;
            }

            throw new PhoneShopException(404, "NOT_FOUND", "Artwork not found.");
        }

        public string Register(string email, string password)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            CheckPassword(password);
            if (!email.Contains("@", StringComparison.Ordinal))
            {
                throw new PhoneShopException(400, "INVALID_EMAIL", "Enter an email address.");
            }

            lock (_gate)
            {
                if (_file.users.Any(user => user.email == email))
                {
                    throw new PhoneShopException(409, "EMAIL_IN_USE", "An account with this email already exists.");
                }

                var user = new PhoneUser
                {
                    id = Guid.NewGuid().ToString(),
                    email = email,
                    passwordHash = Hash(password),
                    displayName = email.Split('@')[0],
                    createdAt = Now(),
                };
                user.accessToken = NewToken();
                _file.users.Add(user);
                Save();
                return Session(user);
            }
        }

        public string Login(string email, string password)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            lock (_gate)
            {
                PhoneUser user = _file.users.FirstOrDefault(entry => entry.email == email);
                if (user == null || user.passwordHash != Hash(password ?? ""))
                {
                    throw new PhoneShopException(401, "INVALID_CREDENTIALS", "Email or password is incorrect.");
                }

                user.accessToken = NewToken();
                Save();
                return Session(user);
            }
        }

        public string Refresh(string token)
        {
            lock (_gate)
            {
                PhoneUser user = Find(token);
                if (user == null)
                {
                    throw new PhoneShopException(401, "REFRESH_INVALID", "Sign in again.");
                }

                return "{\"accessToken\":" + PhoneShopJson.Quote(user.accessToken) + "}";
            }
        }

        public void Logout(string token)
        {
            lock (_gate)
            {
                PhoneUser user = Find(token);
                if (user == null) return;
                user.accessToken = "";
                Save();
            }
        }

        public PhoneUser Require(string token)
        {
            lock (_gate)
            {
                PhoneUser user = Find(token);
                if (user == null)
                {
                    throw new PhoneShopException(401, "UNAUTHORIZED", "Sign in to continue.");
                }

                return user;
            }
        }

        public string Me(PhoneUser user)
        {
            return "{\"id\":" + PhoneShopJson.Quote(user.id)
                + ",\"email\":" + PhoneShopJson.Quote(user.email)
                + ",\"phone\":null,\"emailVerifiedAt\":" + PhoneShopJson.Quote(user.createdAt) + ",\"status\":\"active\",\"roles\":[\"collector\"],\"mfaEnabled\":false,\"createdAt\":"
                + PhoneShopJson.Quote(user.createdAt) + "}";
        }

        public string Collector(PhoneUser user)
        {
            return "{\"displayName\":" + PhoneShopJson.Quote(user.displayName)
                + ",\"city\":null,\"countryCode\":null,\"completedAt\":null}";
        }

        public string UpdateCollector(PhoneUser user, string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length < 2)
            {
                throw new PhoneShopException(400, "INVALID_NAME", "Enter a name.");
            }

            lock (_gate)
            {
                user.displayName = displayName.Trim();
                Save();
                return Collector(user);
            }
        }

        public string Artist(PhoneUser user)
        {
            if (!user.hasArtist)
            {
                throw new PhoneShopException(404, "NOT_FOUND", "Artist profile not found.");
            }

            return ArtistJson(user);
        }

        public string SaveArtist(PhoneUser user, Dictionary<string, object> body, bool create)
        {
            lock (_gate)
            {
                if (create && user.hasArtist)
                {
                    throw new PhoneShopException(409, "ARTIST_EXISTS", "This account already has an artist name.");
                }

                if (!create && !user.hasArtist)
                {
                    throw new PhoneShopException(404, "NOT_FOUND", "Artist profile not found.");
                }

                string name = Value(body, "displayName");
                if (create && string.IsNullOrWhiteSpace(name))
                {
                    throw new PhoneShopException(400, "INVALID_NAME", "Enter a name.");
                }

                if (!string.IsNullOrWhiteSpace(name)) user.artistName = name.Trim();
                if (body != null && body.ContainsKey("bio")) user.artistBio = Value(body, "bio");
                if (body != null && body.ContainsKey("city")) user.artistCity = Value(body, "city");
                if (body != null && body.ContainsKey("countryCode")) user.artistCountry = Value(body, "countryCode");
                if (body != null && body.ContainsKey("website")) user.artistWebsite = Value(body, "website");
                if (string.IsNullOrEmpty(user.artistSlug))
                {
                    user.artistSlug = new string((user.artistName ?? "artist").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                    if (user.artistSlug.Length == 0) user.artistSlug = "artist";
                }

                user.hasArtist = true;
                Save();
                return ArtistJson(user);
            }
        }

        public string Journey(PhoneUser user, string kind, string sourceId, string sessionId)
        {
            lock (_gate)
            {
                var point = new PhoneEvent
                {
                    id = Guid.NewGuid().ToString(),
                    userId = user.id,
                    kind = kind,
                    sourceId = sourceId,
                    sessionId = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString() : sessionId,
                    occurredAt = Now(),
                };
                _file.events.Add(point);
                Save();
                return "{\"id\":" + PhoneShopJson.Quote(point.id)
                    + ",\"kind\":" + PhoneShopJson.Quote(point.kind)
                    + ",\"sourceId\":" + PhoneShopJson.Quote(point.sourceId)
                    + ",\"sessionId\":" + PhoneShopJson.Quote(point.sessionId)
                    + ",\"occurredAt\":" + PhoneShopJson.Quote(point.occurredAt) + "}";
            }
        }

        public string Confidence(PhoneUser user)
        {
            lock (_gate)
            {
                bool addressSaved = _file.addresses.Any(address => address.userId == user.id);
                var groups = _file.events
                    .Where(point => point.userId == user.id && !string.IsNullOrEmpty(point.sourceId))
                    .GroupBy(point => point.sourceId);
                var reports = new List<string>();
                int confident = 0;
                int uncertain = 0;
                foreach (IGrouping<string, PhoneEvent> group in groups)
                {
                    string report = Score(group.Key, group.OrderBy(point => point.occurredAt).ToList(), addressSaved, out bool became);
                    reports.Add(report);
                    if (became) confident++;
                    else uncertain++;
                }

                return "{\"artworks\":[" + string.Join(",", reports) + "],\"summary\":{\"worksConsidered\":"
                    + reports.Count + ",\"worksStillUncertain\":" + uncertain + ",\"worksBecameConfident\":" + confident + "}}";
            }
        }

        public string Addresses(PhoneUser user)
        {
            lock (_gate)
            {
                IEnumerable<string> rows = _file.addresses.Where(address => address.userId == user.id).Select(AddressJson);
                return "[" + string.Join(",", rows) + "]";
            }
        }

        public string AddAddress(PhoneUser user, Dictionary<string, object> body)
        {
            lock (_gate)
            {
                var address = new PhoneAddress
                {
                    id = Guid.NewGuid().ToString(),
                    userId = user.id,
                    recipientName = Value(body, "recipientName"),
                    line1 = Value(body, "line1"),
                    line2 = Value(body, "line2"),
                    city = Value(body, "city"),
                    region = Value(body, "region"),
                    postalCode = Value(body, "postalCode"),
                    countryCode = Value(body, "countryCode"),
                    phone = Value(body, "phone"),
                };
                _file.addresses.Add(address);
                _file.events.Add(new PhoneEvent
                {
                    id = Guid.NewGuid().ToString(),
                    userId = user.id,
                    kind = "address_saved",
                    sourceId = "",
                    sessionId = "server",
                    occurredAt = Now(),
                });
                Save();
                return AddressJson(address);
            }
        }

        public string Cart(PhoneUser user)
        {
            lock (_gate)
            {
                List<PhoneCartItem> items = _file.items.Where(item => item.userId == user.id).ToList();
                int total = items.Sum(item => item.lineAmount);
                string currency = items.Count > 0 ? items[0].currency : "USD";
                return "{\"items\":[" + string.Join(",", items.Select(ItemJson)) + "],\"totalAmount\":"
                    + PhoneShopJson.Quote(total.ToString(CultureInfo.InvariantCulture))
                    + ",\"currency\":" + PhoneShopJson.Quote(currency) + "}";
            }
        }

        public string AddCart(PhoneUser user, string sourceId, int quantity)
        {
            CatalogWork work = FindWork(sourceId);
            if (quantity < 1) quantity = 1;
            lock (_gate)
            {
                PhoneCartItem existing = _file.items.FirstOrDefault(item => item.userId == user.id && item.sourceId == work.sourceId);
                if (existing == null)
                {
                    _file.items.Add(Line(user, work, quantity));
                }
                else
                {
                    existing.quantity = quantity;
                    existing.lineAmount = existing.unitAmount * quantity;
                }

                Save();
                return Cart(user);
            }
        }

        public string UpdateCart(PhoneUser user, string itemId, int quantity)
        {
            lock (_gate)
            {
                PhoneCartItem item = _file.items.FirstOrDefault(entry => entry.userId == user.id && entry.id == itemId);
                if (item == null) throw new PhoneShopException(404, "NOT_FOUND", "Cart item not found.");
                item.quantity = quantity;
                item.lineAmount = item.unitAmount * quantity;
                Save();
                return Cart(user);
            }
        }

        public string RemoveCart(PhoneUser user, string itemId)
        {
            lock (_gate)
            {
                _file.items.RemoveAll(item => item.userId == user.id && item.id == itemId);
                Save();
                return Cart(user);
            }
        }

        public string Checkout(PhoneUser user, string addressId, string sessionId)
        {
            lock (_gate)
            {
                PhoneAddress address = _file.addresses.FirstOrDefault(entry => entry.userId == user.id && entry.id == addressId);
                if (address == null) throw new PhoneShopException(404, "NOT_FOUND", "Address not found.");
                List<PhoneCartItem> items = _file.items.Where(item => item.userId == user.id).ToList();
                if (items.Count == 0) throw new PhoneShopException(400, "CART_EMPTY", "The cart is empty.");

                var order = new PhoneOrder
                {
                    number = "M" + DateTime.UtcNow.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant(),
                    userId = user.id,
                    status = "awaiting_payment",
                    currency = items[0].currency,
                    totalAmount = items.Sum(item => item.lineAmount),
                    placedAt = Now(),
                    recipientName = address.recipientName,
                    line1 = address.line1,
                    line2 = address.line2,
                    city = address.city,
                    region = address.region,
                    postalCode = address.postalCode,
                    countryCode = address.countryCode,
                    items = items.Select(item => new PhoneOrderItem
                    {
                        sourceId = item.sourceId,
                        title = item.title,
                        artist = item.artist,
                        quantity = item.quantity,
                        unitAmount = item.unitAmount,
                        widthCm = item.widthCm,
                        heightCm = item.heightCm,
                        depthCm = item.depthCm,
                    }).ToList(),
                };
                _file.orders.Add(order);
                foreach (PhoneCartItem item in items)
                {
                    _file.events.Add(new PhoneEvent
                    {
                        id = Guid.NewGuid().ToString(),
                        userId = user.id,
                        kind = "checkout_started",
                        sourceId = item.sourceId,
                        sessionId = string.IsNullOrEmpty(sessionId) ? "server" : sessionId,
                        occurredAt = Now(),
                    });
                }

                _file.items.RemoveAll(item => item.userId == user.id);
                Save();
                return OrderJson(order);
            }
        }

        public string Orders(PhoneUser user)
        {
            lock (_gate)
            {
                IEnumerable<string> rows = _file.orders.Where(order => order.userId == user.id).Reverse().Select(OrderJson);
                return "[" + string.Join(",", rows) + "]";
            }
        }

        private CatalogWork FindWork(string sourceId)
        {
            CatalogWork work = _works.FirstOrDefault(entry => entry.sourceId == sourceId);
            if (work == null) throw new PhoneShopException(404, "NOT_FOUND", "Artwork not found.");
            return work;
        }

        private PhoneUser Find(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            return _file.users.FirstOrDefault(user => user.accessToken == token);
        }

        private void Save()
        {
            File.WriteAllText(_storePath, JsonUtility.ToJson(_file));
        }

        private string Slice(string sourceId)
        {
            string key = "\"sourceId\":\"" + sourceId + "\"";
            int at = _listJson.IndexOf(key, StringComparison.Ordinal);
            if (at < 0)
            {
                at = _listJson.IndexOf("\"sourceId\": \"" + sourceId + "\"", StringComparison.Ordinal);
            }

            if (at < 0) return JsonUtility.ToJson(FindWork(sourceId));
            int start = _listJson.LastIndexOf('{', at);
            int depth = 0;
            for (int index = start; index < _listJson.Length; index++)
            {
                if (_listJson[index] == '{') depth++;
                else if (_listJson[index] == '}')
                {
                    depth--;
                    if (depth == 0) return _listJson.Substring(start, index - start + 1);
                }
            }

            return JsonUtility.ToJson(FindWork(sourceId));
        }

        private static PhoneCartItem Line(PhoneUser user, CatalogWork work, int quantity)
        {
            int unit = int.Parse(work.priceAmount, CultureInfo.InvariantCulture);
            return new PhoneCartItem
            {
                id = Guid.NewGuid().ToString(),
                userId = user.id,
                sourceId = work.sourceId,
                title = work.title,
                artist = work.artist,
                quantity = quantity,
                unitAmount = unit,
                lineAmount = unit * quantity,
                currency = (work.currency ?? "USD").Trim(),
                widthCm = work.widthCm,
                heightCm = work.heightCm,
                depthCm = work.depthCm,
                thumbnailUrl = work.thumbnailUrl,
            };
        }

        private static string Score(string sourceId, List<PhoneEvent> points, bool addressSaved, out bool becameConfident)
        {
            int score = 0;
            var signals = new List<string>();
            bool seenView = false;
            var sessions = new HashSet<string>();
            bool returned = false;
            bool viewedInRoom = false;
            bool purchased = false;
            foreach (PhoneEvent point in points)
            {
                if (point.kind == "artwork_viewed")
                {
                    if (!seenView)
                    {
                        score += 15;
                        seenView = true;
                        signals.Add("first_view");
                    }
                    else if (!sessions.Contains(point.sessionId))
                    {
                        score += 15;
                        returned = true;
                        signals.Add("returned");
                    }

                    sessions.Add(point.sessionId);
                }
                else if (point.kind == "dimensions_checked") { score += 10; signals.Add("dimensions_checked"); }
                else if (point.kind == "credit_reviewed") { score += 10; signals.Add("credit_reviewed"); }
                else if (point.kind == "viewed_in_room") { score += 25; viewedInRoom = true; signals.Add("viewed_in_room"); }
                else if (point.kind == "checkout_started") { score += 10; signals.Add("checkout_started"); }
                else if (point.kind == "checkout_abandoned") { score -= 20; signals.Add("checkout_abandoned"); }
                else if (point.kind == "purchase_completed") { score += 20; purchased = true; signals.Add("purchase_completed"); }
            }

            if (addressSaved)
            {
                score += 10;
                signals.Add("address_saved");
            }

            score = Math.Max(0, Math.Min(100, score));
            becameConfident = seenView && (returned || viewedInRoom) && (purchased || score >= 70);
            string stage = purchased ? "purchased" : viewedInRoom ? "reassuring" : returned ? "considering" : score >= 40 ? "ready" : "browsing";
            int gain = seenView ? Math.Max(0, score - 15) : score;
            string signalJson = string.Join(",", signals.Distinct().Select(PhoneShopJson.Quote));
            return "{\"sourceId\":" + PhoneShopJson.Quote(sourceId)
                + ",\"score\":" + score
                + ",\"startedUncertain\":" + (seenView ? "true" : "false")
                + ",\"becameConfident\":" + (becameConfident ? "true" : "false")
                + ",\"gain\":" + gain
                + ",\"stage\":" + PhoneShopJson.Quote(stage)
                + ",\"signals\":[" + signalJson + "]}";
        }

        private static string Session(PhoneUser user)
        {
            return "{\"accessToken\":" + PhoneShopJson.Quote(user.accessToken)
                + ",\"expiresIn\":3600,\"tokenType\":\"Bearer\",\"user\":{\"id\":" + PhoneShopJson.Quote(user.id)
                + ",\"email\":" + PhoneShopJson.Quote(user.email)
                + ",\"roles\":[\"collector\"],\"emailVerifiedAt\":" + PhoneShopJson.Quote(user.createdAt) + ",\"mfaEnabled\":false}}";
        }

        private static string ArtistJson(PhoneUser user)
        {
            return "{\"displayName\":" + PhoneShopJson.Quote(user.artistName)
                + ",\"slug\":" + PhoneShopJson.Quote(user.artistSlug)
                + ",\"bio\":" + PhoneShopJson.Quote(user.artistBio)
                + ",\"city\":" + PhoneShopJson.Quote(user.artistCity)
                + ",\"countryCode\":" + PhoneShopJson.Quote(user.artistCountry)
                + ",\"website\":" + PhoneShopJson.Quote(user.artistWebsite) + "}";
        }

        private static string AddressJson(PhoneAddress address)
        {
            return "{\"id\":" + PhoneShopJson.Quote(address.id)
                + ",\"recipientName\":" + PhoneShopJson.Quote(address.recipientName)
                + ",\"line1\":" + PhoneShopJson.Quote(address.line1)
                + ",\"line2\":" + PhoneShopJson.Quote(address.line2)
                + ",\"city\":" + PhoneShopJson.Quote(address.city)
                + ",\"region\":" + PhoneShopJson.Quote(address.region)
                + ",\"postalCode\":" + PhoneShopJson.Quote(address.postalCode)
                + ",\"countryCode\":" + PhoneShopJson.Quote(address.countryCode)
                + ",\"phone\":" + PhoneShopJson.Quote(address.phone)
                + ",\"isDefaultShipping\":false}";
        }

        private static string ItemJson(PhoneCartItem item)
        {
            return "{\"id\":" + PhoneShopJson.Quote(item.id)
                + ",\"sourceId\":" + PhoneShopJson.Quote(item.sourceId)
                + ",\"title\":" + PhoneShopJson.Quote(item.title)
                + ",\"artist\":" + PhoneShopJson.Quote(item.artist)
                + ",\"quantity\":" + item.quantity
                + ",\"unitAmount\":" + PhoneShopJson.Quote(item.unitAmount.ToString(CultureInfo.InvariantCulture))
                + ",\"lineAmount\":" + PhoneShopJson.Quote(item.lineAmount.ToString(CultureInfo.InvariantCulture))
                + ",\"currency\":" + PhoneShopJson.Quote(item.currency)
                + ",\"widthCm\":" + item.widthCm.ToString(CultureInfo.InvariantCulture)
                + ",\"heightCm\":" + item.heightCm.ToString(CultureInfo.InvariantCulture)
                + ",\"depthCm\":" + item.depthCm.ToString(CultureInfo.InvariantCulture)
                + ",\"thumbnailUrl\":" + PhoneShopJson.Quote(item.thumbnailUrl) + "}";
        }

        private static string OrderJson(PhoneOrder order)
        {
            string items = string.Join(",", order.items.Select(item =>
                "{\"sourceId\":" + PhoneShopJson.Quote(item.sourceId)
                + ",\"title\":" + PhoneShopJson.Quote(item.title)
                + ",\"artist\":" + PhoneShopJson.Quote(item.artist)
                + ",\"quantity\":" + item.quantity
                + ",\"unitAmount\":" + PhoneShopJson.Quote(item.unitAmount.ToString(CultureInfo.InvariantCulture))
                + ",\"widthCm\":" + item.widthCm.ToString(CultureInfo.InvariantCulture)
                + ",\"heightCm\":" + item.heightCm.ToString(CultureInfo.InvariantCulture)
                + ",\"depthCm\":" + item.depthCm.ToString(CultureInfo.InvariantCulture) + "}"));
            return "{\"number\":" + PhoneShopJson.Quote(order.number)
                + ",\"status\":" + PhoneShopJson.Quote(order.status)
                + ",\"currency\":" + PhoneShopJson.Quote(order.currency)
                + ",\"totalAmount\":" + PhoneShopJson.Quote(order.totalAmount.ToString(CultureInfo.InvariantCulture))
                + ",\"placedAt\":" + PhoneShopJson.Quote(order.placedAt)
                + ",\"paidAt\":null,\"shipping\":{\"recipientName\":" + PhoneShopJson.Quote(order.recipientName)
                + ",\"line1\":" + PhoneShopJson.Quote(order.line1)
                + ",\"line2\":" + PhoneShopJson.Quote(order.line2)
                + ",\"city\":" + PhoneShopJson.Quote(order.city)
                + ",\"region\":" + PhoneShopJson.Quote(order.region)
                + ",\"postalCode\":" + PhoneShopJson.Quote(order.postalCode)
                + ",\"countryCode\":" + PhoneShopJson.Quote(order.countryCode)
                + "},\"items\":[" + items + "]}";
        }

        private static void CheckPassword(string password)
        {
            bool ok = !string.IsNullOrEmpty(password)
                && password.Length >= 12
                && password.Any(char.IsLower)
                && password.Any(char.IsUpper)
                && password.Any(char.IsDigit);
            if (!ok)
            {
                throw new PhoneShopException(400, "WEAK_PASSWORD", "Password must include upper and lower case letters and a number");
            }
        }

        private static string Hash(string password)
        {
            using SHA256 algorithm = SHA256.Create();
            byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes("cinnarios-phone:" + password));
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private static string NewToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        private static string Now() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        private static string Value(Dictionary<string, object> body, string key)
        {
            if (body != null && body.TryGetValue(key, out object value) && value != null)
            {
                return Convert.ToString(value);
            }

            return "";
        }
    }

    [Serializable]
    public sealed class CatalogFile
    {
        public CatalogWork[] artworks;
    }

    [Serializable]
    public sealed class CatalogWork
    {
        public string source;
        public string sourceId;
        public string title;
        public string artist;
        public string date;
        public string creditLine;
        public string mediumDescription;
        public string medium;
        public float widthCm;
        public float heightCm;
        public float depthCm;
        public string imageUrl;
        public string thumbnailUrl;
        public string priceAmount;
        public string currency;
    }

    [Serializable]
    public sealed class PhoneShopFile
    {
        public List<PhoneUser> users;
        public List<PhoneCartItem> items;
        public List<PhoneAddress> addresses;
        public List<PhoneOrder> orders;
        public List<PhoneEvent> events;
    }

    [Serializable]
    public sealed class PhoneUser
    {
        public string id;
        public string email;
        public string passwordHash;
        public string displayName;
        public string accessToken;
        public string createdAt;
        public bool hasArtist;
        public string artistName;
        public string artistSlug;
        public string artistBio;
        public string artistCity;
        public string artistCountry;
        public string artistWebsite;
    }

    [Serializable]
    public sealed class PhoneCartItem
    {
        public string id;
        public string userId;
        public string sourceId;
        public string title;
        public string artist;
        public int quantity;
        public int unitAmount;
        public int lineAmount;
        public string currency;
        public float widthCm;
        public float heightCm;
        public float depthCm;
        public string thumbnailUrl;
    }

    [Serializable]
    public sealed class PhoneAddress
    {
        public string id;
        public string userId;
        public string recipientName;
        public string line1;
        public string line2;
        public string city;
        public string region;
        public string postalCode;
        public string countryCode;
        public string phone;
    }

    [Serializable]
    public sealed class PhoneOrder
    {
        public string number;
        public string userId;
        public string status;
        public string currency;
        public int totalAmount;
        public string placedAt;
        public string recipientName;
        public string line1;
        public string line2;
        public string city;
        public string region;
        public string postalCode;
        public string countryCode;
        public List<PhoneOrderItem> items;
    }

    [Serializable]
    public sealed class PhoneOrderItem
    {
        public string sourceId;
        public string title;
        public string artist;
        public int quantity;
        public int unitAmount;
        public float widthCm;
        public float heightCm;
        public float depthCm;
    }

    [Serializable]
    public sealed class PhoneEvent
    {
        public string id;
        public string userId;
        public string kind;
        public string sourceId;
        public string sessionId;
        public string occurredAt;
    }
}
