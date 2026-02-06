using MetaFrm.Api;
using MetaFrm.Api.Models;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;

namespace MetaFrm.Service
{
    /// <summary>
    /// ClientApi
    /// </summary>
    public class ClientApi : IService, ILoginService, IAccessCodeService
    {
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
            return ((IService)this).RequestAsync(serviceData).GetAwaiter().GetResult();
        }

        async Task<Response> IService.RequestAsync(ServiceData serviceData)
        {
            try
            {
                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/Service")
                {
                    Headers = { { HeaderNames.Accept, MediaTypeNames.Application.Json } },
                    Content = JsonContent.Create(serviceData, options: JsonSerializerOptions)
                };

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, Factory.AccessKey == serviceData.Token ? Factory.ProjectService.Token : serviceData.Token);

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadFromJsonAsync<Response>(JsonSerializerOptions).ConfigureAwait(false) ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                Factory.Logger.Error(exception, "{0}", JsonSerializer.Serialize(serviceData));

                throw new MetaFrmException(exception);
            }
        }

        UserInfo ILoginService.Login(string email, string password)
        {
            return ((ILoginService)this).LoginAsync(email, password).GetAwaiter().GetResult();
        }
        async Task<UserInfo> ILoginService.LoginAsync(string email, string password)
        {
            try
            {
                email = await email.AesEncryptToBase64StringAsync(Factory.ProjectService.Token!, Auth.AuthType.Login);
                password = await (await password.ComputeHashAsync()).AesEncryptToBase64StringAsync(email, Factory.ProjectService.Token!);

                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/Login")
                {
                    Headers = { { HeaderNames.Accept, MediaTypeNames.Application.Json } },
                    Content = JsonContent.Create(new Login { Email = email, Password = password }, options: JsonSerializerOptions)
                };

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, Factory.ProjectService.Token);

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadFromJsonAsync<UserInfo>(JsonSerializerOptions).ConfigureAwait(false) ?? new();
                else
                    return new();
            }
            catch (Exception exception)
            {
                Factory.Logger.Error(exception, "{0}", email);

                throw new MetaFrmException(exception);
            }
        }

        string IAccessCodeService.GetJoinAccessCode(string email)
        {
            return ((IAccessCodeService)this).GetAccessCodeAsync(Factory.ProjectService.Token!, email, Auth.AuthType.Join).GetAwaiter().GetResult();
        }
        async Task<string> IAccessCodeService.GetJoinAccessCodeAsync(string email)
        {
            return await ((IAccessCodeService)this).GetAccessCodeAsync(Factory.ProjectService.Token!, email, Auth.AuthType.Join).ConfigureAwait(false);
        }

        string IAccessCodeService.GetAccessCode(string token, string email, string accessGroup)
        {
            return ((IAccessCodeService)this).GetAccessCodeAsync(token, email, accessGroup).GetAwaiter().GetResult();
        }
        async Task<string> IAccessCodeService.GetAccessCodeAsync(string token, string email, string accessGroup)
        {
            try
            {
                email = email.AesEncryptToBase64String(token, accessGroup);

                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/AccessCode")
                {
                    Headers = { { HeaderNames.Accept, MediaTypeNames.Application.Json }, { Headers.AccessGroup, accessGroup } },
                    Content = JsonContent.Create(email, options: JsonSerializerOptions)
                };

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, token);

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var accessCode = await httpResponseMessage.Content.ReadFromJsonAsync<string>().ConfigureAwait(false);

                    if (accessCode != null)
                        return await accessCode.AesDecryptorToBase64StringAsync(token, accessGroup).ConfigureAwait(false);
                    else
                        return "";
                }
                else
                    return "";
            }
            catch (Exception exception)
            {
                Factory.Logger.Error(exception, "{0}, {1}, {2}", token, email, accessGroup);

                throw new MetaFrmException(exception);
            }
        }
    }
}