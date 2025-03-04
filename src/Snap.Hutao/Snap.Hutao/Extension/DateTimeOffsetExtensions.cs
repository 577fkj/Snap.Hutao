﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Extension;

/// <summary>
/// <see cref="DateTimeOffset"/> 扩展
/// </summary>
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Converts the current <see cref="DateTimeOffset"/> to a <see cref="DateTimeOffset"/> that represents the local time.
    /// </summary>
    /// <param name="dateTimeOffset">时间偏移</param>
    /// <param name="keepTicks">保留主时间部分</param>
    /// <returns>A <see cref="DateTimeOffset"/> that represents the local time.</returns>
    public static DateTimeOffset ToLocalTime(this DateTimeOffset dateTimeOffset, bool keepTicks)
    {
        if (keepTicks)
        {
            dateTimeOffset += TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).Negate();
        }

        return dateTimeOffset.ToLocalTime();
    }
}