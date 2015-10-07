﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.BingAds.Internal;
using Microsoft.BingAds.Internal.Utilities;
using Microsoft.BingAds.V10.Bulk.Entities;
using Microsoft.BingAds.V10.Internal.Bulk;

namespace Microsoft.BingAds.V10.Bulk
{
    /// <summary>
    /// Provides high level methods for uploading and downloading entities using the Bulk API functionality. Also provides methods for submitting upload or download operations.
    /// </summary>
    /// <remarks>
    /// <see cref="DownloadFileAsync(DownloadParameters)"/> will submit the download request to the bulk service, 
    /// poll until the status is completed (or returns an error), and downloads the file locally. 
    /// If instead you want to manage the low level details you would first call <see cref="SubmitDownloadAsync"/>, 
    /// wait for the results file to be prepared using either <see cref="BulkOperation{DownloadStatus}.GetStatusAsync()"/> 
    /// or <see cref="BulkOperation{TStatus}.TrackAsync()"/>, and then download the file with the 
    /// <see cref="BulkOperation{TStatus}.DownloadResultFileAsync(string,string,bool)"/> method.
    /// </remarks>
    public class BulkServiceManager
    {
        private readonly AuthorizationData _authorizationData;

        internal IHttpService HttpService { get; set; }

        internal IZipExtractor ZipExtractor { get; set; }

        internal IFileSystem FileSystem { get; set; }

        internal IBulkFileReaderFactory BulkFileReaderFactory { get; set; }

        internal const string FormatVersion = "4.0";

        internal const int DefaultStatusPollIntervalInMilliseconds = 5000;

        /// <summary>
        /// The time interval in milliseconds between two status polling attempts. The default value is 15000 (15 seconds).
        /// </summary>
        public int StatusPollIntervalInMilliseconds { get; set; }

        /// <summary>
        /// Directory for storing temporary files needed for some operations (for example <see cref="UploadEntitiesAsync(Microsoft.BingAds.V10.Bulk.EntityUploadParameters)"/> creates a temporary upload file).
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Initializes a new instance of this class with the specified <see cref="AuthorizationData"/>.
        /// </summary>
        /// <param name="authorizationData">Represents a user who intends to access the corresponding customer and account. </param>
        public BulkServiceManager(AuthorizationData authorizationData)
        {
            if (authorizationData == null)
            {
                throw new ArgumentNullException("authorizationData");
            }

            _authorizationData = authorizationData;

            HttpService = new HttpService();

            ZipExtractor = new ZipExtractor();

            FileSystem = new FileSystem();

            BulkFileReaderFactory = new BulkFileReaderFactory();

            StatusPollIntervalInMilliseconds = DefaultStatusPollIntervalInMilliseconds;

            WorkingDirectory = Path.Combine(Path.GetTempPath(), "BingAdsSDK");
        }

