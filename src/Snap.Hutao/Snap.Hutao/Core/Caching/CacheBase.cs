﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Snap.Hutao.Core.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Snap.Hutao.Core.Caching;

/// <summary>
/// Provides methods and tools to cache files in a folder
/// 经过简化
/// </summary>
/// <typeparam name="T">Generic type as supplied by consumer of the class</typeparam>
public abstract class CacheBase<T>
    where T : class
{
    private readonly SemaphoreSlim cacheFolderSemaphore = new(1);
    private readonly ConcurrentDictionary<string, Task<T?>> concurrentTasks = new();
    private readonly ILogger logger;
    private readonly HttpClient httpClient;

    private StorageFolder? baseFolder;
    private string? cacheFolderName;
    private StorageFolder? cacheFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheBase{T}"/> class.
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="httpClient">http客户端</param>
    protected CacheBase(ILogger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;

        CacheDuration = TimeSpan.FromDays(30);
        RetryCount = 3;
    }

    /// <summary>
    /// Gets or sets the life duration of every cache entry.
    /// </summary>
    public TimeSpan CacheDuration { get; set; }

    /// <summary>
    /// Gets or sets the number of retries trying to ensure the file is cached.
    /// </summary>
    public uint RetryCount { get; set; }

    /// <summary>
    /// Clears all files in the cache
    /// </summary>
    /// <returns>awaitable task</returns>
    public async Task ClearAsync()
    {
        StorageFolder folder = await GetCacheFolderAsync().ConfigureAwait(false);
        IReadOnlyList<StorageFile> files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);

        await InternalClearAsync(files).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes cached files that have expired
    /// </summary>
    /// <param name="duration">Optional timespan to compute whether file has expired or not. If no value is supplied, <see cref="CacheDuration"/> is used.</param>
    /// <returns>awaitable task</returns>
    public async Task RemoveExpiredAsync(TimeSpan? duration = null)
    {
        TimeSpan expiryDuration = duration ?? CacheDuration;

        StorageFolder? folder = await GetCacheFolderAsync().ConfigureAwait(false);
        IReadOnlyList<StorageFile>? files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);

        List<StorageFile>? filesToDelete = new();

        foreach (StorageFile? file in files)
        {
            if (file == null)
            {
                continue;
            }

            if (await IsFileOutOfDateAsync(file, expiryDuration, false).ConfigureAwait(false))
            {
                filesToDelete.Add(file);
            }
        }

        await InternalClearAsync(filesToDelete).ConfigureAwait(false);
    }

    /// <summary>
    /// Removed items based on uri list passed
    /// </summary>
    /// <param name="uriForCachedItems">Enumerable uri list</param>
    /// <returns>awaitable Task</returns>
    public async Task RemoveAsync(IEnumerable<Uri> uriForCachedItems)
    {
        if (uriForCachedItems == null || !uriForCachedItems.Any())
        {
            return;
        }

        StorageFolder folder = await GetCacheFolderAsync().ConfigureAwait(false);
        IReadOnlyList<StorageFile> files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);

        List<StorageFile> filesToDelete = new();
        List<string> keys = new();

        Dictionary<string, StorageFile> hashDictionary = new();

        foreach (StorageFile file in files)
        {
            hashDictionary.Add(file.Name, file);
        }

        foreach (Uri uri in uriForCachedItems)
        {
            string fileName = GetCacheFileName(uri);
            if (hashDictionary.TryGetValue(fileName, out StorageFile? file))
            {
                filesToDelete.Add(file);
                keys.Add(fileName);
            }
        }

        await InternalClearAsync(filesToDelete).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves item represented by Uri from the cache. If the item is not found in the cache, it will try to downloaded and saved before returning it to the caller.
    /// </summary>
    /// <param name="uri">Uri of the item.</param>
    /// <param name="throwOnError">Indicates whether or not exception should be thrown if item cannot be found / downloaded.</param>
    /// <returns>an instance of Generic type</returns>
    public Task<T?> GetFromCacheAsync(Uri uri, bool throwOnError = false)
    {
        return GetItemAsync(uri, throwOnError);
    }

    /// <summary>
    /// Gets the StorageFile containing cached item for given Uri
    /// </summary>
    /// <param name="uri">Uri of the item.</param>
    /// <returns>a StorageFile</returns>
    public async Task<StorageFile?> GetFileFromCacheAsync(Uri uri)
    {
        StorageFolder folder = await GetCacheFolderAsync().ConfigureAwait(false);

        string fileName = GetCacheFileName(uri);

        IStorageItem? item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);

        if (item == null)
        {
            StorageFile? baseFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
            await DownloadFileAsync(uri, baseFile).ConfigureAwait(false);
            item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);
        }

        return item as StorageFile;
    }

    /// <summary>
    /// Cache specific hooks to process items from HTTP response
    /// </summary>
    /// <param name="stream">input stream</param>
    /// <returns>awaitable task</returns>
    protected abstract Task<T> InitializeTypeAsync(Stream stream);

    /// <summary>
    /// Cache specific hooks to process items from HTTP response
    /// </summary>
    /// <param name="baseFile">storage file</param>
    /// <returns>awaitable task</returns>
    protected abstract Task<T> InitializeTypeAsync(StorageFile baseFile);

    /// <summary>
    /// Override-able method that checks whether file is valid or not.
    /// </summary>
    /// <param name="file">storage file</param>
    /// <param name="duration">cache duration</param>
    /// <param name="treatNullFileAsOutOfDate">option to mark uninitialized file as expired</param>
    /// <returns>bool indicate whether file has expired or not</returns>
    protected virtual async Task<bool> IsFileOutOfDateAsync(StorageFile file, TimeSpan duration, bool treatNullFileAsOutOfDate = true)
    {
        if (file == null)
        {
            return treatNullFileAsOutOfDate;
        }

        BasicProperties? properties = await file.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);

        return properties.Size == 0 || DateTime.Now.Subtract(properties.DateModified.DateTime) > duration;
    }

    private static string GetCacheFileName(Uri uri)
    {
        return CreateHash64(uri.ToString()).ToString();
    }

    private static ulong CreateHash64(string str)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(str);

        ulong value = (ulong)utf8.Length;
        for (int n = 0; n < utf8.Length; n++)
        {
            value += (ulong)utf8[n] << ((n * 5) % 56);
        }

        return value;
    }

    private async Task<T?> GetItemAsync(Uri uri, bool throwOnError)
    {
        T? instance = default(T);

        string fileName = GetCacheFileName(uri);
        concurrentTasks.TryGetValue(fileName, out Task<T?>? request);

        // complete previous task first
        if (request != null)
        {
            await request.ConfigureAwait(false);
            request = null;
        }

        if (request == null)
        {
            request = GetFromCacheOrDownloadAsync(uri, fileName);

            concurrentTasks[fileName] = request;
        }

        try
        {
            instance = await request.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(EventIds.CacheException, ex, "Exception happened when caching.");
            if (throwOnError)
            {
                throw;
            }
        }
        finally
        {
            concurrentTasks.TryRemove(fileName, out _);
        }

        return instance;
    }

    private async Task<T?> GetFromCacheOrDownloadAsync(Uri uri, string fileName)
    {
        T? instance = default;

        StorageFolder folder = await GetCacheFolderAsync().ConfigureAwait(false);
        StorageFile? baseFile = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false) as StorageFile;

        bool downloadDataFile = baseFile == null || await IsFileOutOfDateAsync(baseFile, CacheDuration).ConfigureAwait(false);
        baseFile ??= await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);

        if (downloadDataFile)
        {
            uint retries = 0;
            try
            {
                while (retries < RetryCount)
                {
                    try
                    {
                        instance = await DownloadFileAsync(uri, baseFile).ConfigureAwait(false);

                        if (instance != null)
                        {
                            break;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                    }

                    retries++;
                }
            }
            catch (Exception)
            {
                await baseFile.DeleteAsync().AsTask().ConfigureAwait(false);
                throw; // re-throwing the exception changes the stack trace. just throw
            }
        }

        if (EqualityComparer<T>.Default.Equals(instance, default(T)))
        {
            instance = await InitializeTypeAsync(baseFile).ConfigureAwait(false);
        }

        return instance;
    }

    private async Task<T?> DownloadFileAsync(Uri uri, StorageFile baseFile)
    {
        T? instance = default;

        using (MemoryStream memory = new())
        {
            using (Stream httpStream = await httpClient.GetStreamAsync(uri))
            {
                await httpStream.CopyToAsync(memory);
                await memory.FlushAsync();

                memory.Position = 0;

                using (Stream fs = await baseFile.OpenStreamForWriteAsync())
                {
                    await memory.CopyToAsync(fs);
                    await fs.FlushAsync();

                    memory.Position = 0;
                }
            }

            instance = await InitializeTypeAsync(memory).ConfigureAwait(false);
        }

        return instance;
    }

    [SuppressMessage("", "CA1822")]
    private async Task InternalClearAsync(IEnumerable<StorageFile> files)
    {
        foreach (StorageFile file in files)
        {
            try
            {
                await file.DeleteAsync().AsTask().ConfigureAwait(false);
            }
            catch
            {
                // Just ignore errors for now
            }
        }
    }

    /// <summary>
    /// Initializes with default values if user has not initialized explicitly
    /// </summary>
    /// <returns>awaitable task</returns>
    private async Task InitializeInternalAsync()
    {
        if (cacheFolder != null)
        {
            return;
        }

        await cacheFolderSemaphore.WaitAsync().ConfigureAwait(false);

        baseFolder ??= ApplicationData.Current.TemporaryFolder;

        if (string.IsNullOrWhiteSpace(cacheFolderName))
        {
            cacheFolderName = GetType().Name;
        }

        try
        {
            cacheFolder = await baseFolder
                .CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists)
                .AsTask()
                .ConfigureAwait(false);
        }
        finally
        {
            cacheFolderSemaphore.Release();
        }
    }

    private async Task<StorageFolder> GetCacheFolderAsync()
    {
        if (cacheFolder == null)
        {
            await InitializeInternalAsync().ConfigureAwait(false);
        }

        return Must.NotNull(cacheFolder!);
    }
}