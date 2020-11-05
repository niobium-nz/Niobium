using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class WindcaveIntegration
    {
        private const string Host = "https://sec.windcave.com/api/v1";
        private readonly Lazy<IConfigurationProvider> configuration;

        public WindcaveIntegration(Lazy<IConfigurationProvider> configuration) => this.configuration = configuration;

        public async Task<OperationResult<WindcaveCard>> QueryCardAsync(string transactionID)
        {
            var result = await this.QueryTransactionAsync(transactionID);
            if (!result.IsSuccess)
            {
                return new OperationResult<WindcaveCard>(result.Code)
                {
                    Reference = result.Reference,
                };
            }
            else
            {
                return new OperationResult<WindcaveCard>(result.Result.Card);
            }
        }

        internal async Task<OperationResult<WindcaveTransaction>> CreateTransactionAsync(
            PaymentKind kind,
            Currency currency,
            int amount,
            string reference,
            Uri notificationUri,
            string cardID)
            => await this.CreateTransactionAsync(Guid.NewGuid(), kind, currency, amount, reference, notificationUri, cardID, null, 3);

        internal async Task<OperationResult> CompleteTransactionAsync(
            Currency currency,
            int amount,
            string reference,
            Uri notificationUri,
            string transactionID)
            => await this.CreateTransactionAsync(Guid.NewGuid(), PaymentKind.Complete, currency, amount, reference, notificationUri, null, transactionID, 3);

        internal async Task<OperationResult<WindcaveTransaction>> QueryTransactionAsync(string transactionID)
        {
            var key = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_SECRET");
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            using var httprequest = new HttpRequestMessage(HttpMethod.Get, $"{Host}/transactions/{transactionID.Trim()}");
            var auth = Base64.Encode($"{key}:{secret}");
            httprequest.Headers.Authorization = new AuthenticationHeaderValue(Authentication.BasicLoginScheme, auth);
            var resp = await httpclient.SendAsync(httprequest);
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status == 200)
            {
                var result = JsonSerializer.DeserializeObject<WindcaveTransaction>(json);
                if (result.ID == transactionID)
                {
                    return new OperationResult<WindcaveTransaction>(result);
                }
            }

            if (Logger.Instance != null)
            {
                Logger.Instance.LogError($"An error occurred while trying to query WindCave payment transaction {transactionID}: {json}");
            }
            return new OperationResult<WindcaveTransaction>(InternalError.BadGateway) { Reference = json };
        }

        internal async Task<OperationResult<CreateWindcaveSessionResponse>> QuerySessionAsync(string id)
        {
            var key = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_SECRET");
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            using var httprequest = new HttpRequestMessage(HttpMethod.Get, $"{Host}/sessions/{id.Trim()}");
            var auth = Base64.Encode($"{key}:{secret}");
            httprequest.Headers.Authorization = new AuthenticationHeaderValue(Authentication.BasicLoginScheme, auth);
            var resp = await httpclient.SendAsync(httprequest);
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status == 202)
            {
                return new OperationResult<CreateWindcaveSessionResponse>(InternalError.PaymentRequired);
            }
            else if (status == 200)
            {
                var result = JsonSerializer.DeserializeObject<CreateWindcaveSessionResponse>(json);
                if (result.ID == id)
                {
                    return new OperationResult<CreateWindcaveSessionResponse>(result);
                }
            }

            if (Logger.Instance != null)
            {
                Logger.Instance.LogError($"An error occurred while trying to query WindCave payment session {id}: {json}");
            }
            return new OperationResult<CreateWindcaveSessionResponse>(InternalError.BadGateway) { Reference = json };
        }

        internal async Task<OperationResult<PaymentSession>> CreateSessionAsync(
            PaymentKind kind,
            Currency currency,
            int amount,
            string reference,
            Uri notificationUri,
            Uri approvedUri,
            Uri declinedUri,
            Uri canceledUri)
            => await this.CreateSessionAsync(Guid.NewGuid(), kind, currency, amount, reference, notificationUri, approvedUri, declinedUri, canceledUri, 3);

        private async Task<OperationResult<WindcaveTransaction>> CreateTransactionAsync(
            Guid requestID,
            PaymentKind kind,
            Currency currency,
            int amount,
            string reference,
            Uri notificationUri,
            string cardID,
            string transactionID,
            int retryCount)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<WindcaveTransaction>(InternalError.BadGateway);
            }
            var key = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_SECRET");
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false)
            {
#if !DEBUG
                Timeout = TimeSpan.FromSeconds(15),
#endif
            };
            var request = new CreateWindcaveTransactionRequest(kind, currency, amount, transactionID: transactionID, cardID: cardID)
            {
                MerchantReference = reference,
                NotificationUrl = notificationUri.AbsoluteUri,
            };
            var data = JsonSerializer.SerializeObject(request, JsonSerializationFormat.CamelCase);
            using var content = new StringContent(data, Encoding.UTF8, "application/json");
            using var httprequest = new HttpRequestMessage(HttpMethod.Post, $"{Host}/transactions")
            {
                Content = content,
            };
            var auth = Base64.Encode($"{key}:{secret}");
            httprequest.Headers.Authorization = new AuthenticationHeaderValue(Authentication.BasicLoginScheme, auth);
            httprequest.Headers.TryAddWithoutValidation("X-ID", requestID.ToString("N"));
            try
            {
                var resp = await httpclient.SendAsync(httprequest);
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonSerializer.DeserializeObject<WindcaveTransaction>(json);
                    if (!String.IsNullOrWhiteSpace(result.ID))
                    {
                        return new OperationResult<WindcaveTransaction>(result);
                    }
                    else
                    {
                        if (Logger.Instance != null)
                        {
                            Logger.Instance.LogError($"An server error occurred while trying to create WindCave payment session {kind}->{currency}${amount} on {reference} with status code={status}: {json}");
                        }
                    }
                }
                else if (status >= 400 && status < 500)
                {
                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An client error occurred while trying to create WindCave payment session: {data}");
                    }
                    return new OperationResult<WindcaveTransaction>(InternalError.BadRequest) { Reference = json };
                }
                else
                {
                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An upstream error occurred while trying to create WindCave payment session: {data}");
                    }
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.CreateTransactionAsync(requestID, kind, currency, amount, reference, notificationUri, cardID, transactionID, --retryCount);
        }

        private async Task<OperationResult<PaymentSession>> CreateSessionAsync(
            Guid requestID,
            PaymentKind kind,
            Currency currency,
            int amount,
            string reference,
            Uri notificationUri,
            Uri approvedUri,
            Uri declinedUri,
            Uri canceledUri,
            int retryCount)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentSession>(InternalError.BadGateway);
            }

            var key = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("WINDCAVE_SECRET");

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var request = new CreateWindcaveSessionRequest(kind, currency, amount)
            {
                MerchantReference = reference,
                NotificationUrl = notificationUri.AbsoluteUri,
                CallbackUrls = new WindcaveRedirectionDefinition
                {
                    Approved = approvedUri.AbsoluteUri,
                    Declined = declinedUri.AbsoluteUri,
                    Cancelled = canceledUri.AbsoluteUri,
                }
            };
            var data = JsonSerializer.SerializeObject(request, JsonSerializationFormat.CamelCase);
            using var content = new StringContent(data, Encoding.UTF8, "application/json");
            using var httprequest = new HttpRequestMessage(HttpMethod.Post, $"{Host}/sessions")
            {
                Content = content,
            };
            var auth = Base64.Encode($"{key}:{secret}");
            httprequest.Headers.Authorization = new AuthenticationHeaderValue(Authentication.BasicLoginScheme, auth);
            httprequest.Headers.TryAddWithoutValidation("X-ID", requestID.ToString("N"));

            try
            {
                var resp = await httpclient.SendAsync(httprequest);
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status == 202)
                {
                    var result = JsonSerializer.DeserializeObject<CreateWindcaveSessionResponse>(json);
                    if (!String.IsNullOrWhiteSpace(result.ID) && result.GetSubmitCardLink() != null)
                    {
                        return new OperationResult<PaymentSession>(new PaymentSession
                        {
                            ID = result.ID,
                            SubmitCardLink = result.GetSubmitCardLink()
                        });
                    }
                    else
                    {
                        if (Logger.Instance != null)
                        {
                            Logger.Instance.LogError($"An server error occurred while trying to create WindCave payment session {kind}->{currency}${amount} on {reference} with status code={status}: {json}");
                        }
                    }
                }
                else if (status == 200)
                {
                    requestID = Guid.NewGuid();
                }
                else if (status >= 400 && status < 500)
                {
                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An client error occurred while trying to create WindCave payment session: {data}");
                    }

                    return new OperationResult<PaymentSession>(InternalError.BadRequest) { Reference = json };
                }
                else
                {
                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An upstream error occurred while trying to create WindCave payment session: {data}");
                    }
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.CreateSessionAsync(requestID, kind, currency, amount, reference, notificationUri, approvedUri, declinedUri, canceledUri, --retryCount);
        }
    }
}
