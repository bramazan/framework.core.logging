using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IO;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Framework.Core.Logging.Helper
{

    public static class ExtensionHelper
    {
        private const string emptyJson = "{}";

        public static string ClearSensitiveValues(this string content)
        {
            return content.MaskCardNumber().RemoveBearerAuthorization().RemoveClientSecret().RemoveBasicAuthorization().RemoveJWTToken();
        }

        public static string MaskCardNumber(this string content)
        {
            string result = content;
            string pattern = @"\b([3-6]\d{3})(?: *-* *\d{4}){2}";
            Match m = Regex.Match(content, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            if (m.Success)
            {
                string tempMValue = m.Value.Substring(0, 4) + " **** **** ";
                result = content.Replace(m.Value, tempMValue);
            }

            return result;
        }

        public static string RemoveBearerAuthorization(this string content)
        {
            return Regex.Replace(content, @"Bearer\s[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.?[A-Za-z0-9-_.+=]*", "Bearer ==DELETED==", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
        }

        public static string RemoveBasicAuthorization(this string content)
        {
            return Regex.Replace(content, @"Basic\s[a-zA-Z0-9=-_]+", "Basic ==DELETED==", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
        }

        public static string RemoveClientSecret(this string content)
        {
            return Regex.Replace(content, @"(\\""ClientSecret\\""[:=]\\"").*?(\\"")", @"$1==DELETED==$2", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
        }

        public static string RemoveJWTToken(this string content)
        {
            return Regex.Replace(content, @"(eyJ[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+)", "==TOKEN REMOVED==", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
        }

        public static string Crop(this string inp, int maxLen)
        {
            if (inp.Length > maxLen)
            {
                return $"{inp[..maxLen]} [CROPPED]";
            }
            else
            {
                return inp;
            }
        }

        public static string GetHeaderJson(this IHeaderDictionary headers, int maxLen)
        {
            var dict = new Dictionary<string, string>();
            var length = 2; // json başlangıç ve bitiş token sayısı: ( {} )
            const int jsonExtraCharsPerItem = 6; // string key ve string value'i sarmallayan token sayısı: ( "":"", )

            if (headers?.Count > 0)
            {
                foreach (var header in headers)
                {
                    var itemLength = (header.Key?.Length ?? 0) + (header.Value.Sum(f => (f ?? string.Empty).Length));
                    length += itemLength;
                    length += jsonExtraCharsPerItem;

                    if (length > maxLen)
                        break;

                    if (header.Key == "Authorization" && header.Value.ToString().Contains("Bearer"))
                    {
                        DeSerializeBearerToken(dict, header);
                    }
                    else if (header.Key != "Authorization")
                    {
                        AddToHeaderDictionary(dict, header.Key!, header.Value.ToString());
                    }
                }

                return JsonConvert.SerializeObject(dict, Formatting.None);
            }
            else
            {
                return emptyJson;
            }

            static void DeSerializeBearerToken(Dictionary<string, string> dict, KeyValuePair<string, StringValues> header)
            {
                try
                {
                    var jwtValue = header.Value.ToString().Replace("Bearer ", string.Empty);
                    var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtValue);
                    foreach (var claim in token.Claims)
                    {
                        AddToHeaderDictionary(dict, claim.Type, claim.Value);
                    }
                }
                catch (Exception)
                {
                    //Token malformed olmasi ihtimali oluyor. bu durumda tum logu yok etmemek icin.
                    AddToHeaderDictionary(dict, "sid", "Malformatted or Faulty JWT");
                }
            }
        }

        private static void AddToHeaderDictionary(Dictionary<string, string> dict, string key, string value)
        {
            if (key == null)
                return;

            if (dict.ContainsKey(key))
            {
                dict[key] = $"{dict[key]}, {value}";
            }
            else
            {
                dict.Add(key, value);
            }
        }

        public static string ReadPartialFromStream(this Stream body, long maxLength)
        {
            if (body == null) return null;
            if (!body.CanSeek) return null;

            int length = (int)Math.Min(body.Length, maxLength);

            body.Seek(0, SeekOrigin.Begin);
            string bodyText = "";

            if (body is RecyclableMemoryStream memory)
            {
                var seq = memory.GetReadOnlySequence();
                var slice = seq.Slice(0, length);
                bodyText = Encoding.UTF8.GetString(slice.ToArray());
            }
            else
            {
                //RecyclableMemoryStream GetReadOnlySequence async desteklemedigi icin alternatif senaryolarda
                //alltaki kodu kullanmak mumkun olamiyor. Ozellikle request stream de sorun
                //olmasi durumunda devreye alinmasi icin alternatif olarak birakiyorum
                //byte[] buffer = new byte[length]
                //await body.ReadAsync(buffer)
                //bodyText = Encoding.UTF8.GetString(buffer)
                throw new NotImplementedException();
            }

            body.Seek(0, SeekOrigin.Begin);
            return bodyText;
        }

        public static async Task<string> ReadStreamAsync(this Stream stream, long maxLength = 180 * 1024)
        {
            if (stream == null || !stream.CanRead) return string.Empty;

            var originalPosition = stream.CanSeek ? stream.Position : 0;
            
            try
            {
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);

                var length = (int)Math.Min(stream.Length > 0 ? stream.Length : maxLength, maxLength);
                var buffer = new byte[length];
                var bytesRead = await stream.ReadAsync(buffer, 0, length);
                
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            finally
            {
                if (stream.CanSeek) stream.Seek(originalPosition, SeekOrigin.Begin);
            }
        }

        public static string MaskSensitiveFields(this string content, IEnumerable<string> sensitiveFields)
        {
            if (string.IsNullOrEmpty(content) || sensitiveFields == null) return content;

            var result = content;
            foreach (var field in sensitiveFields)
            {
                // JSON field masking: "fieldName":"value" -> "fieldName":"***"
                var jsonPattern = $@"(""{field}"":\s*"")[^""]*("")";
                result = Regex.Replace(result, jsonPattern, @"$1***$2", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                
                // Form data masking: fieldName=value -> fieldName=***
                var formPattern = $@"({field}=)[^&\s]*";
                result = Regex.Replace(result, formPattern, @"$1***", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            }
            return result;
        }

        public static string FilterSensitiveHeaders(this IHeaderDictionary headers, IEnumerable<string> sensitiveHeaders, int maxLen)
        {
            var dict = new Dictionary<string, string>();
            var length = 2; // json başlangıç ve bitiş token sayısı
            const int jsonExtraCharsPerItem = 6;

            if (headers?.Count > 0)
            {
                foreach (var header in headers)
                {
                    var isSensitive = sensitiveHeaders?.Any(s => string.Equals(s, header.Key, StringComparison.OrdinalIgnoreCase)) == true;
                    
                    var itemLength = (header.Key?.Length ?? 0) + (isSensitive ? 3 : header.Value.Sum(f => (f ?? string.Empty).Length)); // 3 for "***"
                    length += itemLength + jsonExtraCharsPerItem;

                    if (length > maxLen) break;

                    if (header.Key == "Authorization" && header.Value.ToString().Contains("Bearer"))
                    {
                        DeSerializeBearerToken(dict, header);
                    }
                    else if (!isSensitive)
                    {
                        AddToHeaderDictionary(dict, header.Key!, header.Value.ToString());
                    }
                    else
                    {
                        AddToHeaderDictionary(dict, header.Key!, "***");
                    }
                }

                return JsonConvert.SerializeObject(dict, Formatting.None);
            }

            return emptyJson;

            static void DeSerializeBearerToken(Dictionary<string, string> dict, KeyValuePair<string, StringValues> header)
            {
                try
                {
                    var jwtValue = header.Value.ToString().Replace("Bearer ", string.Empty);
                    var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtValue);
                    foreach (var claim in token.Claims)
                    {
                        AddToHeaderDictionary(dict, claim.Type, claim.Value);
                    }
                }
                catch (Exception)
                {
                    AddToHeaderDictionary(dict, "sid", "Malformatted or Faulty JWT");
                }
            }
        }

        public static bool ShouldIgnorePath(this string path, IEnumerable<string> ignoredPaths)
        {
            if (string.IsNullOrEmpty(path) || ignoredPaths == null) return false;

            return ignoredPaths.Any(ignored =>
                path.StartsWith(ignored, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ignored, StringComparison.OrdinalIgnoreCase));
        }
    }
}

