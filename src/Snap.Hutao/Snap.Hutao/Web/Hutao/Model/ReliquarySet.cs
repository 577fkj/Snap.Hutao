﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Hutao.Model;

/// <summary>
/// 圣遗物套装
/// </summary>
public class ReliquarySet
{
    /// <summary>
    /// 构造一个新的圣遗物套装
    /// </summary>
    /// <param name="set">简单套装字符串</param>
    public ReliquarySet(string set)
    {
        string[]? deconstructed = set.Split('-');

        Id = int.Parse(deconstructed[0]);
        Count = int.Parse(deconstructed[1]);
    }

    /// <summary>
    /// Id
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 个数
    /// </summary>
    public int Count { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id}-{Count}";
    }
}
