using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Core.Telemetry;
using DataEncryptionService.Core.Telemetry.Names;
using DataEncryptionService.CryptoEngines;
using DataEncryptionService.Storage;
using DataEncryptionService.Telemetry;
using Microsoft.Extensions.Logging;

namespace DataEncryptionService.Core
{
    public class DataEncryptionManager : IDataEncryptionManager
    {
        private readonly ILogger _log;
        private readonly ITelemetrySourceClient _telemetry;
        private readonly ICryptoEngineFactory _cryptoEngineFactory;
        private readonly IStringHasher _hasher;
        private readonly IStorageProvider _storage;
        private readonly HashMethod _defaultHashMethod;
        private readonly bool _allowRequestParameters;

        public DataEncryptionManager(DataEncryptionServiceConfiguration config, ICryptoEngineFactory engineFactory, IStringHasher hasher, IStorageProviderFactory storageProviderFactory, ILogger<DataEncryptionManager> log, ITelemetrySourceClient telemetry)
        {
            _log = log;
            _telemetry = telemetry;
            _cryptoEngineFactory = engineFactory;
            _hasher = hasher;
            _defaultHashMethod = config.Hashing.DefaultHash;
            _allowRequestParameters = config.Encryption.AllowRequestParameters;

            // Create the default (configured) storage provider
            _storage = storageProviderFactory.CreateProvider();
        }

        public async Task<DataEncryptResponse> EncryptDataAsync(DataEncryptRequest request)
        {
            var response = DataEncryptResponse.CreateDefault();
            IPersistedSecureData dataDoc = null;
            var spans = new List<TelemetrySpan>();
            try
            {
                var topSpan = SpanMeasure.Start(SpanName.Encryption_Request, spans);
                ICryptographicEngine engine = _cryptoEngineFactory.GetDefaultEngine();
                if (null == engine)
                {
                    response.Error = ErrorCode.Crypto_Engine_Not_Available;
                    response.Message = "Cannot access required encryption engine. Cannot complete the request.";
                    return response;
                }

                Dictionary<string, object> parameters = null;
                if (_allowRequestParameters)
                {
                    parameters = request.Parameters;
                }
                EncryptionResult result;
                using (SpanMeasure.Start(SpanName.Encryption_Crypto_Engine_Call, spans, topSpan))
                {
                    result = await engine.EncryptAsync(request.Data, parameters);
                }
                if (ErrorCode.None == result.Code)
                {
                    HashMethod? hashMethod = request?.Options?.Hash;
                    if (!hashMethod.HasValue)
                    {
                        hashMethod = _defaultHashMethod;
                    }

                    hashMethod = hashMethod == HashMethod.None ? _defaultHashMethod : hashMethod;

                    int i = 0;
                    foreach (var kvPair in result.Data)
                    {
                        var item = new EncryptedValue()
                        {
                            Name = kvPair.Key,
                            Cipher = result.Data.ElementAt(i).Value,
                            Hash = _hasher.ComputeHash(kvPair.Value.ToLower(), hashMethod.Value).ToBase64()
                        };
                        i++;
                        response.Data.Add(kvPair.Key, item);
                    }

                    dataDoc = _storage.AllocateNewData();
                    dataDoc.KeyScope = Parameters.GetValue(result.Parameters, CommonParameterNames.KeyScope, string.Empty);
                    dataDoc.KeyName = Parameters.GetValue(result.Parameters, CommonParameterNames.KeyName, string.Empty);
                    dataDoc.KeyVersion = Parameters.GetValue(result.Parameters, CommonParameterNames.KeyVersion, 1);
                    dataDoc.HashMethod = hashMethod.ToString();
                    dataDoc.Tags = request.Tags;

                    dataDoc.EngineId = engine.EngineId;
                    dataDoc.EngineRequestId = result.RequestId;
                    dataDoc.EncryptionParameters = result.Parameters;
                    dataDoc.EncryptionParameters.Remove(CommonParameterNames.KeyScope);
                    dataDoc.EncryptionParameters.Remove(CommonParameterNames.KeyName);
                    dataDoc.EncryptionParameters.Remove(CommonParameterNames.KeyVersion);
                    if (dataDoc.EncryptionParameters.Count == 0)
                    {
                        dataDoc.EncryptionParameters = null;
                    }

                    dataDoc.Data.AddEncryptedValues(response.Data.Values);

                    try
                    {
                        using (SpanMeasure.Start(SpanName.Encryption_Save_Encrypted_Data, spans, topSpan))
                        {
                            await _storage.SaveEncryptedDataAsync(dataDoc);
                        }
                        response.Label = dataDoc.Label;
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, e.Message, response.RequestId);
                        response.Error = ErrorCode.Storage_Cannot_Commit_Values;
                        response.Message = e.Message;
                    }
                }
                else
                {
                    _log.LogWarning("Error encrypting data: {@ErrorMessage} (Code: {@ErrorCode})", result.Message, result.Code, response.RequestId);
                    response.Error = result.Code;
                    response.Message = result.Message;
                }

                return response;
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>();
                if (null != dataDoc)
                {
                    eventAttributes.Add("DataLabel", dataDoc.Label);
                    eventAttributes.Add("EngineId", dataDoc.EngineId);
                    eventAttributes.Add("EngineRequestId", dataDoc.EngineRequestId);

                    eventAttributes.Add(CommonParameterNames.KeyScope, dataDoc.KeyScope);
                    eventAttributes.Add(CommonParameterNames.KeyName, dataDoc.KeyName);
                    eventAttributes.Add(CommonParameterNames.KeyVersion, dataDoc.KeyVersion);
                    eventAttributes.Add(CommonParameterNames.HashMethod, dataDoc.HashMethod);

                    if (null != dataDoc.EncryptionParameters)
                    {
                        eventAttributes.Add("Parameters", dataDoc.EncryptionParameters);
                    }
                }

                if (ErrorCode.None != response.Error)
                {
                    await _telemetry.RaiseErrorAsync((int)response.Error, response.Message, correlationKey: response.RequestId);
                }

                await _telemetry.RaiseEventAsync(EventName.DataEncryptCompleted, spans, response.RequestId, eventAttributes);
            }
        }

