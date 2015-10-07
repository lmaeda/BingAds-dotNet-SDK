﻿using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.BingAds.Internal;
using Microsoft.BingAds.Internal.Utilities;

namespace Microsoft.BingAds.Reporting
{
    /// <summary>
    /// Provides high level methods for downloading entities using the Reporting API functionality. Also provides methods for submitting download operations.
    /// </summary>
    /// <remarks>
    /// <see cref="DownloadFileAsync(ReportingDownloadParameters)"/> will submit the download request to the reporting service, 
    /// poll until the status is completed (or returns an error), and downloads the file locally. 
    /// If instead you want to manage the low level details you would first call <see cref="SubmitDownloadAsync"/>, 
    /// wait for the results file to be prepared using either <see cref="ReportingDownloadOperation.GetStatusAsync()"/> 
    /// or <see cref="ReportingDownloadOperation.TrackAsync()"/>, and then download the file with the 
    /// <see cref="ReportingDownloadOperation.DownloadResultFileAsync(string,string,bool)"/> method.
    /// </remarks>
    public class ReportingServiceManager
    {
        private readonly AuthorizationData _authorizationData;

        internal IHttpService HttpService { get; set; }

        internal IZipExtractor ZipExtractor { get; set; }

        internal IFileSystem FileSystem { get; set; }

        internal const int DefaultStatusPollIntervalInMilliseconds = 5000;

        /// <summary>
        /// The time interval in milliseconds between two status polling attempts. The default value is 5000 (5 seconds).
        /// </summary>
        public int StatusPollIntervalInMilliseconds { get; set; }

        /// <summary>
        /// Directory for storing downloaded files if result folder is not specified.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Initializes a new instance of this class with the specified <see cref="AuthorizationData"/>.
        /// </summary>
        /// <param name="authorizationData">Represents a user who intends to access the corresponding customer and account. </param>
        public ReportingServiceManager(AuthorizationData authorizationData)
        {
            if (authorizationData == null)
            {
                throw new ArgumentNullException("authorizationData");
            }

            _authorizationData = authorizationData;

            HttpService = new HttpService();

            ZipExtractor = new ZipExtractor();

            FileSystem = new FileSystem();

            StatusPollIntervalInMilliseconds = DefaultStatusPollIntervalInMilliseconds;

            WorkingDirectory = Path.Combine(Path.GetTempPath(), "BingAdsSDK", "Reporting");
        }

        /// <summary>
        /// Downloads the specified reporting entities to a local file. 
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download and where the file should be downloaded.
        /// Please see <see cref="ReportingDownloadParameters"/> for more information about available parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the local reporting file path.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="ReportingOperationCouldNotBeCompletedException">Thrown if the reporting operation has failed </exception>
        public Task<string> DownloadFileAsync(ReportingDownloadParameters parameters)
        {
            return DownloadFileAsync(parameters, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the specified reporting entities to a local file. 
        /// </summary>
        /// <param name="parameters">Determines various download parameters, for example what entities to download and where the file should be downloaded.
        /// Please see <see cref="ReportingDownloadParameters"/> for more information about available parameters.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the tracking of the reporting operation on the client. Doesn't cancel the actual reporting operation on the server.</param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the local reporting file path.</returns>
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        /// <exception cref="ReportingOperationCouldNotBeCompletedException">Thrown if the reporting operation has failed </exception>
        public Task<string> DownloadFileAsync(ReportingDownloadParameters parameters, CancellationToken cancellationToken)
        {
            return DownloadFileAsyncImpl(parameters, cancellationToken);
        }

        /// <summary>
        /// Submits a download request to the Bing Ads reporting service with the specified parameters.
        /// </summary>
        /// <param name="request">Determines various download parameters, for example what entities to download. </param>
        /// <returns>A task that represents the asynchronous operation. The task result will be the submitted download operation.</returns>        
        /// <exception cref="FaultException{TDetail}">Thrown if a fault is returned from the Bing Ads service.</exception>
        /// <exception cref="OAuthTokenRequestException">Thrown if tokens can't be refreshed due to an error received from the Microsoft Account authorization server.</exception>  
        public Task<ReportingDownloadOperation> SubmitDownloadAsync(ReportRequest request)
        {
            return SubmitDownloadAsyncImpl(request);
        }

        private async Task<ReportingDownloadOperation> SubmitDownloadAsyncImpl(ReportRequest request)
        {
            var submitRequest = new SubmitGenerateReportRequest {ReportRequest = request,};
            SubmitGenerateReportResponse response;

            using (var apiService = new ServiceClient<IReportingService>(_authorizationData))
            {
                response = await apiService.CallAsync((s, r) => s.SubmitGenerateReportAsync(r), submitRequest).ConfigureAwait(false);
            }

            return new ReportingDownloadOperation(response.ReportRequestId, _authorizationData, response.TrackingId) {StatusPollIntervalInMilliseconds = StatusPollIntervalInMilliseconds};
        }

        private async Task<string> DownloadFileAsyncImpl(ReportingDownloadParameters parameters, CancellationToken cancellationToken)
        {
            using (var operation = await SubmitDownloadAsyncImpl(parameters.ReportRequest).ConfigureAwait(false))
            {
                await operation.TrackAsync(cancellationToken).ConfigureAwait(false);

                return await DownloadReportingFile(parameters.ResultFileDirectory, parameters.ResultFileName, parameters.OverwriteResultFile, operation).ConfigureAwait(false);
            }
        }

        private async Task<string> DownloadReportingFile(string resultFileDirectory, string resultFileName, bool overwrite, ReportingDownloadOperation operation)
        {
            operation.HttpService = HttpService;
            operation.ZipExtractor = ZipExtractor;
            operation.FileSystem = FileSystem;

            CreateWorkingDirectoryIfNeeded();

            var localFile = await operation.DownloadResultFileAsync(resultFileDirectory ?? WorkingDirectory, resultFileName, true, overwrite).ConfigureAwait(false);

            return localFile;
        }

        private void CreateWorkingDirectoryIfNeeded()
        {
            FileSystem.CreateDirectoryIfDoesntExist(WorkingDirectory);
        }
    }
}