        /// <summary>
        /// Downloads the specified Bulk entities. 
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download. Please see <see cref="DownloadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be an enumerable list of <see cref="BulkEntity"/> objects.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed.</exception>
        public Task<IEnumerable<BulkEntity>> DownloadEntitiesAsync(DownloadParameters parameters)
        {
            return DownloadEntitiesAsync(parameters, null, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the specified Bulk entities. 
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download. Please see <see cref="DownloadParameters"/> for more information about available parameters.</param>
        /// <param name="progress">A class implementing <see cref="IProgress{T}"/> for tracking the percent complete progress information for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the tracking of the bulk operation on the client. Doesn't cancel the actual bulk operation on the server.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be an enumerable list of <see cref="BulkEntity"/> objects.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed.</exception>
        public Task<IEnumerable<BulkEntity>> DownloadEntitiesAsync(DownloadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            ValidateSubmitDownloadParameters(parameters.SubmitDownloadParameters);

            ValidateUserData();

            return DownloadEntitiesAsyncImpl(parameters, progress, cancellationToken);
        }

        /// <summary>
        /// Downloads the specified Bulk entities to a local file. 
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download and where the file should be downloaded.
        /// Please see <see cref="DownloadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the local bulk file path.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed </exception>
        public Task<string> DownloadFileAsync(DownloadParameters parameters)
        {
            return DownloadFileAsync(parameters, null, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the specified Bulk entities to a local file. 
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download and where the file should be downloaded.
        /// Please see <see cref="DownloadParameters"/> for more information about available parameters.</param>
        /// <param name="progress">A class implementing <see cref="IProgress{T}"/> for tracking the percent complete progress information for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the tracking of the bulk operation on the client. Doesn't cancel the actual bulk operation on the server.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the local bulk file path.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed </exception>
        public Task<string> DownloadFileAsync(DownloadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            ValidateUserData();

            ValidateSubmitDownloadParameters(parameters.SubmitDownloadParameters);

            return DownloadFileAsyncImpl(parameters, progress, cancellationToken);
        }

        /// <summary>
        /// Uploads the specified Bulk entities.
        /// </summary>
        /// <param name="parameters">Determines various upload parameters, for example what entities to upload. Please see <see cref="EntityUploadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be an enumerable list of <see cref="BulkEntity"/> objects.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed.</exception>
        public Task<IEnumerable<BulkEntity>> UploadEntitiesAsync(EntityUploadParameters parameters)
        {
            return UploadEntitiesAsync(parameters, null, CancellationToken.None);
        }

        /// <summary>
        /// Uploads the specified Bulk entities.
        /// </summary>
        /// <param name="parameters">Determines various upload parameters, for example what entities to upload. Please see <see cref="EntityUploadParameters"/> for more information about available parameters.</param>
        /// <param name="progress">A class implementing <see cref="IProgress{T}"/> for tracking the percent complete progress information for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the tracking of the bulk operation on the client. Doesn't cancel the actual bulk operation on the server.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be an enumerable list of <see cref="BulkEntity"/> objects.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed.</exception>
        public Task<IEnumerable<BulkEntity>> UploadEntitiesAsync(EntityUploadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            ValidateEntityUploadParameters(parameters);

            ValidateUserData();

            var fileUploadParameters = CreateFileUploadParameters(parameters);

            return UploadEntitiesAsyncImpl(progress, cancellationToken, fileUploadParameters);
        }

        /// <summary>
        /// Uploads the specified Bulk file.
        /// </summary>
        /// <param name="parameters">Determines various upload parameters, for example what file to upload. Please see <see cref="FileUploadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be an enumerable list of <see cref="BulkEntity"/> objects.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed.</exception>
        public Task<string> UploadFileAsync(FileUploadParameters parameters)
        {
            return UploadFileAsync(parameters, null, CancellationToken.None);
        }

        /// <summary>
        /// Uploads the specified Bulk file.
        /// </summary>
        /// <param name="parameters">Determines various upload parameters, for example what file to upload. Please see <see cref="FileUploadParameters"/> for more information about available parameters.</param>
        /// <param name="progress">A class implementing <see cref="IProgress{T}"/> for tracking the percent complete progress information for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the tracking of the bulk operation on the client. Doesn't cancel the actual bulk operation on the server.</param>        
        /// <returns>A task that represents the asynchronous operation. The task result will be an enumerable list of <see cref="BulkEntity"/> objects.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="BulkOperationCouldNotBeCompletedException{TStatus}">Thrown if the bulk operation has failed.</exception>
        public Task<string> UploadFileAsync(FileUploadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            ValidateSubmitUploadParameters(parameters.SubmitUploadParameters);

            ValidateUserData();

            return UploadFileAsyncImpl(parameters, progress, cancellationToken);
        }

        /// <summary>
        /// Submits a download request to the Bing Ads bulk service with the specified parameters.
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download. Please see <see cref="SubmitDownloadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the submitted download operation.</returns>        
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        public Task<BulkDownloadOperation> SubmitDownloadAsync(SubmitDownloadParameters parameters)
        {
            ValidateSubmitDownloadParameters(parameters);

            ValidateUserData();

            return SubmitDownloadAsyncImpl(parameters);
        }

        /// <summary>
        /// Submits an upload request to the Bing Ads bulk service with the specified parameters.         
        /// </summary>
        /// <param name="parameters">Determines various upload parameters, for example what file to upload. Please see <see cref="SubmitUploadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the submitted upload operation.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        public Task<BulkUploadOperation> SubmitUploadAsync(SubmitUploadParameters parameters)
        {
            ValidateSubmitUploadParameters(parameters);

            ValidateUserData();

            return SubmitUploadAsyncImpl(parameters);
        }

        /// <summary>
        /// Removes temporary files from <see cref="WorkingDirectory"/>.
        /// </summary>
        public void CleanupTempFiles()
        {
            foreach (var file in FileSystem.GetFilesFromDirectory(WorkingDirectory))
            {
                FileSystem.DeleteFile(file);
            }
        }

        private async Task<IEnumerable<BulkEntity>> DownloadEntitiesAsyncImpl(DownloadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            var resultFile = await DownloadFileAsyncImpl(parameters, progress, cancellationToken).ConfigureAwait(false);

            var resultFileType = parameters.LastSyncTimeInUTC == null ? ResultFileType.FullDownload : ResultFileType.PartialDownload;

            return new BulkFileReaderEnumerable(BulkFileReaderFactory.CreateBulkFileReader(resultFile, resultFileType, parameters.FileType));
        }

        private async Task<string> DownloadFileAsyncImpl(DownloadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            using (var operation = await SubmitDownloadAsyncImpl(parameters.SubmitDownloadParameters).ConfigureAwait(false))
            {
                await operation.TrackAsync(progress, cancellationToken).ConfigureAwait(false);

                return await DownloadBulkFile(parameters.ResultFileDirectory, parameters.ResultFileName, parameters.OverwriteResultFile, operation).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<BulkEntity>> UploadEntitiesAsyncImpl(IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken, FileUploadParameters fileUploadParameters)
        {
            var resultFile = await UploadFileAsyncImpl(fileUploadParameters, progress, cancellationToken).ConfigureAwait(false);

            return new BulkFileReaderEnumerable(BulkFileReaderFactory.CreateBulkFileReader(resultFile, ResultFileType.Upload, DownloadFileType.Csv));
        }

        private async Task<string> UploadFileAsyncImpl(FileUploadParameters parameters, IProgress<BulkOperationProgressInfo> progress, CancellationToken cancellationToken)
        {
            using (var operation = await SubmitUploadAsync(parameters.SubmitUploadParameters).ConfigureAwait(false))
            {
                await operation.TrackAsync(progress, cancellationToken).ConfigureAwait(false);

                return await DownloadBulkFile(parameters.ResultFileDirectory, parameters.ResultFileName, parameters.OverwriteResultFile, operation).ConfigureAwait(false);
            }
        }

        private async Task<string> DownloadBulkFile<TStatus>(string resultFileDirectory, string resultFileName, bool overwrite, BulkOperation<TStatus> operation)
        {
            operation.HttpService = HttpService;
            operation.ZipExtractor = ZipExtractor;
            operation.FileSystem = FileSystem;

            CreateWorkingDirectoryIfNeeded();

            var localFile = await operation.DownloadResultFileAsync(resultFileDirectory ?? WorkingDirectory, resultFileName, true, overwrite).ConfigureAwait(false);

            return localFile;
        }

        private async Task<BulkDownloadOperation> SubmitDownloadAsyncImpl(SubmitDownloadParameters parameters)
        {
            if (parameters.CampaignIds == null)
            {
                var request = new DownloadCampaignsByAccountIdsRequest
                {
                    AccountIds = new[] { _authorizationData.AccountId },
                    DataScope = parameters.DataScope,
                    DownloadFileType = parameters.FileType,
                    Entities = parameters.Entities,
                    FormatVersion = FormatVersion,
                    LastSyncTimeInUTC = parameters.LastSyncTimeInUTC,
                    PerformanceStatsDateRange = parameters.PerformanceStatsDateRange,
                };

                DownloadCampaignsByAccountIdsResponse response;

                using (var apiService = new ServiceClient<IBulkService>(_authorizationData))
                {
                    response = await apiService.CallAsync((s, r) => s.DownloadCampaignsByAccountIdsAsync(r), request).ConfigureAwait(false);
                }

                return new BulkDownloadOperation(response.DownloadRequestId, _authorizationData, response.TrackingId)
                {
                    StatusPollIntervalInMilliseconds = StatusPollIntervalInMilliseconds
                };
            }
            else
            {
                var request = new DownloadCampaignsByCampaignIdsRequest
                {
                    Campaigns = parameters.CampaignIds.Select(c => new CampaignScope { CampaignId = c, ParentAccountId = _authorizationData.AccountId }).ToList(),
                    DataScope = parameters.DataScope,
                    DownloadFileType = parameters.FileType,
                    Entities = parameters.Entities,
                    FormatVersion = FormatVersion,
                    LastSyncTimeInUTC = parameters.LastSyncTimeInUTC,
                    PerformanceStatsDateRange = parameters.PerformanceStatsDateRange
                };

                DownloadCampaignsByCampaignIdsResponse response;

                using (var apiService = new ServiceClient<IBulkService>(_authorizationData))
                {
                    response = await apiService.CallAsync((s, r) => s.DownloadCampaignsByCampaignIdsAsync(r), request).ConfigureAwait(false);
                }

                return new BulkDownloadOperation(response.DownloadRequestId, _authorizationData, response.TrackingId)
                {
                    StatusPollIntervalInMilliseconds = StatusPollIntervalInMilliseconds
                };
            }
        }

        private async Task<BulkUploadOperation> SubmitUploadAsyncImpl(SubmitUploadParameters parameters)
        {
            var request = new GetBulkUploadUrlRequest
            {
                ResponseMode = parameters.ResponseMode
            };

            GetBulkUploadUrlResponse getUploadUrlResponse;

            using (var apiService = new ServiceClient<IBulkService>(_authorizationData))
            {
                getUploadUrlResponse = await apiService.CallAsync((s, r) => s.GetBulkUploadUrlAsync(r), request).ConfigureAwait(false);
            }

            var uploadUrl = getUploadUrlResponse.UploadUrl;

            Action<HttpRequestHeaders> addHeaders = headers =>
            {
                headers.Add("DeveloperToken", _authorizationData.DeveloperToken);
                headers.Add("CustomerId", _authorizationData.CustomerId.ToString());
                headers.Add("AccountId", _authorizationData.AccountId.ToString());

                _authorizationData.Authentication.AddAuthenticationHeaders(headers);
            };

            string effectiveFileUploadPath = parameters.UploadFilePath;

            if (parameters.RenameUploadFileToMatchRequestId)
            {
                effectiveFileUploadPath = RenameUploadFileToMatchRequestId(effectiveFileUploadPath, getUploadUrlResponse);
            }

            var shouldCompress = parameters.CompressUploadFile && Path.GetExtension(effectiveFileUploadPath) != ".zip";

            string compressedFilePath = null;

            if (shouldCompress)
            {
                compressedFilePath = CompressUploadFile(effectiveFileUploadPath);

                effectiveFileUploadPath = compressedFilePath;
            }

            await HttpService.UploadFileAsync(new Uri(uploadUrl), effectiveFileUploadPath, addHeaders).ConfigureAwait(false);

            if (shouldCompress && compressedFilePath != null)
            {
                FileSystem.DeleteFile(compressedFilePath);
            }

            return new BulkUploadOperation(getUploadUrlResponse.RequestId, _authorizationData, getUploadUrlResponse.TrackingId)
            {
                StatusPollIntervalInMilliseconds = StatusPollIntervalInMilliseconds
            };
        }

        private string CompressUploadFile(string effectiveFileUploadPath)
        {
            CreateWorkingDirectoryIfNeeded();

            var compressedFilePath = Path.Combine(WorkingDirectory, Path.GetFileNameWithoutExtension(effectiveFileUploadPath) + "_" + Guid.NewGuid() + ".zip");

            ZipExtractor.CompressFile(effectiveFileUploadPath, compressedFilePath);            

            return compressedFilePath;            
        }

        private void CreateWorkingDirectoryIfNeeded()
        {
            FileSystem.CreateDirectoryIfDoesntExist(WorkingDirectory);
        }

        private string RenameUploadFileToMatchRequestId(string uploadFilePath, GetBulkUploadUrlResponse getUploadUrlResponse)
        {
            var effectiveFileUploadPath = Path.Combine(Path.GetDirectoryName(uploadFilePath), "upload_" + getUploadUrlResponse.RequestId + ".csv");

            FileSystem.RenameFile(uploadFilePath, effectiveFileUploadPath);

            return effectiveFileUploadPath;
        }

        private static void ValidateSubmitDownloadParameters(SubmitDownloadParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }
        }

        private static void ValidateSubmitUploadParameters(SubmitUploadParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (parameters.UploadFilePath == null)
            {
                throw new InvalidOperationException(ErrorMessages.UploadFilePathMustNotBeNull);
            }
        }

        private static void ValidateEntityUploadParameters(EntityUploadParameters parameters)
        {
            if (parameters == null)
            {
                throw new NullReferenceException("parameters");
            }

            if (parameters.Entities == null)
            {
                throw new ArgumentException(ErrorMessages.EntitiesMustNotBeNull);
            }
        }

        private void ValidateUserData()
        {
            _authorizationData.Validate();
        }

        private FileUploadParameters CreateFileUploadParameters(EntityUploadParameters parameters)
        {
            CreateWorkingDirectoryIfNeeded();

            var fileName = Path.Combine(WorkingDirectory, Guid.NewGuid() + ".csv");

            using (var writer = new BulkFileWriter(fileName))
            {
                foreach (var entity in parameters.Entities)
                {
                    writer.WriteEntity(entity);
                }
            }

            var fileUploadParameters = new FileUploadParameters
            {
                UploadFilePath = fileName,
                ResponseMode = parameters.ResponseMode,
                ResultFileDirectory = parameters.ResultFileDirectory,
                ResultFileName = parameters.ResultFileName,
                OverwriteResultFile = parameters.OverwriteResultFile,
                RenameUploadFileToMatchRequestId = true
            };

            return fileUploadParameters;
        }
    }
}
