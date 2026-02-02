using MetaFrm.Api;
using MetaFrm.Api.Models;
using MetaFrm.Data;
using Microsoft.Extensions.Logging;
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
            ServiceDataShort serviceDataShort = new()
            {
                S = serviceData.ServiceName,
                Ts = serviceData.TransactionScope,
                T = serviceData.Token,
            };

            foreach (var c in serviceData.Commands)
            {
                Dictionary<string, ParameterShort> parameters = [];

                foreach (var p in c.Value.Parameters)
                {
                    parameters.Add(p.Key, new()
                    {
                        T = p.Value.DbType,
                        S = p.Value.Size,
                        C = p.Value.TargetCommandName,
                        P = p.Value.TargetParameterName,
                    });
                }

                Dictionary<int, Dictionary<string, DataValueShort>> values = [];

                foreach (var v in c.Value.Values)
                {
                    Dictionary<string, DataValueShort> dv = [];

                    foreach (var item in v.Value)
                    {
                        DataTableShort? dataTableValue = null;

                        if (item.Value.DataTableValue != null)
                        {
                            List<DataColumnShort> dataColumns = [];

                            foreach (var col in item.Value.DataTableValue.DataColumns)
                            {
                                dataColumns.Add(new()
                                {
                                    F = col.FieldName,
                                    C = col.Caption,
                                    N = col.DataTypeFullNamespace,
                                });
                            }

                            List<DataRowShort> dataRows = [];

                            foreach (var row in item.Value.DataTableValue.DataRows)
                            {
                                Dictionary<string, DataValueShort> dataValues = [];

                                foreach (var vs in row.Values)
                                {
                                    dataValues.Add(vs.Key, new()
                                    {
                                        Vt = item.Value.ValueType,
                                        C = item.Value.CharValue,
                                        Cs= item.Value.CharsValue,
                                        B = item.Value.ByteValue,
                                        Bs = item.Value.BytesValue,
                                        Dt = item.Value.DateTimeValue,
                                        D = item.Value.DecimalValue,
                                        Do = item.Value.DoubleValue,

                                        F = item.Value.FloatValue,
                                        I = item.Value.IntValue,
                                        L = item.Value.LongValue,
                                        Sb = item.Value.SbyteValue,
                                        Sbs = item.Value.SbytesValue,
                                        St = item.Value.ShortValue,

                                        S = item.Value.StringValue,
                                        Ui = item.Value.UintValue,
                                        Ul = item.Value.UlongValue,
                                        Us = item.Value.UshortValue,
                                        Bl = item.Value.BooleanValue,

                                        G = item.Value.GuidValue,
                                        Ts = item.Value.TimeSpanValue,
                                        O = item.Value.DateTimeOffsetValue,
                                        J = item.Value.JsonValue,
                                        V = item.Value.VectorValue,
                                    });
                                }

                                dataRows.Add(new()
                                {
                                    V = dataValues,
                                });
                            }

                            dataTableValue = new DataTableShort()
                            {
                                N = item.Value.DataTableValue.DataTableName,
                                C = dataColumns,
                                R = dataRows,
                            }
                            ;
                        }

                        dv.Add(item.Key, new()
                        {
                            Vt = item.Value.ValueType,
                            C = item.Value.CharValue,
                            Cs = item.Value.CharsValue,
                            B = item.Value.ByteValue,
                            Bs = item.Value.BytesValue,
                            Dt = item.Value.DateTimeValue,
                            D = item.Value.DecimalValue,
                            Do = item.Value.DoubleValue,

                            F = item.Value.FloatValue,
                            I = item.Value.IntValue,
                            L = item.Value.LongValue,
                            Sb = item.Value.SbyteValue,
                            Sbs = item.Value.SbytesValue,
                            St = item.Value.ShortValue,

                            S = item.Value.StringValue,
                            Ui = item.Value.UintValue,
                            Ul = item.Value.UlongValue,
                            Us = item.Value.UshortValue,
                            Bl = item.Value.BooleanValue,

                            G = item.Value.GuidValue,
                            T = dataTableValue,
                            Ts = item.Value.TimeSpanValue,
                            O = item.Value.DateTimeOffsetValue,
                            J = item.Value.JsonValue,
                            V = item.Value.VectorValue,
                        }
                        );
                    }

                    values.Add(v.Key, dv);
                }

                serviceDataShort.C.Add(c.Key, new()
                {
                    N = c.Value.ConnectionName,
                    C = c.Value.CommandText,
                    T = c.Value.CommandType,
                    P = parameters,
                    V = values,
                });
            }





            try
            {
                using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/Service")
                {
                    Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
                    },
                    Content = JsonContent.Create(serviceDataShort)
                };

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, Factory.AccessKey == serviceData.Token ? Factory.ProjectService.Token : serviceData.Token);

                using HttpResponseMessage httpResponseMessage = await Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    ResponseShort? responseShort = await httpResponseMessage.Content.ReadFromJsonAsync<ResponseShort>(JsonSerializerOptions).ConfigureAwait(false) ?? new();



                    DataSet? dataSet = null;

                    if (responseShort.D != null)
                    {
                        List<DataTable> dataTables = [];

                        foreach (var table in responseShort.D.T)
                        {
                            List<DataColumn> dataColumns = [];

                            foreach (var col in table.C)
                            {
                                dataColumns.Add(new()
                                {
                                    FieldName = col.F,
                                    Caption = col.C,
                                    DataTypeFullNamespace = col.N,
                                });
                            }

                            List<DataRow> dataRows = [];

                            foreach (var row in table.R)
                            {
                                Dictionary<string, DataValue> dv = [];

                                foreach (var dataValue in row.V)
                                {
                                    dv.Add(dataValue.Key, new()
                                    {
                                        ValueType = dataValue.Value.Vt,
                                        CharValue = dataValue.Value.C,
                                        CharsValue = dataValue.Value.Cs,
                                        ByteValue = dataValue.Value.B,
                                        BytesValue = dataValue.Value.Bs,
                                        DateTimeValue = dataValue.Value.Dt,
                                        DecimalValue = dataValue.Value.D,
                                        DoubleValue = dataValue.Value.Do,

                                        FloatValue = dataValue.Value.F,
                                        IntValue = dataValue.Value.I,
                                        LongValue = dataValue.Value.L,
                                        SbyteValue = dataValue.Value.Sb,
                                        SbytesValue = dataValue.Value.Sbs,
                                        ShortValue = dataValue.Value.St,

                                        StringValue = dataValue.Value.S,
                                        UintValue = dataValue.Value.Ui,
                                        UlongValue = dataValue.Value.Ul,
                                        UshortValue = dataValue.Value.Us,
                                        BooleanValue = dataValue.Value.Bl,

                                        GuidValue = dataValue.Value.G,
                                        TimeSpanValue = dataValue.Value.Ts,
                                        DateTimeOffsetValue = dataValue.Value.O,
                                        JsonValue = dataValue.Value.J,
                                        VectorValue = dataValue.Value.V,
                                    });
                                }

                                dataRows.Add(new()
                                {
                                    Values = dv,
                                });
                            }

                            dataTables.Add(new()
                            {
                                DataTableName = table.N,
                                DataColumns = dataColumns,
                                DataRows = dataRows,
                            });
                        }

                        dataSet = new()
                        {
                            DataTables = dataTables,
                        };
                    }

                    return new()
                    {
                        Status = responseShort.S,
                        Message = responseShort.M,
                        DataSet = dataSet,
                    };
                }
                else
                    return new();
            }
            catch (Exception exception)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{serviceData}", JsonSerializer.Serialize(serviceData));

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
                    Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
                    },
                    Content = JsonContent.Create(new Login { Email = email, Password = password })
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
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{email}", email);

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
                    Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
                        { Headers.AccessGroup, accessGroup },
                    },
                    Content = JsonContent.Create(email)
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
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(exception, "{token}, {email}, {accessGroup}", token, email, accessGroup);

                throw new MetaFrmException(exception);
            }
        }
    }
}