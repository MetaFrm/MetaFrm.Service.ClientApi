using MetaFrm.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MetaFrm.Service
{
    /// <summary>
    /// ClientApi
    /// </summary>
    public class ClientApi : IService, IServiceAsync, ILoginService, ILoginServiceAsync, IAccessCodeService, IAccessCodeServiceAsync
    {
        //private readonly List<ServiceInfo> listServicePool;
        //private readonly int servicePoolMaxCount;
        //private int tryConnectCount;
        //private readonly Uri BaseAddress;
        private static readonly JsonSerializerOptions JsonSerializerOptions;

        static ClientApi()
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
            };
        }

        /// <summary>
        /// ClientApi 생성자
        /// </summary>
        public ClientApi() { }

        Response IService.Request(ServiceData serviceData)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/Service")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", serviceData.Token },
                    },
                    Content = new StringContent(JsonSerializer.Serialize(serviceData, JsonSerializerOptions), Encoding.UTF8, "application/json")
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                    return httpResponseMessage.Content.ReadFromJsonAsync<Response>().Result ?? new();
                else 
                    return new();
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        async Task<Response> IServiceAsync.RequestAsync(ServiceData serviceData)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/Service")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", serviceData.Token },
                    },
                    Content = new StringContent(JsonSerializer.Serialize(serviceData, JsonSerializerOptions), Encoding.UTF8, "application/json")
                };

                HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadFromJsonAsync<Response>() ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        UserInfo ILoginService.Login(string email, string password)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/Login?email={email}&password={password}")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", Factory.AccessKey },
                    },
                    Content = new FormUrlEncodedContent([new(nameof(email), email), new(nameof(password), password)])
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                    return httpResponseMessage.Content.ReadFromJsonAsync<UserInfo>().Result ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }
        async Task<UserInfo> ILoginServiceAsync.LoginAsync(string email, string password)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/Login?email={email}&password={password}")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", Factory.AccessKey },
                    },
                    Content = new FormUrlEncodedContent([new(nameof(email), email), new(nameof(password), password)])
                };

                HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadFromJsonAsync<UserInfo>() ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        string IAccessCodeService.GetJoinAccessCode(string email)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AccessCode?email={email}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", Factory.AccessKey },
                        { "accessGroup", "JOIN" },
                    }
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                    return httpResponseMessage.Content.ReadAsStringAsync().Result.AesDecryptorToBase64String(Factory.AccessKey, "MetaFrm");
                else
                    return "";
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }
        async Task<string> IAccessCodeServiceAsync.GetJoinAccessCodeAsync(string email)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AccessCode?email={email}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", Factory.AccessKey },
                        { "accessGroup", "JOIN" },
                    }
                };

                HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await (await httpResponseMessage.Content.ReadAsStringAsync()).AesDecryptorToBase64StringAsync(Factory.AccessKey, "MetaFrm");
                else
                    return "";
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        string IAccessCodeService.GetAccessCode(string token, string email, string accessGroup)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AccessCode?email={email}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", token },
                        { "accessGroup", accessGroup },
                    }
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                    return httpResponseMessage.Content.ReadAsStringAsync().Result.AesDecryptorToBase64String(token, "MetaFrm");
                else
                    return "";
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }
        async Task<string> IAccessCodeServiceAsync.GetAccessCodeAsync(string token, string email, string accessGroup)
        {
            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AccessCode?email={email}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", token },
                        { "accessGroup", accessGroup },
                    }
                };

                HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await (await httpResponseMessage.Content.ReadAsStringAsync()).AesDecryptorToBase64StringAsync(token, "MetaFrm");
                else
                    return "";
            }
            catch (Exception exception)
            {
                Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }
    }
}