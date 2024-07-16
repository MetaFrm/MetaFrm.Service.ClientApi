using MetaFrm.Api.Models;
using MetaFrm.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MetaFrm.Service
{
    /// <summary>
    /// ClientApi
    /// </summary>
    public class ClientApi : IService, IServiceAsync, ILoginService, ILoginServiceAsync, IAccessCodeService, IAccessCodeServiceAsync
    {
        private readonly List<ServiceInfo> listServicePool;
        private readonly int servicePoolMaxCount;
        private int tryConnectCount;
        private readonly Uri BaseAddress;
        private readonly JsonSerializerOptions JsonSerializerOptions;

        /// <summary>
        /// ClientApi 생성자
        /// </summary>
        public ClientApi()
        {
            this.listServicePool = new();
            this.tryConnectCount = 0;

            try
            {
                this.servicePoolMaxCount = this.GetAttributeInt("ServicePoolMaxCount");
            }
            catch(Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));
                this.servicePoolMaxCount = 1;
            }

            this.BaseAddress = new Uri(this.GetAttribute("BaseAddress"));

            this.JsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
            };
        }

        Response IService.Request(ServiceData serviceData)
        {
            ServiceInfo? serviceInfo;
            HttpResponseMessage response;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", serviceData.Token);
                response = serviceInfo.HttpClient.PostAsJsonAsync("api/Service", serviceData, this.JsonSerializerOptions).Result;
                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsAsync<Response>().Result;
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));

                if (this.tryConnectCount > 3)
                    throw new MetaFrmException(exception);

                this.tryConnectCount += 1;

                if (serviceInfo != null)
                    this.RemoveService(serviceInfo);

                return ((IService)this).Request(serviceData);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }

        async Task<Response> IServiceAsync.RequestAsync(ServiceData serviceData)
        {
            ServiceInfo? serviceInfo;
            HttpResponseMessage response;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", serviceData.Token);
                response = await serviceInfo.HttpClient.PostAsJsonAsync("api/Service", serviceData, this.JsonSerializerOptions);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<Response>();
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));

                if (this.tryConnectCount > 3)
                    throw new MetaFrmException(exception);

                this.tryConnectCount += 1;

                if (serviceInfo != null)
                    this.RemoveService(serviceInfo);

                return await ((IServiceAsync)this).RequestAsync(serviceData);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }


        private ServiceInfo GetService()
        {
            var service = from Tmp in this.listServicePool
                          where Tmp.IsBusy.Equals(false)
                          select Tmp;

            if (!service.Any())
            {
                if (this.listServicePool.Count >= this.servicePoolMaxCount)
                {
                    DiagnosticsTool.MyTrace(new MetaFrmException("서비스 풀이 가득 찼습니다."));

                    return this.listServicePool[0];
                }
                else
                {
                    this.CreateService();
                    return this.GetService();
                }
            }
            else
            {
                return service.First();
            }
        }

        private void CreateService()
        {
            HttpClient client = new()
            {
                BaseAddress = this.BaseAddress
            };

            this.listServicePool.Add(new ServiceInfo(client));
        }

        private void RemoveService(ServiceInfo serviceInfo)
        {
            this.listServicePool.RemoveAll(x => x.Equals(serviceInfo));
        }

        UserInfo ILoginService.Login(string email, string password)
        {
            ServiceInfo? serviceInfo;
            HttpResponseMessage response;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                var values = new List<KeyValuePair<string, string>> { new(nameof(email), email), new(nameof(password), password) };

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", Factory.AccessKey);
                response = serviceInfo.HttpClient.PostAsJsonAsync($"api/Login?email={email}&password={password}", new FormUrlEncodedContent(values), this.JsonSerializerOptions).Result;
                response.EnsureSuccessStatusCode();

                var userInfo = response.Content.ReadAsAsync<UserInfo>().Result;

                if (userInfo.Status == Status.OK)
                {
                    this.listServicePool.Remove(serviceInfo);
                }

                return userInfo;
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));

                if (this.tryConnectCount > 3)
                    throw new MetaFrmException(exception);

                this.tryConnectCount += 1;

                if (serviceInfo != null)
                    this.RemoveService(serviceInfo);

                return ((ILoginService)this).Login(email, password);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }
        async Task<UserInfo> ILoginServiceAsync.LoginAsync(string email, string password)
        {
            ServiceInfo? serviceInfo;
            HttpResponseMessage response;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                var values = new List<KeyValuePair<string, string>> { new(nameof(email), email), new(nameof(password), password) };

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", Factory.AccessKey);
                response = await serviceInfo.HttpClient.PostAsJsonAsync($"api/Login?email={email}&password={password}", values, this.JsonSerializerOptions);
                response.EnsureSuccessStatusCode();

                var userInfo = await response.Content.ReadAsAsync<UserInfo>();

                if (userInfo.Status == Status.OK)
                {
                    this.listServicePool.Remove(serviceInfo);
                }

                return userInfo;
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));

                if (this.tryConnectCount > 3)
                    throw new MetaFrmException(exception);

                this.tryConnectCount += 1;

                if (serviceInfo != null)
                    this.RemoveService(serviceInfo);

                return await((ILoginServiceAsync)this).LoginAsync(email, password);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }

        string IAccessCodeService.GetJoinAccessCode(string email)
        {
            ServiceInfo? serviceInfo;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", Factory.AccessKey);
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("accessGroup", "JOIN");

                return serviceInfo.HttpClient.GetStringAsync($"api/AccessCode?email={email}").Result.AesDecryptorToBase64String(Factory.AccessKey, "MetaFrm");
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));
                throw new MetaFrmException(exception);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }
        async Task<string> IAccessCodeServiceAsync.GetJoinAccessCodeAsync(string email)
        {
            ServiceInfo? serviceInfo;
            string accessCode;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", Factory.AccessKey);
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("accessGroup", "JOIN");

                accessCode = await serviceInfo.HttpClient.GetStringAsync($"api/AccessCode?email={email}&accessGroup=JOIN");
                return accessCode.AesDecryptorToBase64String(Factory.AccessKey, "MetaFrm");
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));
                throw new MetaFrmException(exception);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }

        string IAccessCodeService.GetAccessCode(string token, string email, string accessGroup)
        {
            ServiceInfo? serviceInfo;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;
                
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", token);
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("accessGroup", accessGroup);

                return serviceInfo.HttpClient.GetStringAsync($"api/AccessCode?email={email}").Result.AesDecryptorToBase64String(token, "MetaFrm");
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));
                throw new MetaFrmException(exception);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }
        async Task<string> IAccessCodeServiceAsync.GetAccessCodeAsync(string token, string email, string accessGroup)
        {
            ServiceInfo? serviceInfo;
            string accessCode;

            serviceInfo = null;

            try
            {
                serviceInfo = this.GetService();
                serviceInfo.IsBusy = true;

                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Clear();
                serviceInfo.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("token", token);
                serviceInfo.HttpClient.DefaultRequestHeaders.Add("accessGroup", accessGroup);

                accessCode = await serviceInfo.HttpClient.GetStringAsync($"api/AccessCode?email={email}");
                return accessCode.AesDecryptorToBase64String(token, "MetaFrm");
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(new MetaFrmException(exception));
                throw new MetaFrmException(exception);
            }
            finally
            {
                serviceInfo?.End();
                this.tryConnectCount = 0;//정상적으로 처리가 되면 재시도 횟수 초기화
            }
        }
    }
}