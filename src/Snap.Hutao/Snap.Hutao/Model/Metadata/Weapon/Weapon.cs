﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Intrinsic;

namespace Snap.Hutao.Model.Metadata.Weapon;

/// <summary>
/// 武器
/// </summary>
public class Weapon
{
    /// <summary>
    /// Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 武器类型
    /// </summary>
    public WeaponType WeaponType { get; set; }

    /// <summary>
    /// 等级
    /// </summary>
    public ItemQuality RankLevel { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; } = default!;

    /// <summary>
    /// 觉醒图标
    /// </summary>
    public string AwakenIcon { get; set; } = default!;

    /// <summary>
    /// 属性
    /// </summary>
    public PropertyInfo Property { get; set; } = default!;
}
