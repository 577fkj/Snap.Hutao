﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Service.Achievement;

/// <summary>
/// 导入选项
/// </summary>
public enum ImportOption
{
    /// <summary>
    /// 贪婪合并
    /// </summary>
    AggressiveMerge = 0,

    /// <summary>
    /// 懒惰合并
    /// </summary>
    LazyMerge = 1,

    /// <summary>
    /// 完全覆盖
    /// </summary>
    Overwrite = 2,
}