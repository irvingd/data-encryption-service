using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataEncryptionService.Telemetry;
using DataEncryptionService.WebApi.Models;
using DataEncryptionService.WebApi.Telemetry;
using DataEncryptionService.WebApi.Telemetry.Names;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataEncryptionService.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("data")]
    public class DataServiceController : ControllerBase
    {
        private readonly ITelemetrySourceClient _telemetry;
        private readonly ILogger _log;
        private readonly IDataEncryptionManager _dataManager;

        public DataServiceController(IDataEncryptionManager dataManager, ILogger<DataServiceController> log, ITelemetrySourceClient telemetry)
        {
            _telemetry = telemetry;
            _log = log;
            _dataManager = dataManager;
        }

        [HttpPost("encrypt")]
        public async Task<ActionResult<DataEncryptResponse>> PostDataEncrypt([FromBody] ApiDataEncryptRequest apiRequest)
        {
            ObjectResult result = null;
            var apiResponse = new ApiDataEncryptResponse() { RequestId = Guid.NewGuid().ToString() };
            var spans = new List<TelemetrySpan>();
            try
            {
                using (SpanMeasure.Start(SpanName.Data_Encryption_Request, spans))
                {
                    if (Validation.IsInvalid(apiRequest))
                    {
                        apiResponse.ErrorMessage = ErrorMessages.BadRequest;
                        result = BadRequest(ApiErrorResponse.FromResponse(apiResponse));
                    }
                    else
                    {
                        try
                        {
                            DataEncryptRequest request = apiRequest.ToRequest();
                            DataEncryptResponse response = await _dataManager.EncryptDataAsync(request);
                            apiResponse = response.ToApiResponse();
                            if (apiResponse.ErrorCode > 0)
                            {
                                result = StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                            }
                            else
                            {
                                result = new OkObjectResult(apiResponse);
                            }
                        }
                        catch (Exception e)
                        {
                            _log.LogError(e, e.Message, apiResponse.RequestId);
                            apiResponse.ErrorMessage = ErrorMessages.GeneralException;
                            result = StatusCode(StatusCodes.Status500InternalServerError, ApiErrorResponse.FromResponse(apiResponse));
                        }
                    }
                }
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>
                {
                    { EventAttributes.RequestingUserName, User?.Identity?.Name },
                    { EventAttributes.ResponseStatusCode, result.StatusCode }
                };

                await _telemetry.RaiseEventAsync(EventName.WebApiDataEncryptRequestCompleted, spans, apiResponse.RequestId, eventAttributes);
            }

            return result;
        }

        [HttpPost("decrypt")]
        public async Task<ActionResult<ApiDataDecryptResponse>> PostDataDecrypt([FromBody] ApiDataDecryptRequest apiRequest)
        {
            ObjectResult result = null;
            var apiResponse = new ApiDataDecryptResponse() { RequestId = Guid.NewGuid().ToString() };
            var spans = new List<TelemetrySpan>();
            try
            {
                using (SpanMeasure.Start(SpanName.Data_Decryption_Request, spans))
                {
                    if (Validation.IsInvalid(apiRequest))
                    {
                        apiResponse.ErrorMessage = ErrorMessages.BadRequest;
                        result = BadRequest(ApiErrorResponse.FromResponse(apiResponse));
                    }
                    else
                    {
                        try
                        {
                            DataDecryptRequest request = apiRequest.ToRequest();
                            DataDecryptResponse response = await _dataManager.DecryptDataAsync(request);
                            apiResponse = response.ToApiResponse();
                            if (apiResponse.ErrorCode > 0)
                            {
                                result = StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                            }
                            else
                            {
                                result = new OkObjectResult(apiResponse);
                            }
                        }
                        catch (Exception e)
                        {
                            _log.LogError(e, e.Message, apiResponse.RequestId);
                            apiResponse.ErrorMessage = ErrorMessages.GeneralException;
                            result = StatusCode(StatusCodes.Status500InternalServerError, ApiErrorResponse.FromResponse(apiResponse));
                        }
                    }
                }
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>
                {
                    { EventAttributes.RequestingUserName, User?.Identity?.Name },
                    { EventAttributes.ResponseStatusCode, result.StatusCode }
                };

                await _telemetry.RaiseEventAsync(EventName.WebApiDataDecryptRequestCompleted, spans, apiResponse.RequestId, eventAttributes);
            }

            return result;
        }

        [HttpDelete("delete")]
        public async Task<ActionResult<ApiDataDeleteResponse>> DeleteData([FromBody] ApiDataDeleteRequest apiRequest)
        {
            ObjectResult result = null;
            var apiResponse = new ApiDataDeleteResponse() { RequestId = Guid.NewGuid().ToString() };
            var spans = new List<TelemetrySpan>();
            try
            {
                using (SpanMeasure.Start(SpanName.Data_Delete_Request, spans))
                {
                    if (Validation.IsInvalid(apiRequest))
                    {
                        apiResponse.ErrorMessage = ErrorMessages.BadRequest;
                        result = BadRequest(ApiErrorResponse.FromResponse(apiResponse));
                    }
                    else
                    {
                        try
                        {
                            DataDeleteRequest request = apiRequest.ToRequest();
                            DataDeleteResponse response = await _dataManager.DeleteDataAsync(request);
                            apiResponse = response.ToApiResponse();
                            if (apiResponse.ErrorCode > 0)
                            {
                                result = StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                            }
                            else
                            {
                                result = new OkObjectResult(apiResponse);
                            }
                        }
                        catch (Exception e)
                        {
                            _log.LogError(e, e.Message, apiResponse.RequestId);
                            apiResponse.ErrorMessage = ErrorMessages.GeneralException;
                            result = StatusCode(StatusCodes.Status500InternalServerError, ApiErrorResponse.FromResponse(apiResponse));
                        }
                    }
                }
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>
                {
                    { EventAttributes.RequestingUserName, User?.Identity?.Name },
                    { EventAttributes.ResponseStatusCode, result.StatusCode }
                };

                await _telemetry.RaiseEventAsync(EventName.WebApiDataDeleteRequestCompleted, spans, apiResponse.RequestId, eventAttributes);
            }

            return result;
        }

        private static class EventAttributes
        {
            public const string RequestingUserName = "RequestingUserName";
            public const string ResponseStatusCode = "ResponseStatusCode";
        }

        private static class ErrorMessages
        {
            public const string BadRequest = "Invalid or missing parameters for this request.";
            public const string GeneralException = "An internal error occured while processing the request.";
        }
    }
}