        public async Task<DataDecryptResponse> DecryptDataAsync(DataDecryptRequest request)
        {
            var response = DataDecryptResponse.CreateDefault();

            var requestedLabels = new HashSet<string>();
            var spans = new List<TelemetrySpan>();
            try
            {
                var topSpan = SpanMeasure.Start(SpanName.Decryption_Request, spans);
                if (null == request.LabeledData)
                {
                    response.Error = ErrorCode.Crypto_Invalid_Tagged_Data_List;
                    response.Message = "The labeled data list in the request is null.";
                    return response;
                }

                IEnumerable<LabeledEncryptedData> requestedLabeledData = CoalesceRequestedLabelsAndFields(request.LabeledData);
                foreach (LabeledEncryptedData labeledEncryptedDataItem in requestedLabeledData)
                {
                    try
                    {
                        requestedLabels.Add(labeledEncryptedDataItem.Label);
                        IPersistedSecureData dataDoc = null;
                        using (SpanMeasure.Start(SpanName.Decryption_Load_Encrypted_Data, spans, topSpan))
                        {
                            dataDoc = await _storage.LoadEncryptedDataAsync(labeledEncryptedDataItem.Label);
                        }
                        if (null != dataDoc)
                        {
                            ICryptographicEngine engine = _cryptoEngineFactory.GetEngine(dataDoc.EngineId);
                            if (null != engine)
                            {
                                var kvCipherText = new Dictionary<string, string>();
                                if (labeledEncryptedDataItem?.Items?.Count > 0)
                                {
                                    // Only return the requested names or hashes
                                    foreach (string item in labeledEncryptedDataItem.Items)
                                    {
                                        // Look for the value by name first, then by Hash if not found
                                        var storedItem = dataDoc.Data.Where(x => x.Name.Equals(item, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                        if (storedItem is null)
                                        {
                                            storedItem = dataDoc.Data.Where(x => x.Hash == item).FirstOrDefault();
                                        }

                                        // If we found the item to decrypt, do so now
                                        if (null != storedItem)
                                        {
                                            kvCipherText.TryAdd(storedItem.Name, storedItem.Cipher);
                                        }
                                    }
                                }
                                else
                                {
                                    // return all the values stored under this label
                                    foreach (var storedItem in dataDoc.Data)
                                    {
                                        kvCipherText.Add(storedItem.Name, storedItem.Cipher);
                                    }
                                }

                                var parameters = new Dictionary<string, object>()
                                    {
                                        {  CommonParameterNames.KeyScope, dataDoc.KeyScope },
                                        {  CommonParameterNames.KeyName, dataDoc.KeyName },
                                        {  CommonParameterNames.KeyVersion, dataDoc.KeyVersion }
                                    };

                                DecryptionResult result;
                                using (SpanMeasure.Start(SpanName.Decryption_Crypto_Engine_Call, spans, topSpan))
                                {
                                    result = await engine.DecryptAsync(kvCipherText, parameters);
                                }
                                if (ErrorCode.None == result.Code)
                                {
                                    response.LabeledData.Add(LabeledDecryptedData.CreateDefault(labeledEncryptedDataItem.Label, result.Data));
                                }
                                else
                                {
                                    _log.LogWarning(result.Message, dataDoc.EngineId, labeledEncryptedDataItem.Label, response.RequestId, result.RequestId);
                                    response.HasErrors = true;
                                    response.LabeledData.Add(LabeledDecryptedData.CreateForError(result.Code, result.Message));
                                }
                            }
                            else
                            {
                                string message = $"The crypto engine [{dataDoc.EngineId}] need to decrypt labeled data [{labeledEncryptedDataItem.Label}] is not available.";
                                _log.LogWarning(message, dataDoc.EngineId, labeledEncryptedDataItem.Label, response.RequestId);
                                response.HasErrors = true;
                                response.LabeledData.Add(LabeledDecryptedData.CreateForError(ErrorCode.Crypto_Engine_Not_Available, message));
                            }
                        }
                        else
                        {
                            response.HasErrors = true;
                            response.LabeledData.Add(LabeledDecryptedData.CreateForError(ErrorCode.Storage_Labeled_Values_Not_Found, $"Labeled values [{labeledEncryptedDataItem.Label}] not found."));
                        }
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Failed to load a labeled values {@Label}", labeledEncryptedDataItem.Label, response.RequestId);
                        response.LabeledData.Add(LabeledDecryptedData.CreateForError(ErrorCode.Storage_Labeled_Values_Failed_To_Load, e.Message));
                    }
                }

                if (response.HasErrors)
                {
                    response.Message = "At least one of the labeled data value lists could not be decrypted.";
                }

                return response;
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>
                {
                    { "RequestedLabels", string.Join(",", requestedLabels) }
                };

                if (ErrorCode.None != response.Error)
                {
                    await _telemetry.RaiseErrorAsync((int)response.Error, response.Message, correlationKey: response.RequestId);
                }
                else if (response.HasErrors)
                {
                    var warnings = new List<TelemetryWarningTuple>();
                    foreach (var item in response.LabeledData)
                    {
                        warnings.Add(new TelemetryWarningTuple()
                        {
                            Key = item.Label,
                            Error = (int)item.Error,
                            Message = item.Message
                        });
                    }

                    await _telemetry.RaiseWarningAsync(response.Message, warnings, correlationKey: response.RequestId);
                }

                await _telemetry.RaiseEventAsync(EventName.DataDecryptCompleted, spans, response.RequestId, eventAttributes);
            }
        }

        private IEnumerable<LabeledEncryptedData> CoalesceRequestedLabelsAndFields(List<LabeledEncryptedData> labeledData)
        {
            var coalescedMap = new Dictionary<string, LabeledEncryptedData>();
            foreach (var oneLabel in labeledData)
            {
                if (coalescedMap.TryGetValue(oneLabel.Label, out LabeledEncryptedData existingItem))
                {
                    existingItem.Items.UnionWith(oneLabel.Items);
                }
                else
                {
                    coalescedMap.Add(oneLabel.Label, oneLabel);
                }
            }

            return coalescedMap.Values;
        }

        public async Task<DataDeleteResponse> DeleteDataAsync(DataDeleteRequest request)
        {
            var response = DataDeleteResponse.CreateDefault();
            var metadata = new Dictionary<string, object>
            {
                { TelemetryMetadataProperty.DataLabels.ToString(), request.Labels }
            };
            var spans = new List<TelemetrySpan>();
            try
            {
                using (SpanMeasure.Start(SpanName.Delete_Request, spans))
                {
                    foreach (string label in request.Labels)
                    {
                        ErrorCode error = ErrorCode.None;
                        string errorMessage = null;
                        try
                        {
                            bool result = await _storage.DeleteEncryptedDataAsync(label);
                            if (!result)
                            {
                                error = ErrorCode.Storage_Label_Not_Found_Cannot_Delete;
                                errorMessage = "Data label not found.";
                            }
                        }
                        catch (Exception e)
                        {
                            await _telemetry.RaiseErrorAsync(e, response.Message, correlationKey: response.RequestId);

                            _log.LogError(e, e.Message, response.RequestId);
                            error = ErrorCode.Storage_Error_Deleting_Data;
                            errorMessage = e.Message;
                        }

                        response.LabelResponses.Add(DeleteLabelResponse.Create(label, error, errorMessage));
                        if (error != ErrorCode.None)
                        {
                            response.HasErrors = true;
                        }
                    }

                    if (response.HasErrors)
                    {
                        response.Message = "At least one of the labeled data value lists could not be deleted.";
                    }

                    return response;
                }
            }
            finally
            {
                if ((int)ErrorCode.None != response.Error)
                {
                    await _telemetry.RaiseErrorAsync((int)response.Error, response.Message, correlationKey: response.RequestId);
                }
                else if (response.HasErrors)
                {
                    var warnings = new List<TelemetryWarningTuple>();
                    foreach (var item in response.LabelResponses)
                    {
                        warnings.Add(new TelemetryWarningTuple()
                        {
                            Key = item.Label,
                            Error = (int)item.Error,
                            Message = item.Message
                        });
                    }

                    await _telemetry.RaiseWarningAsync(response.Message, warnings, correlationKey: response.RequestId);
                }

                await _telemetry.RaiseEventAsync(EventName.DataDeleteCompleted, spans, response.RequestId, metadata);
            }
        }

        public async Task<RotateEncryptionResponse> RotateEncryptionKeyAsync(RotateEncryptionRequest request)
        {
            var response = RotateEncryptionResponse.CreateDefault();
            var spans = new List<TelemetrySpan>();
            bool withErrors = false;
            try
            {
                using (SpanMeasure.Start(SpanName.Key_Rotation_Request, spans))
                {
                    request.EngineId ??= WellKnownConstants.Vault.CryptoEngineUUID;
                    if (request.EngineId != WellKnownConstants.Vault.CryptoEngineUUID)
                    {
                        response.Error = ErrorCode.Crypto_Functionality_Not_Supported;
                        response.Message = "Key rotation and re-encryption not supported by the specified engine.";
                        return response;
                    }

                    ICryptographicEngine engine = _cryptoEngineFactory.GetEngine(request.EngineId.Value);
                    if (null == engine)
                    {
                        response.Error = ErrorCode.Crypto_Engine_Not_Available;
                        response.Message = "The an active instance of the specified crypto engine is not available.";
                        return response;
                    }

                    // Get the key details and the latest (numeric) version of the key
                    EncryptionKeyVersionResult result = await engine.GetEncryptionKeyVersionInfoAsync(request.KeyName, request.KeyScope);
                    if (ErrorCode.None != result.Code)
                    {
                        _log.LogError($"Failed to get encryption key version details. {result.Message}", response.RequestId, result.Code);
                        response.Error = result.Code;
                        response.Message = result.Message;
                        return response;
                    }

                    int currentKeyVersion = result.CurrentVersion;
                    int lastKeyVersion = currentKeyVersion - 1;
                    if (lastKeyVersion <= 1)
                    {
                        // Nothing to do - this means the key is at version 1, so no update necessary
                        return response;
                    }

                    // Iterate through the stored records and "re-encrypt" as necessary
                    // For every stored record, process and call callback (if one was supplied)
                    var reencryptParameters = new Dictionary<string, object>();
                    var kvCipherText = new Dictionary<string, string>();
                    var iterator = await _storage.GetEnumerableListAsync(request.StartingLabel, request.EngineId.Value, request.KeyName, request.KeyScope, keyVersion: lastKeyVersion, request.FromEncryptedOn);
                    foreach (var dataDoc in iterator)
                    {
                        reencryptParameters.Clear();
                        reencryptParameters.Add(CommonParameterNames.KeyName, dataDoc.KeyName);
                        reencryptParameters.Add(CommonParameterNames.KeyScope, dataDoc.KeyScope);

                        kvCipherText.Clear();
                        foreach (var dataItem in dataDoc.Data)
                        {
                            kvCipherText.Add(dataItem.Name, dataItem.Cipher);
                        }

                        int oldKeyVersion = dataDoc.KeyVersion;
                        EncryptionResult reencryptResult = await engine.ReencryptAsync(kvCipherText, reencryptParameters);
                        if (ErrorCode.None == reencryptResult.Code)
                        {
                            dataDoc.KeyVersion = currentKeyVersion;
                            foreach (var dataItem in dataDoc.Data)
                            {
                                dataItem.Cipher = reencryptResult.Data[dataItem.Name];
                            }

                            //try
                            //{
                            //    await _storage.SaveEncryptedDataAsync(dataDoc);
                            //}
                            //catch (Exception e)
                            //{
                            //    _log.LogError(e, $"Failed to save re-encrypted data.", response.RequestId);
                            //    withErrors = true;
                            //}
                        }

                        if (null != request.ProgressCallback)
                        {
                            try
                            {
                                request.ProgressCallback(dataDoc.Label, oldKeyVersion, reencryptResult.Code, reencryptResult.Message);
                            }
                            catch (Exception e)
                            {
                                _log.LogWarning(e, "An error occured in the specified progress callback function.", response.RequestId);
                            }
                        }

                        if (null != request.CancelToken && request.CancelToken.IsCancellationRequested)
                        {
                            break; // out of the foreach()
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message, response.RequestId);
                // TODO: use a better error code
                response.Error = ErrorCode.Generic_Undefined_Error;
                response.Message = e.Message;
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>();
                //eventAttributes.Add("DataLabel", label);

                if (ErrorCode.None == response.Error)
                {
                    if (withErrors)
                    {
                        //await _telemetry.RaiseWarningAsync((int)response.Error, response.Message, correlationKey: response.RequestId);
                    }
                }
                else
                {
                    await _telemetry.RaiseErrorAsync((int)response.Error, response.Message, correlationKey: response.RequestId);
                }

                await _telemetry.RaiseEventAsync(EventName.EncryptionKeyRotationCompleted, spans, response.RequestId, eventAttributes);
            }

            return response;
        }
    }
}