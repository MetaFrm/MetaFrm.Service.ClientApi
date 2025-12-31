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
                WriteIndented = false,
            };
        }

        /// <summary>
        /// ClientApi 생성자
        /// </summary>
        public ClientApi() { }

        Response IService.Request(ServiceData serviceData)
        {
            return ((IServiceAsync)this).RequestAsync(serviceData).GetAwaiter().GetResult();
        }

        async Task<Response> IServiceAsync.RequestAsync(ServiceData serviceData)
        {
            try
            {
                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/Service")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", serviceData.Token },
                    },
                    Content = new StringContent(JsonSerializer.Serialize(serviceData, JsonSerializerOptions), Encoding.UTF8, "application/json")
                };

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadFromJsonAsync<Response>(JsonSerializerOptions) ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        UserInfo ILoginService.Login(string email, string password)
        {
            return ((ILoginServiceAsync)this).LoginAsync(email, password).GetAwaiter().GetResult();
        }
        async Task<UserInfo> ILoginServiceAsync.LoginAsync(string email, string password)
        {
            try
            {
                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/Login?email={email}&password={password}")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", Factory.AccessKey },
                    },
                    Content = JsonContent.Create(new { email, password })
                };

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadFromJsonAsync<UserInfo>().ConfigureAwait(false) ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        string IAccessCodeService.GetJoinAccessCode(string email)
        {
            return ((IAccessCodeServiceAsync)this).GetJoinAccessCodeAsync(email).GetAwaiter().GetResult();
        }
        async Task<string> IAccessCodeServiceAsync.GetJoinAccessCodeAsync(string email)
        {
            try
            {
                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AccessCode?email={email}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", Factory.AccessKey },
                        { "accessGroup", "JOIN" },
                    }
                };

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await (await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false)).AesDecryptorToBase64StringAsync(Factory.AccessKey, "MetaFrm").ConfigureAwait(false);
                else
                    return "";
            }
            catch (Exception exception)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }

        string IAccessCodeService.GetAccessCode(string token, string email, string accessGroup)
        {
            return ((IAccessCodeServiceAsync)this).GetAccessCodeAsync(token, email, accessGroup).GetAwaiter().GetResult();
        }
        async Task<string> IAccessCodeServiceAsync.GetAccessCodeAsync(string token, string email, string accessGroup)
        {
            try
            {
                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AccessCode?email={email}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", token },
                        { "accessGroup", accessGroup },
                    }
                };

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await (await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false)).AesDecryptorToBase64StringAsync(token, "MetaFrm").ConfigureAwait(false);
                else
                    return "";
            }
            catch (Exception exception)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{Message}", exception.Message);
                throw new MetaFrmException(exception);
            }
        }
    }
}