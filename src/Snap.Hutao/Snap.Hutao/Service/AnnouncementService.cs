﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Service.Abstraction;
using Snap.Hutao.Web.Hoyolab.Hk4e.Common.Announcement;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Snap.Hutao.Service;

/// <inheritdoc/>
[Injection(InjectAs.Transient, typeof(IAnnouncementService))]
internal class AnnouncementService : IAnnouncementService
{
    private static readonly string CacheKey = $"{nameof(AnnouncementService)}.Cache.{nameof(AnnouncementWrapper)}";

    private readonly AnnouncementClient announcementClient;
    private readonly IMemoryCache memoryCache;

    /// <summary>
    /// 构造一个新的公告服务
    /// </summary>
    /// <param name="announcementClient">公告提供器</param>
    /// <param name="memoryCache">缓存</param>
    public AnnouncementService(AnnouncementClient announcementClient, IMemoryCache memoryCache)
    {
        this.announcementClient = announcementClient;
        this.memoryCache = memoryCache;
    }

    /// <inheritdoc/>
    public async ValueTask<AnnouncementWrapper> GetAnnouncementsAsync(CancellationToken cancellationToken = default)
    {
        // 缓存中存在记录，直接返回
        if (memoryCache.TryGetValue(CacheKey, out object? cache))
        {
            return Must.NotNull((AnnouncementWrapper)cache);
        }

        AnnouncementWrapper? wrapper = await announcementClient
            .GetAnnouncementsAsync(cancellationToken)
            .ConfigureAwait(false);
        List<AnnouncementContent> contents = await announcementClient
            .GetAnnouncementContentsAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<int, string> contentMap = contents
            .ToDictionary(id => id.AnnId, content => content.Content);

        Must.NotNull(wrapper!);

        // 将活动公告置于上方
        wrapper.List.Reverse();

        // 将公告内容联入公告列表
        JoinAnnouncements(contentMap, wrapper.List);

        return memoryCache.Set(CacheKey, wrapper, TimeSpan.FromMinutes(30));
    }

    private static void JoinAnnouncements(Dictionary<int, string> contentMap, List<AnnouncementListWrapper> announcementListWrappers)
    {
        // 匹配特殊的时间格式: <t>(.*?)</t>
        Regex timeTagRegrex = new("&lt;t.*?&gt;(.*?)&lt;/t&gt;", RegexOptions.Multiline);

        announcementListWrappers.ForEach(listWrapper =>
        {
            listWrapper.List.ForEach(item =>
            {
                if (contentMap.TryGetValue(item.AnnId, out string? rawContent))
                {
                    // remove <t/> tag
                    rawContent = timeTagRegrex.Replace(rawContent!, x => x.Groups[1].Value);
                }

                item.Content = rawContent ?? string.Empty;
            });
        });
    }
}